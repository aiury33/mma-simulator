using Microsoft.Extensions.DependencyInjection;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Simulation.Data;
using MmaSimulator.Simulation.Engines;
using MmaSimulator.Simulation.Narration;
using MmaSimulator.Simulation.Providers;
using MmaSimulator.Simulation.Simulators;

namespace MmaSimulator.Simulation.DependencyInjection;

public static class SimulationServiceExtensions
{
    public static IServiceCollection AddSimulationServices(this IServiceCollection services, int? randomSeed = null)
    {
        if (randomSeed.HasValue)
            services.AddSingleton<IRandomProvider>(new SeededRandomProvider(randomSeed.Value));
        else
            services.AddSingleton<IRandomProvider, RandomProvider>();

        services.AddSingleton<NarrationBuilder>();
        services.AddSingleton<IStrikingEngine>(sp =>
            new StrikingEngine(sp.GetRequiredService<IRandomProvider>(), sp.GetRequiredService<NarrationBuilder>()));
        services.AddSingleton<IGrapplingEngine, GrapplingEngine>();
        services.AddSingleton<IStaminaEngine, StaminaEngine>();
        services.AddSingleton<IJudgeScoringEngine, JudgeScoringEngine>();
        services.AddSingleton<IRoundSimulator, RoundSimulator>();
        services.AddSingleton<IFightSimulator, FightSimulator>();
        services.AddSingleton<IFighterRepository, FighterRepository>();

        return services;
    }
}
