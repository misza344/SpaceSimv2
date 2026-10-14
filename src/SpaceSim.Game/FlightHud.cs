using Godot;
using SpaceSim.Core;
using NV = System.Numerics.Vector3;

public partial class FlightHud : Control
{
    public Main Game { get; init; } = null!;
    private readonly Color _cyan = new("69e8e0"), _amber = new("ffbf78"), _white = new("edf4f8"), _muted = new("91aabd");
    private readonly Color _panel = new(.025f, .052f, .078f, .88f);
    private Button _nav = null!, _dock = null!, _buy = null!, _sell = null!, _target = null!, _resume = null!;
    private Label _status = null!, _trader = null!;
    private readonly List<Button> _buttons = [];
    private World W => Game.Universe;
    private Font Font => ThemeDB.FallbackFont;
    private float Width => GetViewportRect().Size.X;
    private float Height => GetViewportRect().Size.Y;
    private float Right => Width - 294;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        _target = Button("T  Zmień cel", Key.T);
        _nav = Button("G  Autopilot", Key.G);
        _dock = Button("E  Zadokuj", Key.E);
        _buy = Button("B  Kup 10 rudy", Key.B);
        _sell = Button("V  Sprzedaj 10 rudy", Key.V);
        _resume = Button("Wróć do lotu", Key.P);
        _status = Label(16, _white);
        _trader = Label(13, _muted);
        Refresh();
    }

    private Label Label(int size, Color color)
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color);
        AddChild(label); return label;
    }
    private Button Button(string text, Key action)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", _white);
        var normal = new StyleBoxFlat { BgColor = new Color(.05f,.13f,.17f,.95f), BorderColor = new Color(.22f,.4f,.47f,.9f), BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4 };
        button.AddThemeStyleboxOverride("normal", normal);
        var hover = (StyleBoxFlat)normal.Duplicate(); hover.BgColor = new Color("234a55"); hover.BorderColor = _cyan;
        button.AddThemeStyleboxOverride("hover", hover);
        button.Pressed += () => Game.Act(action);
        AddChild(button); _buttons.Add(button); return button;
    }
    public void Refresh()
    {
        if (_nav is null) return;
        float x = Right + 16;
        var buttons = new[] { _target, _nav, _dock, _buy, _sell };
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Position = new(x, 348 + i * 42); buttons[i].Size = new(246, 34);
            buttons[i].Visible = !Game.MapOpen && !Game.Paused;
        }
        _nav.Text = W.Autopilot ? "G  Przerwij autopilota" : W.Player.DockedAt is null ? "G  Leć i zadokuj" : "G  Odleć do celu";
        _dock.Text = W.Player.DockedAt is null ? "E  Zadokuj" : "E  Odłącz dok";
        _buy.Disabled = W.Player.DockedAt != "mine" || W.Player.Ore == Ship.Capacity;
        _sell.Disabled = W.Player.DockedAt != "refinery" || W.Player.Ore == 0;
        _dock.Disabled = W.Player.DockedAt is null && (NV.Distance(W.Player.Position, W.NearestStation(W.Player).DockPosition) > World.DockRange || W.Player.Velocity.Length() > World.DockSpeed);
        _status.Position = new(38, Height - 111); _status.Size = new(Width - 76, 45);
        _status.Text = W.LastEvent; _status.Visible = !Game.MapOpen && !Game.Paused;
        _trader.Position = new(42, 434); _trader.Size = new(216, 70); _trader.Text = W.Trader.Status;
        _trader.Visible = !Game.MapOpen && !Game.Paused;
        _resume.Position = new(Width / 2 - 135, Height / 2 + 46); _resume.Size = new(270, 42); _resume.Visible = Game.Paused;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_nav is null) return;
        DrawMarkers();
        // Thin cockpit framing keeps the flight view open.
        DrawRect(new Rect2(0, 0, Width, 82), new Color(.018f,.035f,.055f,.94f));
        Text(new(29, 36), "SPACE", 25, _white); Text(new(114, 36), "SIM", 25, _cyan);
        Text(new(29, 61), "KEPLER RUN   /   PROTOTYP 0.2", 11, _muted);
        Line(new(229, 21), new(229, 61), new Color("334854"));
        Text(new(251, 36), "PAS KEPLERA", 17, _white);
        Text(new(251, 59), "SEKTOR 01   •   RUCH HANDLOWY AKTYWNY", 11, _muted);
        Text(new(Width - 292, 35), $"{W.Player.Credits:N0}  KR", 24, _cyan);
        Text(new(Width - 292, 60), $"ŁADOWNIA   {W.Player.Ore:00} / {Ship.Capacity}   RUDA", 12, _white);
        if (Game.Paused) { PausePanel(); return; }
        if (Game.MapOpen) { Map(); return; }
        LeftPanel(); RightPanel();
        float cx = Width / 2, cy = Height / 2;
        var reticle = new Color(.7f,.91f,.94f,.5f);
        foreach (int sign in new[] { -1, 1 })
        {
            Line(new(cx + sign * 10, cy), new(cx + sign * 23, cy), reticle);
            Line(new(cx, cy + sign * 10), new(cx, cy + sign * 23), reticle);
        }
        DrawCircle(new(cx, cy), 1.5f, _cyan);
        Text(new(cx - 73, Height - 158), $"{W.Player.Velocity.Length():000}", 38, _white);
        Text(new(cx + 4, Height - 158), "m/s", 15, _muted);
        string mode = W.Player.DockedAt is not null ? "POŁĄCZONO Z DOKIEM" : W.Autopilot ? "AUTOPILOT AKTYWNY" : "STEROWANIE RĘCZNE";
        Center(new(cx, Height - 140), mode, 12, W.Autopilot ? _cyan : _muted);
        if (Game.Cockpit)
        {
            DrawPolyline([new(0, Height-135), new(Width*.22f, Height-176), new(Width*.36f, Height-138)], new Color(.1f,.18f,.23f,.8f), 8, true);
            DrawPolyline([new(Width, Height-135), new(Width*.78f, Height-176), new(Width*.64f, Height-138)], new Color(.1f,.18f,.23f,.8f), 8, true);
        }
        Panel(new(24, Height - 126, Width - 48, 108));
        Text(new(39, Height - 53), "W/S  ciąg     A/D  w bok     Q/Z  góra/dół     PPM + mysz / strzałki  kierunek     SHIFT  dopalacz     SPACJA  hamulec", 12, _muted);
        Text(new(39, Height - 31), "G  autopilot     T  cel     E  dok     B/V  handel     C  kamera     TAB  mapa     P / ESC  pauza", 12, _cyan);
        Center(new(cx, 110), "C  /  " + (Game.Cockpit ? "KAMERA POKŁADOWA" : "KAMERA POŚCIGOWA"), 11, _muted);
    }

    private void LeftPanel()
    {
        Panel(new(26, 130, 244, 203));
        Text(new(42, 157), "PIONIER  /  01", 18, _white);
        Text(new(42, 184), "TRANSPORTOWIEC LEKKI", 10, _muted);
        Line(new(42, 199), new(254, 199), new Color("344956"));
        Text(new(42, 225), $"PUŁAP    {W.Player.Position.Y:0} m", 13, _white);
        Text(new(42, 250), $"RUDA     {W.Player.Ore} / {Ship.Capacity}", 13, _white);
        Bar(new(42, 262, 212, 4), W.Player.Ore / (float)Ship.Capacity, _cyan);
        Text(new(42, 295), W.PlayerDeliveredOre > 0 ? "✓ PIERWSZY KURS UKOŃCZONY" : W.Player.Ore > 0 ? "02 / DOSTARCZ RUDĘ DO HELIOS" : "01 / KUP RUDĘ W KEPLER", 11, W.PlayerDeliveredOre > 0 ? _cyan : _amber);
        Text(new(42, 316), W.Player.Ore > 0 ? "G — lot do celu  •  V — sprzedaż" : "G — lot do celu  •  B — zakup", 11, _muted);
        Panel(new(26, 355, 244, 154));
        Text(new(42, 382), "KURIER  /  AUTONOMICZNY", 12, _amber);
        Text(new(42, 407), $"Ładunek: {W.Trader.Ore} rudy", 13, _white);
        Text(new(42, 430), $"Dostawy sektora: {W.DeliveredOre}", 12, _muted);
        MiniRadar(new(26, 531, 244, 104));
    }

    private void RightPanel()
    {
        var target = W.Target;
        var trade = W.DockedStation(W.Player) ?? target;
        float x = Right + 16;
        Panel(new(Right, 130, 278, 427));
        Text(new(x, 157), "OBIEKT NAWIGACJI", 11, _muted);
        Text(new(x, 189), target.IsMine ? "KEPLER" : "HELIOS", 27, target.IsMine ? _cyan : _amber);
        Text(new(x, 212), target.IsMine ? "KOPALNIA  /  PORT 01" : "RAFINERIA  /  PORT 01", 11, _muted);
        Text(new(x, 241), $"Do portu   {Distance(NV.Distance(W.Player.Position, target.DockPosition))}", 16, _white);
        Line(new(x, 256), new(x + 246, 256), new Color("344956"));
        Text(new(x, 280), $"{(trade.IsMine ? "Zakup" : "Sprzedaż")} rudy   {trade.Price} kr / szt.", 14, _white);
        Text(new(x, 302), $"Magazyn {trade.Ore} rudy  •  {trade.Metal} metali", 12, _muted);
        Text(new(x, 325), W.Player.DockedAt is null ? "Dok: do 75 m / poniżej 25 m/s" : $"Terminal: {(trade.IsMine ? "Kepler" : "Helios")}", 12, _cyan);
    }

    private void DrawMarkers()
    {
        foreach (var station in new[] { W.Mine, W.Refinery })
        {
            if (W.Player.DockedAt == station.Id) continue;
            var world = SpaceGeometry.Convert(station.DockPosition);
            bool behind = Game.FlightCamera.IsPositionBehind(world);
            var p = Game.FlightCamera.UnprojectPosition(world);
            bool onScreen = !behind && p.X > 305 && p.X < Width - 315 && p.Y > 130 && p.Y < Height - 220;
            var color = station.IsMine ? _cyan : _amber;
            bool selected = station == W.Target;
            if (!selected && !onScreen) continue;
            if (behind) p = new Vector2(Width, Height) / 2 - (p - new Vector2(Width, Height) / 2);
            var clamped = new Vector2(Math.Clamp(p.X, 309, Width - 319), Math.Clamp(p.Y, 142, Height - 230));
            if (!onScreen)
            {
                var direction = p - new Vector2(Width / 2, Height / 2);
                if (direction.LengthSquared() < 1) direction = Vector2.Down;
                float angle = direction.Angle();
                var arrow = new[] { new Vector2(9, 0), new Vector2(-7, -6), new Vector2(-7, 6) }.Select(v => clamped + v.Rotated(angle)).ToArray();
                DrawColoredPolygon(arrow, color);
            }
            else
            {
                float r = selected ? 19 : 12;
                foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 })
                {
                    var corner = clamped + new Vector2(sx * r, sy * r);
                    Line(corner, corner - new Vector2(sx * 7, 0), color, 1.5f);
                    Line(corner, corner - new Vector2(0, sy * 7), color, 1.5f);
                }
            }
            string name = station.IsMine ? "KEPLER" : "HELIOS";
            Center(clamped + new Vector2(0, -28), name, 11, color);
            Center(clamped + new Vector2(0, 38), Distance(NV.Distance(W.Player.Position, station.DockPosition)), 12, _white);
        }
    }

    private void MiniRadar(Rect2 rect)
    {
        Panel(rect);
        Text(rect.Position + new Vector2(16, 22), "TAB / MAPA SEKTORA", 11, _muted);
        var center = rect.Position + new Vector2(rect.Size.X / 2, 65);
        Line(center - new Vector2(72, 0), center + new Vector2(72, 0), new Color("294552"));
        foreach (var station in new[] { W.Mine, W.Refinery })
        {
            var relative = station.Position - W.Player.Position;
            var point = center + new Vector2(relative.X, relative.Z) * .025f;
            point = new(Math.Clamp(point.X, rect.Position.X + 12, rect.End.X - 12), Math.Clamp(point.Y, rect.Position.Y + 35, rect.End.Y - 10));
            DrawCircle(point, 3, station.IsMine ? _cyan : _amber);
        }
        DrawCircle(center, 3, _white);
    }

    private Rect2 MapRect => new(Width / 2 - 450, Height / 2 - 270, 900, 540);
    private Vector2 MapPoint(NV v) => MapRect.Position + new Vector2(450 + v.X * .24f, 460 + v.Z * .24f);
    private void Map()
    {
        DrawRect(new Rect2(0, 82, Width, Height - 82), new Color(.015f,.03f,.055f,.92f));
        var rect = MapRect; Panel(rect);
        Text(rect.Position + new Vector2(24, 38), "MAPA SEKTORA / PAS KEPLERA", 21, _white);
        Text(rect.Position + new Vector2(24, 66), "Kliknij stację, aby wybrać cel. G — autopilot. TAB — wróć do lotu.", 14, _muted);
        for (int i = 1; i < 12; i++) Line(rect.Position + new Vector2(i * 75, 90), rect.Position + new Vector2(i * 75, 507), new Color(.13f,.23f,.3f,.3f));
        for (int i = 2; i < 7; i++) Line(rect.Position + new Vector2(20, i * 75), rect.Position + new Vector2(880, i * 75), new Color(.13f,.23f,.3f,.3f));
        foreach (var rock in W.CollisionBodies.Where(b => b.IsAsteroid))
        {
            var point = MapPoint(rock.Position);
            if (point.Y > rect.Position.Y + 95 && rect.HasPoint(point)) DrawCircle(point, rock.Radius * .12f, new Color(.38f,.39f,.42f,.45f));
        }
        DrawDashedLine(MapPoint(W.Mine.Position), MapPoint(W.Refinery.Position), new Color(.2f,.65f,.67f,.5f), 1, 8);
        foreach (var station in new[] { W.Mine, W.Refinery })
        {
            var p = MapPoint(station.Position); var color = station.IsMine ? _cyan : _amber;
            DrawCircle(p, 8, color); if (station == W.Target) DrawArc(p, 16, 0, Mathf.Tau, 40, color, 1.5f, true);
            Text(p + new Vector2(22, -4), station.Name, 16, color);
            Text(p + new Vector2(22, 19), $"Ruda: {station.Ore}  •  Pułap: {station.Position.Y:0} m", 12, _muted);
        }
        var player = MapPoint(W.Player.Position);
        player = new(Math.Clamp(player.X, rect.Position.X + 15, rect.End.X - 15), Math.Clamp(player.Y, rect.Position.Y + 90, rect.End.Y - 20));
        DrawCircle(player, 5, _white); Text(player + new Vector2(12, 32), "PIONIER / TY", 12, _white);
        var trader = MapPoint(W.Trader.Position); DrawCircle(trader, 4, _amber);
        Text(rect.Position + new Vector2(24, 525), $"CEL: {W.Target.Name}    |    {(W.Autopilot ? "AUTOPILOT AKTYWNY" : "G — ROZPOCZNIJ LOT")}", 13, _cyan);
    }
    public override void _Input(InputEvent input)
    {
        if (!Game.MapOpen || Game.Paused || input is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
        foreach (var station in new[] { W.Mine, W.Refinery })
            if (click.Position.DistanceTo(MapPoint(station.Position)) < 60) { W.SelectTarget(station.IsMine); GetViewport().SetInputAsHandled(); Refresh(); break; }
    }
    private void PausePanel()
    {
        DrawRect(new Rect2(0, 82, Width, Height - 82), new Color(.008f,.02f,.035f,.8f));
        Panel(new(Width / 2 - 255, Height / 2 - 120, 510, 255));
        Center(new(Width / 2, Height / 2 - 63), "LOT WSTRZYMANY", 28, _white);
        Center(new(Width / 2, Height / 2 - 18), "Symulacja i handel są zatrzymane.", 16, _muted);
        Center(new(Width / 2, Height / 2 + 12), "P / ESC — wróć do gry", 14, _cyan);
    }
    private void Panel(Rect2 rect) { DrawRect(rect, _panel); DrawRect(rect, new Color(.22f,.38f,.44f,.65f), false, 1); DrawLine(rect.Position, rect.Position + new Vector2(25, 0), _cyan, 2); }
    private void Text(Vector2 p, string s, int size, Color c) => DrawString(Font, p, s, HorizontalAlignment.Left, -1, size, c);
    private void Center(Vector2 p, string s, int size, Color c) => Text(p - new Vector2(Font.GetStringSize(s, HorizontalAlignment.Left, -1, size).X / 2, 0), s, size, c);
    private void Line(Vector2 a, Vector2 b, Color c, float width = 1) => DrawLine(a, b, c, width, true);
    private void Bar(Rect2 r, float fill, Color c) { DrawRect(r, new Color("29414e")); DrawRect(new Rect2(r.Position, new Vector2(r.Size.X * Math.Clamp(fill,0,1), r.Size.Y)), c); }
    private static string Distance(float d) => d >= 1000 ? $"{d / 1000:0.00} km" : $"{d:0} m";
}
