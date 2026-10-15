using System.Numerics;

namespace SpaceSim.Core;

public sealed class Station(string id, string name, Vector3 position, bool isMine, int ore, decimal credits)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public Vector3 Position { get; } = position;
    public Vector3 DockPosition => Position + new Vector3(0, 0, 100);
    public bool IsMine { get; } = isMine;
    public int Ore { get; internal set; } = ore;
    public int Metal { get; internal set; }
    public decimal Credits { get; internal set; } = credits;
    public const int Capacity = 200;
    public int Price => IsMine ? 8 + (Capacity - Ore) / 40 : 24 - Ore / 25;
}

public sealed class Ship(string id, Vector3 position, decimal credits)
{
    public string Id { get; } = id;
    public Vector3 Position { get; internal set; } = position;
    public Vector3 Velocity { get; internal set; }
    public float Heading { get; internal set; }
    public float Pitch { get; internal set; }
    public Vector3 Forward => new(MathF.Sin(Heading) * MathF.Cos(Pitch), MathF.Sin(Pitch), -MathF.Cos(Heading) * MathF.Cos(Pitch));
    public Vector3 Right => new(MathF.Cos(Heading), 0, MathF.Sin(Heading));
    public decimal Credits { get; internal set; } = credits;
    public int Ore { get; internal set; }
    public const int Capacity = 20;
    public string? DockedAt { get; internal set; }
    public string Status { get; internal set; } = "Gotowy";
}

public readonly record struct PilotInput(float Turn, float Thrust, bool Brake, float Pitch = 0, float Strafe = 0, float Lift = 0, bool Boost = false)
{
    public bool HasManualControl => Brake || MathF.Abs(Turn) + MathF.Abs(Thrust) + MathF.Abs(Pitch) + MathF.Abs(Strafe) + MathF.Abs(Lift) > 0.01f;
}
public readonly record struct CollisionBody(Vector3 Position, float Radius, bool IsAsteroid);
public readonly record struct TradeResult(bool Success, string Message, int Quantity = 0);

/// <summary>Authoritative world. No Godot types, rendering or wall-clock dependencies.</summary>
public sealed class World
{
    public const float StepSeconds = 1f / 60f;
    public Station Mine { get; } = new("mine", "KEPLER / Kopalnia", new(-240, 0, -280), true, 90, 5000);
    public Station Refinery { get; } = new("refinery", "HELIOS / Rafineria", new(340, 100, -1250), false, 0, 50000);
    public Ship Player { get; } = new("player", new(-240, 6, -115), 1000);
    public Ship Trader { get; } = new("trader", new(-180, 0, -160), 1200);
    public const float DockRange = 75;
    public const float DockSpeed = 25;
    public const float CruiseSpeed = 140;
    public const float BoostSpeed = 320;
    public const float SectorRadius = 3000;
    public bool Autopilot { get; private set; }
    public Station Target => _targetMine ? Mine : Refinery;
    public int PlayerDeliveredOre { get; internal set; }
    public int CollisionCount { get; private set; }
    public IReadOnlyList<CollisionBody> CollisionBodies { get; }
    private bool _targetMine = true;
    private readonly Queue<Vector3> _waypoints = new();
    private readonly Queue<Vector3> _traderWaypoints = new();

    public bool FlightAssist { get; private set; } = true;
    public bool NavigatingToGate { get; private set; }
    public static readonly Vector3 GatePosition = new(180, 100, 650);
    public void ToggleFlightAssist() { FlightAssist=!FlightAssist; LastEvent=FlightAssist?"Wspomaganie lotu włączone — silniki wyhamowują dryf.":"Wspomaganie wyłączone — lot bezwładnościowy."; }
    public void Notify(string message) => LastEvent=message;
    public void NavigateToGate()
    {
        Player.DockedAt=null; PlanRoute(_waypoints,Player.Position,GatePosition);
        NavigatingToGate=true; Autopilot=true;
        LastEvent="Autopilot do bramy. Po dotarciu J — przejdź do następnego sektora.";
    }

    public World(int sector = 0)
    {
        if (sector == 1)
        {
            Mine=new("mine","VESPER / Złoża lodowe",new(-380,-60,-500),true,150,7000);
            Refinery=new("refinery","AURORA / Przetwórnia",new(540,160,-1500),false,40,42000);
        }
        else if (sector == 2)
        {
            Mine=new("mine","CINDER / Wydobycie",new(-160,140,-650),true,45,12000);
            Refinery=new("refinery","FORGE / Huta",new(650,-80,-1100),false,110,60000);
        }
        var bodies = new List<CollisionBody>
        {
            new(Mine.Position, 63, false), new(Refinery.Position, 70, false),
            new(Mine.Position + new Vector3(-105, 0, 0), 28, false),
            new(Mine.Position + new Vector3(105, 0, 0), 28, false)
        };
        var random = new Random(344 + sector * 713);
        for (int i = 0; i < 42; i++)
            bodies.Add(new(new Vector3(-490 - random.Next(0, 800), random.Next(-150, 170), -150 - random.Next(0, 1500)), random.Next(12, 42), true));
        CollisionBodies = bodies.AsReadOnly();
    }

