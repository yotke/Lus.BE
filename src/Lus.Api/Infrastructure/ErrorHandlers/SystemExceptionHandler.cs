using Microsoft.AspNetCore.Diagnostics;
using Lus.Contracts;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;  // <---- Use this instead of Oracle.ManagedDataAccess.Client

namespace Lus.Infrastructure.ErrorHandlers
{
    public sealed class SystemExceptionHandler
    {
        public static Task UnhandledExceptionsHandler(HttpContext context)
        {
            var exceptionFeature = context.Features[typeof(IExceptionHandlerFeature)] as IExceptionHandlerFeature;

            var (model, statusCode) = exceptionFeature?.Error switch
            {
                // dependencies errors
                MySqlException _
                    => (new ErrorModel(ErrorCodes.ExternalServiceFailed, "MySQLService Unhandled Exception"), 503),

                _ => (new ErrorModel(ErrorCodes.ServerError, "The server encountered an unexpected condition which prevented it from fulfilling the request."), 500)
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.WriteAsync(JsonConvert.SerializeObject(model, SerializationSettings.JsonSettings));

            return Task.CompletedTask;
        }
    }
}




