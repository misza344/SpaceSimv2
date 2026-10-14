# Weryfikacja 0.3 — 14.10.2026

- `scripts/check.sh`: kompilacja bez ostrzeżeń i błędów, 14/14 testów xUnit.
- Test sceny: autopilot do kopalni, zakup, przelot i sprzedaż w rafinerii;
  dostawy AI i produkcja metalu, zero kolizji gracza.
- Rzeczywiste renderowanie OpenGL Compatibility na Linux/Mesa llvmpipe:
  cztery podglądy modeli oraz lot, przełączenie C i ukrywanie HUD przez H.
  Brak błędów shaderów. Sterownik programowy zgłasza brak kontroli V-Sync.
- GLB zaimportowane w Blenderze 4.3.2, geometria nie zawiera NaN/Infinity:
  Pionier 3460 trójkątów, Atlas 3828, Kepler 4084, Helios 20168.
- Eksport Windows x64 0.3: poprawny nagłówek PE, dołączony .NET 8.0.32,
  licencje i instrukcja, poprawne CRC ZIP i suma SHA-256.
- Windows EXE nie był uruchamiany na Windowsie; nie zmierzono wydajności
  na komputerze użytkownika. Liczby trójkątów nie są pomiarem wydajności.

Obrazy w `docs/art/` pochodzą z renderera gry. Widok modeli można odtworzyć
uruchamiając grę z `-- --art-review` w środowisku z działającym ekranem.
Panele w kokpicie są dekoracyjnymi instrumentami; odczyty symulacji są w HUD.
