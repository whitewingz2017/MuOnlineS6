using Client.Main.Models;
using Client.Main.Configuration;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Common;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Client.Main.Controls.UI.Login
{
    public class LoginDialog : PopupFieldDialog
    {
        // Fields
        private readonly TextureControl _line1;
        private readonly TextureControl _line2;
        private readonly LabelControl _titleLabel;
        private readonly LabelControl _userLabel;
        private readonly LabelControl _passwordLabel;
        private readonly TextFieldControl _userInput;
        private readonly LabelControl _serverNameLabel;
        private readonly TextFieldControl _passwordInput;
        private readonly OkButton _okButton;
        private readonly ButtonControl _classicOkButton;
        private readonly ButtonControl _classicCancelButton;
        private readonly RememberLoginCheckBox _rememberLogin;
        private readonly ButtonControl _loginTab;
        private readonly ButtonControl _registerTab;
        private readonly LabelControl _emailLabel;
        private readonly LabelControl _confirmLabel;
        private readonly TextFieldControl _emailInput;
        private readonly TextFieldControl _confirmInput;
        private bool _submitting;
        public bool IsRegistration { get; private set; }
        public string Email => _emailInput.Value.Trim();
        public string Confirmation => _confirmInput.Value;
        public event EventHandler RegistrationAttempt;
        public event EventHandler RegistrationModeChanged;
        private bool _rememberDetails;
        public bool RememberDetails => _rememberDetails;
        public bool CanGoBack
        {
            get => IsRegistration || _classicCancelButton.Interactive;
            set => _classicCancelButton.Interactive = !_submitting && (IsRegistration || value);
        }

        // Properties
        public string ServerName
        {
            get => _serverNameLabel.Text;
            set => _serverNameLabel.Text = value;
        }

        /// <summary>
        /// Gets the username entered in the text field.
        /// </summary>
        public string Username => _userInput.Value;

        /// <summary>
        /// Gets the password entered in the text field.
        /// </summary>
        public string Password => _passwordInput.Value;

        // Events
        /// <summary>
        /// Invoked when the user confirms login (clicks OK or presses Enter in the password field).
        /// </summary>
        public event EventHandler LoginAttempt;
        public event EventHandler CancelAttempt;

        internal void BringToFrontBelowMessages()
        {
            BringToFront();
            // A queued connection-state update must not cover an error that
            // was just shown by the login response handler.
            if (Parent == null) return;
            foreach (var message in Parent.Controls.OfType<MessageWindow>().Where(m => m.Visible).ToArray())
                message.BringToFront();
        }

        // Constructors
        public LoginDialog()
        {
            ControlSize = new Point(300, 200);
            Interactive = true;

            Controls.Add(_titleLabel = new LabelControl
            {
                Text = "MU Online",
                Align = ControlAlign.HorizontalCenter,
                Y = 15,
                FontSize = 12
            });

            Controls.Add(_line1 = new TextureControl
            {
                TexturePath = "Interface/GFx/popup_line_m.ozd",
                X = 10,
                Y = 40,
                AutoViewSize = false
            });

            Controls.Add(_serverNameLabel = new LabelControl
            {
                Text = "OpenMU Server 1",
                Align = ControlAlign.HorizontalCenter,
                Y = 55,
                FontSize = 12,
                TextColor = ModernHudTheme.TextGold
            });

            Controls.Add(_userLabel = new LabelControl
            {
                Text = "Username",
                Y = 90,
                X = 20,
                AutoViewSize = false,
                ViewSize = new Point(70, 20),
                TextAlign = HorizontalAlign.Right,
                FontSize = 12f
            });

            Controls.Add(_passwordLabel = new LabelControl
            {
                Text = "Password",
                Y = 120,
                X = 20,
                AutoViewSize = false,
                ViewSize = new Point(70, 20),
                TextAlign = HorizontalAlign.Right,
                FontSize = 12f
            });

            Controls.Add(_line2 = new TextureControl
            {
                TexturePath = "Interface/GFx/popup_line_m.ozd",
                X = 10,
                Y = 150,
                AutoViewSize = false,
                Alpha = 0.7f
            });

            _userInput = TextFieldControl.Create();
            _userInput.X = 100;
            _userInput.Y = 87;
            _userInput.Skin = TextFieldSkin.NineSlice;

            _passwordInput = TextFieldControl.Create();
            _passwordInput.X = 100;
            _passwordInput.Y = 117;
            _passwordInput.MaskValue = true;
            _passwordInput.Skin = TextFieldSkin.NineSlice;

            _passwordInput.ValueChanged += PasswordInput_EnterPressed; // Use dedicated method
            Controls.Add(_userInput);
            Controls.Add(_passwordInput);

            _userInput.Click += (s, e) => { _userInput.OnFocus(); _passwordInput.OnBlur(); };
            _passwordInput.Click += (s, e) => { _passwordInput.OnFocus(); _userInput.OnBlur(); };

            _okButton = new OkButton
            {
                Y = 160,
                Align = ControlAlign.HorizontalCenter
            };
            _okButton.Click += OkButton_Click; // Use dedicated method
            Controls.Add(_okButton);

            _classicOkButton = CreateClassicButton("OK", ModernHudTheme.Success);
            _classicOkButton.Click += OkButton_Click;
            Controls.Add(_classicOkButton);

            _classicCancelButton = CreateClassicButton("Cancel", new Color(65, 50, 45));
            _classicCancelButton.Click += CancelButton_Click;
            Controls.Add(_classicCancelButton);

            _rememberLogin = new RememberLoginCheckBox();
            _rememberLogin.Click += (s, e) =>
            {
                _rememberDetails = !_rememberDetails;
                _rememberLogin.Checked = _rememberDetails;
                if (!_rememberDetails)
                {
                    try { LoginDetailsStore.Clear(); }
                    catch { MessageWindow.Show("Unable to clear saved login details. Please try again."); }
                }
            };
            Controls.Add(_rememberLogin);
            var saved = LoginDetailsStore.Load();
            if (saved.HasValue)
            {
                _userInput.Value = saved.Value.Username;
                _passwordInput.Value = saved.Value.Password;
                _rememberDetails = true;
                _rememberLogin.Checked = true;
            }

            _loginTab=CreateClassicButton("Login",ModernHudTheme.BgMid);
            _registerTab=CreateClassicButton("Register",ModernHudTheme.BgMid);
            _loginTab.Click+=(_,_)=>SetRegistrationMode(false);
            _registerTab.Click+=(_,_)=>SetRegistrationMode(true);
            Controls.Add(_loginTab);Controls.Add(_registerTab);
            _emailLabel=new LabelControl{Text="Email",FontSize=12,AutoViewSize=false,TextAlign=HorizontalAlign.Right};
            _confirmLabel=new LabelControl{Text="Confirm",FontSize=12,AutoViewSize=false,TextAlign=HorizontalAlign.Right};
            _emailInput=TextFieldControl.Create();
            _confirmInput=TextFieldControl.Create();_confirmInput.MaskValue=true;
            Controls.Add(_emailLabel);Controls.Add(_confirmLabel);Controls.Add(_emailInput);Controls.Add(_confirmInput);
            _confirmInput.ValueChanged+=PasswordInput_EnterPressed;
            foreach(var field in AllFields)
                field.Click+=(_,_)=>FocusField(field);
            ApplyThemeLayout();
        }

        private TextFieldControl[] AllFields => new[]{_userInput,_emailInput,_passwordInput,_confirmInput};
        private void FocusField(TextFieldControl field)
        {
            foreach(var other in AllFields)if(other!=field)other.OnBlur();
            Scene?.FocusControlIfInteractive(field);field.OnFocus();
        }
        public void SetRegistrationMode(bool registration)
        {
            if(_submitting || IsRegistration==registration)return;
            foreach(var field in AllFields)field.OnBlur();
            if(Scene!=null)Scene.FocusControl=null;
            IsRegistration=registration;
            _passwordInput.Value=string.Empty;_confirmInput.Value=string.Empty;
            ApplyThemeLayout();
            RegistrationModeChanged?.Invoke(this,EventArgs.Empty);
        }
        public void SetSubmitting(bool submitting)
        {
            _submitting=submitting;
            ApplyThemeLayout();
        }
        public void RegistrationSucceeded()
        {
            SetSubmitting(false);SetRegistrationMode(false);
        }

        public override async System.Threading.Tasks.Task Load()
        {
            await base.Load();
            // Button loading enables interaction; restore the active-tab and submit guards afterwards.
            ApplyThemeLayout();
        }

        protected override void OnThemeChanged(UiThemeChangedEventArgs e)
        {
            base.OnThemeChanged(e);
            ApplyThemeLayout();
        }

        private static ButtonControl CreateClassicButton(string text, Color color) => new()
        {
            Text = text,
            AutoViewSize = false,
            FontSize = 12f,
            BackgroundColor = color,
            HoverBackgroundColor = Color.Lerp(color, Color.White, 0.15f),
            PressedBackgroundColor = Color.Lerp(color, Color.Black, 0.15f),
            TextColor = ModernHudTheme.TextWhite,
            HoverTextColor = ModernHudTheme.TextWhite,
            BorderColor = ModernHudTheme.BorderInner,
            BorderThickness = 1,
            Interactive = true
        };

        private void ApplyThemeLayout()
        {
            if(_loginTab==null)return;
            bool classic=!LoginUiTheme.UseModernLayout;
            int width=classic?340:380;
            AutoViewSize=false;
            ControlSize=ViewSize=new Point(width,IsRegistration?370:310);
            _titleLabel.Y=15;_titleLabel.TextColor=ModernHudTheme.TextWhite;
            _serverNameLabel.Y=49;_serverNameLabel.TextColor=ModernHudTheme.TextGold;
            _line1.Visible=false;_line2.Visible=false;_okButton.Visible=false;_okButton.Interactive=false;
            _loginTab.X=28;_loginTab.Y=78;_loginTab.ViewSize=new Point((width-66)/2,30);
            _registerTab.X=width/2+5;_registerTab.Y=78;_registerTab.ViewSize=_loginTab.ViewSize;
            _loginTab.Enabled=_loginTab.Interactive=IsRegistration && !_submitting;
            _registerTab.Enabled=_registerTab.Interactive=!IsRegistration && !_submitting;
            _loginTab.BackgroundColor=IsRegistration?ModernHudTheme.BgMid:ModernHudTheme.AccentDim;
            _registerTab.BackgroundColor=IsRegistration?ModernHudTheme.AccentDim:ModernHudTheme.BgMid;
            void Row(LabelControl label,TextFieldControl input,int y,bool visible=true)
            {
                label.X=10;label.Y=y+7;label.ViewSize=new Point(85,26);label.TextColor=ModernHudTheme.TextWhite;
                input.X=104;input.Y=y;input.ViewSize=new Point(width-124,32);
                input.Skin=TextFieldSkin.Flat;input.BackgroundColor=new Color(12,12,16,235);
                input.BorderColor=ModernHudTheme.BorderInner;input.BorderThickness=1;input.TextColor=ModernHudTheme.TextWhite;
                label.Visible=input.Visible=visible;input.Interactive=visible && !_submitting;
            }
            Row(_userLabel,_userInput,120);
            Row(_emailLabel,_emailInput,162,IsRegistration);
            Row(_passwordLabel,_passwordInput,IsRegistration?204:162);
            Row(_confirmLabel,_confirmInput,246,IsRegistration);
            _rememberLogin.X=28;_rememberLogin.Y=202;_rememberLogin.ViewSize=new Point(width-56,32);
            _rememberLogin.Visible=!IsRegistration && LoginDetailsStore.Supported;
            _rememberLogin.Interactive=_rememberLogin.Visible && !_submitting;
            _classicOkButton.Text=_submitting?"Submitting…":IsRegistration?"Submit":"Login";
            _classicOkButton.X=width/2-115;_classicOkButton.Y=IsRegistration?302:246;
            _classicOkButton.ViewSize=new Point(108,40);_classicOkButton.Visible=true;_classicOkButton.Enabled=_classicOkButton.Interactive=!_submitting;
            _classicCancelButton.X=width/2+7;_classicCancelButton.Y=_classicOkButton.Y;
            _classicCancelButton.ViewSize=new Point(108,40);_classicCancelButton.Visible=true;
            _classicCancelButton.Enabled=_classicCancelButton.Interactive=!_submitting;
        }

        public override void Draw(GameTime gameTime)
        {
            if (LoginUiTheme.UseModernLayout)
            {
                base.Draw(gameTime);
                return;
            }

            if (Status != GameControlStatus.Ready || !Visible)
                return;

            SpriteBatch sprite = GraphicsManager.Instance.Sprite;
            Rectangle rect = DisplayRectangle;
            UiDrawHelper.DrawPanel(sprite, rect, ModernHudTheme.BgDark * 0.98f,
                ModernHudTheme.BorderInner, ModernHudTheme.BorderOuter,
                ModernHudTheme.BorderHighlight, withGlow: true,
                glowColor: ModernHudTheme.AccentGlow * 0.45f);

            Rectangle header = new(rect.X + 10, rect.Y + 39, rect.Width - 20, 1);
            sprite.Draw(GraphicsManager.Instance.Pixel, header, ModernHudTheme.Accent * 0.7f);
            Rectangle separator = new(rect.X + 10, rect.Y + _classicOkButton.Y-10, rect.Width - 20, 1);
            sprite.Draw(GraphicsManager.Instance.Pixel, separator, ModernHudTheme.BorderInner * 0.8f);

            _titleLabel.Draw(gameTime);
            _serverNameLabel.Draw(gameTime);
            _loginTab.Draw(gameTime);_registerTab.Draw(gameTime);
            if(IsRegistration){_emailLabel.Draw(gameTime);_emailInput.Draw(gameTime);_confirmLabel.Draw(gameTime);_confirmInput.Draw(gameTime);}
            _userLabel.Draw(gameTime);
            _passwordLabel.Draw(gameTime);
            _userInput.Draw(gameTime);
            _passwordInput.Draw(gameTime);
            _classicOkButton.Draw(gameTime);
            _classicCancelButton.Draw(gameTime);
            if(_rememberLogin.Visible)_rememberLogin.Draw(gameTime);
        }

        // Public Methods
        /// <summary>
        /// Sets focus on the username field (called from the scene).
        /// </summary>
        public void FocusUsername()
        {
            MuGame.ScheduleOnMainThread(() =>
            {
                Scene?.FocusControlIfInteractive(_userInput);
                _userInput?.OnFocus();
                _passwordInput?.OnBlur();
            });
        }

        public override void Update(GameTime gameTime)
        {
            if(Visible && !_submitting && MuGame.Instance.Keyboard.IsKeyDown(Keys.Tab) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Tab))
            {
                var fields=AllFields.Where(f=>f.Visible).ToArray();
                int index=Array.FindIndex(fields,f=>f.IsFocused);
                FocusField(fields[(index+1)%fields.Length]);
            }
            base.Update(gameTime);
        }

        // Protected Methods
        protected override void OnScreenSizeChanged()
        {
            ApplyThemeLayout();
            base.OnScreenSizeChanged();
        }

        // Private Methods
        // Method called after clicking the OK button
        private void OkButton_Click(object sender, EventArgs e)
        {
            AttemptLogin();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if(_submitting)return;
            if(IsRegistration){SetRegistrationMode(false);return;}
            if (!CanGoBack) return;
            _userInput.OnBlur();
            _passwordInput.OnBlur();
            if (Scene != null && (Scene.FocusControl == _userInput || Scene.FocusControl == _passwordInput))
                Scene.FocusControl = null;
            CancelAttempt?.Invoke(this, EventArgs.Empty);
        }

        // Method called after pressing Enter in the password field
        private void PasswordInput_EnterPressed(object sender, EventArgs e)
        {
            // ValueChanged is also invoked on text change,
            // so we check if Enter was just pressed.
            bool enterPressed = MuGame.Instance.Keyboard.IsKeyDown(Keys.Enter) &&
                                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Enter);

            if (enterPressed)
            {
                AttemptLogin();
            }
        }

        // Invokes the LoginAttempt event
        private void AttemptLogin()
        {
            if(_submitting)return;
            if(IsRegistration)
            {
                foreach(var field in AllFields)field.OnBlur();
                if(Scene!=null)Scene.FocusControl=null;
                RegistrationAttempt?.Invoke(this,EventArgs.Empty);return;
            }
            // Blur fields to hide soft keyboard (especially on mobile) after submitting.
            _userInput.OnBlur();
            _passwordInput.OnBlur();
            if (Scene != null && (Scene.FocusControl == _userInput || Scene.FocusControl == _passwordInput))
            {
                Scene.FocusControl = null; // keep focus cleared so keyboard stays hidden
            }

            LoginAttempt?.Invoke(this, EventArgs.Empty);
        }
    }
}
