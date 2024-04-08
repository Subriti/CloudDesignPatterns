using Microsoft.AspNetCore.Mvc;
using Polly;

namespace ArchitecturePatterns.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RetryController : ControllerBase
    {
        string requestEndpoint = "api/values";

        private readonly ILogger<RetryController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RetryController(ILogger<RetryController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("Users")]
        public async Task<IActionResult> GetUsers()
        {
            using (HttpClient _httpClient = new HttpClient())
            {
                try
                {
                    var retryHelper = new HttpRetryHelper(_logger); // Instantiate HttpRetryHelper
                    // Define the HTTP request action inside a lambda expression
                    var result = retryHelper.ExecuteWithRetry(() =>
                    {
                        // Make HTTP request using HttpClient
                        return _httpClient.GetAsync("https://jsonplaceholder.typicode.com/users").Result;
                    }, retryCount: 3, retryInterval: TimeSpan.FromSeconds(1));

                    // Check if the HTTP request was successful
                    if (result.IsSuccessStatusCode)
                    {
                        // Return the response content
                        return Content(await result.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        // Return an error message
                        return StatusCode((int)result.StatusCode, "Failed to fetch data from the server.");
                    }
                }
                catch (Exception ex)
                {
                    // Return an error message
                    return StatusCode(500, $"An error occurred: {ex.Message}");
                }
            }
        }

        [HttpGet(Name = "GetPosts")]
        public async Task<IActionResult> GetPosts()
        {
            var retryPolicy = Policy.Handle<Exception>()
                 .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                 .RetryAsync(3, (ex, count) =>
                 {
                     Console.WriteLine($"Retry count {count}");
                 });

            //HttpClient client = new HttpClient();

            var client = _httpClientFactory.CreateClient("errorApiClient");
            HttpResponseMessage response = await retryPolicy.ExecuteAsync(() => client.GetAsync("https://jsonplaceholder.typicode.com/postss"));

            // Check if the HTTP request was successful
            if (response.IsSuccessStatusCode)
            {
                // Return the response content
                return Content(await response.Content.ReadAsStringAsync());
            }
            else
            {
                // Return an error message
                return StatusCode((int)response.StatusCode, "Failed to fetch data from the server.");
            }
        }

        /*[HttpGet("CircuitBreaker")]
        public async Task<string> Get()
        {
            var client = _httpClientFactory.CreateClient("errorApiClient");
            return await client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/200");
        }*/

        [HttpGet("CircuitBreaker")]
        public async Task<IActionResult> GetWithCircuitBreaker()
        {
            var client = _httpClientFactory.CreateClient("errorApiClient");
            HttpResponseMessage response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts/200");

            if (response.IsSuccessStatusCode)
            {
                return Content(await response.Content.ReadAsStringAsync());
            }
            else
            {
                return StatusCode((int)response.StatusCode, "Failed to fetch data from the server.");
            }
        }

        [HttpGet("CB1")]
        public async Task<IActionResult> ImplementCircuitBreaker()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("errorApiClient");
                // Simulate multiple requests
                for (int i = 0; i < 5; i++)
                {
                    HttpResponseMessage response = await client.GetAsync(requestEndpoint);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Request failed: {response.StatusCode}");
                        // Simulate changing endpoint for each request
                        return Redirect($"https://jsonplaceholder.typicode.com/posts/{i + 1}");
                    }
                    await Task.Delay(3000); // Simulate some delay between requests
                }
                return Ok("All requests succeeded.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /*[HttpGet("CBdhdh")]
        public async Task Implement()
        {
            setup();
            for (int i=0; i<5; i++)
            {
                try
                {
                    Thread.Sleep(3000);
                    fetch().GetAwaiter().GetResult();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    requestEndpoint = "https://jsonplaceholder.typicode.com/posts/1";
                }
            }
        }

        public async Task<IActionResult> fetch()
        {
            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync(requestEndpoint);
            if (response.IsSuccessStatusCode)
            {
                // Return the response content
                return Content(await response.Content.ReadAsStringAsync());
            }
            else
            {
                // Return an error message
                return StatusCode((int)response.StatusCode, "Failed to fetch data from the server.");
            }
        }

        public void setup()
        {
            var breakerPolicy= Policy.HandleResult<HttpResponseMessage>(r=> !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(5));
        }
    }*/

    }
}