    public long Tick { get; private set; }
    public int DeliveredOre { get; private set; }
    public int ProducedOre { get; private set; }
    public int ConsumedOre { get; private set; }
    public string LastEvent { get; private set; } = "Pierwszy kurs: G — autopilot do kopalni, następnie B — kup rudę.";
    private string _traderTarget = "mine";
    private int _traderWait;
    public Station? DockedStation(Ship ship) => ship.DockedAt switch { "mine" => Mine, "refinery" => Refinery, _ => null };
    public Station NearestStation(Ship ship) => Vector3.DistanceSquared(ship.Position, Mine.DockPosition) < Vector3.DistanceSquared(ship.Position, Refinery.DockPosition) ? Mine : Refinery;

    public void Step(PilotInput input = default)
    {
        Tick++;
        MovePlayer(input);
        // Staggered economy updates; one tick always represents the same amount of simulation time.
        if (Tick % 120 == 0 && Mine.Ore < Station.Capacity) { Mine.Ore++; ProducedOre++; }
        if (Tick % 180 == 0 && Refinery.Ore >= 2 && Refinery.Metal < Station.Capacity)
        {
            Refinery.Ore -= 2;
            Refinery.Metal++;
            ConsumedOre += 2;
        }
        UpdateTrader();
    }

    public void SelectTarget(bool mine)
    {
        _targetMine = mine;
        NavigatingToGate=false;
        Autopilot = false;
        LastEvent = $"Wybrano cel: {Target.Name}. G — rozpocznij lot automatyczny.";
    }

    public void CycleTarget() => SelectTarget(!_targetMine);

    public void ToggleAutopilot()
    {
        NavigatingToGate=false;
        if (Autopilot) { Autopilot = false; LastEvent = "Autopilot wyłączony. Sterowanie ręczne."; return; }
        Player.DockedAt = null;
        PlanRoute(_waypoints, Player.Position, Target.DockPosition);
        Autopilot = true;
        LastEvent = $"Autopilot → {Target.Name}. Sterowanie ręczne lub G przerywa lot.";
    }

    // Departure corridors avoid station hulls. Cruise above the fixed asteroid belt;
    // this is a deliberately small-sector route planner, not general galaxy navigation.
    private void PlanRoute(Queue<Vector3> route, Vector3 start, Vector3 dock)
    {
        route.Clear();
        if (Vector3.Distance(start, dock) > 150 || !ClearSegment(start, dock))
        {
            float safeHeight = MathF.Max(350, start.Y + 100);
            // Escape locally if a manually positioned ship is underneath a hull.
            var escape = start;
            for (int i = 0; i < 30 && !ClearSegment(escape, new(escape.X, safeHeight, escape.Z)); i++)
                escape += new Vector3(0, 0, 30);
            if (escape != start) route.Enqueue(escape);
            route.Enqueue(new(escape.X, safeHeight, escape.Z));
            route.Enqueue(new(dock.X, safeHeight, dock.Z + 120));
            route.Enqueue(dock + new Vector3(0, 0, 120));
        }
        route.Enqueue(dock);
    }

    private bool ClearSegment(Vector3 from, Vector3 to) => !CollisionBodies.Any(body => SegmentHit(from, to, body, out _));

