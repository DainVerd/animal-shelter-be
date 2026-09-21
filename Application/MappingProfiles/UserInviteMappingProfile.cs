using Application.Dtos;
using Application.Dtos.Animal;
using Application.Entities;
using Application.Entities.Animals;
using Application.ViewModels;
using Application.ViewModels.Animal;
using AutoMapper;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.MappingProfiles;

public class UserInviteMappingProfile : Profile
{
    public UserInviteMappingProfile()
    {
        CreateMap<UserInviteFilterViewModel, UserInviteFilter>();

        CreateMap<UserInvite, UserInviteDto>();

    }
}

