using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NRedisPlus;

namespace Redis.Developer.AspDotnetCore
{
    public static class RedisMiddleware
    {
        public static IServiceCollection AddRedis(this IServiceCollection services,
            string connectionString)
        {
            var provider = new RedisConnectionProvider(connectionString);
            return services.AddSingleton(provider);
        }

        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["REDIS_CONNECTION_STRING"];
            return services.AddSingleton(new RedisConnectionProvider(connectionString));
        }
    }
}