using Godot;
using SpaceSim.Core;
using NVector = System.Numerics.Vector3;

public partial class Main : Node3D
{
    public Galaxy Galaxy { get; private set; } = new();
    public World Universe => Galaxy.Active;
    public bool Walking { get; private set; }
    public bool MouseFlight { get; private set; }
    public bool NearSeat => _walkPosition.DistanceTo(Cabin.Seat)<=1.6f;
    public float GateDistance => NVector.Distance(Universe.Player.Position,World.GatePosition);
    private Node3D _sectorVisual = null!, _cabin = null!;
    private Vector3 _walkPosition = new(0,.65f,-.6f);
    private float _walkYaw, _walkPitch;
    private Vector2 _steering;
    public Camera3D FlightCamera { get; private set; } = null!;
    public bool Paused { get; private set; }
    public bool Cockpit { get; private set; }
    public bool MapOpen { get; private set; }
    private Node3D _player = null!, _trader = null!;
    private Node3D _mine = null!, _refinery = null!;
    private Node3D _cockpitFrame = null!;
    private bool _artReview;
    private FlightHud _hud = null!;
    private Vector2 _mouseDelta;
    private bool _smoke;
    private int _frames;
    private AudioStreamPlayer _engine = null!, _beep = null!;

    public override void _Ready()
    {
        BuildSector();
        _player = SpaceGeometry.Ship(this, false);
        _trader = SpaceGeometry.Ship(this, true);
        _trader.Scale = Vector3.One * 1.5f;
        FlightCamera = new Camera3D { Fov = 68, Near = .15f, Far = 16000, Current = true };
        AddChild(FlightCamera);
        _cockpitFrame=SpaceGeometry.CockpitFrame(FlightCamera);
        _cabin=Cabin.Build(this);
        UpdateShips();
        UpdateCamera(1, true);
        var canvas = new CanvasLayer(); AddChild(canvas);
        _hud = new FlightHud { Game = this }; canvas.AddChild(_hud);
        _engine = new AudioStreamPlayer { Stream = Tone(52, 1, true), VolumeDb = -40 };
        AddChild(_engine); _engine.Play();
        _beep = new AudioStreamPlayer { Stream = Tone(680, .12f, false), VolumeDb = -21 }; AddChild(_beep);
        _smoke = OS.GetCmdlineUserArgs().Contains("--smoke-test");
        if (OS.GetCmdlineUserArgs().Contains("--export-models")) ExportModels();
        if (OS.GetCmdlineUserArgs().Contains("--art-review")) { _artReview=true; CaptureModels(); }
    }