    private void MovePlayer(PilotInput input)
    {
        if (Player.DockedAt is not null) return;
        if (Autopilot && input.HasManualControl)
        {
            Autopilot = false;
            NavigatingToGate=false;
            LastEvent = "Przejęto sterowanie ręczne.";
        }
        Vector3 before = Player.Position;
        if (Autopilot)
        {
            if (FollowRoute(Player, _waypoints, 120))
            {
                Autopilot = false;
                if (NavigatingToGate) { NavigatingToGate=false; LastEvent="Brama gotowa. J — przejście do kolejnego sektora."; }
                else ToggleDock();
                return;
            }
        }
        else
        {
            Player.Heading = MathF.IEEERemainder(Player.Heading + Math.Clamp(input.Turn, -1, 1) * 1.35f * StepSeconds, MathF.Tau);
            Player.Pitch = Math.Clamp(Player.Pitch + Math.Clamp(input.Pitch, -1, 1) * StepSeconds, -1.45f, 1.45f);
            var desired = Player.Forward * Math.Clamp(input.Thrust, -0.5f, 1) + Player.Right * Math.Clamp(input.Strafe, -1, 1) * 0.7f + Vector3.UnitY * Math.Clamp(input.Lift, -1, 1) * 0.7f;
            if (desired.LengthSquared() > 1) desired = Vector3.Normalize(desired);
            Player.Velocity += desired * (input.Boost ? 130 : 75) * StepSeconds;
            if (input.Brake) Player.Velocity *= .91f;
            else if (FlightAssist)
            {
                // Damp perpendicular drift while thrust is applied; releasing thrust brakes smoothly.
                var longitudinal=Player.Forward*Vector3.Dot(Player.Velocity,Player.Forward);
                Player.Velocity = longitudinal + (Player.Velocity-longitudinal)*.96f;
                if (MathF.Abs(input.Thrust)<.01f && MathF.Abs(input.Strafe)<.01f && MathF.Abs(input.Lift)<.01f)
                    Player.Velocity *= .975f;
            }
            else Player.Velocity *= .9995f;
            float limit = input.Boost ? BoostSpeed : CruiseSpeed;
            if (Player.Velocity.LengthSquared() > limit * limit)
                Player.Velocity = Vector3.Normalize(Player.Velocity) * MathF.Max(limit, Player.Velocity.Length() - 100 * StepSeconds);
            Player.Position += Player.Velocity * StepSeconds;
        }
        ResolvePlayerCollision(before);
        if (Player.Position.LengthSquared() > SectorRadius * SectorRadius)
        {
            Player.Position = Vector3.Normalize(Player.Position) * SectorRadius;
            Player.Velocity = Vector3.Zero;
            Autopilot = false;
            LastEvent = "Granica sektora. Zawróć lub wybierz stację i naciśnij G.";
        }
    }

    private static bool SegmentHit(Vector3 from, Vector3 to, CollisionBody body, out float time)
    {
        var offset = from - body.Position;
        var delta = to - from;
        float radius = body.Radius + 5;
        float c = offset.LengthSquared() - radius * radius;
        time = 0;
        if (c < 0) return true;
        float a = delta.LengthSquared();
        if (a < 0.000001f) return false;
        float b = Vector3.Dot(offset, delta);
        float discriminant = b * b - a * c;
        if (discriminant < 0 || b >= 0) return false;
        time = (-b - MathF.Sqrt(discriminant)) / a;
        return time >= 0 && time <= 1;
    }

    private void ResolvePlayerCollision(Vector3 before)
    {
        CollisionBody? nearest = null;
        float first = 2;
        foreach (var body in CollisionBodies)
            if (SegmentHit(before, Player.Position, body, out float t) && t < first) { first = t; nearest = body; }
        if (nearest is not { } hit) return;
        Vector3 at = Vector3.Lerp(before, Player.Position, first);
        var normal = at - hit.Position;
        normal = normal.LengthSquared() < 0.001f ? Vector3.UnitZ : Vector3.Normalize(normal);
        Player.Position = hit.Position + normal * (hit.Radius + 5.1f);
        Player.Velocity = Vector3.Reflect(Player.Velocity, normal) * 0.15f;
        CollisionCount++;
        NavigatingToGate=false;
        Autopilot = false;
        LastEvent = "Kontakt z obiektem — wyhamowano. Dokuj przy świecącym znaczniku portu.";
    }

    private static bool FollowRoute(Ship ship, Queue<Vector3> route, float maxSpeed)
    {
        if (route.Count == 0) return true;
        var delta = route.Peek() - ship.Position;
        float distance = delta.Length();
        if (distance < 2 && ship.Velocity.Length() < 5)
        {
            ship.Position = route.Dequeue();
            ship.Velocity = Vector3.Zero;
            return route.Count == 0;
        }
        if (distance > 0.001f)
        {
            var direction = delta / distance;
            if (direction.X * direction.X + direction.Z * direction.Z > .0001f)
            {
                float desiredHeading = MathF.Atan2(direction.X, -direction.Z);
                ship.Heading += MathF.IEEERemainder(desiredHeading - ship.Heading, MathF.Tau) * 0.06f;
            }
            ship.Pitch += (MathF.Asin(Math.Clamp(direction.Y, -1, 1)) - ship.Pitch) * 0.06f;
            var desired = direction * MathF.Min(maxSpeed, MathF.Sqrt(2 * 28 * distance));
            var change = desired - ship.Velocity;
            float maxChange = 75 * StepSeconds;
            ship.Velocity += change.Length() > maxChange ? Vector3.Normalize(change) * maxChange : change;
        }
        ship.Position += ship.Velocity * StepSeconds;
        return false;
    }

