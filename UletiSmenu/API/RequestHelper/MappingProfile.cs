using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
using Core.Models.ValueObjects;

namespace API.RequestHelper
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            CreateMap<RegisterEmployerDTO, Employer>()
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.SubscriptionId, opt => opt.Ignore())
                .ForMember(dest => dest.SubscriptionStart, opt => opt.Ignore())
                .ForMember(dest => dest.SubscriptionStop, opt => opt.Ignore())
                .ConstructUsing((dto, context) =>
                    Employer.Create(
                        Guid.NewGuid(),
                        dto.Name,
                        dto.Email,
                        dto.Email,
                        dto.PhoneNumber,
                        dto.ProfilePhoto,
                        PIB.Create(dto.PIB).Value,
                        MB.Create(dto.MB).Value,
                        context.Items["CompanyId"] as Guid? ?? Guid.NewGuid(), 
                        null, null, null
                    ).Value);

            CreateMap<RegisterEmployerDTO, Company>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())
               .ForMember(dest => dest.Posts, opt => opt.Ignore())
                .ConstructUsing((dto, context) => Company.Create(
                context.Items["CompanyId"] as Guid? ?? Guid.NewGuid(),
                dto.CompanyName,
                Address.Create(
                    Street.Create(dto.StreetName, dto.SteetNumber).Value, // Street (Assume empty house number)
                    City.Create(dto.City, PostalCode.Create(dto.PostalCode).Value, Country.Create(dto.Country).Value, Region.Create(dto.Region).Value).Value
                ).Value
            ).Value);



            CreateMap<LoginUserDTO, User>()
                .ForMember(dest => dest.ProfilePhoto, opt => opt.Ignore());

            //CreateMap<CreateCompanyDTO, Company>()
            //    .ForMember(dest => dest.Posts, opt => opt.Ignore())
            //    .ForMember(dest => dest.Address, opt => opt.MapFrom(src =>
            //        Address.Create(
            //            Street.Create(src.Street, "").Value, // Ensure a default number if missing
            //            City.Create(src.City, PostalCode.Create(src.PostalCode).Value, Country.Create(src.Country).Value, Region.Create(src.Region).Value).Value
            //        ).Value
            //    ));

        }
    }
}