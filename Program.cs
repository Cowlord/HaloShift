using System;
using System.Windows.Forms;
using HaloShift;

namespace HaloShift
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var controller = new ControllerManager())
            {
                using (var form = new MainForm(controller))
                {
                    Application.Run(form);
                }
            }
        }
    }
}