    public string ToggleDock()
    {
        NavigatingToGate=false;
        Autopilot = false;
        if (Player.DockedAt is not null)
        {
            Player.DockedAt = null;
            return LastEvent = "Odłączono dok. T — zmień cel, G — autopilot.";
        }
        var station = NearestStation(Player);
        if (Vector3.Distance(Player.Position, station.DockPosition) > DockRange) return LastEvent = "Podejdź do znacznika portu na mniej niż 75 m.";
        if (Player.Velocity.Length() > DockSpeed) return LastEvent = "Za duża prędkość. Przytrzymaj spację, aby wyhamować.";
        Player.DockedAt = station.Id;
        Player.Position = station.DockPosition;
        Player.Heading = 0;
        Player.Pitch = 0;
        Player.Velocity = Vector3.Zero;
        return LastEvent = $"Zadokowano: {station.Name}. {(station.IsMine ? "B — kup rudę." : "V — sprzedaj rudę.")}";
    }

    public TradeResult BuyOre(Ship ship, int requested)
    {
        var station = DockedStation(ship);
        if (station is null) return Fail("Najpierw zadokuj.");
        if (!station.IsMine) return Fail("Rudę kupisz w kopalni.");
        if (requested <= 0) return Fail("Ilość musi być dodatnia.");
        int price = station.Price;
        int count = Math.Min(Math.Min(requested, Ship.Capacity - ship.Ore), Math.Min(station.Ore, (int)Math.Min(Ship.Capacity, ship.Credits / price)));
        if (count <= 0) return Fail("Zakup wstrzymany: brak rudy, kredytów lub miejsca w ładowni.");
        station.Ore -= count;
        ship.Ore += count;
        ship.Credits -= count * price;
        station.Credits += count * price;
        return new(true, $"Kupiono {count} rudy za {count * price} kr.", count);
    }

    public TradeResult SellOre(Ship ship, int requested)
    {
        var station = DockedStation(ship);
        if (station is null) return Fail("Najpierw zadokuj.");
        if (station.IsMine) return Fail("Rudę sprzedasz w rafinerii.");
        if (requested <= 0) return Fail("Ilość musi być dodatnia.");
        int price = station.Price;
        int count = Math.Min(Math.Min(requested, ship.Ore), Math.Min(Station.Capacity - station.Ore, (int)Math.Min(Ship.Capacity, station.Credits / price)));
        if (count <= 0) return Fail("Sprzedaż wstrzymana: pusty ładunek, pełny magazyn lub brak budżetu stacji.");
        ship.Ore -= count;
        station.Ore += count;
        ship.Credits += count * price;
        station.Credits -= count * price;
        DeliveredOre += count;
        if (ship == Player) PlayerDeliveredOre += count;
        return new(true, $"Sprzedano {count} rudy za {count * price} kr.", count);
    }

    public void PlayerBuy()
    {
        var result = BuyOre(Player, 10);
        if (result.Success) _targetMine = false;
        LastEvent = result.Message + (result.Success ? " G — lot do rafinerii." : "");
    }
    public void PlayerSell()
    {
        var result = SellOre(Player, 10);
        if (result.Success && Player.Ore == 0) _targetMine = true;
        LastEvent = result.Message + (result.Success ? " Pierwszy obieg gospodarczy działa!" : "");
    }
    private static TradeResult Fail(string reason) => new(false, reason);

    private void UpdateTrader()
    {
        if (_traderWait > 0) { _traderWait--; return; }
        var target = _traderTarget == "mine" ? Mine : Refinery;
        if (_traderWaypoints.Count == 0 && Trader.DockedAt != target.Id)
            PlanRoute(_traderWaypoints, Trader.Position, target.DockPosition + new Vector3(24, 0, 0));
        if (_traderWaypoints.Count > 0)
        {
            Trader.DockedAt = null;
            Trader.Status = $"Lot → {target.Name.Split('/')[0].Trim()}";
            if (!FollowRoute(Trader, _traderWaypoints, 95)) return;
        }
        Trader.Velocity = Vector3.Zero;
        Trader.DockedAt = target.Id;
        var trade = target.IsMine ? BuyOre(Trader, Ship.Capacity) : SellOre(Trader, Ship.Capacity);
        Trader.Status = trade.Message;
        _traderWait = 120;
        if (trade.Success) _traderTarget = Trader.Ore > 0 ? "refinery" : "mine";
    }
}
