using Client.Main.Controls.UI.Common;
using System;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI;

/// <summary>A keyboard and touch friendly prompt for a store title or item price.</summary>
public sealed class InventoryTextDialog : PopupFieldDialog
{
    private readonly TextFieldControl _input;

    private InventoryTextDialog(string title, string value, Action<string> accepted)
    {
        AutoViewSize = false;
        ControlSize = ViewSize = new Point(360, 180);
        Interactive = true;
        Controls.Add(new LabelControl { Text = title, X = 20, Y = 20,
            AutoViewSize = false, ViewSize = new Point(320, 30), FontSize = 14 });
        _input = TextFieldControl.Create();
        _input.X = 20;
        _input.Y = 62;
        _input.ControlSize = _input.ViewSize = new Point(320, 38);
        _input.FontSize = 16;
        _input.Value = value;
        Controls.Add(_input);
        var ok = new ButtonControl { Text = "OK", X = 20, Y = 120,
            AutoViewSize = false, ControlSize = new Point(150, 36), ViewSize = new Point(150, 36) };
        var cancel = new ButtonControl { Text = "Cancel", X = 190, Y = 120,
            AutoViewSize = false, ControlSize = new Point(150, 36), ViewSize = new Point(150, 36) };
        ok.Click += (_, _) => { var text = _input.Value; Close(); accepted(text); };
        cancel.Click += (_, _) => Close();
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    public static void Show(string title, string value, Action<string> accepted)
    {
        var dialog = new InventoryTextDialog(title, value, accepted);
        dialog.ShowDialog();
        dialog.BringToFront();
    }
}
