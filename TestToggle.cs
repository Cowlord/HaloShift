using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace HaloShift
{
    /// <summary>
    /// Test class to verify toggle functionality without needing actual ESO running
    /// </summary>
    public class TestToggle
    {
        public static void RunTest()
        {
            Console.WriteLine("Testing toggle functionality...");
            
            // Test 1: Check if we can detect processes
            Console.WriteLine("Current processes containing 'eso':");
            var esoProcesses = Process.GetProcesses().Where(p => 
                p.ProcessName.ToLower().Contains("eso")).ToList();
            
            foreach (var proc in esoProcesses)
            {
                Console.WriteLine($"  - {proc.ProcessName} (PID: {proc.Id})");
            }
            
            // Test 2: Check if our ESO detection works
            bool esoRunning = Process.GetProcessesByName("eso64").Any();
            Console.WriteLine($"ESO64 detected: {esoRunning}");
            
            // Test 3: Create a simple ModeManager test
            var modeManager = new ModeManager();
            Console.WriteLine($"Current mode: {modeManager.CurrentMode}");
            
            Console.WriteLine("\nTo test the LB+RB+View toggle:");
            Console.WriteLine("1. Launch ESO (eso64.exe) if available");
            Console.WriteLine("2. Start HaloShift");
            Console.WriteLine("3. Press LB + RB + View simultaneously");
            Console.WriteLine("4. Check Debug output for toggle messages");
            
            // Alternative: Create a fake eso64.exe for testing
            Console.WriteLine("\nCreating temporary eso64.exe for testing...");
            try
            {
                // Start notepad but we can't rename it programmatically easily
                // This is just to show the concept
                Process.Start("notepad.exe");
                Console.WriteLine("Started notepad.exe - this won't be detected as eso64.exe");
                Console.WriteLine("For real testing, you need to launch actual ESO or rename an exe to eso64.exe");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not start test process: {ex.Message}");
            }
        }
    }
}
