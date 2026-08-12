namespace Core.JobPosts
{
    public class JobPostLifecycleSettings
    {
        public const string SectionName = "JobPostLifecycle";

        /// <summary>When false, the background expiry worker does not run.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>How often to scan for stale Active posts.</summary>
        public int IntervalMinutes { get; set; } = 15;

        /// <summary>Max posts updated per tick (keeps each run bounded).</summary>
        public int BatchSize { get; set; } = 500;
    }
}
