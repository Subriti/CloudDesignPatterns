using Polly;

namespace ArchitecturePatterns
{
    public class HttpRetryHelper
    {
        private readonly ILogger _logger;

        public HttpRetryHelper(ILogger logger)
        {
            _logger = logger;
        }

        public T ExecuteWithRetry<T>(Func<T> action, int retryCount, TimeSpan retryInterval)
        {
            var policy = Policy
                .Handle<Exception>()
                .Or<HttpRequestException>()
                .Or<TimeoutException>()
                .WaitAndRetry(retryCount, retryAttempt =>
                {
                    _logger.LogInformation($"Retry attempt {retryAttempt}");
                    return retryInterval;
                }, (exception, timeSpan, retryAttempt, context) =>
                {
                    if (retryAttempt == retryCount)
                    {
                        throw new ApplicationException($"Failed after {retryCount} attempts. Please try again later.");
                    }
                });

            return policy.Execute(action);
        }
    }
}