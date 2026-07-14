namespace Core.Models.Entities
{
    public class GeographyCity
    {
        public string Code { get; private set; } = string.Empty;
        public string RegionCode { get; private set; } = string.Empty;
        public GeographyRegion Region { get; private set; } = null!;
        public string Name { get; private set; } = string.Empty;
        public string NativeName { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        private GeographyCity() { }

        public GeographyCity(string code, string regionCode, string name, string nativeName)
        {
            Code = code.Trim();
            RegionCode = regionCode.Trim();
            Name = name.Trim();
            NativeName = nativeName.Trim();
            IsActive = true;
        }

        public void Update(string regionCode, string name, string nativeName)
        {
            RegionCode = regionCode.Trim();
            Name = name.Trim();
            NativeName = nativeName.Trim();
            IsActive = true;
        }

        public void Deactivate() => IsActive = false;
    }
}
