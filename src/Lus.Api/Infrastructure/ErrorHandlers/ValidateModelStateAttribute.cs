using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Lus.Application;
using Lus.Contracts;
using Lus.Contracts.Users;
using Lus.Infrastructure.Exceptions;

namespace Lus.Infrastructure.ErrorHandlers
{
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.CreateUser, out var createUserDto);
                if (result && createUserDto is CreateUserDto)
                {
                    if (context.ModelState.Keys.Contains("Email"))
                    {
                        throw new MembershipException(15);
                    }
                    if (context.ModelState.Keys.Contains("Password"))
                    {
                        throw new MembershipException(16);
                    }
                }

                result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.UserResetPassword, out var userResetPasswordDto);
                if (result && userResetPasswordDto is UserResetPasswordDto)
                {
                    if (context.ModelState.Keys.Contains("Email"))
                    {
                        throw new MembershipException(15);
                    }
                }

                result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.UserPasswordFromSms, out var passwordFromSmsDto);
                if (result && passwordFromSmsDto is UserResetPasswordFromSmsDto)
                {
                    if (context.ModelState.Keys.Contains("Email"))
                    {
                        throw new MembershipException(15);
                    }
                    if (context.ModelState.Keys.Contains("Password"))
                    {
                        throw new MembershipException(16);
                    }
                }

                result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.UserPasswordFromMail, out var passwordFromMailDto);
                if (result && passwordFromMailDto is UserResetPasswordFromMailDto)
                {
                    if (context.ModelState.Keys.Contains("Email"))
                    {
                        throw new MembershipException(15);
                    }
                    if (context.ModelState.Keys.Contains("Password"))
                    {
                        throw new MembershipException(16);
                    }
                }

                result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.CheckUserSmsCode, out var checkUserSmsCodeDto);
                if (result && checkUserSmsCodeDto is CheckUserSmsCodeDto)
                {
                    if (context.ModelState.Keys.Contains("Email"))
                    {
                        throw new MembershipException(15);
                    }
                }

                result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.SendOneTimeToken, out var sendOneTimeTokenDto);
                if (result && sendOneTimeTokenDto is SendOneTimeTokenDto)
                {
                    if (context.ModelState.Keys.Contains("Email"))
                    {
                        throw new MembershipException(15);
                    }
                }

                result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.CheckPassword, out var checkPasswordDto);
                if (result && checkPasswordDto is CheckPasswordDto)
                {
                    throw new MembershipException(12);
                }

                result = context.ActionArguments.TryGetValue(ApplicationConstants.ActionModelNames.SsoCheckUser, out var ssoCheckUserDto);
                if (result && ssoCheckUserDto is SsoCheckUserDto)
                {
                    if (context.ModelState.Keys.Contains("Email"))
                    {
                        throw new MembershipException(15);
                    }
                }

                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }
    }

    public static class ValidationHelpers
    {
        public static Func<ActionContext, IActionResult> InvalidModelStateResponseFactory = context =>
        {
            var result = new ErrorModel
            {
                Code = ErrorCodes.EntityInvalid,
                Message = "Validation error"
                //Errors = new Dictionary<string, IEnumerable<string>>()
            };

            var actionParamNames = context is ActionExecutingContext actionExecutingContext
                ? actionExecutingContext.ActionArguments.Keys
                : Enumerable.Empty<string>();

            context.ModelState.ToList().ForEach(i =>
            {
                if (i.Value.Errors.Any())
                {
                    var errorMessages = i.Value.Errors
                        .Select(error => string.IsNullOrEmpty(error.ErrorMessage)
                            ? i.Key + " is not valid"
                            : error.ErrorMessage)
                        .ToList();

                    var key = i.Key;

                    if (actionParamNames.Any(p => i.Key.StartsWith($"{p}.", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        key = key.Remove(0, key.IndexOf(".", StringComparison.OrdinalIgnoreCase) + 1);
                    }

                    //result.Errors.Add(key, errorMessages);
                }
            });

            return new BadRequestObjectResult(result);
        };

        public static BadRequestObjectResult ToBadResult(this ValidationResult validationResult)
        {
            var errorModel = CreateErrorModel();

            //errorModel.Errors = validationResult?.Errors?.GroupBy(e => e.PropertyName).ToDictionary(grouped => grouped.Key,
            //    grouped => grouped.Select(g => g.ErrorMessage));

            return new BadRequestObjectResult(errorModel);
        }

        private static ErrorModel CreateErrorModel() =>
            new ErrorModel
            {
                Code = ErrorCodes.EntityInvalid,
                Message = "Validation error",
                //Errors = new Dictionary<string, IEnumerable<string>>()
            };
    }
}
