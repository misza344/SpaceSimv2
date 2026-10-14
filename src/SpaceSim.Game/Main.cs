using Godot;
using SpaceSim.Core;
using NVector = System.Numerics.Vector3;

public partial class Main : Node3D
{
    public World Universe { get; } = new();
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
        BuildEnvironment();
        _mine = SpaceGeometry.Station(this, Universe.Mine);
        _refinery = SpaceGeometry.Station(this, Universe.Refinery);
        SpaceGeometry.Asteroids(this, Universe);
        _player = SpaceGeometry.Ship(this, false);
        _trader = SpaceGeometry.Ship(this, true);
        _trader.Scale = Vector3.One * 1.5f;
        FlightCamera = new Camera3D { Fov = 68, Near = .15f, Far = 16000, Current = true };
        AddChild(FlightCamera);
        _cockpitFrame=SpaceGeometry.CockpitFrame(FlightCamera);
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
        AddChild(new WorldEnvironment { Environment = env });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-32, -35, 0), LightColor = new Color("ffe4bd"), LightEnergy = 1.15f, ShadowEnabled = true, DirectionalShadowMaxDistance = 650 });
        AddChild(new DirectionalLight3D { RotationDegrees = new(25, 145, 0), LightColor = new Color("577ab7"), LightEnergy = .6f });
        var planet = SpaceGeometry.Sphere(this, 1050, new(1300, -640, -4100), new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/planet.gdshader") }, 96);
        planet.RotationDegrees = new(0, 25, 22);
        SpaceGeometry.Sphere(this, 1090, planet.Position, new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/atmosphere.gdshader") }, 96);
        var distantMoon = SpaceGeometry.Sphere(this, 190, new(-1500, 850, -5000), SpaceGeometry.Material("85838b", .1f, .9f), 40);
        distantMoon.RotationDegrees = new(0, 60, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_artReview) return;
        if (!Paused)
        {
            float yaw = Axis(Key.Right, Key.Left) + _mouseDelta.X * .032f;
            float pitch = Axis(Key.Up, Key.Down) - _mouseDelta.Y * .032f;
            var input = MapOpen ? default : new PilotInput(yaw, Axis(Key.W, Key.S), Input.IsPhysicalKeyPressed(Key.Space), pitch, Axis(Key.D, Key.A), Axis(Key.Q, Key.Z), Input.IsPhysicalKeyPressed(Key.Shift));
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
        _player.Visible = !Cockpit;
        _cockpitFrame.Visible = Cockpit;
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

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Right) && !Paused && !MapOpen)
            _mouseDelta += motion.Relative;
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        Act(key.PhysicalKeycode);
    }

    public void Act(Key key)
    {
        if (key == Key.P || key == Key.Escape) { Paused = !Paused; _hud.Refresh(); return; }
        if (key == Key.Tab) { MapOpen = !MapOpen; _hud.Refresh(); return; }
        if (key == Key.H) { _hud.Visible = !_hud.Visible; return; }
        if (key == Key.C) { Cockpit = !Cockpit; return; }
        if (Paused) return;
        switch (key)
        {
            case Key.E: Universe.ToggleDock(); break;
            case Key.B: Universe.PlayerBuy(); break;
            case Key.V: Universe.PlayerSell(); break;
            case Key.T: Universe.CycleTarget(); break;
            case Key.G: Universe.ToggleAutopilot(); break;
            default: return;
        }
        _beep.Play();
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
            Act(Key.C); Act(Key.Tab);
            UpdateShips(); UpdateCamera(1, true); _hud.Refresh();
            GD.Print($"SMOKE PASS 3D: scene, autopilot trade route, player trade, AI delivered {Universe.DeliveredOre}, metal {Universe.Refinery.Metal}, no collisions");
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
