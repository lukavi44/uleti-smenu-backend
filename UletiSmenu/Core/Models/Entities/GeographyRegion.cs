namespace Core.Models.Entities
{
    public class GeographyRegion
    {
        public string Code { get; private set; } = string.Empty;
        public string CountryCode { get; private set; } = string.Empty;
        public GeographyCountry Country { get; private set; } = null!;
        public string Name { get; private set; } = string.Empty;
        public string NativeName { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;
        public ICollection<GeographyCity> Cities { get; private set; } = new List<GeographyCity>();

        private GeographyRegion() { }

        public GeographyRegion(string code, string countryCode, string name, string nativeName)
        {
            Code = code.Trim();
            CountryCode = countryCode.Trim().ToUpperInvariant();
            Name = name.Trim();
            NativeName = nativeName.Trim();
            IsActive = true;
        }

        public void Update(string countryCode, string name, string nativeName)
        {
            CountryCode = countryCode.Trim().ToUpperInvariant();
            Name = name.Trim();
            NativeName = nativeName.Trim();
            IsActive = true;
        }

        public void Deactivate() => IsActive = false;
    }
}
