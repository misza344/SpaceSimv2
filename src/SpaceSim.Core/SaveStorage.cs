using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceSim.Core;

public sealed record ShipSave([property: JsonRequired] Vector3 Position, [property: JsonRequired] Vector3 Velocity, [property: JsonRequired] float Heading, [property: JsonRequired] float Pitch,
    [property: JsonRequired] decimal Credits, [property: JsonRequired] int Ore, [property: JsonRequired] string? DockedAt, [property: JsonRequired] string Status)
{
    internal static ShipSave Capture(Ship ship) => new(ship.Position, ship.Velocity, ship.Heading,
        ship.Pitch, ship.Credits, ship.Ore, ship.DockedAt, ship.Status);
    internal void Restore(Ship ship)
    {
        ship.Position=Position; ship.Velocity=Velocity; ship.Heading=Heading; ship.Pitch=Pitch;
        ship.Credits=Credits; ship.Ore=Ore; ship.DockedAt=DockedAt; ship.Status=Status;
    }
}
public sealed record StationSave([property: JsonRequired] int Ore, [property: JsonRequired] int Metal, [property: JsonRequired] decimal Credits)
{
    internal static StationSave Capture(Station station) => new(station.Ore, station.Metal, station.Credits);
    internal void Restore(Station station) { station.Ore=Ore; station.Metal=Metal; station.Credits=Credits; }
}
public sealed class WorldSave
{
    public required long Tick { get; init; }
    public required int DeliveredOre { get; init; }
    public required int ProducedOre { get; init; }
    public required int ConsumedOre { get; init; }
    public required int PlayerDeliveredOre { get; init; }
    public required int CollisionCount { get; init; }
    public required bool Autopilot { get; init; }
    public required bool FlightAssist { get; init; }
    public required bool NavigatingToGate { get; init; }
    public required bool TargetMine { get; init; }
    public required string TraderTarget { get; init; }
    public required int TraderWait { get; init; }
    public required string LastEvent { get; init; }
    public required ShipSave Player { get; init; }
    public required ShipSave Trader { get; init; }
    public required StationSave Mine { get; init; }
    public required StationSave Refinery { get; init; }
    public required Vector3[] Waypoints { get; init; }
    public required Vector3[] TraderWaypoints { get; init; }
}
public sealed partial class World
{
    internal WorldSave Capture() => new()
    {
        Tick = Tick,
        DeliveredOre = DeliveredOre,
        ProducedOre = ProducedOre,
        ConsumedOre = ConsumedOre,
        PlayerDeliveredOre = PlayerDeliveredOre,
        CollisionCount = CollisionCount,
        Autopilot = Autopilot,
        FlightAssist = FlightAssist,
        NavigatingToGate = NavigatingToGate,
        TargetMine = _targetMine,
        TraderTarget = _traderTarget,
        TraderWait = _traderWait,
        LastEvent = LastEvent,
        Player=ShipSave.Capture(Player), Trader=ShipSave.Capture(Trader),
        Mine=StationSave.Capture(Mine), Refinery=StationSave.Capture(Refinery),
        Waypoints=_waypoints.ToArray(), TraderWaypoints=_traderWaypoints.ToArray()
    };
    internal void Restore(WorldSave save)
    {
        Tick = save.Tick;
        DeliveredOre = save.DeliveredOre;
        ProducedOre = save.ProducedOre;
        ConsumedOre = save.ConsumedOre;
        PlayerDeliveredOre = save.PlayerDeliveredOre;
        CollisionCount = save.CollisionCount;
        Autopilot = save.Autopilot;
        FlightAssist = save.FlightAssist;
        NavigatingToGate = save.NavigatingToGate;
        _targetMine = save.TargetMine;
        _traderTarget = save.TraderTarget;
        _traderWait = save.TraderWait;
        LastEvent = save.LastEvent;
        save.Player.Restore(Player); save.Trader.Restore(Trader);
        save.Mine.Restore(Mine); save.Refinery.Restore(Refinery);
        _waypoints.Clear(); foreach(var point in save.Waypoints) _waypoints.Enqueue(point);
        _traderWaypoints.Clear(); foreach(var point in save.TraderWaypoints) _traderWaypoints.Enqueue(point);
    }
}
public sealed partial class Galaxy
{
    public GalaxySave Capture() => new(Sector, _worlds.Select(world => world.Capture()).ToArray());
    public static Galaxy Restore(GalaxySave save)
    {
        SaveStorage.ValidateGalaxy(save);
        var galaxy = new Galaxy { Sector=save.Sector };
        for(int i=0;i<galaxy._worlds.Length;i++) galaxy._worlds[i].Restore(save.Worlds[i]);
        return galaxy;
    }
}
public sealed record GalaxySave([property: JsonRequired] int Sector, [property: JsonRequired] WorldSave[] Worlds);
public sealed record PilotView([property: JsonRequired] bool Walking, [property: JsonRequired] bool Cockpit, [property: JsonRequired] bool MouseFlight,
    [property: JsonRequired] Vector3 Position, [property: JsonRequired] float Yaw, [property: JsonRequired] float Pitch);
