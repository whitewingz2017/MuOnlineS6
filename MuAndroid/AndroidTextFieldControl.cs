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
        private Android.Widget.EditText _inlineEditor;
        private Android.Views.View _keyboardRoot;
        private EventHandler _keyboardLayoutChanged;
        private Android.Views.SoftInput? _previousSoftInputMode;
        private bool _inlineDisposed;
        private int _promptOpen;
        public override void OnFocus()
        {
            if(!Visible || _inlineDisposed)return;
            base.OnFocus();
            var game=Client.Main.MuGame.Instance;
            bool tapped=DisplayRectangle.Contains(game.UiMouseState.Position) &&
                (game.UiMouseState.LeftButton==ButtonState.Pressed || game.PrevUiMouseState.LeftButton==ButtonState.Pressed);
            // Programmatic focus from a button may select a field, but must not open IME.
            if(!tapped)return;
            if(SubmitOnMobileKeyboardDone && Client.Main.Controls.UI.UiThemeManager.IsMobileInterfaceActive)
            {
                ShowInlineChatKeyboard();return;
            }
            if(System.Threading.Interlocked.Exchange(ref _promptOpen,1)!=0)return;

            Task.Run(async () =>
            {
                try
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
                }
                finally{System.Threading.Interlocked.Exchange(ref _promptOpen,0);}
            }).ConfigureAwait(false);


            //// Subscribe to Android text input event (Critical for soft keyboard and scrcpy)
            //AndroidKeyboard.TextInput += OnTextInput;
            //AndroidKeyboard.Show();
        }

        private void ShowInlineChatKeyboard()
        {
            var activity=AndroidKeyboard.Activity;if(activity==null)return;
            activity.RunOnUiThread(()=>
            {
                if(_inlineDisposed || !IsFocused || !Visible)return;
                var imm=activity.GetSystemService(Android.Content.Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;
                if(_inlineEditor!=null){_inlineEditor.RequestFocus();imm?.ShowSoftInput(_inlineEditor,Android.Views.InputMethods.ShowFlags.Implicit);return;}
                _previousSoftInputMode=activity.Window?.Attributes?.SoftInputMode;
                activity.Window?.SetSoftInputMode(Android.Views.SoftInput.AdjustNothing);
                var editor=new Android.Widget.EditText(activity);
                _inlineEditor=editor;
                editor.SetSingleLine(true);editor.Text=Value;editor.Alpha=0;
                editor.ImeOptions=Android.Views.InputMethods.ImeAction.Send | (Android.Views.InputMethods.ImeAction)Android.Views.InputMethods.ImeFlags.NoExtractUi;
                editor.InputType=Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextFlagCapSentences;
                activity.AddContentView(editor,new Android.Views.ViewGroup.LayoutParams(1,1));
                editor.TextChanged+=(_,args)=>
                {
                    string value=editor.Text ?? string.Empty;
                    Client.Main.MuGame.ScheduleOnMainThread(()=>{if(Visible && ReferenceEquals(_inlineEditor,editor))Value=value;});
                };
                editor.EditorAction+=(_,args)=>
                {
                    if(args.ActionId is Android.Views.InputMethods.ImeAction.Send or Android.Views.InputMethods.ImeAction.Done)
                    {
                        args.Handled=true;string value=editor.Text ?? string.Empty;
                        Client.Main.MuGame.ScheduleOnMainThread(()=>{if(Visible){Value=value;OnEnterKeyPressed();}});
                    }
                };
                _keyboardRoot=activity.Window?.DecorView;
                _keyboardLayoutChanged=(_,_)=>
                {
                    if(_keyboardRoot==null)return;
                    using var visible=new Android.Graphics.Rect();_keyboardRoot.GetWindowVisibleDisplayFrame(visible);
                    int inset=Math.Max(0,_keyboardRoot.Height-visible.Bottom);
                    if(Android.OS.Build.VERSION.SdkInt>=Android.OS.BuildVersionCodes.R && _keyboardRoot.RootWindowInsets is { } insets)
                        inset=insets.GetInsets(Android.Views.WindowInsets.Type.Ime()).Bottom;
                    Client.Main.Controls.UI.Game.Hud.MobileHudLayout.KeyboardInsetPixels=inset>_keyboardRoot.Height/5?inset:0;
                };
                if(_keyboardRoot?.ViewTreeObserver!=null)_keyboardRoot.ViewTreeObserver.GlobalLayout+=_keyboardLayoutChanged;
                editor.RequestFocus();imm?.ShowSoftInput(editor,Android.Views.InputMethods.ShowFlags.Implicit);
            });
        }
        private void HideInlineChatKeyboard()
        {
            var editor=_inlineEditor;if(editor==null)return;
            _inlineEditor=null;
            AndroidKeyboard.Activity?.RunOnUiThread(()=>
            {
                if(_keyboardRoot?.ViewTreeObserver!=null && _keyboardLayoutChanged!=null)
                    _keyboardRoot.ViewTreeObserver.GlobalLayout-=_keyboardLayoutChanged;
                _keyboardRoot=null;_keyboardLayoutChanged=null;
                if(_previousSoftInputMode.HasValue)AndroidKeyboard.Activity.Window?.SetSoftInputMode(_previousSoftInputMode.Value);
                _previousSoftInputMode=null;
                var imm=AndroidKeyboard.Activity.GetSystemService(Android.Content.Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;
                imm?.HideSoftInputFromWindow(editor.WindowToken,Android.Views.InputMethods.HideSoftInputFlags.None);
                (editor.Parent as Android.Views.ViewGroup)?.RemoveView(editor);editor.Dispose();
                Client.Main.Controls.UI.Game.Hud.MobileHudLayout.KeyboardInsetPixels=0;
            });
        }
        public override void Dispose(){_inlineDisposed=true;HideInlineChatKeyboard();base.Dispose();}

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
            HideInlineChatKeyboard();
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
