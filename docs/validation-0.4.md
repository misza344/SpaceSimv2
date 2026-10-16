# Weryfikacja 0.4 — 15.10.2026

- Kompilacja: 0 błędów, 0 ostrzeżeń C#.
- 21/21 testów xUnit: ekonomia, kolizje, dryf i wspomaganie, anulowanie
  nawigacji, ograniczenia skoku, zachowanie ładunku/kredytów/stacji po obiegu
  trzech sektorów, dostępność obu stacji i bramy w każdym sektorze.
- Scena Godota: zakup i sprzedaż, AI, wyjście z fotela, granice kabiny,
  blokada sterowania na piechotę, powrót wyłącznie przy fotelu, trzy skoki.
- Kontrola w rzeczywistym oknie OpenGL/Mesa na Linux: spacer i powrót do
  pilotowania, podejście do bramy i przejście do Vesper, HUD i widok kabiny.
- Portable Windows x64: dołączony runtime .NET, sprawdzone PE, CRC i SHA-256.
  Sam EXE nie był uruchamiany na Windows; brak pomiaru na sprzęcie użytkownika.
- Sterownik llvmpipe zgłasza brak obsługi przełączania V-Sync.
- Niektóre uruchomienia headless zgłosiły ObjectDB przy wyjściu; ponowne
  uruchomienia z --verbose zakończyły się czysto. Ostrzeżenie nie unieważnia
  asercji sceny, ale wymaga obserwacji przy dalszej rozbudowie cyklu zasobów.

Ograniczenia zakresu: trzy sektory w pętli bram, zawieszone nieaktywne światy,
chodzenie tylko po kabinie, uproszczone granice ruchu pieszego, brak EVA,
wnętrz stacji i zapisu gry na dysku. Instrumenty 3D są dekoracyjne.

## Otwarty problem renderowania HUD

Kontrola zrzutów F12 oraz `--travel-review` ujawniła znikające fragmenty
napisów po podróżach w środowisku Linux/Mesa llvmpipe. Testy logiki nie
wykrywają tego błędu. Zmiana czcionki, MSAA, buforów i osobny viewport
nie dały powtarzalnej poprawy; eksperymentalne zmiany usunięto.
Vulkan nie mógł zostać przetestowany (brak VK_KHR_surface).
Problem pozostaje otwarty. Nie potwierdzono, czy występuje także na Windows.
Nie należy traktować tego wydania jako wizualnie pozbawionego błędów.

Trzy sceny sektorów są buforowane i zwalniane przy zamknięciu. F12 zapisuje
obraz bezpośrednio z renderera. `--travel-review` zapisuje cztery kolejne
wizyty w katalogu danych gry / travel-review i ułatwia odtworzenie problemu.
