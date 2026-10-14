using System.Numerics;
using SpaceSim.Core;
using Xunit;

namespace SpaceSim.Core.Tests;

public class WorldTests
{
    [Fact]
    public void UndockedTradeCannotChangeInventoryOrMoney()
    {
        var w = new World();
        Assert.False(w.BuyOre(w.Player, 10).Success);
        Assert.Equal(90, w.Mine.Ore);
        Assert.Equal(1000m, w.Player.Credits);
    }

    [Fact]
    public void TradeTransfersRealCargoAndConservesMoney()
    {
        var w = new World();
        var initialMoney = w.Player.Credits + w.Mine.Credits + w.Refinery.Credits;
        w.ToggleDock();
        Assert.True(w.BuyOre(w.Player, 10).Success);
        Assert.Equal(80, w.Mine.Ore);
        Assert.Equal(10, w.Player.Ore);
        w.ToggleDock();
        w.Player.Position = w.Refinery.DockPosition;
        w.ToggleDock();
        Assert.True(w.SellOre(w.Player, 10).Success);
        Assert.Equal(10, w.Refinery.Ore);
        Assert.Equal(0, w.Player.Ore);
        Assert.True(w.Player.Credits > 1000);
        Assert.Equal(initialMoney, w.Player.Credits + w.Mine.Credits + w.Refinery.Credits);
    }

    [Fact]
    public void CapacityFundsAndInvalidQuantitiesAreRespected()
    {
        var w = new World();
        w.ToggleDock();
        Assert.False(w.BuyOre(w.Player, -1).Success);
        w.Player.Credits = 21;
        Assert.Equal(2, w.BuyOre(w.Player, 100).Quantity);
        w.Player.Credits = 10000;
        Assert.Equal(18, w.BuyOre(w.Player, 100).Quantity);
        Assert.False(w.BuyOre(w.Player, 1).Success);
        Assert.Equal(Ship.Capacity, w.Player.Ore);
    }

    [Fact]
    public void DockRequiresProximityAndLowSpeed()
    {
        var w = new World();
        w.Player.Velocity = new Vector3(100, 0, 0);
        w.ToggleDock();
        Assert.Null(w.Player.DockedAt);
        w.Player.Velocity = Vector3.Zero;
        w.Player.Position = Vector3.Zero;
        w.ToggleDock();
        Assert.Null(w.Player.DockedAt);
        w.Player.Position = w.Mine.DockPosition;
        w.ToggleDock();
        Assert.Equal("mine", w.Player.DockedAt);
    }

    [Fact]
    public void NoSupplyStopsProductionAndDeliveryRestartsIt()
    {
        var w = new World();
        w.Trader.Credits = 0;
        for (var i = 0; i < 600; i++) w.Step();
        Assert.Equal(0, w.Refinery.Metal);
        w.ToggleDock();
        w.BuyOre(w.Player, 10);
        w.ToggleDock();
        w.Player.Position = w.Refinery.DockPosition;
        w.ToggleDock();
        w.SellOre(w.Player, 10);
        for (var i = 0; i < 600; i++) w.Step();
        Assert.True(w.Refinery.Metal > 0);
    }

    [Fact]
    public void AutonomousTraderRunsAndLongSimulationConservesResources()
    {
        var w = new World();
        var initialMoney = w.Player.Credits + w.Trader.Credits + w.Mine.Credits + w.Refinery.Credits;
        for (var i = 0; i < 60 * 60 * 60 * 2; i++)
        {
            w.Step();
            Assert.InRange(w.Mine.Ore, 0, Station.Capacity);
            Assert.InRange(w.Refinery.Ore, 0, Station.Capacity);
            Assert.InRange(w.Trader.Ore, 0, Ship.Capacity);
        }
        Assert.True(w.DeliveredOre > 20);
        Assert.True(w.Refinery.Metal > 0);
        Assert.Equal(90 + w.ProducedOre, w.Mine.Ore + w.Refinery.Ore + w.Player.Ore + w.Trader.Ore + w.ConsumedOre);
        Assert.Equal(initialMoney, w.Player.Credits + w.Trader.Credits + w.Mine.Credits + w.Refinery.Credits);
    }

    [Fact]
    public void FullRefineryKeepsCargoOnTraderAndExplainsWait()
    {
        var w = new World();
        w.Refinery.Ore = Station.Capacity;
        w.Refinery.Metal = Station.Capacity;
        for (var i = 0; i < 60 * 120; i++) w.Step();
        Assert.Equal(20, w.Trader.Ore);
        Assert.Equal(0, w.DeliveredOre);
        Assert.Contains("pełny magazyn", w.Trader.Status);
    }

