using Client.Main.Configuration;
using Client.Main.Controls;
using Client.Main.Core.Client;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// Presentation shell for a gameplay HUD. Shells share game state and controllers;
    /// they do not own combat, networking, inventory, party, or chat behavior.
    /// </summary>
    internal interface IHudShell
    {
        HudTheme Theme { get; }

        void Attach(GameScene scene);
        void Detach();
        void Update(GameTime gameTime);

        void BindCharacterState(CharacterState state);
        void BindSkillController(GameSceneSkillController skillController);
        void BindPartyData(GameControl partyControl);
        void BindChat(GameControl chatLog, GameControl chatInput);
        void BindInventory(GameControl inventory);
    }
}
