using FluentValidation;
using FluentValidation.Validators;

namespace Lus.Application.Common.Extensions.Validators
{
    public class EnumTypeValidator<T> : PropertyValidator<T, int>
     where T : struct, Enum
    {
        public override string Name => GetType().Name;

        protected override string GetDefaultMessageTemplate(string errorCode) => "'{PropertyValue}' is not valid for the destination type. Please specify one of the valid values: Customer, SystemUser.";

        public override bool IsValid(ValidationContext<T> context, int value) => Enum.GetValues<T>().Any(orgValue =>
            string.Equals(orgValue.ToString(), value.ToString(), StringComparison.InvariantCultureIgnoreCase));
    }
}
