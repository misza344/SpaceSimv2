using System.Numerics;
using Xunit;
namespace SpaceSim.Core.Tests;
public class ExplorationTests
{
    [Fact]
    public void FlightAssistStopsReleasedThrustButFreeFlightRetainsMomentum()
    {
        var assisted=new World();var free=new World();free.ToggleFlightAssist();
        assisted.Player.Position=free.Player.Position=new(0,700,0);
        for(int i=0;i<60;i++) {assisted.Step(new(0,1,false));free.Step(new(0,1,false));}
        for(int i=0;i<240;i++){assisted.Step();free.Step();}
        Assert.True(assisted.Player.Velocity.Length()<1);
        Assert.True(free.Player.Velocity.Length()>50);
    }
    [Fact]
    public void GateRejectsDistanceSpeedAndDockedShips()
    {
        var g=new Galaxy(); Assert.False(g.Travel());
        g.Active.Player.Position=World.GatePosition;g.Active.Player.Velocity=new(0,0,30);
        Assert.False(g.Travel());g.Active.Player.Velocity=Vector3.Zero;g.Active.Player.DockedAt="mine";
        Assert.False(g.Travel());Assert.Equal(0,g.Sector);
    }
    [Fact]
    public void ManualControlAndTargetSelectionCancelGateNavigation()
    {
        var w=new World();w.NavigateToGate();w.Step(new(1,0,false));
        Assert.False(w.Autopilot);Assert.False(w.NavigatingToGate);
        w.NavigateToGate();w.SelectTarget(false);
        Assert.False(w.Autopilot);Assert.False(w.NavigatingToGate);
    }
    [Fact]
    public void RoundTripPreservesCargoMoneyAndStationStocks()
    {
        var g=new Galaxy();var initial=g.Active;
        initial.ToggleDock();Assert.True(initial.BuyOre(initial.Player,10).Success);
        int stock=initial.Mine.Ore;decimal money=initial.Player.Credits;
        for(int i=0;i<3;i++)
        {
            g.Active.Player.DockedAt=null;g.Active.Player.Position=World.GatePosition;
            Assert.True(g.Travel());Assert.Equal(10,g.Active.Player.Ore);Assert.Equal(money,g.Active.Player.Credits);
        }
        Assert.Same(initial,g.Active);Assert.Equal(stock,g.Active.Mine.Ore);
        Assert.False(g.Travel()); // Arrival is outside the activation zone.
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void AllSectorsHaveReachableStationsAndGate(int sector)
    {
        var w=new World(sector);
        foreach(bool mine in new[]{true,false})
        {
            w.SelectTarget(mine);w.ToggleAutopilot();
            for(int i=0;i<18000&&w.Autopilot;i++)w.Step();
            Assert.Equal(mine?"mine":"refinery",w.Player.DockedAt);
        }
        w.NavigateToGate();for(int i=0;i<18000&&w.Autopilot;i++)w.Step();
        Assert.False(w.Autopilot);Assert.Null(w.Player.DockedAt);
        Assert.True(Vector3.Distance(w.Player.Position,World.GatePosition)<2);
        Assert.Equal(0,w.CollisionCount);
    }
}
