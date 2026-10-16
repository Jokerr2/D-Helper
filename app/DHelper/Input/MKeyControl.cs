namespace GHelper.Input
{
    /// <summary>Dell-only stub — ASUS M-key HID remapping removed.</summary>
    public static class MKeyControl
    {
        public static bool IsFirmware(string name) => false;
        public static string? CarrierSlot(int eventId) => null;
        public static bool IsSupported() => false;
        public static void ApplyAll() { }
        public static void Reset() { }
    }
}
