using Godot;
using SpaceSim.Core;
using NVector = System.Numerics.Vector2;

public partial class Main : Node2D
{
    private World _world = new();
    private bool _paused;
    private bool _smoke;
    private int _smokeFrames;
    private readonly Vector2 _origin = new(475, 418);
    private readonly Color _cyan = new("63e3ed"), _amber = new("ffbc70"), _muted = new("8b9daf"), _white = new("e9f1f7");
    private readonly List<(Vector2 Point, float Size, float Alpha)> _stars = [];
    private Label _traderStatus = null!;
    private Button _dock = null!, _buy = null!, _sell = null!;
    private Font Font => ThemeDB.FallbackFont;

    public override void _Ready()
    {
        var random = new Random(42);
        for (int i = 0; i < 200; i++) _stars.Add((new Vector2(random.Next(18, 950), random.Next(140, 700)), i % 7 == 0 ? 1.5f : 0.7f, 0.15f + (float)random.NextDouble() * 0.5f));
        _dock = AddButton("E   Zadokuj / odłącz", new(995, 425), () => _world.ToggleDock());
        _buy = AddButton("B   Kup 10 rudy", new(995, 472), () => _world.PlayerBuy());
        _sell = AddButton("V   Sprzedaj 10 rudy", new(995, 519), () => _world.PlayerSell());
        _traderStatus = new Label { Position = new(995, 644), Size = new(250, 61), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _traderStatus.AddThemeFontSizeOverride("font_size", 13);
        _traderStatus.AddThemeColorOverride("font_color", _muted);
        AddChild(_traderStatus);
        AddButton("P   Pauza / kontynuuj", new(995, 712), () => _paused = !_paused, false);
        _smoke = OS.GetCmdlineUserArgs().Contains("--smoke-test");
        if (_smoke)
        {
            _world.ToggleDock();
            _world.PlayerBuy();
            if (_world.Player.Ore != 10) { GD.PushError("SMOKE FAIL: player trade"); GetTree().Quit(1); }
        }
    }

    private Button AddButton(string text, Vector2 position, Action action, bool requiresRunning = true)
    {
        var button = new Button { Text = text, Position = position, Size = new(250, 38), FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 16);
        var style = new StyleBoxFlat { BgColor = new Color("152d3a"), BorderColor = new Color("305363"), BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5, CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5 };
        button.AddThemeStyleboxOverride("normal", style);
        var hover = (StyleBoxFlat)style.Duplicate(); hover.BgColor = new Color("234958");
        button.AddThemeStyleboxOverride("hover", hover);
        button.Pressed += () => { if (!_paused || !requiresRunning) action(); QueueRedraw(); };
        AddChild(button);
        return button;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_paused)
        {
            var turn = (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0);
            var thrust = (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up) ? 1f : 0) - (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down) ? 0.45f : 0);
            _world.Step(new PilotInput(turn, thrust, Input.IsPhysicalKeyPressed(Key.Space)));
        }
        _traderStatus.Text = _world.Trader.Status;
        _dock.Disabled = _paused;
        _buy.Disabled = _paused || _world.Player.DockedAt != "mine";
        _sell.Disabled = _paused || _world.Player.DockedAt != "refinery";
        if (_smoke && ++_smokeFrames >= 180)
        {
            // Accelerated integration scenario in the actual Godot process.
            for (int i = 0; i < 7200; i++) _world.Step();
            if (_world.DeliveredOre <= 0 || _world.Refinery.Metal <= 0)
            { GD.PushError("SMOKE FAIL: logistics/production"); GetTree().Quit(1); }
            else { GD.Print($"SMOKE PASS: scene, player purchase, AI delivered {_world.DeliveredOre}, metal {_world.Refinery.Metal}"); GetTree().Quit(); }
        }
        QueueRedraw();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.PhysicalKeycode == Key.P) _paused = !_paused;
        if (_paused) return;
        switch (key.PhysicalKeycode)
        {
            case Key.E: _world.ToggleDock(); break;
            case Key.B: _world.PlayerBuy(); break;
            case Key.V: _world.PlayerSell(); break;
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, 1280, 800), new Color("080f1a"));
        // Quiet grid and a seeded star field keep the first prototype asset-free.
        for (int x = 30; x < 960; x += 60) DrawLine(new(x, 145), new(x, 697), new Color(0.15f, 0.25f, 0.35f, 0.15f));
        for (int y = 157; y < 700; y += 60) DrawLine(new(20, y), new(956, y), new Color(0.15f, 0.25f, 0.35f, 0.15f));
        foreach (var star in _stars) DrawCircle(star.Point, star.Size, new Color(0.65f, 0.8f, 1, star.Alpha));
        DrawCircle(new(195, 363), 112, new Color(0.12f, 0.26f, 0.3f, 0.12f));
        DrawCircle(new(755, 473), 112, new Color(0.3f, 0.2f, 0.12f, 0.12f));
        DrawDashedLine(Screen(_world.Mine.Position), Screen(_world.Refinery.Position), new Color(0.3f, 0.65f, 0.7f, 0.25f), 1, 10);
        Text(new(28, 40), "SPACESIM", 25, _white);
        Text(new(199, 39), "/  FIRST CARGO", 15, _cyan);
        Text(new(29, 70), "SEKTOR 01   /   PAS KEPLERA", 13, _muted);
        Text(new(683, 40), _paused ? "●  SYMULACJA WSTRZYMANA" : "●  SYMULACJA AKTYWNA", 13, _paused ? _amber : _cyan);
        Text(new(683, 66), $"CZAS   {TimeSpan.FromSeconds(_world.Tick / 60):mm\\:ss}", 14, _muted);
        DrawLine(new(25, 88), new(951, 88), new Color("253748"));
        Text(new(29, 119), "01  KUP RUDĘ", 14, _world.Player.Ore > 0 ? _cyan : _white);
        Text(new(254, 119), "02  PRZELEĆ DO HELIOS", 14, _white);
        Text(new(560, 119), "03  SPRZEDAJ ŁADUNEK", 14, _white);
        DrawStation(_world.Mine, _cyan);
        DrawStation(_world.Refinery, _amber);
        DrawShip(_world.Trader, _amber, false);
        DrawShip(_world.Player, _cyan, true);
        DrawSidebar();
        DrawRect(new Rect2(20, 712, 936, 66), new Color("101e2b"));
        Text(new(34, 738), _world.LastEvent, 16, _white);
        Text(new(34, 762), "W/S  ciąg     A/D  obrót     SPACJA  hamulec     E  dok     B/V  handel     P  pauza", 14, _muted);
    }

    private void DrawStation(Station station, Color color)
    {
        Vector2 p = Screen(station.Position);
        DrawArc(p, 85, 0, Mathf.Tau, 72, new Color(color, 0.18f), 1, true);
        DrawArc(p, 37, 0, Mathf.Tau, 6, color, 2, true);
        DrawRect(new Rect2(p - new Vector2(19, 19), new(38, 38)), new Color("142633"));
        DrawRect(new Rect2(p - new Vector2(19, 19), new(38, 38)), color, false, 2);
        for (int side = -1; side <= 1; side += 2)
        {
            var rect = new Rect2(p + new Vector2(side * 51 - 10, -24), new(20, 48));
            DrawRect(rect, new Color(color, 0.2f)); DrawRect(rect, color, false, 1);
            DrawLine(p + new Vector2(side * 20, 0), p + new Vector2(side * 41, 0), color, 3);
        }
        DrawCircle(p, 5, color);
        Text(p + new Vector2(-83, -104), station.Name, 16, color);
        Text(p + new Vector2(-83, -83), station.IsMine ? "WYDOBYCIE  /  RUDA" : "PRODUKCJA  /  METALE", 11, _muted);
        Text(p + new Vector2(-80, 111), $"Ruda {station.Ore,3} / {Station.Capacity}   •   {station.Price} kr", 14, _white);
        if (!station.IsMine) Text(p + new Vector2(-80, 132), $"Metale {station.Metal}   |   {(station.Metal >= Station.Capacity ? "MAGAZYN PEŁNY" : station.Ore >= 2 ? "PRACA" : "BRAK RUDY")}", 12, _muted);
    }

    private void DrawShip(Ship ship, Color color, bool player)
    {
        Vector2 p = Screen(ship.Position);
        Vector2[] shape = [new(16, 0), new(-10, -9), new(-6, 0), new(-10, 9)];
        var points = shape.Select(v => p + v.Rotated(ship.Heading)).ToArray();
        DrawColoredPolygon(points, color);
        if (ship.Velocity.Length() > 10)
        {
            var rear = new Vector2(-10, 0).Rotated(ship.Heading);
            DrawLine(p + rear, p + rear * (1.5f + ship.Velocity.Length() / 130), new Color(color, 0.6f), 3, true);
        }
        if (player) DrawArc(p, 24, 0, Mathf.Tau, 32, new Color(color, 0.4f), 1, true);
        Text(p + new Vector2(27, 5), player ? "TY / PIONIER" : "AI / KURIER", 11, color);
    }

    private void DrawSidebar()
    {
        DrawRect(new Rect2(976, 0, 304, 800), new Color("101b28"));
        DrawLine(new(976, 0), new(976, 800), new Color("294052"));
        Text(new(995, 39), "TWÓJ STATEK", 13, _muted);
        Text(new(995, 76), "PIONIER / 01", 24, _white);
        Text(new(995, 112), $"{_world.Player.Credits:N0} kr", 28, _cyan);
        Text(new(995, 147), $"Ruda  {_world.Player.Ore} / {Ship.Capacity}", 16, _white);
        DrawRect(new Rect2(995, 160, 250, 5), new Color("263d4e"));
        DrawRect(new Rect2(995, 160, 250 * _world.Player.Ore / Ship.Capacity, 5), _cyan);
        Text(new(995, 200), $"Prędkość   {_world.Player.Velocity.Length():0} m/s", 16, _white);
        Text(new(995, 229), _world.Player.DockedAt is null ? "Status   Swobodny lot" : "Status   Zadokowany", 15, _muted);
        Rule(252);
        var station = _world.DockedStation(_world.Player) ?? _world.NearestStation(_world.Player);
        Text(new(995, 282), _world.Player.DockedAt is null ? "NAJBLIŻSZA STACJA" : "TERMINAL HANDLOWY", 12, _muted);
        Text(new(995, 312), station.IsMine ? "KEPLER / Kopalnia" : "HELIOS / Rafineria", 19, station.IsMine ? _cyan : _amber);
        Text(new(995, 343), $"Dystans  {NVector.Distance(_world.Player.Position, station.Position):0} m", 15, _white);
        Text(new(995, 372), $"{(station.IsMine ? "Sprzedaż" : "Skup")} rudy:  {station.Price} kr / szt.", 15, _white);
        Text(new(995, 402), "Dokowanie: do 85 m, poniżej 35 m/s", 12, _muted);
        Rule(578);
        Text(new(995, 606), "AUTOMATYCZNY TRANSPORT", 12, _amber);
        Text(new(995, 635), $"Kurier   /   {_world.Trader.Ore} rudy   /   {_world.Trader.Credits:0} kr", 14, _white);
        Text(new(995, 783), "PROTOTYP 0.1  /  2D • PC", 12, _muted);
    }

    private void Rule(float y) => DrawLine(new(995, y), new(1245, y), new Color("294052"));
    private Vector2 Screen(NVector v) => _origin + new Vector2(v.X, v.Y);
    private void Text(Vector2 at, string value, int size, Color color) => DrawString(Font, at, value, HorizontalAlignment.Left, -1, size, color);
}
