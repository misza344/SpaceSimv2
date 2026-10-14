# Modele SpaceSim 0.3

Własne modele generowane przez `src/SpaceSim.Game/SpaceGeometry.cs`:

- `PionierCourier.glb` — lekki transportowiec gracza.
- `AtlasFreighter.glb` — kontenerowy statek NPC (w grze skala 1.5).
- `KeplerExtraction.glb` — kopalnia z panelem dokowania.
- `HeliosFoundry.glb` — rafineria.

GLB można otworzyć w Blenderze lub Godocie. Początek układu modelu znajduje
się w jego środku; przód statków jest skierowany w -Z, port stacji w +Z.
Geometria i materiały są własne, bez zasobów Egosoft. Napisy Label3D oraz
animacje runtime nie są częścią eksportu. Gra tworzy modele z kodu,
więc edycję GLB trzeba osobno zintegrować z fabryką modeli, aby ją zobaczyć.

Odtworzenie eksportu po kompilacji:

```bash
source scripts/env.sh
dotnet build SpaceSim.sln --no-restore
"$GODOT" --headless --path src/SpaceSim.Game -- --export-models
```

Materiały GLB zachowują kolory, metaliczność i chropowatość. Projekcja
triplanarna paneli jest funkcją Godota i może wyglądać inaczej po imporcie
w innym programie; do dalszej pracy artystycznej należy przygotować UV.
