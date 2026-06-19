namespace HaloShift
{
    /// <summary>
    /// Test stub replacing the real Avalonia-based ControlsWindow.
    /// </summary>
    internal static class ControlsWindow
    {
        public static bool IsOpen { get; set; }

        public static void CloseIfOpen() => IsOpen = false;

        public static void ShowOrActivate() => IsOpen = true;

        public static void Reset() => IsOpen = false;
    }
}
