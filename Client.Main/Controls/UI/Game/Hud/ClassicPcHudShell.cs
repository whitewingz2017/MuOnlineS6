using System;
using Client.Main.Configuration;
using Client.Main.Controls;
using Client.Main.Core.Client;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// Optional classic PC HUD shell. It adds a separate PC bottom bar and temporarily
    /// hides the existing Hybrid presentation controls without changing their state or logic.
    /// </summary>
    internal sealed class ClassicPcHudShell : IHudShell
    {
        private readonly Classic.ClassicPcBottomBarControl _bottomBar;
        private readonly Action _hideHybridHud;
        private CharacterState _characterState;
        private GameSceneSkillController _skillController;
        private GameControl _partyControl;
        private GameControl _chatLog;
        private GameControl _chatInput;
        private GameControl _inventory;
        private GameScene _scene;

        public HudTheme Theme => HudTheme.ClassicPc;

        public ClassicPcHudShell(CharacterState state, ModernBottomHud hud, Action hideHybridHud)
        {
            _characterState = state ?? throw new ArgumentNullException(nameof(state));
            _hideHybridHud = hideHybridHud ?? throw new ArgumentNullException(nameof(hideHybridHud));
            _bottomBar = new Classic.ClassicPcBottomBarControl(state, hud);
            _bottomBar.Visible = false;
        }

        public void Attach(GameScene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _hideHybridHud();
            if (_bottomBar.Parent != scene)
                scene.Controls.Add(_bottomBar);
            _bottomBar.Visible = true;
            _bottomBar.Interactive = true;
            _bottomBar.BringToFront();
        }

        public void Detach()
        {
            _bottomBar.Visible = false;
            _bottomBar.Interactive = false;
        }

        public void Update(GameTime gameTime)
        {
            // The bottom bar is a GameScene child and receives its normal update exactly once.
        }

        public void BindCharacterState(CharacterState state) => _characterState = state;
        public void BindSkillController(GameSceneSkillController skillController) => _skillController = skillController;
        public void BindPartyData(GameControl partyControl) => _partyControl = partyControl;
        public void BindChat(GameControl chatLog, GameControl chatInput)
        {
            _chatLog = chatLog;
            _chatInput = chatInput;
        }
        public void BindInventory(GameControl inventory) => _inventory = inventory;
    }
}
