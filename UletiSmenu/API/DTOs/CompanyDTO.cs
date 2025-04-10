namespace API.DTOs
{
    public class CompanyDTO
    {
        public Guid CompanyId { get; set; }
        public string Name { get; set; }
        public AddressDTO Address { get; set; }
    }
}

/*AutoMapper cannot use parameterized constructors (like those in record types) unless you use special configuration or MapToConstructor.

By default, it only works with:

Classes with parameterless constructors (public CompanyDTO())

Or if you explicitly map to constructor parameters (extra config)*/

/* If you really want to use record
You'd need to do constructor parameter mapping manually, like:

CreateMap<Company, CompanyDTO>()
    .ConstructUsing(src => new CompanyDTO(src.Id, src.Name, ...));
But that’s more boilerplate, and using class here is simpler and more AutoMapper-friendly.*/