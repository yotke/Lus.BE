using FluentValidation;
using Lus.Application.Common.Options;

namespace Lus.Application.Common.Extensions
{
    public static class ValidationExtensions
    {
        public static IRuleBuilderOptions<T, string> PasswordValidate<T>(this IRuleBuilder<T, string> ruleBuilder, PasswordConfigOptions passwordConfigOptions) =>
            ruleBuilder.NotEmpty().Matches(
                $"(?=(.*[a-z]){{{passwordConfigOptions.MinRequiredSmallLetter},}})(?=(.*[A-Z]){{{passwordConfigOptions.MinRequiredCapitalLetter},}})(?=(.*\\d){{{passwordConfigOptions.MinRequiredNumericCharacters},}})(?=(.*[@$!%*?&_]){{{passwordConfigOptions.MinRequiredNonAlphanumericCharacters},}})[A-Za-z\\d@$!%*?&._]{{{passwordConfigOptions.MinRequiredPasswordLength},}}");

        public static IRuleBuilderOptions<T, string> EmailValidate<T>(this IRuleBuilder<T, string> ruleBuilder) =>
            ruleBuilder.NotEmpty()
                .Matches(@"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|([-a-z0-9!#$%&'*+\/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$");
    }
}
