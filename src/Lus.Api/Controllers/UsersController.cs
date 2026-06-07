using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Application;
using Lus.Application.Users.Commands.AddOrganizationToUser;
using Lus.Application.Users.Commands.CheckEmailToken;
using Lus.Application.Users.Commands.CheckPassword;
using Lus.Application.Users.Commands.CheckUserSmsCode;
using Lus.Application.Users.Commands.CreateUser;
using Lus.Application.Users.Commands.ModifyRoleToUser;
using Lus.Application.Users.Commands.ResetPassword;
using Lus.Application.Users.Commands.SendOneTimeToken;
using Lus.Application.Users.Commands.SsoCheckUser;
using Lus.Application.Users.Commands.UpdateUserInfo;
using Lus.Application.Users.Commands.UserResetPassword;
using Lus.Application.Users.Commands.UserResetPasswordFromMail;
using Lus.Application.Users.Commands.UserResetPasswordFromSms;
using Lus.Application.Users.Queries.GetUserFullInfo;
using Lus.Application.Users.Queries.GetUserInfo;
using Lus.Application.Users.Queries.GetUserPasswordSettings;
using Lus.Application.Users.Queries.GetUsersInfo;
using Lus.Application.Users.Queries.GetUsersInfoByOrganization;
using Lus.Contracts;
using Lus.Contracts.Users;
using Lus.Infrastructure.IdentityServer.Permissions;

namespace Lus.Controllers
{
    [Route("v1/users")]
    [Consumes("application/json"), Produces("application/json")]
    [ApiController]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public UsersController(IMediator mediator, IMapper mapper)
            => (this.mediator, this.mapper) = (mediator, mapper);

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="createUserDto">Create user dto model</param>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> CreateUser(CreateUserDto createUserDto)
        {
            await this.mediator.Send(this.mapper.Map<CreateUserCommand>(createUserDto));

            return StatusCode(StatusCodes.Status201Created, new UserResposneDto());
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            return Ok();
        }

        [HttpPost("CheckPassword")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> CheckPassword(CheckPasswordDto checkPasswordDto)
        {
            await this.mediator.Send(new CheckPasswordCommand(checkPasswordDto.Password));

            return Ok(new UserResposneDto());
        }

        [HttpGet("GetUserInfo")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserInfoDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> GetUserInfo()
        {
            var userInfo = await this.mediator.Send(new GetUserInfoQuery());

            return Ok(userInfo);
        }

        [HttpGet("{id:int}")]
        [Permission(ApplicationConstants.AuthPolicies.AdminRoles)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserFullInfoDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> GetUser(int id)
        {
            var userInfo = await this.mediator.Send(new GetUserFullInfoQuery(id));

            return Ok(userInfo);
        }

        [Permission(ApplicationConstants.AuthPolicies.AdminRoles)]
        [HttpPost("Search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<UserInfoDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> SearchUsers(SearchUserQueryDto searchData)
        {
            var usersInfo = await this.mediator.Send(this.mapper.Map<GetUsersInfoQuery>(searchData));

            return Ok(usersInfo);
        }

        [Permission(ApplicationConstants.AuthPolicies.AdminRolesAndCommitteesManager)]
        [HttpPost("SearchUsersByOrganization")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<UserInfoDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> SearchUsersByOrganization(SearchUserQueryDto searchData)
        {
            var usersInfo = await this.mediator.Send(this.mapper.Map<GetUsersInfoByOrganizationQuery>(searchData));

            return Ok(usersInfo);
        }

        [HttpGet("GetPasswordSettings")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserPasswordSettingsDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> GetPasswordSettings()
        {
            var usersInfo = await this.mediator.Send(new GetUserPasswordSettingsQuery());

            return Ok(usersInfo);
        }

        [HttpPost("SsoCheck")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserCheckedDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> SsoCheckUser(SsoCheckUserDto ssoCheckUserDto)
        {
            var userChecked = await this.mediator.Send(new SsoCheckUserCommand(ssoCheckUserDto.Email));

            return Ok(userChecked);
        }

        [HttpPost("UserResetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> UserResetPassword(UserResetPasswordDto userResetPasswordDto)
        {
            await this.mediator.Send(new UserResetPasswordCommand(userResetPasswordDto.Email, userResetPasswordDto.ResetPasswordType));

            return Ok(new UserResposneDto());
        }

        [HttpPost("UserResetPasswordFromSms")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> ResetPasswordFromSmsCode(UserResetPasswordFromSmsDto passwordFromSmsDto)
        {
            await this.mediator.Send(new UserResetPasswordFromSmsCommand(passwordFromSmsDto.Email, passwordFromSmsDto.SmsCode, passwordFromSmsDto.Password));

            return Ok(new UserResposneDto());
        }

        [HttpPost("UserResetPasswordFromEmail")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> UserResetPasswordFromEmail(UserResetPasswordFromMailDto passwordFromMailDto)
        {
            await this.mediator.Send(new UserResetPasswordFromMailCommand(passwordFromMailDto.PasswordVerificationToken, passwordFromMailDto.Password));

            return Ok(new UserResposneDto());
        }

        [HttpPost]
        [Route("CheckUserSmsCode")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> CheckUserSmsCode(CheckUserSmsCodeDto checkUserSmsCodeDto)
        {
            await this.mediator.Send(new CheckUserSmsCodeCommand(checkUserSmsCodeDto.SmsCode, checkUserSmsCodeDto.Email));

            return Ok(new UserResposneDto());
        }

        [HttpPost]
        [Route("CheckEmailTokenPass")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> CheckEmailToken(CheckEmailTokenDto checkEmailTokenDto)
        {
            await this.mediator.Send(new CheckEmailTokenCommand(checkEmailTokenDto.EmailToken));

            return Ok(new UserResposneDto());
        }

        [HttpPost("SendOneTimeToken")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> SendOneTimeToken(SendOneTimeTokenDto sendOneTimeTokenDto)
        {
            await this.mediator.Send(new SendOneTimeTokenCommand(sendOneTimeTokenDto.Email, sendOneTimeTokenDto.Phone));

            return Ok(new UserResposneDto());
        }

        [HttpPost("AddRoles")]
        [Permission(ApplicationConstants.AuthPolicies.AdminRoles)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserFullInfoDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> AddRolesToUser(ModifyRolesToUserDto addRolesDto)
        {
            var userInfo = await this.mediator.Send(this.mapper.Map<ModifyRoleToUserCommand>(addRolesDto));

            return Ok(userInfo);
        }
        [HttpPost("AddOrganizations")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserFullInfoDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> AddOrganizationsToUser(AddOrganizationsToUserDto addOrganizationsDto)
        {
            var userInfo = await this.mediator.Send(new AddOrganizationToUserCommand(addOrganizationsDto.OrganizationsId, addOrganizationsDto.UserId));

            return Ok(userInfo);
        }
        [HttpPost("ResetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            await this.mediator.Send(new ResetPasswordCommand(resetPasswordDto.Password, resetPasswordDto.Email));

            return Ok(new UserResposneDto());
        }

        [HttpPost("UpdateUserInfo")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResposneDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> UpdateUserInfo(UpdateUserInfoDto updateUserInfoDto)
        {
            await this.mediator.Send(this.mapper.Map<UpdateUserInfoCommand>(updateUserInfoDto));

            return Ok(new UserResposneDto());
        }
    }
}
