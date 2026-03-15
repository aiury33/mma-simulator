using Microsoft.Extensions.DependencyInjection;
using MmaSimulator.Console.Flows;
using MmaSimulator.Console.UI;

namespace MmaSimulator.Console.DependencyInjection;

public static class ConsoleServiceExtensions
{
    /// <summary>
    /// Registers all console flows and views required by the interactive application.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddConsoleServices(this IServiceCollection services)
    {
        services.AddTransient<FighterSelectionView>();
        services.AddTransient<FighterStatsView>();
        services.AddTransient<FightSimulationView>();
        services.AddTransient<FightResultView>();
        services.AddTransient<FightFlow>();
        services.AddTransient<MainMenuFlow>();

        return services;
    }
}
