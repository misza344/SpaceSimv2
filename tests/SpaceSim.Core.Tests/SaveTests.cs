using System.Numerics;
using System.Text.Json.Nodes;
using Xunit;
namespace SpaceSim.Core.Tests;

public class SaveTests
{
    private static GameSave Snapshot(Galaxy galaxy) => new()
    {
        Version=SaveStorage.CurrentVersion, Galaxy=galaxy.Capture(),
        View=new(false,true,true,new(0,.65f,-.6f),0,0)
    };
    private static Galaxy RoundTrip(Galaxy galaxy) => Galaxy.Restore(SaveStorage.Decode(SaveStorage.Encode(Snapshot(galaxy))).Galaxy);

    [Fact]
    public void MidflightSaveResumesIdenticalPlayerAndAiSimulation()
    {
        var original=new Galaxy();
        original.Active.SelectTarget(false); original.Active.ToggleAutopilot();
        for(int i=0;i<777;i++) original.Active.Step();
        var loaded=RoundTrip(original);
        Assert.Equal(SaveStorage.Encode(Snapshot(original)),SaveStorage.Encode(Snapshot(loaded)));
        for(int i=0;i<12000;i++) { original.Active.Step(); loaded.Active.Step(); }
        Assert.Equal("refinery",loaded.Active.Player.DockedAt);
        Assert.Equal(SaveStorage.Encode(Snapshot(original)),SaveStorage.Encode(Snapshot(loaded)));
    }

    [Fact]
    public void AllSectorStocksCargoAndGateRoutesSurviveDiskRoundTrip()
    {
        var galaxy=new Galaxy();
        for(int i=0;i<2;i++)
        {
            galaxy.Active.ToggleDock(); galaxy.Active.PlayerBuy();
            galaxy.Active.NavigateToGate();
            while(galaxy.Active.Autopilot) galaxy.Active.Step();
            Assert.True(galaxy.Travel());
        }
        galaxy.Active.NavigateToGate(); galaxy.Active.Step();
        var loaded=RoundTrip(galaxy);
        Assert.Equal(2,loaded.Sector);
        Assert.True(loaded.Active.NavigatingToGate);
        Assert.Equal(SaveStorage.Encode(Snapshot(galaxy)),SaveStorage.Encode(Snapshot(loaded)));
        for(int i=0;i<3;i++)
        {
            galaxy.Active.Player.Position=loaded.Active.Player.Position=World.GatePosition;
            galaxy.Active.Player.Velocity=loaded.Active.Player.Velocity=Vector3.Zero;
            Assert.True(galaxy.Travel()); Assert.True(loaded.Travel());
            Assert.Equal(SaveStorage.Encode(Snapshot(galaxy)),SaveStorage.Encode(Snapshot(loaded)));
        }
    }

    [Fact]
    public void DockedTradeDoesNotDuplicateCargoOrCreditsAfterLoad()
    {
        var galaxy=new Galaxy();galaxy.Active.ToggleDock();galaxy.Active.PlayerBuy();
        var loaded=RoundTrip(galaxy);
        Assert.Equal("mine",loaded.Active.Player.DockedAt);
        Assert.Equal(10,loaded.Active.Player.Ore);
        loaded.Active.PlayerBuy();galaxy.Active.PlayerBuy();
        Assert.Equal(SaveStorage.Encode(Snapshot(galaxy)),SaveStorage.Encode(Snapshot(loaded)));
    }

    [Theory]
    [InlineData("version")] [InlineData("sector")] [InlineData("stock")]
    [InlineData("null-world")] [InlineData("route")] [InlineData("missing")]
    public void InvalidSavesAreRejected(string mutation)
    {
        var root=JsonNode.Parse(SaveStorage.Encode(Snapshot(new Galaxy())))!;
        switch(mutation)
        {
            case "version": root["Version"]=900; break;
            case "sector": root["Galaxy"]!["Sector"]=3; break;
            case "stock": root["Galaxy"]!["Worlds"]![0]!["Mine"]!["Ore"]=-1; break;
            case "null-world": root["Galaxy"]!["Worlds"]![1]=null; break;
            case "route": root["Galaxy"]!["Worlds"]![0]!["Autopilot"]=true; break;
            case "missing": root["Galaxy"]!["Worlds"]![0]!.AsObject().Remove("Tick"); break;
        }
        Assert.Throws<InvalidDataException>(()=>SaveStorage.Decode(root.ToJsonString()));
        Assert.Throws<InvalidDataException>(()=>SaveStorage.Decode("{truncated"));
    }

    [Fact]
    public void AtomicReplacementKeepsPreviousSaveAndRejectsInvalidOverwrite()
    {
        string directory=Path.Combine(Path.GetTempPath(),"spacesim-"+Guid.NewGuid());
        string path=Path.Combine(directory,"save.json");
        try
        {
            var galaxy=new Galaxy();SaveStorage.Write(path,Snapshot(galaxy));
            string first=File.ReadAllText(path);
            galaxy.Active.Step();SaveStorage.Write(path,Snapshot(galaxy));
            Assert.Equal(first,File.ReadAllText(path+".bak"));
            Assert.Equal(1,SaveStorage.Read(path).Galaxy.Worlds[0].Tick);
            var invalid=new GameSave { Version=999,Galaxy=galaxy.Capture(),View=Snapshot(galaxy).View };
            Assert.Throws<InvalidDataException>(()=>SaveStorage.Write(path,invalid));
            Assert.Equal(1,SaveStorage.Read(path).Galaxy.Worlds[0].Tick);
            Assert.Empty(Directory.GetFiles(directory,"*.tmp"));
        }
        finally { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
    }
}
