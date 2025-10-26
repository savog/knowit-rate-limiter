using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace RateLimiter.Tests;

public class RateLimiterMiddlewareTests
{
    [Fact]
    public void Create_Returns_Valid_Middleware_Delegate()
    {
        var core = Substitute.For<IRateLimiter>();
        var mw = RateLimiterMiddleware.Create(core);
        Assert.NotNull(mw);
    }

    [Fact]
    public async Task Allowed_Request_Calls_Next_And_Not_429()
    {
        var core = Substitute.For<IRateLimiter>();
        core.Allow(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>()).Returns(true);

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var ctx = new DefaultHttpContext();
        var mw = RateLimiterMiddleware.Create(core);

        await mw(next)(ctx);
        core.Received(1).Allow(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status429TooManyRequests, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Blocked_Request_Returns_429_And_Skips_Next()
    {
        var core = Substitute.For<IRateLimiter>();
        core.Allow(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>()).Returns(false);

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var ctx = new DefaultHttpContext();
        var mw = RateLimiterMiddleware.Create(core);

        await mw(next)(ctx);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, ctx.Response.StatusCode);
    }
}
