# Kierunek wizualny SpaceSim 0.3

## Zakres researchu i źródła

Punktem wyjścia jest wcześniejszy dokument `SpaceSim_Research_X4_i_plan.md`
i ogólna znajomość oprawy X4: Foundations. Próby pobrania oficjalnej galerii
Egosoft, karty Steam i Wikipedii 14.10.2026 zakończyły się HTTP 403.
Nie wykonano więc nowej analizy aktualnych screenshotów ani porównania
wersji gry. Poniższe obserwacje są syntezą kierunku artystycznego,
a nie udokumentowanym przeglądem najnowszej wersji X4.

Źródła do późniejszej weryfikacji wizualnej:
- https://www.egosoft.com/games/x4/screenshots_en.php — oficjalna galeria.
- https://store.steampowered.com/app/392160/X4_Foundations/ — materiały producenta na Steam.
- `SpaceSim_Research_X4_i_plan.md` — wcześniejszy research rozgrywki i modułowych stacji.

## Co jest charakterystyczne dla X4

- Przemysłowa skala: stacje są zespołami połączonych modułów, a doki,
  okna i światła pomagają odczytać wielkość konstrukcji.
- Sylwetka pokazuje funkcję: transportowiec eksponuje ładunek, jednostka
  wojskowa uzbrojenie, fabryka zbiorniki i bloki produkcyjne.
- Język konstrukcji zależy od frakcji: od użytkowych, kanciastych brył
  po bardziej opływowe i geometryczne formy. SpaceSim wybiera własny
  przemysłowy kierunek, nie kopiuje konkretnej frakcji ani statku.
- Duże, spokojne powierzchnie kontrastują z mniejszymi panelami,
  żebrami, antenami, światłami i technicznymi wnękami.
- Kierunkowe światło wydobywa metal i bryłę; planety oraz mgławice
  budują skalę sektora, a kokpit i HUD wspierają orientację.

## Wdrożenie w SpaceSim

| Element | Własny projekt |
|---|---|
| Pionier | Ścięty kadłub, mostek, gondole silników, radiatory, światła nawigacyjne i pasy ochry |
| Atlas | Wspólna rodzina konstrukcyjna, cztery zewnętrzne kontenery z żebrami mocującymi |
| Kepler | Kratownicowe ramiona, zasobniki rudy, panele fotowoltaiczne, pierścień mieszkalny |
| Helios | Pionowa sylwetka, cztery zbiorniki, radiatory, centralny moduł technologiczny |
| Dok | Pokład, pasy ostrzegawcze, oznaczenia portu i narożne światła podejścia |
| Asteroidy | Gęstsza geometria, proceduralne zróżnicowanie skały i żyły mineralne |
| Sektor | Ciepłe światło główne, chłodne doświetlenie, dysk gwiazdy i subtelniejsza mgławica |

Materiały korzystają z metaliczności i chropowatości. Małe światła emisyjne
pozostają akcentami; nie zastępują całych powierzchni kadłuba.
Rendering Compatibility/OpenGL 3.3 zachowuje możliwość uruchomienia portable
na starszych komputerach. Bez wymagania Vulkan, ray tracingu czy instalacji.

Modele powstają z kodu `SpaceGeometry.cs`; ich wersje GLB są w `assets/models`.
Nie pobierano ani nie kopiowano modeli, tekstur lub znaków z X4.
GLB zawierają geometrię i materiały PBR. Napisy stacji są generowane przez
Godota i nie wchodzą do GLB. Animacje pierścieni i silników realizuje gra.
Kolizje nadal korzystają z uproszczonych brył, a nie trójkątów modeli.
