using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Client.Main.Controls.UI.Game.Editor
{
    /// <summary>
    /// Inspects C# syntax without compiling or executing the target UI class.
    /// The live UI tree remains the rendering source for MuHelperWindow; this inspection
    /// validates its source contract and reports the patterns/properties that are code-owned.
    /// </summary>
    internal sealed class OpenMuCSharpUiParser
    {
        private static readonly HashSet<string> RecognizedLayoutFactories = new(StringComparer.Ordinal)
        {
            "AddLabel", "CreateButton", "CreatePage", "AddValueRow", "AddToggleRow", "AddSkillRow",
            "AddClassicLabel", "AddClassicValueLabel", "AddClassicToggle", "AddClassicSkillSlot",
            "AddClassicIconButton", "AddThresholdTrack"
        };

        private static readonly HashSet<string> SupportedControlTypes = new(StringComparer.Ordinal)
        {
            "UIControl", "TextureControl", "SpriteControl", "LabelControl", "ButtonControl", "TextBoxControl",
            "HelperActionButton", "HelperValueButton", "SkillSlotButton", "HelperToggleButton",
            "HelperThresholdSegmentButton", "HelperPageControl"
        };

        private static readonly HashSet<string> SupportedVisualProperties = new(StringComparer.Ordinal)
        {
            "Name", "X", "Y", "ControlSize", "ViewSize", "Visible", "Interactive", "TexturePath",
            "Text", "FontSize", "TextColor", "BackgroundColor", "BorderColor", "BorderThickness", "Alpha"
        };

        public OpenMuCSharpUiInspection InspectFile(string requestedPath, string expectedClassName = "MuHelperWindow")
        {
            string path = ResolveSourcePath(requestedPath);
            string source = File.ReadAllText(path);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), path);
            CompilationUnitSyntax unit = tree.GetCompilationUnitRoot();
            var errors = tree.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString())
                .ToArray();

            ClassDeclarationSyntax classDeclaration = unit.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(candidate => string.Equals(candidate.Identifier.ValueText, expectedClassName, StringComparison.Ordinal));
            if (classDeclaration == null)
                throw new InvalidDataException($"No class named '{expectedClassName}' was found in {path}.");

            string namespaceName = string.Join(".", classDeclaration.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .Select(item => item.Name.ToString()));
            string baseTypes = string.Join(", ", classDeclaration.BaseList?.Types.Select(type => type.Type.ToString()) ?? Enumerable.Empty<string>());
            if (classDeclaration.BaseList == null || !classDeclaration.BaseList.Types.Any(type => type.Type.ToString().EndsWith("UIControl", StringComparison.Ordinal)))
                throw new InvalidDataException($"Class '{expectedClassName}' does not derive from the OpenMU UIControl base class.");

            var factoryCalls = new List<OpenMuCSharpFactoryCall>();
            foreach (InvocationExpressionSyntax invocation in classDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string name = invocation.Expression switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                    _ => string.Empty
                };
                if (RecognizedLayoutFactories.Contains(name))
                {
                    FileLinePositionSpan line = invocation.GetLocation().GetLineSpan();
                    factoryCalls.Add(new OpenMuCSharpFactoryCall(name, line.StartLinePosition.Line + 1, invocation.ToString()));
                }
            }

            string[] methods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Select(method => method.Identifier.ValueText)
                .ToArray();
            string[] customDrawMethods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.ValueText.StartsWith("Draw", StringComparison.Ordinal) &&
                                 method.Modifiers.Any(SyntaxKind.OverrideKeyword))
                .Select(method => method.Identifier.ValueText)
                .ToArray();

            if (errors.Length > 0)
                throw new InvalidDataException($"C# source contains syntax errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Take(8))}");

            List<OpenMuCSharpElementSyntax> constructions = InspectControlConstructions(classDeclaration);
            List<OpenMuCSharpParentAttachment> attachments = InspectParentAttachments(classDeclaration);
            string designerPath = Path.Combine(
                Path.GetDirectoryName(path) ?? AppContext.BaseDirectory,
                expectedClassName + ".Designer.cs");
            bool designerExists = File.Exists(designerPath);
            string designerFingerprint = designerExists
                ? Fingerprint(File.ReadAllText(designerPath))
                : null;

            return new OpenMuCSharpUiInspection(
                path,
                expectedClassName,
                namespaceName,
                baseTypes,
                classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
                methods,
                factoryCalls,
                customDrawMethods)
            {
                SourceFingerprint = Fingerprint(source),
                DesignerPath = designerPath,
                DesignerFileExistedAtOpen = designerExists,
                DesignerFingerprintAtOpen = designerFingerprint,
                ConstructionPatterns = constructions,
                ParentAttachments = attachments
            };
        }

        private static List<OpenMuCSharpElementSyntax> InspectControlConstructions(ClassDeclarationSyntax classDeclaration)
        {
            List<OpenMuCSharpParentAttachment> attachments = InspectParentAttachments(classDeclaration);
            var result = new List<OpenMuCSharpElementSyntax>();

            foreach (ObjectCreationExpressionSyntax creation in classDeclaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = GetSimpleTypeName(creation.Type);
                if (!SupportedControlTypes.Contains(typeName))
                    continue;

                VariableDeclaratorSyntax variable = creation.Ancestors().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
                string variableName = variable?.Identifier.ValueText ?? string.Empty;
                var properties = new Dictionary<string, string>(StringComparer.Ordinal);
                if (creation.Initializer != null)
                {
                    foreach (AssignmentExpressionSyntax assignment in creation.Initializer.Expressions.OfType<AssignmentExpressionSyntax>())
                    {
                        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
                            assignment.Left is IdentifierNameSyntax property &&
                            SupportedVisualProperties.Contains(property.Identifier.ValueText))
                        {
                            properties[property.Identifier.ValueText] = assignment.Right.ToString();
                        }
                    }
                }

                string parentName = attachments
                    .FirstOrDefault(attachment => string.Equals(attachment.ChildExpression, variableName, StringComparison.Ordinal))
                    ?.ParentExpression;
                FileLinePositionSpan line = creation.GetLocation().GetLineSpan();
                result.Add(new OpenMuCSharpElementSyntax(
                    variableName,
                    typeName,
                    line.StartLinePosition.Line + 1,
                    properties,
                    parentName,
                    creation.ToString()));
            }

            return result;
        }

        private static List<OpenMuCSharpParentAttachment> InspectParentAttachments(ClassDeclarationSyntax classDeclaration)
        {
            var result = new List<OpenMuCSharpParentAttachment>();
            foreach (InvocationExpressionSyntax invocation in classDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax addCall ||
                    !string.Equals(addCall.Name.Identifier.ValueText, "Add", StringComparison.Ordinal) ||
                    invocation.ArgumentList.Arguments.Count != 1 ||
                    addCall.Expression is not MemberAccessExpressionSyntax controlsAccess ||
                    !string.Equals(controlsAccess.Name.Identifier.ValueText, "Controls", StringComparison.Ordinal))
                    continue;

                result.Add(new OpenMuCSharpParentAttachment(
                    invocation.ArgumentList.Arguments[0].Expression.ToString(),
                    controlsAccess.Expression.ToString(),
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
            }

            return result;
        }

        private static string GetSimpleTypeName(TypeSyntax type)
        {
            string typeText = type.ToString();
            int genericStart = typeText.IndexOf('<');
            if (genericStart >= 0)
                typeText = typeText[..genericStart];
            int lastDot = typeText.LastIndexOf('.');
            return lastDot >= 0 ? typeText[(lastDot + 1)..] : typeText;
        }

        internal static string Fingerprint(string content) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content ?? string.Empty)));

        public static string ResolveSourcePath(string requestedPath)
        {
            if (string.IsNullOrWhiteSpace(requestedPath))
                throw new InvalidDataException("Enter a C# UI source path first.");

            string expanded = Environment.ExpandEnvironmentVariables(requestedPath.Trim().Trim('"'));
            if (Path.IsPathRooted(expanded))
            {
                string absolute = Path.GetFullPath(expanded);
                if (File.Exists(absolute))
                    return absolute;
                throw new FileNotFoundException($"C# source file was not found: {absolute}", absolute);
            }

            var starts = new[] { Environment.CurrentDirectory, AppContext.BaseDirectory };
            foreach (string start in starts)
            {
                string directory = Path.GetFullPath(start);
                for (int depth = 0; depth < 10 && !string.IsNullOrEmpty(directory); depth++)
                {
                    string candidate = Path.GetFullPath(Path.Combine(directory, expanded));
                    if (File.Exists(candidate))
                        return candidate;
                    DirectoryInfo parent = Directory.GetParent(directory);
                    directory = parent?.FullName;
                }
            }

            throw new FileNotFoundException($"Could not resolve C# source path '{requestedPath}' from the current or application directories.");
        }
    }

    internal sealed record OpenMuCSharpFactoryCall(string Name, int Line, string Syntax);

    internal sealed record OpenMuCSharpParentAttachment(string ChildExpression, string ParentExpression, int Line);

    internal sealed record OpenMuCSharpElementSyntax(
        string VariableName,
        string TypeName,
        int Line,
        IReadOnlyDictionary<string, string> InitializerProperties,
        string ParentExpression,
        string Syntax);

    internal sealed record OpenMuCSharpUiInspection(
        string SourcePath,
        string ClassName,
        string NamespaceName,
        string BaseTypes,
        bool IsPartial,
        IReadOnlyList<string> Methods,
        IReadOnlyList<OpenMuCSharpFactoryCall> FactoryCalls,
        IReadOnlyList<string> CustomDrawMethods)
    {
        public string SourceFingerprint { get; init; }
        public string DesignerPath { get; init; }
        public bool DesignerFileExistedAtOpen { get; set; }
        public string DesignerFingerprintAtOpen { get; set; }
        public IReadOnlyList<OpenMuCSharpElementSyntax> ConstructionPatterns { get; init; } = Array.Empty<OpenMuCSharpElementSyntax>();
        public IReadOnlyList<OpenMuCSharpParentAttachment> ParentAttachments { get; init; } = Array.Empty<OpenMuCSharpParentAttachment>();

        public string Summary => $"{ClassName} : {BaseTypes}; Roslyn found {ConstructionPatterns.Count} supported control constructions, " +
                                 $"{ParentAttachments.Count} child attachments, and {FactoryCalls.Count} layout helper calls; " +
                                 (CustomDrawMethods.Count == 0
                                     ? "the live OpenMU control tree is rendered"
                                     : $"custom drawing: {string.Join(", ", CustomDrawMethods)} (shown by the live OpenMU control)");
    }
}
