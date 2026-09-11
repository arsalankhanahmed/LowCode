using System.Windows;
using LowCode.Platform.Core.Interfaces;
using LowCode.Platform.Core.Services;
using LowCode.Platform.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LowCode.Platform.UI;

/// <summary>
/// Application entry point and DI configuration
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        // Core Services
        services.AddSingleton<IDynamicSqlGenerator, DynamicSqlGenerator>();
        services.AddSingleton<IBusinessRuleEngine, BusinessRuleEngine>();

        // Repositories
        services.AddTransient<IFormRepository, FormRepository>();
        services.AddTransient<IDynamicDataRepository, DynamicDataRepository>();
        services.AddTransient<ISchemaRepository, SchemaRepository>();
        services.AddTransient<IWorkflowRepository, WorkflowRepository>();
        services.AddTransient<ISecurityRepository, SecurityRepository>();
        services.AddTransient<IAuditRepository, AuditRepository>();

        // Unit of Work
        services.AddTransient<IUnitOfWork, UnitOfWork>();

        // ViewModels would be registered here
        
        return services.BuildServiceProvider();
    }
}
