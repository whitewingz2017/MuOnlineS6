using System;
using Foundation;
using System.IO;
using UIKit;

namespace MuIos
{
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    {
        private static Client.Main.MuGame game;
        private UIWindow startupErrorWindow;

        internal static void RunGame()
        {
            game = new Client.Main.MuGame();
            game.Run();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(Program));
        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            BeginInvokeOnMainThread(() =>
            {
                try
                {
                    string settings = Path.Combine(NSBundle.MainBundle.BundlePath, "appsettings.json");
                    if (!File.Exists(settings))
                        throw new FileNotFoundException("The iPhone package is missing appsettings.json.", settings);
                    Client.Main.Constants.SETTINGS_PATH = settings;
                    RunGame();
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                    var controller = new UIViewController();
                    controller.View = new UITextView(UIScreen.MainScreen.Bounds)
                    {
                        Editable = false,
                        BackgroundColor = UIColor.White,
                        TextColor = UIColor.Black,
                        Text = "TwinMU could not start.\n\n" + exception,
                        AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
                    };
                    startupErrorWindow = new UIWindow(UIScreen.MainScreen.Bounds)
                    {
                        RootViewController = controller
                    };
                    startupErrorWindow.MakeKeyAndVisible();
                }
            });
            return true;
        }
    }
}
