using FluentValidation;
using MediatR;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using KRSDealerManagement.Application.Behaviors;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.Mappings;
using System.Reflection;

namespace KRSDealerManagement.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register MediatR with validation pipeline
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Register FluentValidation validators
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register AutoMapper - AutoMapper 16+ requires ILoggerFactory as second argument
            services.AddSingleton<IMapper>(sp =>
            {
                var loggerFactory = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()
                                    ?? NullLoggerFactory.Instance;

                var config = new MapperConfiguration(
                    cfg => cfg.AddProfile<MappingProfile>(),
                    loggerFactory
                );

                return config.CreateMapper();
            });

            // Register Application Services
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IStatusLookupService, StatusLookupService>();
            services.AddScoped<IVehiclePriceService, VehiclePriceService>();
            services.AddScoped<ICommissionRateService, CommissionRateService>();

            return services;
        }
    }
}
