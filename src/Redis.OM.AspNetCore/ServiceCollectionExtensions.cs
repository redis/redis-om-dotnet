using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Redis.OM;
using Microsoft.Extensions.DependencyInjection;
using Redis.OM.Contracts;

namespace Redis.OM.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services,
            string connectionString)
        {
            var provider = new RedisConnectionProvider(connectionString);
            return services.AddSingleton<IRedisConnectionProvider>(provider);
        }

        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["REDIS_CONNECTION_STRING"];
            return services.AddSingleton<IRedisConnectionProvider>(new RedisConnectionProvider(connectionString));
        }
    }
}
