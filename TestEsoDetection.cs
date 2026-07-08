using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace HaloShift
{
    /// <summary>
    /// Simple test class to verify ESO process detection functionality
    /// </summary>
    public class TestEsoDetection
    {
        private static bool IsEsoRunning()
        {
            try
            {
                return Process.GetProcessesByName("eso64").Any();
            }
            catch
            {
                return false;
            }
        }

        public static void RunTest()
        {
            Console.WriteLine("Testing ESO process detection...");
            Console.WriteLine($"ESO running before test: {IsEsoRunning()}");

            // Create a dummy process named eso64.exe to simulate ESO running
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (Process testProcess = Process.Start(startInfo))
            {
                // Note: This won't actually be named eso64.exe, but demonstrates the concept
                // In real testing, you'd need to rename notepad.exe or use a different approach
                Console.WriteLine($"Test process started with ID: {testProcess.Id}");
                Console.WriteLine($"ESO running during test: {IsEsoRunning()}");
                
                Thread.Sleep(2000); // Wait 2 seconds
                
                testProcess.Kill();
            }

            Console.WriteLine($"ESO running after test: {IsEsoRunning()}");
            Console.WriteLine("Test completed. Note: For real ESO testing, launch eso64.exe manually.");
        }
    }
}
