# Kierunek techniczny

Ustalenia rozmowy: gra 2D na PC, bez wymaganego wsparcia telefonów. Realizujemy Godot + C#, z własnym rdzeniem symulacji. Nie budujemy od podstaw renderera ani edytora. Windows to proponowana pierwsza platforma wydania; obecne testy wykonano na Linux.

## Pierwszy wycinek

Jeden sektor, kopalnia, rafineria, statek gracza i niezależny transportowiec AI. Kopalnia tworzy rudę z abstrakcyjnego złoża co 2 sekundy, rafineria przetwarza 2 rudy na metal co 3 sekundy. Handel zmienia stan magazynów i kont obu stron, ceny reagują na zapas, a produkcja zatrzymuje się przy brakach lub pełnym magazynie. To jawne uproszczenie wcześniejszej propozycji ośmiu towarów.

Rdzeń używa stabilnych identyfikatorów, własnego stanu świata i System.Numerics.Vector2. Nie przechowuje węzłów Godota. Interfejs wywołuje polecenia; nie utrzymuje osobnych kopii ładunków i pieniędzy. Krok wynosi 1/60 s, gospodarka aktualizuje się w wielokrotnościach kroku. Kredyty używają decimal, towary liczników całkowitych. Testy sprawdzają bilans transferów i jawnie wytworzone/zużyte zasoby.

Nie deklarujemy jeszcze deterministyczności pomiędzy różnymi platformami: ruch używa float. Test powtarzalności dotyczy tego samego środowiska i identycznej sekwencji wejść.

## Następne etapy

1. Dodać odbiorców metalu i dalszą produkcję, aby zamknąć obieg gospodarczy.
2. Zapis/odczyt z wersją formatu oraz test wznowienia świata w trakcie transportu.
3. Własny drugi statek, rozkazy gracza i rezerwacje towaru, budżetu oraz magazynu.
4. Wiele sektorów i spójne przejście między symulacją lokalną i uproszczoną.
5. Walka, straty dostaw i eskorta wpływająca na gospodarkę.

Wydajność i balans należy mierzyć przy rosnącej liczbie jednostek. Wersja 0.1 nie jest benchmarkiem dużej galaktyki.
