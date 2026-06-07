using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Common.Ports;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Users.Commands.UpdateUserInfo
{
    public class UpdateUserInfoCommandHandler : IRequestHandler<UpdateUserInfoCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IRecaptchaAdapter recaptchaAdapter;
        private readonly IUserAccessor userAccessor;

        public UpdateUserInfoCommandHandler(IUserAccessor userAccessor, IUsersRepository usersRepository, IRecaptchaAdapter recaptchaAdapter)
        {
            this.userAccessor = userAccessor;
            this.usersRepository = usersRepository;
            this.recaptchaAdapter = recaptchaAdapter;
        }

        public async Task<Unit> Handle(UpdateUserInfoCommand command, CancellationToken cancellationToken)
        {
            var user = await ValidateUser(cancellationToken);

            CopyIfDifferent(user, command);

            await this.usersRepository.UpdateAsync(user, cancellationToken);

            return Unit.Value;
        }

        private async Task<User> ValidateUser(CancellationToken cancellationToken)
        {
            var result = await this.recaptchaAdapter.CheckRecaptcha(cancellationToken);
            if (!result)
            {
                throw new MembershipException("Recaptcha not valid", 41);
            }

            return await this.usersRepository.GetSingleEntityAsync(this.userAccessor.ProjectUser.Id, cancellationToken);
        }

        private void CopyIfDifferent(User target, UpdateUserInfoCommand source)
        {
            var listOfChangesName = new List<string> { "FirstName", "LastName", "Phone", "IdNumber" };
            foreach (var prop in target.GetType().GetProperties())
            {
                if (listOfChangesName.Contains(prop.Name))
                {
                    var targetValue = GetPropValue(target, prop.Name);
                    var sourceValue = GetPropValue(source, prop.Name);
                    if (targetValue != null && !targetValue.Equals(sourceValue))
                    {
                        SetPropertyValue(target, prop.Name, sourceValue);
                    }
                    else if (targetValue == null && sourceValue != null)
                    {
                        SetPropertyValue(target, prop.Name, sourceValue);
                    }
                }
            }
        }

        private object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName)?.GetValue(src, null);
        }

        private void SetPropertyValue(object obj, string propName, object value)
        {
            obj.GetType().GetProperty(propName)?.SetValue(obj, value, null);
        }

    }
}
