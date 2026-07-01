namespace Core.Helpers
{
    public static class ChatRules
    {
        public const int ArchiveHoursAfterShiftStart = 12;

        public static bool ShouldArchiveChat(DateTime shiftStartUtc, DateTime utcNow)
        {
            return utcNow >= shiftStartUtc.AddHours(ArchiveHoursAfterShiftStart);
        }
    }
}
