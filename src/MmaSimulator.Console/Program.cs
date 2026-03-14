using Microsoft.Extensions.DependencyInjection;
using MmaSimulator.Console.DependencyInjection;
using MmaSimulator.Console.Flows;
using MmaSimulator.Simulation.DependencyInjection;

var services = new ServiceCollection()
    .AddSimulationServices()
    .AddConsoleServices()
    .BuildServiceProvider();

services.GetRequiredService<MainMenuFlow>().Run();
