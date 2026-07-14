namespace Core.Models.Entities
{
    public class GeographyCountry
    {
        public string Code { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string NativeName { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;
        public ICollection<GeographyRegion> Regions { get; private set; } = new List<GeographyRegion>();

        private GeographyCountry() { }

        public GeographyCountry(string code, string name, string nativeName)
        {
            Code = code.Trim().ToUpperInvariant();
            Name = name.Trim();
            NativeName = nativeName.Trim();
        }

        public void UpdateNames(string name, string nativeName)
        {
            Name = name.Trim();
            NativeName = nativeName.Trim();
            IsActive = true;
        }

        public void Deactivate() => IsActive = false;
    }
}
