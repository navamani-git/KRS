using Microsoft.Extensions.DependencyInjection;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;
using KRSDealerManagement.Infrastructure.Repositories;

namespace KRSDealerManagement.Infrastructure
{
    /// <summary>
    /// Extension method for registering Infrastructure layer services
    /// Call this from Program.cs: services.AddInfrastructureServices(connectionString)
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
        {
            // Per-request context so transaction state is not shared across HTTP requests.
            services.AddScoped(_ => new ApplicationDbContext(connectionString));

            // Register Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
