namespace RateLimiter;

public interface IRateLimiter
{
    bool Allow(string ip, string path, DateTime utcNow);
}