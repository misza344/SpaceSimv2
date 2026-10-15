# Weryfikacja 0.5 — zapis i odczyt

## Sprawdzone lokalnie

- Kompilacja wszystkich plików Core i Game kompilatorem Roslyn z przypiętego .NET SDK 8.0.426; warstwa Game także z generatorem źródeł Godota 4.6.3.
- 31 przypadków testowych (21 dotychczasowych i 10 nowych): wszystkie przeszły. W tym kontynuacja 12 000 ticków po zapisie podczas autopilota daje identyczny stan świata jak nieprzerwany lot; odtwarzane są także trasy i oczekiwanie AI.
- Trzy sektory, zapasy, port dokowania, kredyty, ładunek, podróż do bramy i brak powielania zasobów po odczycie.
- Odrzucenie nieobsługiwanej wersji, brakujących danych, uszkodzonego JSON, nieprawidłowych zapasów i pustej trasy aktywnego autopilota.
- Rzeczywisty zapis pliku, podmiana, zachowanie poprzedniej wersji `.bak`, brak nadpisania przy błędzie walidacji i sprzątanie plików tymczasowych.
- `git diff --check` bez błędów.

## Ograniczenia i dalsza weryfikacja

W tym środowisku standardowy `dotnet` / MSBuild kończy się błędem odczytu informacji procesu z `/proc`. Dlatego kompilację wykonano bezpośrednio przez Roslyn, a przypadki testowe wywołano prostym runnerem refleksyjnym, z oryginalnymi bibliotekami i asercjami xUnit 2.9.3. To nie jest potwierdzenie przejścia standardowego `dotnet test`.

Godot kończy się błędem segmentacji przed uruchomieniem sceny. Lokalnie nie potwierdzono wizualnego wyglądu menu, pełnego testu sceny ani działania Windows EXE. Rozszerzony scenariusz `--smoke-test` sprawdza zapis/odczyt podczas lotu, spaceru po kabinie i bezpieczne odrzucenie uszkodzonego zapisu; używa osobnego katalogu `user://smoke`, aby nie nadpisać zapisu gracza.

Przygotowany lokalnie workflow `.github/workflows/verify.yml` (jeszcze nieuruchomiony) wykonuje standardowy build, testy xUnit, headless Godot i eksport Windows na Ubuntu. Dopiero jego powodzenie potwierdza pełną ścieżkę budowania; Windows EXE nadal wymaga uruchomienia na Windows.

## Kontrola ręczna na Windows

1. Wykonaj zakup rudy, rozpocznij lot do rafinerii, F5, odczekaj i F9. Gra powinna wrócić do miejsca zapisu z aktywnym autopilotem, w pauzie. P wznawia lot.
2. Odwiedź inne sektory, zmień zapasy i zapisz. Zamknij grę, uruchom ponownie i F9. Sprawdź sektor, kredyty, ładunek i zapasy poprzednich sektorów.
3. Zatrzymaj statek, F, odejdź od fotela, rozejrzyj się, F5. Wczytaj i sprawdź pozycję oraz kierunek patrzenia.
4. W menu Esc sprawdź oba przyciski, komunikaty sukcesu/błędu i powrót P/Esc. F9 bez pliku ma zgłosić brak zapisu.
5. Zachowaj kopię pliku, uszkodź JSON i wczytaj. Bieżący świat ma pozostać bez zmian. Ręcznie przywróć `.bak` zgodnie z README.
