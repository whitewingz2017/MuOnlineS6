using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Client.Main.Controls.UI.Game.Layouts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Client.Main.Controls.UI.Game.Editor
{
    /// <summary>
    /// Writes only a generated partial-class descriptor file. The original UI class and its
    /// callbacks remain hand-authored; the runtime applies these descriptors to the existing tree.
    /// </summary>
    internal sealed class OpenMuDesignerCSharpSourceWriter
    {
        private const string ManagedFileMarker = "// This file is managed by the OpenMU Visual UI Designer.";
        private static readonly HashSet<string> SupportedElementTypes = new(StringComparer.Ordinal)
        {
            "container", "image", "text", "button", "panel", "unsupported"
        };

        public string Save(OpenMuCSharpUiInspection inspection, IReadOnlyList<UiLayoutElement> elements)
        {
            if (inspection == null)
                throw new ArgumentNullException(nameof(inspection));
            if (!string.Equals(inspection.ClassName, "MuHelperWindow", StringComparison.Ordinal) ||
                !string.Equals(inspection.NamespaceName, "Client.Main.Controls.UI.Game.Helper", StringComparison.Ordinal) ||
                !inspection.IsPartial)
            {
                throw new InvalidOperationException("C# source generation is currently enabled only for the inspected MuHelperWindow OpenMU UI class.");
            }
            if (elements == null || elements.Count == 0)
                throw new InvalidDataException("The live MuHelperWindow tree is empty; refusing to generate an empty layout override.");

            ValidateSourceSnapshot(inspection);
            ValidateElements(elements);

            string target = Path.GetFullPath(inspection.DesignerPath ?? Path.Combine(
                Path.GetDirectoryName(inspection.SourcePath) ?? AppContext.BaseDirectory,
                inspection.ClassName + ".Designer.cs"));
            string expectedTarget = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(inspection.SourcePath) ?? AppContext.BaseDirectory,
                inspection.ClassName + ".Designer.cs"));
            if (!string.Equals(target, expectedTarget, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The generated C# file must be adjacent to the inspected UI source.");

            bool targetExists = File.Exists(target);
            if (targetExists != inspection.DesignerFileExistedAtOpen)
                throw new IOException("The generated designer file was created or removed after Open UI. Reopen the source before saving.");
            if (targetExists)
            {
                string existing = File.ReadAllText(target);
                if (!string.Equals(OpenMuCSharpUiParser.Fingerprint(existing), inspection.DesignerFingerprintAtOpen, StringComparison.Ordinal))
                    throw new IOException("The generated designer file changed after Open UI. Reopen the source before saving to avoid overwriting external edits.");
                ValidateManagedDesignerFile(existing, inspection.ClassName, inspection.NamespaceName);
            }

            string contents = GenerateSource(inspection.NamespaceName, elements);
            ValidateGeneratedSource(contents, inspection.ClassName, inspection.NamespaceName);
            WriteAtomically(
                target,
                contents,
                targetExists,
                inspection.DesignerFingerprintAtOpen,
                inspection.SourcePath,
                inspection.SourceFingerprint);

            inspection.DesignerFileExistedAtOpen = true;
            inspection.DesignerFingerprintAtOpen = OpenMuCSharpUiParser.Fingerprint(contents);
            return target;
        }

        private static void ValidateSourceSnapshot(OpenMuCSharpUiInspection inspection)
        {
            if (!File.Exists(inspection.SourcePath))
                throw new FileNotFoundException("The inspected C# UI source no longer exists.", inspection.SourcePath);
            string currentSource = File.ReadAllText(inspection.SourcePath);
            if (!string.Equals(OpenMuCSharpUiParser.Fingerprint(currentSource), inspection.SourceFingerprint, StringComparison.Ordinal))
                throw new IOException("The C# UI source changed after Open UI. Reopen it before saving so the live preview and source stay in sync.");
        }

        private static void ValidateElements(IReadOnlyList<UiLayoutElement> elements)
        {
            var byId = new Dictionary<string, UiLayoutElement>(StringComparer.Ordinal);
            var sourcePaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (UiLayoutElement element in elements)
            {
                if (string.IsNullOrWhiteSpace(element.Id) || !byId.TryAdd(element.Id, element))
                    throw new InvalidDataException("Every visual element must have a unique, non-empty designer ID.");
                if (!SupportedElementTypes.Contains(element.Type ?? string.Empty))
                    throw new InvalidDataException($"Element '{element.Name}' has unsupported designer type '{element.Type}'.");
                if (!element.IsDesignerAdded && element.SourcePath == null)
                    throw new InvalidDataException($"Element '{element.Name}' has no safe C# source binding. It cannot be saved.");
                if (!element.IsDesignerAdded && !sourcePaths.Add(element.SourcePath))
                    throw new InvalidDataException($"Multiple elements resolve to the same C# control path '{element.SourcePath}'.");
                if (element.IsDesignerAdded && (string.IsNullOrWhiteSpace(element.ParentId) || element.SourcePath != null))
                    throw new InvalidDataException($"New element '{element.Name}' must have a parent binding and no source control path.");
                if (element.ParentId != null && !elements.Any(candidate => string.Equals(candidate.Id, element.ParentId, StringComparison.Ordinal)))
                    throw new InvalidDataException($"Element '{element.Name}' references a missing parent ID.");
                if (element.Width < 1 || element.Height < 1 || element.Width > 10000 || element.Height > 10000 ||
                    element.X < -100000 || element.X > 100000 || element.Y < -100000 || element.Y > 100000)
                    throw new InvalidDataException($"Element '{element.Name}' has out-of-range geometry.");
                if (!float.IsFinite(element.Opacity) || element.Opacity < 0f || element.Opacity > 1f)
                    throw new InvalidDataException($"Element '{element.Name}' has an invalid opacity value.");
                if (!float.IsFinite(element.FontSize) || element.FontSize < 0f || element.FontSize > 256f)
                    throw new InvalidDataException($"Element '{element.Name}' has an invalid font size.");
                if (element.CanEditAsset && !string.IsNullOrWhiteSpace(element.Asset) && !IsRelativeInterfacePath(element.Asset))
                    throw new InvalidDataException($"Element '{element.Name}' must use a relative Interface/... asset path.");
            }

            foreach (UiLayoutElement element in elements)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal) { element.Id };
                string parentId = element.ParentId;
                while (parentId != null)
                {
                    if (!visited.Add(parentId))
                        throw new InvalidDataException($"Element '{element.Name}' has a cyclic parent hierarchy.");
                    parentId = byId[parentId].ParentId;
                }
            }
        }

        private static bool IsRelativeInterfacePath(string asset)
        {
            if (string.IsNullOrWhiteSpace(asset) || Path.IsPathRooted(asset) ||
                !asset.StartsWith("Interface/", StringComparison.OrdinalIgnoreCase) || asset.Contains('\\'))
                return false;
            return asset.Split('/').Skip(1).All(segment => segment.Length > 0 && segment != "." && segment != "..");
        }

        private static void ValidateManagedDesignerFile(string source, string className, string namespaceName)
        {
            if (!source.StartsWith(ManagedFileMarker, StringComparison.Ordinal))
                throw new IOException("The adjacent .Designer.cs file is not marked as OpenMU Visual UI Designer output; it was left untouched.");
            CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)).GetCompilationUnitRoot();
            if (unit.ContainsDiagnostics || unit.Members.Count != 1 || unit.Members[0] is not NamespaceDeclarationSyntax namespaceNode ||
                namespaceNode.Members.Count != 1)
                throw new IOException("The adjacent .Designer.cs file is not a valid, isolated managed designer file; it was left untouched.");
            ClassDeclarationSyntax classNode = namespaceNode.Members.OfType<ClassDeclarationSyntax>()
                .SingleOrDefault(candidate => string.Equals(candidate.Identifier.ValueText, className, StringComparison.Ordinal));
            if (!string.Equals(namespaceNode.Name.ToString(), namespaceName, StringComparison.Ordinal) || classNode == null ||
                !classNode.Modifiers.Any(SyntaxKind.PartialKeyword) || classNode.Members.Count != 1 ||
                classNode.Members[0] is not FieldDeclarationSyntax field || field.Declaration.Variables.Count != 1 ||
                !string.Equals(field.Declaration.Variables[0].Identifier.ValueText, "VisualDesignerElements", StringComparison.Ordinal) ||
                !field.Modifiers.Any(SyntaxKind.StaticKeyword) || !field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) ||
                field.Declaration.Type is not ArrayTypeSyntax arrayType ||
                !string.Equals(arrayType.ElementType.ToString(), "VisualDesignerElement", StringComparison.Ordinal) ||
                !IsManagedElementArrayInitializer(field.Declaration.Variables[0].Initializer?.Value))
            {
                throw new IOException("The adjacent .Designer.cs file contains content outside the generated visual-layout field; it was left untouched.");
            }
        }

        private static bool IsManagedElementArrayInitializer(ExpressionSyntax initializer)
        {
            if (initializer is ArrayCreationExpressionSyntax arrayCreation && arrayCreation.Initializer != null)
                return arrayCreation.Initializer.Expressions.All(IsManagedElementDescriptor);
            if (initializer is ImplicitArrayCreationExpressionSyntax implicitArray && implicitArray.Initializer != null)
                return implicitArray.Initializer.Expressions.All(IsManagedElementDescriptor);
            if (initializer is InvocationExpressionSyntax emptyArrayCall &&
                emptyArrayCall.Expression is MemberAccessExpressionSyntax member &&
                string.Equals(member.Expression.ToString(), "Array", StringComparison.Ordinal) &&
                string.Equals(member.Name.Identifier.ValueText, "Empty", StringComparison.Ordinal) &&
                emptyArrayCall.ArgumentList.Arguments.Count == 0 &&
                emptyArrayCall.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } &&
                genericName.TypeArgumentList.Arguments.Count == 1 &&
                string.Equals(genericName.TypeArgumentList.Arguments[0].ToString(), "VisualDesignerElement", StringComparison.Ordinal))
                return true;
            return false;
        }

        private static bool IsManagedElementDescriptor(ExpressionSyntax expression) =>
            expression is ObjectCreationExpressionSyntax creation &&
            string.Equals(creation.Type.ToString(), "VisualDesignerElement", StringComparison.Ordinal) &&
            creation.ArgumentList?.Arguments.Count == 24;

        private static void ValidateGeneratedSource(string source, string className, string namespaceName)
        {
            CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)).GetCompilationUnitRoot();
            if (unit.ContainsDiagnostics)
                throw new InvalidDataException("Generated C# did not pass Roslyn syntax validation; no file was written.");
            NamespaceDeclarationSyntax namespaceNode = unit.DescendantNodes().OfType<NamespaceDeclarationSyntax>().SingleOrDefault();
            ClassDeclarationSyntax classNode = namespaceNode?.Members.OfType<ClassDeclarationSyntax>().SingleOrDefault();
            if (!string.Equals(namespaceNode?.Name.ToString(), namespaceName, StringComparison.Ordinal) ||
                !string.Equals(classNode?.Identifier.ValueText, className, StringComparison.Ordinal) ||
                classNode?.Members.OfType<FieldDeclarationSyntax>().Count() != 1)
                throw new InvalidDataException("Generated C# did not contain the expected partial UI class and layout field; no file was written.");
        }

        private static void WriteAtomically(
            string target,
            string contents,
            bool targetExists,
            string expectedDesignerFingerprint,
            string sourcePath,
            string expectedSourceFingerprint)
        {
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                if (File.Exists(target) != targetExists)
                    throw new IOException("The generated designer file changed while the save was being prepared.");
                if (targetExists && !string.Equals(
                        OpenMuCSharpUiParser.Fingerprint(File.ReadAllText(target)),
                        expectedDesignerFingerprint,
                        StringComparison.Ordinal))
                    throw new IOException("The generated designer file changed while the save was being prepared.");
                if (!File.Exists(sourcePath) || !string.Equals(
                        OpenMuCSharpUiParser.Fingerprint(File.ReadAllText(sourcePath)),
                        expectedSourceFingerprint,
                        StringComparison.Ordinal))
                    throw new IOException("The C# UI source changed while the save was being prepared.");

                if (targetExists)
                    File.Replace(temporary, target, null);
                else
                    File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private static string GenerateSource(string namespaceName, IReadOnlyList<UiLayoutElement> elements)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// This file is managed by the OpenMU Visual UI Designer.");
            builder.AppendLine("// User-authored handlers and gameplay logic belong in MuHelperWindow.cs.");
            builder.AppendLine("using System;");
            builder.AppendLine();
            builder.Append("namespace ").Append(namespaceName).AppendLine();
            builder.AppendLine("{");
            builder.AppendLine("    internal sealed partial class MuHelperWindow");
            builder.AppendLine("    {");
            builder.AppendLine("        private static readonly VisualDesignerElement[] VisualDesignerElements = new VisualDesignerElement[]");
            builder.AppendLine("        {");

            foreach (UiLayoutElement element in elements)
            {
                builder.Append("            new VisualDesignerElement(")
                    .Append(CSharpString(element.Id)).Append(", ")
                    .Append(CSharpString(element.ParentId ?? string.Empty)).Append(", ")
                    .Append(element.IsDesignerAdded ? "null" : CSharpString(element.SourcePath ?? string.Empty)).Append(", ")
                    .Append(CSharpString(element.Type ?? "container")).Append(", ")
                    .Append(CSharpString(element.Name ?? string.Empty)).Append(", ")
                    .Append(CSharpString(element.Asset ?? string.Empty)).Append(", ")
                    .Append(CSharpString(element.Text ?? string.Empty)).Append(", ")
                    .Append(element.X.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(element.Y.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(element.Width.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(element.Height.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(FloatLiteral(element.Opacity)).Append(", ")
                    .Append(FloatLiteral(element.FontSize)).Append(", ")
                    .Append(element.Layer.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(Bool(element.Visible)).Append(", ")
                    .Append(Bool(element.Locked)).Append(", ")
                    .Append(Bool(element.IsDesignerAdded)).Append(", ")
                    .Append(Bool(element.CanEditGeometry)).Append(", ")
                    .Append(Bool(element.CanEditText)).Append(", ")
                    .Append(Bool(element.CanEditAsset)).Append(", ")
                    .Append(Bool(element.CanEditOpacity)).Append(", ")
                    .Append(Bool(element.CanEditFontSize)).Append(", ")
                    .Append(Bool(element.CanEditVisibility)).Append(", ")
                    .Append(CSharpString(element.SourceControlType ?? string.Empty)).AppendLine("),");
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string CSharpString(string value)
        {
            LiteralExpressionSyntax literal = LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value ?? string.Empty));
            return literal.ToFullString();
        }

        private static string FloatLiteral(float value)
        {
            if (!float.IsFinite(value))
                throw new InvalidDataException("Cannot generate C# for a non-finite numeric property.");
            return value.ToString("0.####", CultureInfo.InvariantCulture) + "f";
        }

        private static string Bool(bool value) => value ? "true" : "false";
    }
}
