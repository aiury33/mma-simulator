namespace MmaSimulator.Core.Enums;

public enum WeightClass
{
    // Men's divisions
    Flyweight          = 125,
    Bantamweight       = 135,
    Featherweight      = 145,
    Lightweight        = 155,
    Welterweight       = 170,
    Middleweight       = 185,
    LightHeavyweight   = 205,
    Heavyweight        = 265,

    // Women's divisions (offset values to avoid numeric collision)
    WomensStrawweight  = 116,
    WomensFlyweight    = 124
}
