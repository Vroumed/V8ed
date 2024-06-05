using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace V8ed.Extensions
{
    public static class ExceptionExtensions
    {

        public static Dictionary<string, object> GetStatusError(this ControllerBase @base, HttpStatusCode code, string scope, string message)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "https://tools.ietf.org/html/rfc7231" + GetSectionTag(code),
                ["title"] = "One or more validation errors occurred.",
                ["status"] = (int)code,
                ["traceId"] = @base.HttpContext.TraceIdentifier,
                ["errors"] = new Dictionary<string, string[]>
                {
                    [scope] = new string[] { message }
                }
            };
        }

        private static string GetSectionTag(HttpStatusCode code)
        {
            return code switch
            {
                HttpStatusCode.BadRequest => "#section-6.5.1",
                HttpStatusCode.Unauthorized => "#section-6.3.3",
                HttpStatusCode.Forbidden => "#section-6.5.3",
                HttpStatusCode.NotFound => "#section-6.5.4",
                HttpStatusCode.MethodNotAllowed => "#section-6.5.5",
                HttpStatusCode.NotAcceptable => "#section-6.5.6",
                HttpStatusCode.RequestTimeout => "#section-6.5.7",
                HttpStatusCode.Conflict => "#section-6.5.8",
                HttpStatusCode.Gone => "#section-6.5.9",
                HttpStatusCode.LengthRequired => "#section-6.5.10",
                HttpStatusCode.RequestEntityTooLarge => "#section-6.5.11",
                HttpStatusCode.RequestUriTooLong => "#section-6.5.12",
                HttpStatusCode.UnsupportedMediaType => "#section-6.5.13",

                HttpStatusCode.InternalServerError => "#section-6.6.1",
                HttpStatusCode.NotImplemented => "#section-6.6.2",
                HttpStatusCode.BadGateway => "#section-6.6.3",
                HttpStatusCode.ServiceUnavailable => "#section-6.6.4",
                HttpStatusCode.GatewayTimeout => "#section-6.6.5",
                HttpStatusCode.HttpVersionNotSupported => "#section-6.6.6",

                _ => ""
            };
        }
    }
}
