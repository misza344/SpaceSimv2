using System.Numerics;
namespace SpaceSim.Core;

/// <summary>Three persistent sector states; inactive sectors are suspended.</summary>
public sealed class Galaxy
{
    private readonly World[] _worlds = [new(0),new(1),new(2)];
    public static readonly string[] Names = ["PAS KEPLERA", "VESPER — BŁĘKITNA MGŁA", "CINDER — RUBIEŻ PRZEMYSŁOWA"];
    public int Sector { get; private set; }
    public int NextSector => (Sector+1)%_worlds.Length;
    public World Active => _worlds[Sector];
    public string Name => Names[Sector];
    public bool Travel()
    {
        var from=Active;
        if (from.Player.DockedAt is not null || Vector3.Distance(from.Player.Position,World.GatePosition)>90 || from.Player.Velocity.Length()>25)
        {
            from.Notify("Podejdź do bramy na 90 m i wyhamuj poniżej 25 m/s. K — autopilot do bramy.");
            return false;
        }
        if(from.Autopilot) from.ToggleAutopilot();
        var credits=from.Player.Credits; var ore=from.Player.Ore;
        from.Player.Velocity=Vector3.Zero;
        Sector=NextSector;
        var to=Active;
        to.Player.Credits=credits;to.Player.Ore=ore;
        to.PlayerDeliveredOre=from.PlayerDeliveredOre;
        if(to.FlightAssist!=from.FlightAssist) to.ToggleFlightAssist();
        to.Player.Position=World.GatePosition+new Vector3(0,0,-120);
        to.Player.Velocity=Vector3.Zero;to.Player.Heading=0;to.Player.Pitch=0;to.Player.DockedAt=null;
        to.SelectTarget(true);
        to.Notify($"Witamy: {Name}. T — cel, G — stacja. K — następna brama.");
        return true;
    }
}
