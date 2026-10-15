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
