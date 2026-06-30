namespace Core.Admin
{
    public class AdminSeedSettings
    {
        public const string SectionName = "AdminSeed";

        public bool Enabled { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = "+381600000001";
    }
}
