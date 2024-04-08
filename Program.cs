using AspNetCoreRateLimit;
using Microsoft.AspNetCore.RateLimiting;
using Polly;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddLogging();

// Configure HttpClient with Polly Circuit Breaker
builder.Services.AddHttpClient("errorApiClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:7203/swagger/index.html");
}).AddTransientHttpErrorPolicy(policy =>
    policy.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3, // Number of consecutive failures before breaking the circuit
        durationOfBreak: TimeSpan.FromSeconds(30) // Duration of circuit break
    )
);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//global rate limit
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(), factory: partition => new FixedWindowRateLimiterOptions
    {
        AutoReplenishment = true,
        PermitLimit = 20,
        QueueLimit = 0,
        Window = TimeSpan.FromMinutes(1)
    }));
});

//grouped rate limit
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("Api", options =>
    {
        options.AutoReplenishment = true;
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
    });

    options.AddFixedWindowLimiter("Web", options =>
    {
        options.AutoReplenishment = true;
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
    });
});


// Add services required for rate limiting (throttling)
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Register the required processing strategy
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    //for throttling
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//for rate Limiter
app.UseRateLimiter();

// Add rate limiting middleware (throttling)
app.UseIpRateLimiting();

app.Run();