public sealed class GameSave
{
    public required int Version { get; init; }
    public required GalaxySave Galaxy { get; init; }
    public required PilotView View { get; init; }
}

/// <summary>Versioned snapshot, validated before use. File replacement happens only after a flushed write.</summary>
public static class SaveStorage
{
    public const int CurrentVersion=1;
    private const int MaxBytes=1024*1024;
    private static readonly JsonSerializerOptions Options = new() { IncludeFields=true, WriteIndented=true };
    public static string Encode(GameSave save) { Validate(save); return JsonSerializer.Serialize(save, Options); }
    public static GameSave Decode(string json)
    {
        if(json.Length>MaxBytes) throw new InvalidDataException("Zapis jest zbyt duży.");
        GameSave save;
        try { save=JsonSerializer.Deserialize<GameSave>(json,Options) ?? throw new InvalidDataException("Pusty zapis."); }
        catch(JsonException ex) { throw new InvalidDataException("Uszkodzony format zapisu.",ex); }
        Validate(save);
        return save;
    }
    public static void Write(string path, GameSave save)
    {
        string json=Encode(save);
        string full=Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        string temporary=full+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                var bytes=System.Text.Encoding.UTF8.GetBytes(json);
                stream.Write(bytes); stream.Flush(true);
            }
            if(File.Exists(full)) File.Copy(full,full+".bak",true);
            File.Move(temporary,full,true);
        }
        finally { if(File.Exists(temporary)) File.Delete(temporary); }
    }
    public static GameSave Read(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length>MaxBytes) throw new InvalidDataException("Zapis jest zbyt duży.");
        using var reader=new StreamReader(stream);
        return Decode(reader.ReadToEnd());
    }
    private static void Require(bool condition) { if(!condition) throw new InvalidDataException("Zapis zawiera nieprawidłowy stan gry."); }
    private static bool Finite(Vector3 v) => float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    private static bool Point(Vector3 v) => Finite(v)&&v.LengthSquared()<100000000;
    private static bool Dock(string? value) => value is null or "mine" or "refinery";
    private static void ValidateShip(ShipSave ship)
    {
        Require(ship is not null);
        Require(Point(ship!.Position)&&Finite(ship.Velocity)&&ship.Velocity.LengthSquared()<=World.BoostSpeed*World.BoostSpeed+1);
        Require(float.IsFinite(ship.Heading)&&float.IsFinite(ship.Pitch)&&MathF.Abs(ship.Pitch)<=1.6f);
        Require(ship.Credits>=0&&ship.Ore>=0&&ship.Ore<=Ship.Capacity&&Dock(ship.DockedAt)&&ship.Status is {Length:<=2048});
        Require(ship.DockedAt is null || ship.Velocity==Vector3.Zero);
    }
    internal static void ValidateGalaxy(GalaxySave galaxy)
    {
        Require(galaxy is not null && galaxy.Worlds is {Length:3} && galaxy.Sector is >=0 and <3);
        foreach(var world in galaxy!.Worlds)
        {
            Require(world is not null);
            Require(world!.Tick>=0&&world.DeliveredOre>=0&&world.ProducedOre>=0&&world.ConsumedOre>=0&&world.PlayerDeliveredOre>=0&&world.CollisionCount>=0);
            Require(world.TraderTarget is "mine" or "refinery" && world.TraderWait is >=0 and <=120);
            Require(world.LastEvent is {Length:<=2048});
            ValidateShip(world.Player); ValidateShip(world.Trader);
            foreach(var station in new[]{world.Mine,world.Refinery})
                Require(station is not null && station.Ore is >=0 and <=Station.Capacity && station.Metal is >=0 and <=Station.Capacity && station.Credits>=0);
            foreach(var route in new[]{world.Waypoints,world.TraderWaypoints})
                Require(route is {Length:<=64} && route.All(Point));
            Require(!world.Autopilot || (world.Player.DockedAt is null && world.Waypoints.Length>0));
            Require(!world.NavigatingToGate || world.Autopilot);
        }
    }
    private static void Validate(GameSave save)
    {
        if(save.Version!=CurrentVersion) throw new InvalidDataException($"Nieobsługiwana wersja zapisu: {save.Version}.");
        ValidateGalaxy(save.Galaxy);
        var view=save.View;
        Require(view is not null && Point(view.Position)&&float.IsFinite(view.Yaw)&&float.IsFinite(view.Pitch)&&MathF.Abs(view.Pitch)<=1.25f);
        Require(view!.Position.X is >=-1.25f and <=1.25f && view.Position.Z is >=-1.7f and <=2.5f && MathF.Abs(view.Position.Y-.65f)<.001f);
        if(view.Walking)
        {
            var world=save.Galaxy.Worlds[save.Galaxy.Sector];
            Require(!world.Autopilot && world.Player.Velocity.Length()<=.5f);
        }
    }
}
