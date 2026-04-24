using System;
using System.Threading;
using System.Windows.Forms;

namespace HaloShift
{
    static class Program
    {
        private const string SingleInstanceMutexName = @"Local\HaloShiftSingleInstance";

        [STAThread]
        static void Main()
        {
            using var instanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew)
                return;

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
