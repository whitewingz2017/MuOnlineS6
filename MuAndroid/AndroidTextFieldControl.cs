using Client.Main.Controls.UI;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MuAndroid
{
    public class AndroidTextFieldControl : TextFieldControl
    {
        public override void OnFocus()
        {
            base.OnFocus();

            Task.Run(async () =>
            {
                var result = SubmitOnMobileKeyboardDone
                    ? await ShowChatKeyboardAsync()
                    : await KeyboardInput.Show(
                    title: Label,
                    description: Placeholder,
                    defaultText: Value,
                    usePasswordMode: MaskValue
                );

                if (result != null)
                    Client.Main.MuGame.ScheduleOnMainThread(() =>
                    {
                        // Ignore dialog completion after the user closed the field.
                        if (!Visible) return;
                        Value = result;
                        if (SubmitOnMobileKeyboardDone)
                            OnEnterKeyPressed();
                    });
            }).ConfigureAwait(false);


            //// Subscribe to Android text input event (Critical for soft keyboard and scrcpy)
            //AndroidKeyboard.TextInput += OnTextInput;
            //AndroidKeyboard.Show();
        }

        private Task<string> ShowChatKeyboardAsync()
        {
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var activity = AndroidKeyboard.Activity;
            if (activity == null) { completion.SetResult(null); return completion.Task; }
            activity.RunOnUiThread(() =>
            {
                var input = new Android.Widget.EditText(activity);
                input.SetSingleLine(true);
                input.Text = Value;
                input.Hint = Placeholder;
                input.ImeOptions = Android.Views.InputMethods.ImeAction.Send;
                var dialog = new Android.App.AlertDialog.Builder(activity)
                    .SetTitle(Label)
                    .SetView(input)
                    .SetPositiveButton("Send", (_, _) => completion.TrySetResult(input.Text))
                    .SetNegativeButton("Cancel", (_, _) => completion.TrySetResult(null))
                    .Create();
                input.EditorAction += (_, args) =>
                {
                    if (args.ActionId == Android.Views.InputMethods.ImeAction.Send ||
                        args.ActionId == Android.Views.InputMethods.ImeAction.Done ||
                        (args.Event?.KeyCode == Android.Views.Keycode.Enter && args.Event.Action == Android.Views.KeyEventActions.Down))
                    {
                        args.Handled = true;
                        completion.TrySetResult(input.Text);
                        dialog.Dismiss();
                    }
                };
                dialog.DismissEvent += (_, _) => completion.TrySetResult(null);
                dialog.Show();
                input.RequestFocus();
                dialog.Window?.SetSoftInputMode(Android.Views.SoftInput.StateAlwaysVisible);
            });
            return completion.Task;
        }

        public override void OnBlur()
        {
            base.OnBlur();

            //AndroidKeyboard.TextInput -= OnTextInput;
            //AndroidKeyboard.Hide();
        }

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            bool textChanged = false;

            // Handle control keys by character or key code
            if (e.Character == '\r' || e.Key == Keys.Enter)
            {
                OnEnterKeyPressed();
                OnValueChanged();
                return; // Enter usually consumes the event
            }
            else if (e.Character == '\b' || e.Key == Keys.Back)
            {
                // Backspace - delete last character
                if (_inputText.Length > 0)
                {
                    _inputText.Remove(_inputText.Length - 1, 1);
                    textChanged = true;
                }
            }
            else if (e.Character != '\0' && !char.IsControl(e.Character))
            {
                // Standard printable character input
                _inputText.Append(e.Character);
                textChanged = true;
            }

            if (textChanged)
            {
                UpdateScrollOffset();
                MoveCursorToEnd();
                OnValueChanged();
            }
        }
    }
}
