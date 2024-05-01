
using System;
using System.Linq;
using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemberDTO>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(
                    src => src.Photos.FirstOrDefault(x => x.IsMain).Url
                ));
            CreateMap<Photo, UserPhotoDTO>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(
                    src => src.appUser.UserName
                ));
            CreateMap<Photo, PhotoDTO>();
            CreateMap<MemberUpdateDTO, AppUser>();
            CreateMap<RegisterDTO, AppUser>();
            CreateMap<Message, MessageDTO>()
                .ForMember(dest => dest.SenderPhotoUrl, opt => opt.MapFrom(
                    src => src.Sender.Photos.FirstOrDefault(p => p.IsMain).Url
                ))
                .ForMember(dest => dest.ReceiverPhotoUrl, opt => opt.MapFrom(
                    src => src.Receiver.Photos.FirstOrDefault(p => p.IsMain).Url
                ));
            CreateMap<DateTime, DateTime>().ConvertUsing(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
        }
    }
}