using RateLimiter.Core;

namespace RateLimiter.Tests.Core;

public class DefaultRateLimiterTests
{
    private static DateTime StartTime => new(2025, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void General_Allows_Until_Limit_Then_Blocks()
    {
        var opts = new RateLimiterOptions
        {
            RequestLimiterEnabled = true,
            DefaultRequestLimitCount = 3,
            DefaultRequestLimitMs = 1000
        };

        var core = new DefaultRateLimiter(opts);

        Assert.True(core.Allow("1.2.3.4", "/api/a", StartTime));
        Assert.True(core.Allow("1.2.3.4", "/api/b", StartTime.AddMilliseconds(100)));
        Assert.True(core.Allow("1.2.3.4", "/api/c", StartTime.AddMilliseconds(200)));
        Assert.False(core.Allow("1.2.3.4", "/api/d", StartTime.AddMilliseconds(300)));
        Assert.True(core.Allow("1.2.3.4", "/api/e", StartTime.AddMilliseconds(1001)));
    }

    [Fact]
    public void Endpoint_Rule_Overrides_Default_For_Same_Ip()
    {
        var opts = new RateLimiterOptions
        {
            RequestLimiterEnabled = true,
            DefaultRequestLimitCount = 1,
            DefaultRequestLimitMs = 1000,
            EndpointLimits = new()
            {
                new EndpointLimitOptions { Endpoint = "/api/books", RequestLimitCount = 3, RequestLimitMs = 1000 }
            }
        };

        var core = new DefaultRateLimiter(opts);

        Assert.True(core.Allow("9.9.9.9", "/api/books?x=1", StartTime));
        Assert.True(core.Allow("9.9.9.9", "/api/books/",   StartTime.AddMilliseconds(100)));
        Assert.True(core.Allow("9.9.9.9", "/API/BOOKS#f",  StartTime.AddMilliseconds(200)));
        Assert.False(core.Allow("9.9.9.9", "/api/books",   StartTime.AddMilliseconds(300)));
    }

    [Fact]
    public void Different_Ips_Have_Isolated_Buckets()
    {
        var opts = new RateLimiterOptions
        {
            RequestLimiterEnabled = true,
            DefaultRequestLimitCount = 1,
            DefaultRequestLimitMs = 1000
        };

        var core = new DefaultRateLimiter(opts);

        Assert.True(core.Allow("1.1.1.1", "/x", StartTime));
        Assert.False(core.Allow("1.1.1.1", "/y", StartTime));
        Assert.True(core.Allow("2.2.2.2", "/z", StartTime));
    }

    [Theory]
    [InlineData("/api/x/?a=1")]
    [InlineData("/API/X#frag")]
    [InlineData("/api/x/")]
    [InlineData("api/x")]
    public void Path_Normalization_Treats_Variants_As_Same_Endpoint(string variant)
    {
        var opts = new RateLimiterOptions
        {
            RequestLimiterEnabled = true,
            DefaultRequestLimitCount = 2,
            DefaultRequestLimitMs = 1000,
            EndpointLimits = new() { new() { Endpoint = "/api/x", RequestLimitCount = 2, RequestLimitMs = 1000 } }
        };

        var core = new DefaultRateLimiter(opts);

        Assert.True(core.Allow("7.7.7.7", variant, StartTime));
        Assert.True(core.Allow("7.7.7.7", "/api/x", StartTime.AddMilliseconds(1)));
        Assert.False(core.Allow("7.7.7.7", "/api/x", StartTime.AddMilliseconds(2)));
    }

    [Fact]
    public void Allows_Request_After_Limit_Period()
    {
        var opts = new RateLimiterOptions
        {
            RequestLimiterEnabled = true,
            DefaultRequestLimitCount = 2,
            DefaultRequestLimitMs = 1000
        };

        var core = new DefaultRateLimiter(opts);

        Assert.True(core.Allow("3.3.3.3", "/api/edge", StartTime));
        Assert.True(core.Allow("3.3.3.3", "/api/edge", StartTime.AddMilliseconds(10)));
        Assert.False(core.Allow("3.3.3.3", "/api/edge", StartTime.AddMilliseconds(20)));
        Assert.True(core.Allow("3.3.3.3", "/api/edge", StartTime.AddMilliseconds(1000)));
    }

    [Fact]
    public void When_Disabled_Always_Allows()
    {
        var opts = new RateLimiterOptions
        {
            RequestLimiterEnabled = false,
            DefaultRequestLimitCount = 1,
            DefaultRequestLimitMs = 1000
        };

        var core = new DefaultRateLimiter(opts);

        for (int i = 0; i < 100; i++)
        {
            Assert.True(core.Allow("5.5.5.5", "/api/test", StartTime.AddMilliseconds(i * 10)));
        }
    }

    [Fact]
    public async Task Concurrent_Requests_Should_Not_Throw_Or_Deadlock()
    {
        var opts = new RateLimiterOptions
        {
            RequestLimiterEnabled = true,
            DefaultRequestLimitCount = 5,
            DefaultRequestLimitMs = 1000
        };

        var core = new DefaultRateLimiter(opts);
        const string ip = "10.10.10.10";
        const string path = "/api/concurrent";
        var startTime = StartTime;

        var tasks = Enumerable.Range(0, 200)
            .Select(_ => Task.Run(() => core.Allow(ip, path, startTime)))
            .ToArray();
        
        var results = await Task.WhenAll(tasks);

        var allowedCount = results.Count(x => x);
        Assert.Equal(5, allowedCount);
    }
}
