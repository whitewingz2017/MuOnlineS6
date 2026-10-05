using System;
using System.Collections.Generic;
using Client.Main.Configuration;
using Client.Main.Controls;
using Client.Main.Core.Client;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// Adapter around the existing Hybrid HUD tree. It intentionally delegates visibility
    /// to GameScene so the current ModernBottomHud, touch, imprint, and theme behavior stay unchanged.
    /// </summary>
    internal sealed class HybridHudShell : IHudShell
    {
        private readonly Action _restoreExistingHud;
        private readonly Action _hideExistingHud;
        private readonly IReadOnlyList<GameControl> _controls;
        private CharacterState _characterState;
        private GameSceneSkillController _skillController;
        private GameControl _partyControl;
        private GameControl _chatLog;
        private GameControl _chatInput;
        private GameControl _inventory;
        private GameScene _scene;

        public HudTheme Theme => HudTheme.Hybrid;

        public HybridHudShell(
            IReadOnlyList<GameControl> controls,
            Action restoreExistingHud,
            Action hideExistingHud)
        {
            _controls = controls ?? throw new ArgumentNullException(nameof(controls));
            _restoreExistingHud = restoreExistingHud ?? throw new ArgumentNullException(nameof(restoreExistingHud));
            _hideExistingHud = hideExistingHud ?? throw new ArgumentNullException(nameof(hideExistingHud));
        }

        public void Attach(GameScene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _restoreExistingHud();
        }

        public void Detach()
        {
            _hideExistingHud();
        }

        public void Update(GameTime gameTime)
        {
            // Existing HUD controls are children of GameScene and receive their normal update.
            // This method is intentionally empty to avoid updating them twice.
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
