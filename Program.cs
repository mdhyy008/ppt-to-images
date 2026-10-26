using System;
using System.Windows.Forms;

namespace SlideShot
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppPaths.Prepare();
            Application.Run(new MainForm());
        }
    }
}
