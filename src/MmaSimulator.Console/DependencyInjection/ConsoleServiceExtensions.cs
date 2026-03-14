using Microsoft.Extensions.DependencyInjection;
using MmaSimulator.Console.Flows;
using MmaSimulator.Console.UI;

namespace MmaSimulator.Console.DependencyInjection;

public static class ConsoleServiceExtensions
{
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
