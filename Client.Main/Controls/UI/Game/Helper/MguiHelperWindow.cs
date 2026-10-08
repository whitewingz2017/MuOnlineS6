#if WINDOWS_DX

using Microsoft.Xna.Framework;
using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using MonoGame.Extended;

namespace Client.Main.Controls.UI.Game.Helper;

internal sealed class MguiHelperWindow
{
    private readonly MGWindow _window;

    public MguiHelperWindow(MGDesktop desktop)
    {
        _window = new MGWindow(
            desktop,
            450,
            120,
            380,
            420);

        _window.TitleText = "Official Realm of Legends Helper";

        var panel = new MGStackPanel(_window, Orientation.Vertical)
        {
            Padding = new Thickness(12),
            Spacing = 6
        };

        panel.TryAddChild(new MGTextBlock(_window, "Official Realm of Legends Helper", FontSize: 16));

        var huntingButton = new MGButton(_window);
        huntingButton.SetContent("Hunting");

        var obtainingButton = new MGButton(_window);
        obtainingButton.SetContent("Obtaining");

        var partyButton = new MGButton(_window);
        partyButton.SetContent("Party");

        panel.TryAddChild(huntingButton);
        panel.TryAddChild(obtainingButton);
        panel.TryAddChild(partyButton);

        var potion = new MGCheckBox(_window);
        potion.SetContent("Use Potion");
        panel.TryAddChild(potion);

        var counterAttack = new MGCheckBox(_window);
        counterAttack.SetContent("Long Distance Counter Attack");
        panel.TryAddChild(counterAttack);

        var originalPosition = new MGCheckBox(_window);
        originalPosition.SetContent("Return to Original Position");
        panel.TryAddChild(originalPosition);

        panel.TryAddChild(new MGTextBlock(_window, "Hunting Range"));

        var range = new MGTextBox(_window);
        range.SetText("6");
        panel.TryAddChild(range);

        var saveButton = new MGButton(_window);
        saveButton.SetContent("Save Setting");
        panel.TryAddChild(saveButton);

        _window.SetContent(panel);

        desktop.Windows.Add(_window);
    }
}

#endif