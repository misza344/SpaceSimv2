# Kierunek techniczny — wersja 0.5

14.10.2026: na życzenie użytkownika zmieniono kierunek z 2D na **3D na PC**. Nadal używamy Godota 4.6.3 .NET oraz C#. Nie ma wymogu obsługi telefonów. Grę udostępniamy jako ZIP portable dla Windows x64, z własnym runtime .NET; użytkownik nie potrzebuje instalacji ani administratora.

## Architektura

`SpaceSim.Core` jest autorytatywnym stanem świata, niezależnym od Godota. Używa `System.Numerics.Vector3`, stałego kroku 1/60 s, identyfikatorów jednostek i stacji, całkowitych liczników towarów oraz `decimal` dla kredytów. Godot odczytuje ten stan, rysuje scenę i przekazuje polecenia. Produkcja i decyzje handlowe zachodzą rzadziej niż aktualizacja ruchu.

Ruch obsługuje obrót, pochylenie, ciąg, silniki boczne i pionowe, dopalacz i hamowanie. To wspomagany model lotu, bez pełnej fizyki orbitalnej i obrotu wokół osi podłużnej. Kolizja gracza sprawdza odcinek między pozycjami względem sfer przeszkód, aby ograniczyć przenikanie przy dużej prędkości. Bryły kolizji są przybliżeniami, a nie dokładnymi siatkami. W tym etapie kontakt nie zadaje obrażeń.

Port ma osobną pozycję przed stacją. Gracz i AI faktycznie przemieszczają się do celu. Autopilot korzysta z korytarzy wyjścia, przelotu nad pasem asteroid i podejścia do portu. Planer jest dostosowany do trzech przygotowanych sektorów; nie jest ogólnym algorytmem nawigacji po dowolnie generowanym świecie. Ruch ręczny wyłącza autopilota. Statki nie zderzają się ze sobą; Kurier ma oddzielny punkt dokowania obok portu gracza.

Warstwa Godota: kamera pościgowa i pokładowa, proceduralne modele statków i stacji, zdeformowane asteroidy, sferyczna planeta z shaderem terenu i atmosfery, shader gwiazd, oświetlenie oraz HUD i mapa sektora. Renderer Compatibility (OpenGL 3.3) pozostaje rozsądnym punktem startowym dla różnych komputerów. Nie zakładamy wydajności bez pomiarów na sprzęcie użytkownika.

## Zakres gospodarki

Kopalnia tworzy rudę z abstrakcyjnego złoża co 2 sekundy; rafineria przetwarza 2 rudy na metal co 3 sekundy. Handel przenosi towar i pieniądze; produkcja zatrzymuje się przy braku wejścia lub pełnym magazynie. To jawne uproszczenie proponowanych w researchu ośmiu towarów. Zachowujemy bilans zasobów, zamiast dublować ładunki na potrzeby grafiki.

## Następne kroki

1. Odbiorcy metalu i dalsze etapy produkcji zamykające obieg gospodarczy.
2. Rozszerzenie zapisu o rezerwacje, kiedy zostaną wprowadzone; podstawowy zapis świata działa w 0.5.
3. Drugi statek gracza, zlecenia i rezerwacje magazynów, towarów oraz pieniędzy.
4. Symulacja nieaktywnych sektorów i rozbudowa sieci bram.
5. Walka, straty dostaw i eskorta; rozbudowa modeli i efektów po potwierdzeniu pętli gry.

Oryginalny research pozostaje dokumentem źródłowym. Nie deklarujemy deterministyczności ruchu float pomiędzy różnymi platformami ani maksymalnej liczby jednostek. Test powtarzalności dotyczy jednego środowiska.


## Eksploracja 0.4

`Galaxy` przechowuje trzy instancje `World`. Skok wymaga bliskości bramy,
małej prędkości i odłączenia od doku. Przenosi kredyty, rudę, postęp dostaw
oraz ustawienie wspomagania. Zapasy stacji pozostają w poszczególnych światach;
nieaktywne światy są wstrzymane. Godot przebudowuje tylko wizualny sektor.

Kabina działa w lokalnym układzie statku. Ograniczenia X/Z pilota odpowiadają
ścianom i zabudowie; wysokość oczu jest stała. Wyjście jest blokowane podczas
lotu autopilota i przy prędkości ponad 0.5 m/s. Chodzenie blokuje komendy
pilota; powrót wymaga podejścia do fotela. To wnętrze, bez EVA i bez wnętrz stacji.

## Zapis 0.5

`SaveStorage` serializuje jawny, wersjonowany model DTO w JSON. `World.Capture`
i `World.Restore` zachowują liczniki czasu, kolejki punktów trasy i oczekiwanie AI.
`Galaxy.Restore` tworzy nową galaktykę po walidacji; bieżący obiekt nie jest częściowo
modyfikowany w razie błędu. Geometria statyczna jest odtwarzana z indeksu sektora.
Zmiana generatora geometrii wymaga decyzji o migracji i wersji formatu.
Warstwa Godota zapisuje tryb lotu/kabiny, pozycję pilota i widok. Po odczycie
zawsze włącza pauzę, zeruje wejście myszy i przebudowuje wizualny sektor.
Rezerwacje nie istnieją jeszcze w modelu świata. Nie dodano autosave ani menu slotów.
