using Client.Main.Controls.UI.Game.Editor;
using Client.Main.Controls.UI.Game.Layouts;
using Client.Main.Controls.UI.Game.Helper;
using Client.Main.Scenes;
using Microsoft.Extensions.Logging;

namespace Client.Main.Controllers
{
    internal sealed class GameSceneUiEditorController
    {
        private readonly GameScene _scene;
        private readonly ILogger _logger;
        private readonly UiLayoutRuntimeControl _runtimeLayout;
        private readonly MuHelperWindow _muHelperWindow;

        private GameUiEditorControl _editorControl;

        public bool IsOpen => _editorControl?.Visible == true;

        public GameSceneUiEditorController(
            GameScene scene,
            ILogger logger = null,
            UiLayoutRuntimeControl runtimeLayout = null,
            MuHelperWindow muHelperWindow = null)
        {
            _scene = scene;
            _logger = logger;
            _runtimeLayout = runtimeLayout;
            _muHelperWindow = muHelperWindow;
        }

        public void Initialize()
        {
            if (_editorControl != null)
            {
                return;
            }

            _editorControl = new GameUiEditorControl(_runtimeLayout, _muHelperWindow)
            {
                Visible = false,
                Interactive = false
            };

            _scene.Controls.Add(_editorControl);

            _logger?.LogInformation("[UI Editor] Initialized.");
        }

        public void Toggle()
        {
            if (_editorControl == null)
            {
                Initialize();
            }

            _editorControl.Toggle();

            if (_editorControl.Visible)
            {
                _editorControl.BringToFront();

                _logger?.LogInformation(
                    "[UI Editor] Opened.");
            }
            else
            {
                _logger?.LogInformation(
                    "[UI Editor] Closed.");
            }
        }
    }
}