using System;
using System.Windows.Forms;

namespace CraftWar
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LaunchWindow launchWindow = new LaunchWindow();
            launchWindow.ShowDialog();

            if (launchWindow.DialogResult == DialogResult.OK)
            {
                using (Game1 game = new Game1(launchWindow.networkManager, launchWindow.seed))
                {
                    launchWindow.Close();
                    game.Run();
                }
            }
            else
            {
                launchWindow.Close();
            }
        }
    }
#endif
}
