using AutoMapper;
using Lus.Application.Users.Commands.AddRoleToUser;
using Lus.Application.Users.Commands.CreateUser;
using Lus.Application.Users.Commands.ModifyRoleToUser;
using Lus.Application.Users.Commands.UpdateUserInfo;
using Lus.Application.Users.Commands.UserLoginAttempts;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Queries.GetUsersInfo;
using Lus.Application.Users.Queries.GetUsersInfoByOrganization;

using Lus.Contracts.Users;
using Newtonsoft.Json;

namespace Lus.Application.Users
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<UserLoginAttemptCommand, UserLoginAttempt>();
            CreateMap<CreateUserDto, CreateUserCommand>()
                .ForMember(m => m.Email, opt => opt.MapFrom(s => s.Email.ToLower()));

            CreateMap<User, UserFullInfoDto>();


            CreateMap<UpdateUserInfoDto, UpdateUserInfoCommand>();
            CreateMap<SearchUserQueryDto, GetUsersInfoQuery>();
            CreateMap<SearchUserQueryDto, GetUsersInfoByOrganizationQuery>();
            CreateMap<User, UserDto>();
            CreateMap<User, UserInfoDto>();
            CreateMap<User, UserTenderDto>();
            CreateMap<User, AuthUserInfo>();
            CreateMap<User, UserDataDto>();
            CreateMap<UserLoginAttempt, UserLoginAttemptDto>();
            CreateMap<AddRolesToUserDto, AddRoleToUserCommand>();
            CreateMap<ModifyRolesToUserDto, ModifyRoleToUserCommand>();
       

            CreateMap<CreateUserCommand, User>()
                .ForMember(m => m.UserName, opt => opt.MapFrom(s => s.Email.ToLower()))
                .ForMember(m => m.Email, opt => opt.MapFrom(s => s.Email.ToLower()))
                .ForMember(m => m.AllowedScopes, opt => opt.Ignore())
                .ForMember(m => m.ConfirmationToken, opt => opt.Ignore())
                .ForMember(m => m.AllowedGrantTypes, opt => opt.Ignore())
                .ForMember(m => m.ClientSecrets, opt => opt.Ignore())
                .ForMember(m => m.Claims, opt => opt.Ignore())
                .ForMember(m => m.Active, opt => opt.MapFrom(_ => true))
                .ForMember(m => m.IsConfirmed, opt => opt.MapFrom(_ => false))
                .ForMember(m => m.Id, opt => opt.Ignore())
                .ForMember(m => m.PasswordHash, opt => opt.Ignore())
                .ForMember(m => m.IsDeleted, opt => opt.Ignore())
                .ForMember(m => m.CreatedOn, opt => opt.Ignore())
                .ForMember(m => m.CreatedById, opt => opt.Ignore())
                .ForMember(m => m.UpdatedOn, opt => opt.Ignore())
                .ForMember(m => m.UpdatedById, opt => opt.Ignore())
                .ForMember(m => m.DeletedOn, opt => opt.Ignore())
                .ForMember(m => m.DeletedById, opt => opt.Ignore());
        }

        private T ToClass<T>(string json)
        {
            var serializerSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }
    }
}