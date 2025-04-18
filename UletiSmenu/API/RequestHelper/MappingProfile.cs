using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Models.ValueObjects;

namespace API.RequestHelper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegisterEmployerDTO, Employer>()
            .ForMember(dest => dest.SubscriptionId, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionStart, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionStop, opt => opt.Ignore())
            .ForMember(dest => dest.ProfilePhoto, opt => opt.Ignore())
            .ConstructUsing((dto, context) =>
                Employer.Create(
                    Guid.NewGuid(),
                    dto.Name,
                    dto.Email,
                    dto.Email,
                    dto.PhoneNumber,
                    "",
                    PIB.Create(dto.PIB).Value,
                    MB.Create(dto.MB).Value,
                    null, null, null,
                    Address.Create(
                        Street.Create(dto.StreetName, dto.StreetNumber).Value,
                        City.Create(dto.City,
                            PostalCode.Create(dto.PostalCode).Value,
                            Country.Create(dto.Country).Value,
                            Region.Create(dto.Region).Value
                        ).Value
                    ).Value
                ).Value
            );

            CreateMap<LoginUserDTO, User>()
                .ForMember(dest => dest.ProfilePhoto, opt => opt.Ignore());


            CreateMap<Address, AddressDTO>()
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street.Name))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.Name))
                .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.City.PostalCode.Value))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.City.Country.Name))
                .ForMember(dest => dest.Region, opt => opt.MapFrom(src => src.City.Region.Name));

            CreateMap<JobPost, JobPostDTO>()
              .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<JobPostDTO, JobPost>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<JobStatusEnum>(src.Status)))
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // If you generate it elsewhere
                .ForMember(dest => dest.Employer, opt => opt.Ignore()); // To avoid circular references or context issues

        }
    }
}