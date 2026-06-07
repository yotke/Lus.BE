using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Lus.Application.Common.Exceptions;
using Lus.Contracts;
using Lus.Infrastructure.Exceptions;

namespace Lus.Infrastructure.ErrorHandlers
{
    public sealed class DomainExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            static IActionResult CreateErrorModel(
                CommonApplicationException exception,
                int statusCode,
                string fallbackErrorCode = null) => CreateActionResult(
                statusCode,
                exception.Code ?? fallbackErrorCode,
                exception.Message,
                exception.ExceptionId,
                exception.LockTimeLeft);

            static IActionResult CreateActionResult(
                int statusCode,
                string errorCode,
                string message,
                int exceptionId,
                double? lockTimeLeft) => new ObjectResult(new ErrorModel(
                errorCode,
                message,
                exceptionId,
                lockTimeLeft
                ))
                {
                    StatusCode = statusCode
                };

            bool IsGetRequest() =>
                context.HttpContext.Request.Method.Equals("get", StringComparison.InvariantCultureIgnoreCase);

            context.Result = context.Exception switch
            {
                MembershipException membershipError => CreateErrorModel(membershipError, 400, membershipError.Code),
                //NotFoundException notFound => CreateErrorModel(notFound, 404, ErrorCodes.EntityNotFound),
                //DomainException domain => CreateErrorModel(domain, 400, ErrorCodes.EntityInvalid),
                //ForbiddenException forbidden => CreateErrorModel(forbidden, 403),
                //EntityNotFoundException entityNotFound when IsGetRequest() => CreateErrorModel(entityNotFound, 404,
                //    entityNotFound.Code),
                SummonExeption entityValidation => CreateErrorModel(entityValidation, 666, entityValidation.Code),
                EntityNotFoundException entityNotFound => CreateErrorModel(entityNotFound, 400, entityNotFound.Code),
                EntityValidationException entityValidation => CreateErrorModel(entityValidation, 400, entityValidation.Code),
                //NotUniqueEntity notUnique => CreateErrorModel(notUnique, 400, notUnique.Code),
                _ => null,
                
            };
        }
    }
}
