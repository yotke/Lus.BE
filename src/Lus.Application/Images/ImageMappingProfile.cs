using AutoMapper;
using Lus.Application.Images.Entities;
using Lus.Application.Roles.Commands.ModifyImage;
using Lus.Contracts.Images;

namespace Lus.Application.Images
{
    public class ImageMappingProfile : Profile
    {
        public ImageMappingProfile()
        {
            CreateMap<Image, ImageDto>();
            CreateMap<ModifyImageStatusDto, ModifyImageStatusCommand>();
        }
    }
}