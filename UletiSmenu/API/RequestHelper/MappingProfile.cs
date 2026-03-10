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

            //CreateMap<Employer, RegisterEmployerDTO>();

            CreateMap<Employer, EmployerDTO>()
             .ForMember(dest => dest.PIB, opt => opt.MapFrom(src => src.PIB.Value))
             .ForMember(dest => dest.MB, opt => opt.MapFrom(src => src.MB.Value))
             .IncludeBase<User, EmployerDTO>();

            CreateMap<User, EmployerDTO>();

            CreateMap<Employee, EmployeeDTO>();

            CreateMap<RegisterEmployeeDTO, Employee>()
                .ForMember(dest => dest.Applications, opt => opt.Ignore())
                .ForMember(dest => dest.ProfilePhoto, opt => opt.Ignore())
                 .ConstructUsing((dto, context) =>
                 Employee.Create(
                     Guid.NewGuid(),
                     dto.Email,
                     dto.Email,
                     dto.PhoneNumber,
                     "",
                     null,
                     dto.FirstName,
                     dto.LastName).Value);

            CreateMap<Employee, RegisterEmployeeDTO>();

            CreateMap<Address, AddressDTO>()
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street.Name))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.Name))
                .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.City.PostalCode.Value))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.City.Country.Name))
                .ForMember(dest => dest.Region, opt => opt.MapFrom(src => src.City.Region.Name));

            CreateMap<RestaurantLocation, RestaurantLocationDTO>();

            CreateMap<JobPost, JobPostDTO>()
              .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
              .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => src.IsArchived(DateTime.UtcNow)))
              .ForMember(dest => dest.RestaurantLocationName, opt => opt.MapFrom(src => src.RestaurantLocation != null ? src.RestaurantLocation.Name : null))
              .ForMember(dest => dest.RestaurantLocationCity, opt => opt.MapFrom(src => src.RestaurantLocation != null ? src.RestaurantLocation.City : null))
              .ForMember(dest => dest.Employer, opt => opt.MapFrom(src => src.Employer));

            CreateMap<JobPostCreateDTO, JobPost>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) 
                .ForMember(dest => dest.Employer, opt => opt.Ignore())
                .ForMember(dest => dest.EmployerId, opt => opt.Ignore())
                .ConstructUsing((dto, context) =>
                {
                    var employerId = (Guid)context.Items["EmployerId"];
                    
                    return JobPost.Create(
                        Guid.NewGuid(),
                        dto.Title,
                        dto.Description,
                        Enum.Parse<JobStatusEnum>(dto.Status),
                        dto.StartingDate,
                        dto.VisibleUntil,
                        employerId,
                        dto.RestaurantLocationId,
                        dto.Salary,
                        dto.Position
                    ).Value;
                });
        }
    }
}

            //CreateMap<LoginUserDTO, User>()
            //    .ForMember(dest => dest.ProfilePhoto, opt => opt.Ignore());

            //CreateMap<RegisterEmployeeDTO, Employee>();