using Application.Dtos.Invites;
using Application.Entities;
using Application.ViewModels;
using AutoMapper;
using Domain.Models;

namespace Application.MappingProfiles;

public class UserInviteMappingProfile : Profile
{
    public UserInviteMappingProfile()
    {
        CreateMap<UserInviteFilterViewModel, UserInviteFilter>();

        CreateMap<UserInvite, UserInviteDto>()
            .ForMember(dest => dest.InvitedBy, opt => opt.MapFrom(src => src.InvitedByUser));

        CreateMap<User, InviteUserDto>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.UserName));
    }
}