    private void BuildSector()
    {
        if (_sectorVisual is not null) { RemoveChild(_sectorVisual); _sectorVisual.QueueFree(); }
        _sectorVisual=new Node3D {Name="Sector"};AddChild(_sectorVisual);
        BuildEnvironment();
        _mine=SpaceGeometry.Station(_sectorVisual,Universe.Mine);
        _refinery=SpaceGeometry.Station(_sectorVisual,Universe.Refinery);
        SpaceGeometry.Asteroids(_sectorVisual,Universe);
        var gate=new Node3D {Position=SpaceGeometry.Convert(World.GatePosition)};_sectorVisual.AddChild(gate);
        SpaceGeometry.Ring(gate,44,4,Vector3.Zero,SpaceGeometry.Material("71838d",.6f,.4f),new(90,0,0));
        SpaceGeometry.Ring(gate,39,.7f,Vector3.Zero,SpaceGeometry.Material("69dfea",0,.4f,1),new(90,0,0));
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.Tau/8;
            SpaceGeometry.Box(gate,new(8,8,13),new(Mathf.Cos(a)*44,Mathf.Sin(a)*44,0),SpaceGeometry.Material("344554",.5f,.5f));
        }
        gate.AddChild(new Label3D {Text=$"BRAMA / {Galaxy.Names[Galaxy.NextSector]}\nJ — skok   /   K — podejście",Position=new(0,57,0),PixelSize=.08f,FontSize=32,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled});
    }

    private void BuildEnvironment()
    {
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/space.gdshader") }, RadianceSize = Sky.RadianceSizeEnum.Size128 },
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("718cae"), AmbientLightEnergy = .48f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            TonemapExposure = .9f
        };
        ((ShaderMaterial)env.Sky.SkyMaterial).SetShaderParameter("sector_tint", new Color(Galaxy.Sector==1?"508cae":Galaxy.Sector==2?"d28d56":"a88cc9"));
        _sectorVisual.AddChild(new WorldEnvironment { Environment = env });
        _sectorVisual.AddChild(new DirectionalLight3D { RotationDegrees = new(-32, -35, 0), LightColor = new Color("ffe4bd"), LightEnergy = 1.15f, ShadowEnabled = true, DirectionalShadowMaxDistance = 650 });
        _sectorVisual.AddChild(new DirectionalLight3D { RotationDegrees = new(25, 145, 0), LightColor = new Color("577ab7"), LightEnergy = .6f });
        var planet = SpaceGeometry.Sphere(_sectorVisual, 1050, new(1300-Galaxy.Sector*1700, -640+Galaxy.Sector*350, -4100), new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/planet.gdshader") }, 96);
        planet.RotationDegrees = new(0, 25+Galaxy.Sector*53, 22);
        ((ShaderMaterial)planet.MaterialOverride).SetShaderParameter("sector_tint", new Color(Galaxy.Sector==1?"8cbce0":Galaxy.Sector==2?"dfab79":"ffffff"));
        SpaceGeometry.Sphere(_sectorVisual, 1090, planet.Position, new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/atmosphere.gdshader") }, 96);
        var distantMoon = SpaceGeometry.Sphere(_sectorVisual, 190, new(-1500, 850, -5000), SpaceGeometry.Material("85838b", .1f, .9f), 40);
        distantMoon.RotationDegrees = new(0, 60, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_artReview) return;
        if (!Paused)
        {
            float yaw = Axis(Key.Right, Key.Left) + _mouseDelta.X * .032f;
            float pitch = Axis(Key.Up, Key.Down) - _mouseDelta.Y * .032f;
            _steering=_steering.Lerp(new(yaw,pitch),.22f);
            var input = MapOpen || Walking ? default : new PilotInput(_steering.X, Axis(Key.W, Key.S), Input.IsPhysicalKeyPressed(Key.Space), _steering.Y, Axis(Key.D, Key.A), Axis(Key.Q, Key.Z), Input.IsPhysicalKeyPressed(Key.Shift));
            if (Walking && !MapOpen) MoveOnFoot((float)delta);
            Universe.Step(input);
        }
        _mouseDelta = Vector2.Zero;
        UpdateShips();
        UpdateCamera((float)delta);
        _hud.Refresh();
        _engine.VolumeDb = Paused || Universe.Player.DockedAt is not null ? -60 : -37 + MathF.Min(Universe.Player.Velocity.Length() / 20, 11);
        _engine.PitchScale = .7f + Universe.Player.Velocity.Length() / 250;
        if (_smoke && ++_frames == 35) RunSmokeScenario();
    }

    private static float Axis(Key positive, Key negative) => (Input.IsPhysicalKeyPressed(positive) ? 1 : 0) - (Input.IsPhysicalKeyPressed(negative) ? 1 : 0);

    private void UpdateShips()
    {
        SyncShip(_player, Universe.Player);
        SyncShip(_trader, Universe.Trader);
        _mine.GetNode<Node3D>("HabitatRing").Rotation = new(0, 0, Universe.Tick / 60f * .035f);
        _refinery.GetNode<Node3D>("HabitatRing").Rotation = new(0, 0, -Universe.Tick / 60f * .022f);
        _player.Visible = !Cockpit && !Walking;
        _cockpitFrame.Visible = Cockpit && !Walking;
        _cabin.Visible = Walking;
        _cabin.Transform=_player.Transform;
    }

    private static void SyncShip(Node3D model, Ship ship)
    {
        model.Position = SpaceGeometry.Convert(ship.Position);
        model.Rotation = new(ship.Pitch, -ship.Heading, 0);
        foreach (string name in new[] { "PlumeLeft", "PlumeRight" })
        {
            var plume = model.GetNode<Node3D>(name);
            plume.Visible = ship.DockedAt is null;
            float length=.3f + MathF.Min(ship.Velocity.Length() / 100, 2);
            plume.Scale = new(1,length,1);
            plume.Position = new(plume.Position.X,plume.Position.Y,6.68f+1.7f*length);
        }
    }

    private void UpdateCamera(float delta, bool instant = false)
    {
        if (Walking)
        {
            FlightCamera.Transform=_cabin.Transform * new Transform3D(Basis.FromEuler(new(_walkPitch,_walkYaw,0)),_walkPosition);
            FlightCamera.Fov=75;
            return;
        }
        var pos = SpaceGeometry.Convert(Universe.Player.Position);
        var forward = SpaceGeometry.Convert(Universe.Player.Forward);
        var right = SpaceGeometry.Convert(Universe.Player.Right);
        var up = right.Cross(forward).Normalized();
        Vector3 desired = Cockpit ? pos + up * 1.5f + forward * 4 : pos - forward * 38 + up * 11;
        FlightCamera.Position = instant ? desired : FlightCamera.Position.Lerp(desired, 1 - MathF.Exp(-8 * delta));
        var lookAt = pos + forward * (Cockpit ? 100 : 48) + up * 1.5f;
        var direction = (lookAt - FlightCamera.Position).Normalized();
        var cameraUp = MathF.Abs(direction.Dot(up)) > .98f ? right : up;
        FlightCamera.LookAt(lookAt, cameraUp);
        FlightCamera.Fov = Mathf.Lerp(FlightCamera.Fov, Universe.Player.Velocity.Length() > World.CruiseSpeed ? 79 : 68, .035f);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && _hud is not null && !_smoke && !_artReview)
        {
            Paused=true;_hud.Visible=true;UpdateMouseMode();_hud.Refresh();
        }
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion && !Paused && !MapOpen)
        {
            if (Walking) { _walkYaw-=motion.Relative.X*.0025f; _walkPitch=Mathf.Clamp(_walkPitch-motion.Relative.Y*.0025f,-1.25f,1.25f); }
            else if (MouseFlight || Input.IsMouseButtonPressed(MouseButton.Right)) _mouseDelta += motion.Relative;
        }
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        Act(key.PhysicalKeycode);
    }

    public void Act(Key key)
    {
        if (key == Key.F5) { SaveProgress(); return; }
        if (key == Key.F9) { LoadProgress(); return; }
        if (key == Key.P || key == Key.Escape) { Paused = !Paused; _hud.Visible=true; UpdateMouseMode(); _hud.Refresh(); return; }
        if (key == Key.Tab) { MapOpen = !MapOpen; _hud.Visible=true; UpdateMouseMode(); _hud.Refresh(); return; }
        if (key == Key.H) { _hud.Visible = !_hud.Visible; return; }
        if (Paused) return;
        if (key == Key.F) { ToggleWalking(); return; }
        if (Walking) return;
        if (key == Key.M) { MouseFlight=!MouseFlight; UpdateMouseMode(); return; }
        if (key == Key.C) { Cockpit = !Cockpit; return; }
        switch (key)
        {
            case Key.X: Universe.ToggleFlightAssist(); break;
            case Key.K: _steering=Vector2.Zero; Universe.NavigateToGate(); break;
            case Key.J: if (Galaxy.Travel()) { BuildSector(); UpdateShips(); UpdateCamera(1,true); } break;
            case Key.E: Universe.ToggleDock(); break;
            case Key.B: Universe.PlayerBuy(); break;
            case Key.V: Universe.PlayerSell(); break;
            case Key.T: Universe.CycleTarget(); break;
            case Key.G: _steering=Vector2.Zero; Universe.ToggleAutopilot(); break;
            default: return;
        }
        _beep.Play();
    }

    private string SavePath => ProjectSettings.GlobalizePath(_smoke ? "user://smoke/quicksave.json" : "user://saves/quicksave.json");
    public string SaveMessage { get; private set; } = "F5 — zapisz • F9 — wczytaj ostatni zapis";

    private GameSave CaptureProgress() => new()
    {
        Version=SaveStorage.CurrentVersion, Galaxy=Galaxy.Capture(),
        View=new PilotView(Walking,Cockpit,MouseFlight,
            new NVector(_walkPosition.X,_walkPosition.Y,_walkPosition.Z),_walkYaw,_walkPitch)
    };

    private bool SaveProgress()
    {
        try
        {
            SaveStorage.Write(SavePath,CaptureProgress());
            SaveMessage="Zapisano postęp. F9 — wczytaj.";
        }
        catch (Exception ex) when (ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException)
        {
            SaveMessage="Nie udało się zapisać: "+ex.Message;
            Universe.Notify(SaveMessage); _hud.Visible=true; _hud.Refresh(); return false;
        }
        Universe.Notify(SaveMessage); _hud.Visible=true; _hud.Refresh(); return true;
    }

    private bool LoadProgress()
    {
        GameSave save;
        Galaxy restored;
        try
        {
            save=SaveStorage.Read(SavePath);
            restored=Galaxy.Restore(save.Galaxy);
        }
        catch (Exception ex) when (ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException)
        {
            SaveMessage=ex is System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException
                ? "Brak zapisu. F5 — zapisz bieżącą grę."
                : "Nie udało się wczytać: "+ex.Message;
            Universe.Notify(SaveMessage); _hud.Visible=true; _hud.Refresh(); return false;
        }
        Galaxy=restored;
        Walking=save.View.Walking; Cockpit=save.View.Cockpit; MouseFlight=save.View.MouseFlight;
        _walkPosition=Cabin.Clamp(SpaceGeometry.Convert(save.View.Position));
        _walkYaw=save.View.Yaw; _walkPitch=save.View.Pitch;
        // Loading always pauses, so held input cannot move the restored ship unexpectedly.
        Paused=true; MapOpen=false; _hud.Visible=true;
        BuildSector(); UpdateShips(); UpdateCamera(1,true); UpdateMouseMode();
        SaveMessage="Wczytano postęp. P / ESC — kontynuuj.";
        Universe.Notify(SaveMessage); _hud.Refresh(); return true;
    }

    private void UpdateMouseMode()
    {
        Input.MouseMode= !Paused && !MapOpen && (Walking || MouseFlight) ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        _mouseDelta=Vector2.Zero;_steering=Vector2.Zero;
    }

    private void ToggleWalking()
    {
        if (Walking)
        {
            if (!NearSeat) { Universe.Notify("Podejdź do fotela z przodu kabiny i naciśnij F."); return; }
            Walking=false;Cockpit=true;
        }
        else
        {
            if (Universe.Autopilot || Universe.Player.Velocity.Length()>.5f) { Universe.Notify("Najpierw wyłącz autopilota i zatrzymaj statek spacją, potem F."); return; }
            Walking=true; _walkPosition=new(0,.65f,-.6f); _walkYaw=0;_walkPitch=0;
        }
        MapOpen=false;_hud.Visible=true;UpdateMouseMode();UpdateShips();UpdateCamera(1,true);
    }

    private void MoveOnFoot(float delta)
    {
        var direction=new Vector3(Axis(Key.D,Key.A),0,-Axis(Key.W,Key.S));
        if(direction.LengthSquared()>1) direction=direction.Normalized();
        _walkPosition=Cabin.Clamp(_walkPosition+new Basis(Vector3.Up,_walkYaw)*direction*delta*(Input.IsPhysicalKeyPressed(Key.Shift)?3.2f:1.8f));
    }

    private void RunSmokeScenario()
    {
        try
        {
            Act(Key.G);
            for (int i = 0; i < 18000 && Universe.Player.DockedAt != "mine"; i++) SmokeStep();
            if (Universe.Player.DockedAt != "mine") throw new Exception("autopilot did not dock at mine");
            Act(Key.B);
            if (Universe.Player.Ore != 10) throw new Exception("player purchase failed");
            Act(Key.G);
            for (int i = 0; i < 18000 && Universe.Player.DockedAt != "refinery"; i++) SmokeStep();
            if (Universe.Player.DockedAt != "refinery") throw new Exception("autopilot did not dock at refinery");
            Act(Key.V);
            if (Universe.PlayerDeliveredOre != 10 || Universe.Player.Credits <= 1000 || Universe.CollisionCount != 0) throw new Exception("trade/collision invariant failed");
            for (int i = 0; i < 7200; i++) Universe.Step();
            if (Universe.DeliveredOre <= 10 || Universe.Refinery.Metal == 0) throw new Exception("AI economy failed");
            // A stopped pilot can walk, stays within cabin bounds, and must approach the chair to return.
            Act(Key.F);
            if (!Walking) throw new Exception("could not leave pilot seat");
            Act(Key.K);Act(Key.G);
            if(Universe.Autopilot) throw new Exception("ship controls active while walking");
            _walkPosition=Cabin.Clamp(new(50,50,50));
            if (_walkPosition.X>1.3f || _walkPosition.Z>2.6f) throw new Exception("cabin bounds failed");
            Act(Key.F);
            if (!Walking) throw new Exception("returned to seat remotely");
            _walkPosition=new(0,.65f,-1.3f); Act(Key.F);
            if (Walking) throw new Exception("could not return to seat");
            for(int sector=0;sector<3;sector++)
            {
                Act(Key.K);
                for(int i=0;i<18000 && Universe.Autopilot;i++) SmokeStep();
                if(GateDistance>5 || Universe.CollisionCount!=0) throw new Exception("gate approach failed");
                int before=Galaxy.Sector; Act(Key.J);
                if(Galaxy.Sector==(before)) throw new Exception("sector travel failed");
            }
            // Exercise the real disk/UI path in flight and in the cabin, using an isolated save.
            Act(Key.G);
            for(int i=0;i<120;i++) SmokeStep();
            var flightPosition=Universe.Player.Position;
            long flightTick=Universe.Tick;
            if(!SaveProgress()) throw new Exception("flight save failed");
            for(int i=0;i<120;i++) SmokeStep();
            if(!LoadProgress() || !Paused || !Universe.Autopilot || Universe.Tick!=flightTick || Universe.Player.Position!=flightPosition)
                throw new Exception("flight restore failed");
            Act(Key.P);
            for(int i=0;i<18000 && Universe.Autopilot;i++) SmokeStep();
            Act(Key.F);
            if(!Walking) throw new Exception("cabin save setup failed");
            _walkPosition=new(0,.65f,1); _walkYaw=.4f;
            if(!SaveProgress()) throw new Exception("cabin save failed");
            _walkPosition=new(0,.65f,-1.3f); Act(Key.F);
            if(!LoadProgress() || !Walking || _walkPosition.Z!=1 || _walkYaw!=.4f)
                throw new Exception("cabin restore failed");
            var beforeFailure=Galaxy;
            System.IO.File.WriteAllText(SavePath,"{broken");
            if(LoadProgress() || !ReferenceEquals(beforeFailure,Galaxy)) throw new Exception("invalid save changed galaxy");
            System.IO.File.Delete(SavePath);
            System.IO.File.Delete(SavePath+".bak");
            Act(Key.P);
            _walkPosition=new(0,.65f,-1.3f); Act(Key.F);
            Act(Key.C); Act(Key.Tab);
            UpdateShips(); UpdateCamera(1, true); _hud.Refresh();
            GD.Print($"SMOKE PASS 3D: disk save/load in flight and cabin, corrupt save rejected, cabin walk/seat, three sector jumps, autopilot trade route, player trade, AI delivered {Universe.DeliveredOre}, metal {Universe.Refinery.Metal}, no collisions");
            FinishSmoke(0);
        }
        catch (Exception ex) { GD.PushError($"SMOKE FAIL 3D: {ex.Message}"); FinishSmoke(1); }
    }

    private async void CaptureModels()
    {
        _hud.Visible=false; _cockpitFrame.Visible=false; StopSounds();
        var models=new[]{_player,_trader,_mine,_refinery};
        foreach(var model in models) model.Visible=false;
        // Separate art review: rendered by the same camera/material pipeline as the game.
        string directory=ProjectSettings.GlobalizePath("res://../../docs/art");
        System.IO.Directory.CreateDirectory(directory);
        foreach(var model in models)
        {
            model.Visible=true; model.Position=Vector3.Zero; model.Rotation=Vector3.Zero; model.Scale=Vector3.One;
            bool ship=model==_player||model==_trader;
            FlightCamera.Position=ship?new(15,10,-22):new(135,85,210);
            FlightCamera.LookAt(ship?new Vector3(0,0,0):new Vector3(0,0,15)); FlightCamera.Fov=50;
            for(int i=0;i<12;i++) await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var screenshot=GetViewport().GetTexture().GetImage();
            var result=screenshot.SavePng($"{directory}/{model.Name}.png");
            if(result!=Error.Ok) { GD.PushError($"Screenshot failed: {result}"); FinishSmoke(1); return; }
            GD.Print($"ART CAPTURED: {model.Name}");model.Visible=false;
        }
        FinishSmoke(0);
    }

    private void ExportModels()
    {
        string directory = ProjectSettings.GlobalizePath("res://../../assets/models");
        System.IO.Directory.CreateDirectory(directory);
        foreach(var model in new[]{_player,_trader,_mine,_refinery})
        {
            var copy = (Node3D)model.Duplicate();
            copy.Position=Vector3.Zero; copy.Rotation=Vector3.Zero; copy.Scale=Vector3.One;
            // Text signage is generated in-engine; glTF stores geometry and PBR materials.
            void StripText(Node node) { foreach(var child in node.GetChildren()) { if(child is Label3D) child.Free(); else StripText(child); } }
            StripText(copy);
            using var document=new GltfDocument(); using var state=new GltfState();
            var error=document.AppendFromScene(copy,state);
            if(error==Error.Ok) error=document.WriteToFilesystem(state,$"{directory}/{model.Name}.glb");
            copy.Free();
            if(error!=Error.Ok) { GD.PushError($"Model export failed: {model.Name}: {error}"); FinishSmoke(1); return; }
            GD.Print($"MODEL EXPORTED: {model.Name}");
        }
        FinishSmoke(0);
    }

    private void StopSounds()
    {
        foreach (var player in new[] { _engine, _beep })
        {
            if (!GodotObject.IsInstanceValid(player)) continue;
            player.Stop();
            var stream = player.Stream;
            player.Stream = null;
            stream?.Dispose();
        }
    }

    public override void _ExitTree() => StopSounds();

    private async void FinishSmoke(int code)
    {
        StopSounds();
        // Let the audio server consume its queued stop commands before destroying the scene.
        await ToSignal(GetTree().CreateTimer(.1), SceneTreeTimer.SignalName.Timeout);
        GetTree().Quit(code);
    }

    private void SmokeStep()
    {
        Universe.Step();
        if (Universe.Tick % 8 == 0)
        {
            UpdateShips(); UpdateCamera(World.StepSeconds * 8);
            if (!FlightCamera.GlobalPosition.IsFinite() || !FlightCamera.GlobalBasis.IsFinite())
                throw new Exception("non-finite flight camera transform");
        }
    }

    private static AudioStreamWav Tone(float frequency, float seconds, bool loop)
    {
        const int rate = 22050;
        int samples = (int)(rate * seconds);
        var data = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / rate;
            double envelope = loop ? .28 : Math.Sin(Math.PI * i / samples) * .35;
            short value = (short)(Math.Sin(t * frequency * Math.Tau) * envelope * short.MaxValue);
            data[i * 2] = (byte)value; data[i * 2 + 1] = (byte)(value >> 8);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Data = data, LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled, LoopBegin = 0, LoopEnd = samples };
    }
}
