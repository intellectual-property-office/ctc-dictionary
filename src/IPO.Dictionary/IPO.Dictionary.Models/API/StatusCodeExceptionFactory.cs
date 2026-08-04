using IPO.Common.Infrastructure;

namespace IPO.Dictionary.Models.API
{
    public static class StatusCodeExceptionFactory
    {
        public static StatusCodeException CreateFileNotExistsStatusCodeException<T>(string errorCode, string id)
        {
            var error = Error.Create<T>(errorCode);
            error.Description += $" The requested file with id({id}) can not be found.";
            return new StatusCodeException(error, $"The requested file with id({id}) can not be found.", null, 404);
        }

    }
}
