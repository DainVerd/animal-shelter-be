using Application.Dtos.Animal;
using Application.Entities.Animals;
using Application.ViewModels.Animal;
using AutoMapper;
using Domain.Models;

namespace Application.MappingProfiles;

public class AnimalMappingProfile : Profile
{
    public AnimalMappingProfile()
    {
        CreateMap<CreateAnimalViewModel, Animal>()
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth));

        CreateMap<Animal, AnimalDto>();
        CreateMap<AnimalFilterViewModel, AnimalsFilter>();
        CreateMap<AnimalImage, AnimalImageDto>();

        CreateMap<UpdateAnimalViewModel, Animal>()
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth));

        CreateMap<PublicAnimal, PublicAnimalDto>();

        CreateMap<Animal, PublicAnimal>()
            .ForMember(
                dest => dest.Url,
                opt => opt.MapFrom(src =>
                    src.Images
                        .Select(x => x.Url)
                        .FirstOrDefault() ?? string.Empty));
    }
}
