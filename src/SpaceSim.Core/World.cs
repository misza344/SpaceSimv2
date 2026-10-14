using System.Numerics;

namespace SpaceSim.Core;

public sealed class Station(string id, string name, Vector2 position, bool isMine, int ore, decimal credits)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public Vector2 Position { get; } = position;
    public bool IsMine { get; } = isMine;
    public int Ore { get; internal set; } = ore;
    public int Metal { get; internal set; }
    public decimal Credits { get; internal set; } = credits;
    public const int Capacity = 200;
    public int Price => IsMine ? 8 + (Capacity - Ore) / 40 : 24 - Ore / 25;
}

public sealed class Ship(string id, Vector2 position, decimal credits)
{
    public string Id { get; } = id;
    public Vector2 Position { get; internal set; } = position;
    public Vector2 Velocity { get; internal set; }
    public float Heading { get; internal set; } = -MathF.PI / 2;
    public decimal Credits { get; internal set; } = credits;
    public int Ore { get; internal set; }
    public const int Capacity = 20;
    public string? DockedAt { get; internal set; }
    public string Status { get; internal set; } = "Gotowy";
}

public readonly record struct PilotInput(float Turn, float Thrust, bool Brake);
public readonly record struct TradeResult(bool Success, string Message, int Quantity = 0);

/// <summary>Authoritative world. No Godot types, rendering or wall-clock dependencies.</summary>
public sealed class World
{
    public const float StepSeconds = 1f / 60f;
    public Station Mine { get; } = new("mine", "KEPLER / Kopalnia", new(-280, -55), true, 90, 5000);
    public Station Refinery { get; } = new("refinery", "HELIOS / Rafineria", new(280, 55), false, 0, 50000);
    public Ship Player { get; } = new("player", new(-280, 20), 1000);
    public Ship Trader { get; } = new("trader", new(-200, -55), 1200);
    public long Tick { get; private set; }
    public int DeliveredOre { get; private set; }
    public int ProducedOre { get; private set; }
    public int ConsumedOre { get; private set; }
    public string LastEvent { get; private set; } = "Podejdź do kopalni, zadokuj [E] i kup rudę [B].";
    private string _traderTarget = "mine";
    private int _traderWait;
    public Station? DockedStation(Ship ship) => ship.DockedAt switch { "mine" => Mine, "refinery" => Refinery, _ => null };
    public Station NearestStation(Ship ship) => Vector2.DistanceSquared(ship.Position, Mine.Position) < Vector2.DistanceSquared(ship.Position, Refinery.Position) ? Mine : Refinery;

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

    private void MovePlayer(PilotInput input)
    {
        if (Player.DockedAt is not null) return;
        Player.Heading += Math.Clamp(input.Turn, -1, 1) * 2.7f * StepSeconds;
        var facing = new Vector2(MathF.Cos(Player.Heading), MathF.Sin(Player.Heading));
        Player.Velocity += facing * Math.Clamp(input.Thrust, -0.45f, 1) * 140 * StepSeconds;
        Player.Velocity *= input.Brake ? 0.94f : 0.998f;
        if (Player.Velocity.LengthSquared() > 220 * 220) Player.Velocity = Vector2.Normalize(Player.Velocity) * 220;
        Player.Position += Player.Velocity * StepSeconds;
        // Prototype sector boundary; no invisible wrap or teleport to another sector.
        var clamped = Vector2.Clamp(Player.Position, new(-435, -250), new(435, 250));
        if (clamped != Player.Position) Player.Velocity = Vector2.Zero;
        Player.Position = clamped;
    }

    public string ToggleDock()
    {
        if (Player.DockedAt is not null)
        {
            Player.DockedAt = null;
            return LastEvent = "Odłączono dok. Możesz lecieć dalej.";
        }
        var station = NearestStation(Player);
        if (Vector2.Distance(Player.Position, station.Position) > 85) return LastEvent = "Podejdź na mniej niż 85 m od stacji.";
        if (Player.Velocity.Length() > 35) return LastEvent = "Za duża prędkość. Przytrzymaj spację, aby wyhamować.";
        Player.DockedAt = station.Id;
        Player.Position = station.Position + new Vector2(0, 65);
        Player.Velocity = Vector2.Zero;
        return LastEvent = $"Zadokowano: {station.Name}.";
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
        return new(true, $"Sprzedano {count} rudy za {count * price} kr.", count);
    }

    public void PlayerBuy() => LastEvent = BuyOre(Player, 10).Message;
    public void PlayerSell() => LastEvent = SellOre(Player, 10).Message;
    private static TradeResult Fail(string reason) => new(false, reason);

    private void UpdateTrader()
    {
        if (_traderWait > 0) { _traderWait--; return; }
        var target = _traderTarget == "mine" ? Mine : Refinery;
        var delta = target.Position - Trader.Position;
        if (delta.Length() > 4)
        {
            Trader.DockedAt = null;
            Trader.Velocity = Vector2.Normalize(delta) * 70;
            Trader.Heading = MathF.Atan2(delta.Y, delta.X);
            Trader.Position += Trader.Velocity * StepSeconds;
            Trader.Status = $"Lot → {target.Name.Split('/')[0].Trim()}";
            return;
        }
        Trader.Position = target.Position;
        Trader.Velocity = Vector2.Zero;
        Trader.DockedAt = target.Id;
        var trade = target.IsMine ? BuyOre(Trader, Ship.Capacity) : SellOre(Trader, Ship.Capacity);
        Trader.Status = trade.Message;
        _traderWait = 120;
        if (trade.Success) _traderTarget = Trader.Ore > 0 ? "refinery" : "mine";
    }
}
