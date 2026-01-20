using System;
using System.Windows.Forms;
using System.Diagnostics;
using HaloShift;

namespace HaloShift
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Check for and close existing HaloShift processes
            Process currentProcess = Process.GetCurrentProcess();
            Process[] existingProcesses = Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (Process process in existingProcesses)
            {
                // Don't kill the current process
                if (process.Id != currentProcess.Id)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(2000); // Wait up to 2 seconds for graceful shutdown
                    }
                    catch
                    {
                        // Ignore errors if process already closed
                    }
                }
            }

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