    [Fact]
    public void BuyerBudgetLimitsSaleWithoutLosingUnsoldCargo()
    {
        var w = new World();
        w.ToggleDock();
        w.BuyOre(w.Player, 10);
        w.ToggleDock();
        w.Player.Position = w.Refinery.DockPosition;
        w.ToggleDock();
        w.Refinery.Credits = 25;
        Assert.Equal(1, w.SellOre(w.Player, 10).Quantity);
        Assert.Equal(9, w.Player.Ore);
        Assert.Equal(1m, w.Refinery.Credits);
        Assert.False(w.SellOre(w.Player, 10).Success);
    }

    [Fact]
    public void FixedInputProducesRepeatableSimulation()
    {
        var a = new World(); var b = new World();
        for (var i = 0; i < 1200; i++)
        {
            var input = new PilotInput(i < 100 ? 1 : 0, 1, i > 1000);
            a.Step(input); b.Step(input);
        }
        Assert.Equal(a.Player.Position, b.Player.Position);
        Assert.Equal(a.Trader.Position, b.Trader.Position);
        Assert.Equal(a.Refinery.Ore, b.Refinery.Ore);
        Assert.InRange(a.Player.Velocity.Length(), 0, 220);
    }
    [Fact]
    public void PitchAndVerticalThrustersMoveOutOfTheOldPlane()
    {
        var w = new World();
        var start = w.Player.Position;
        for (int i = 0; i < 90; i++) w.Step(new PilotInput(0, 0, false, Pitch: 1, Lift: 1));
        Assert.True(w.Player.Position.Y > start.Y + 20);
        Assert.InRange(w.Player.Pitch, 1, 1.45f);
        Assert.True(w.Player.Forward.Y > .8f);
    }

    [Fact]
    public void DockingUsesHeightAsWellAsHorizontalDistance()
    {
        var w = new World();
        w.Player.Position = w.Mine.DockPosition + Vector3.UnitY * 90;
        w.ToggleDock();
        Assert.Null(w.Player.DockedAt);
        w.Player.Position = w.Mine.DockPosition + Vector3.UnitY * 40;
        w.ToggleDock();
        Assert.Equal("mine", w.Player.DockedAt);
    }

    [Fact]
    public void BoostCannotTunnelThroughStationHull()
    {
        var w = new World();
        w.Player.Position = w.Mine.Position + Vector3.UnitZ * 72;
        w.Player.Velocity = -Vector3.UnitZ * World.BoostSpeed;
        w.Step(new PilotInput(0, 1, false, Boost: true));
        Assert.Equal(1, w.CollisionCount);
        Assert.True(Vector3.Distance(w.Player.Position, w.Mine.Position) >= 68);
        Assert.True(w.Player.Velocity.Length() < 60);
    }

    [Fact]
    public void AutopilotCompletesPlayerTradeRouteWithoutTeleportOrCollision()
    {
        var w = new World();
        w.ToggleAutopilot();
        FlyUntilDocked(w, "mine");
        w.PlayerBuy();
        Assert.Equal("refinery", w.Target.Id);
        Assert.Equal(10, w.Player.Ore);
        w.ToggleAutopilot();
        FlyUntilDocked(w, "refinery");
        w.PlayerSell();
        Assert.Equal(10, w.PlayerDeliveredOre);
        Assert.True(w.Player.Credits > 1000);
        Assert.Equal(0, w.CollisionCount);
        w.ToggleAutopilot();
        FlyUntilDocked(w, "mine");
        Assert.Equal(0, w.CollisionCount);
    }

    [Fact]
    public void ManualInputCancelsAutopilotAndBrakesReduceSpeed()
    {
        var w = new World();
        w.ToggleAutopilot();
        w.Step(new PilotInput(1, 0, false));
        Assert.False(w.Autopilot);
        w.Player.Velocity = new Vector3(80, 20, 0);
        for (int i = 0; i < 90; i++) w.Step(new PilotInput(0, 0, true));
        Assert.True(w.Player.Velocity.Length() < 1);
    }

    private static void FlyUntilDocked(World world, string expectedStation)
    {
        for (int i = 0; i < 60 * 180 && world.Player.DockedAt is null; i++)
        {
            Vector3 before = world.Player.Position;
            world.Step();
            Assert.True(Vector3.Distance(before, world.Player.Position) < 3, "Autopilot teleported the ship.");
        }
        Assert.Equal(expectedStation, world.Player.DockedAt);
    }
}
