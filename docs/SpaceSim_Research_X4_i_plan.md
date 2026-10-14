# SpaceSim — research X4: Foundations i plan projektu

Data: 13.10.2026. Etap: analiza i projektowanie, bez kodu gry.

## 1. Kierunek

SpaceSim ma odtwarzać główne zależności rozgrywki X4: samodzielny pilotaż, rozwój majątku, logistykę, produkcję i dowodzenie. Własne nazwy, statki, mapa, oprawa i fabuła. Najważniejsza obietnica: gracz może zacząć jednym statkiem i stopniowo zarządzać przedsiębiorstwem oraz flotą w świecie, który działa samodzielnie.

Nie ustalono jeszcze: 2D czy 3D, platformy docelowej, sterowania mobilnego ani technologii. Poniższe liczby to propozycje zakresu, nie ustalenia użytkownika i nie parametry X4.

## 2. Co potwierdzają źródła o X4

| Obszar | Ustalenie | Źródło |
|---|---|---|
| Skala rozgrywki | Bezpośredni pilotaż różnych statków łączy się ze zdalnymi poleceniami z mapy i rozwojem imperium. | S1 |
| Świat | Frakcje rozbudowują stacje i reagują na gospodarkę oraz zagrożenia. | S1 |
| Handel | Zapasy stacji wpływają na ceny. Informacje o ofertach mogą się dezaktualizować; rozpoznanie ma wartość gospodarczą. | S2 |
| Surowce | Energia, minerały i gazy są podstawą produkcji; potrzebne są właściwe jednostki i magazyny. | S2 |
| Automatyzacja | Rozkazy jednorazowe, zachowania domyślne i przypisania do dowódcy są odrębnymi mechanizmami. | S3 |
| Stacje | Modułowa budowa wymaga materiałów i wykonawcy. Menedżerowie organizują podległe jednostki; ustawienia obejmują magazyny, ceny i reguły handlu. | S4 |
| Walka | Osłony, kadłub i uzbrojenie tworzą podstawę starć; większe jednostki mają także osobne elementy wyposażenia możliwe do atakowania. | S5 |
| Przejmowanie | Opuszczone małe jednostki można przejąć; abordaż dużych obejmuje podejście, wejście do kadłuba i walkę załóg. | S6 |
| Terytorium | Kontrola sektorów jest powiązana z infrastrukturą administracyjną. | S6 |
| Pilotaż | Wspomaganie lotu upraszcza sterowanie, a tryb podróżny służy pokonywaniu większych odległości. | S7 |

### Aktualność

Potwierdzona duża aktualizacja: Empire Update 9.00, wydana 10.06.2026. Wprowadza m.in. rozkazy priorytetowe, przebudowę regionów wydobycia, zmiany AI, balans walki i poprawki mapy. Złoża są bardziej lokalne; po wyczerpaniu zasoby mogą pojawiać się gdzie indziej w regionie. Sondy wskazują obszary pracy, zamiast zwiększać efektywność wydobycia. Źródło: S8. To punkt odniesienia, a nie potwierdzenie numeru najnowszego hotfixa.

Aktualizacja 8.00 rozwinęła dyplomację, w tym ambasadę i agentów. To dalszy poziom złożoności, a nie warunek pierwszego prototypu. Źródło: S9.

Podręcznik na domenie Egosoft jest wiki społecznościowym i zawiera także stare lub oznaczone jako niepotwierdzone fragmenty. W konflikcie dotyczącym zmian 9.00 pierwszeństwo mają komunikaty producenta. Nie ustalono dokładnych wewnętrznych algorytmów, częstotliwości aktualizacji AI ani wszystkich wyjątków ekonomii X4. Projekt poniżej jest naszą propozycją, nie opisem kodu Egosoft.

## 3. Główna pętla SpaceSim — propozycja

1. Odkryj sektor, stację i ofertę lub złoże.
2. Wykonaj pierwszy handel, wydobycie albo zlecenie.
3. Kup drugi statek i zatrudnij pilota.
4. Zleć mu stałą pracę; sam zajmij się eksploracją lub walką.
5. Wybuduj fabrykę i zapewnij jej dostawy.
6. Zorganizuj eskortę, aby chronić dochód.
7. Rozbuduj produkcję i flotę, przechodząc od wykonywania zadań do zarządzania.

Gracz powinien móc wrócić do samodzielnego pilotażu na każdym etapie. Mapa i kokpit korzystają z tego samego stanu świata.

## 4. Ekonomia — proponowany model

Pierwsze 8 towarów: energia, ruda, krzem, metale, elektronika, części kadłuba, części napędów, części uzbrojenia. Uproszczone receptury:

| Wejście | Wyjście |
|---|---|
| Energia + ruda | Metale |
| Energia + krzem | Elektronika |
| Energia + metale | Części kadłuba |
| Energia + metale + elektronika | Części napędów |
| Energia + metale + elektronika | Części uzbrojenia |
| Części kadłuba + napędów + uzbrojenia | Wyposażony statek |

Ilości i czasy wymagają późniejszego balansu. Elektrownia produkuje energię bez wejściowego towaru, z ograniczoną wydajnością. Dzięki temu podstawowy łańcuch nie wymaga sam siebie do uruchomienia.

Zasady:

- Każdy towar znajduje się w konkretnym magazynie albo ładowni.
- Dostawa trwa; kontrakt rezerwuje towar, pieniądze i miejsce docelowe.
- Rezerwacja ma termin ważności i jest zwalniana po anulowaniu, zniszczeniu statku lub utracie dostępu.
- Produkcja rusza tylko przy komplecie wejść i wolnym miejscu na wynik.
- Ceny reagują na zapas względem docelowego poziomu, w ustalonych granicach.
- Handlarz ocenia oczekiwany zysk na czas podróży, dostępność, ryzyko i zgodność ładowni.
- Straty statków, budowa oraz wyposażanie floty tworzą zapotrzebowanie na towary.
- Na starcie świat dostaje zapasy i środki rozruchowe. Ewentualne późniejsze dopłaty frakcji muszą być jawnie określoną regułą symulacji.
- Handel NPC może nasycić rynek: automat nie gwarantuje wiecznego zysku.

Najważniejsza próba: odcięcie dostaw do stoczni powinno opóźnić budowę jednostek. Przywrócenie transportu powinno odblokować kolejkę.

## 5. AI i dowodzenie — propozycja

Trzy warstwy:

- Statek: podróż, dokowanie, wydobycie, handel, walka, odwrót.
- Menedżer stacji lub dowódca floty: przydziela zadania i zapotrzebowanie podwładnym.
- Frakcja: wybiera inwestycje, uzupełnia straty i ustala cele militarne.

Jednostka ma kolejkę rozkazów oraz zachowanie domyślne. Rozkaz awaryjny może przerwać zwykłą pracę. Po zakończeniu wraca do niej, jeżeli nadal jest wykonalna.

Minimum rozkazów: leć, dokuj, kup, sprzedaj, wydobywaj, eskortuj, atakuj, broń obszaru, wycofaj się, powtarzaj trasę. Ucieczka zależy od progu osłon/kadłuba i dostępności bezpiecznego celu. Nie powinna prowadzić ponownie przez tę samą blokadę.

Każdy automat pokazuje powód oczekiwania, np. brak odbiorcy, zajęty dok, niewystarczający budżet, niebezpieczna trasa. To wymóg funkcjonalny, nie dodatek kosmetyczny.

## 6. Świat i walka — propozycja

Mapa jest grafem sektorów połączonych bramami. Sektor zawiera stacje, lokalne obszary surowców, ruch handlowy i patrole. Rozpoznanie odsłania obiekty; podgląd rynku wymaga aktualnej informacji.

Pierwsze role statków: zwiadowca, myśliwiec, górnik, transportowiec. Później korweta, niszczyciel, lotniskowiec i jednostka zaopatrzeniowa. Każda rola musi mieć cel ekonomiczny lub taktyczny; samo zwiększanie punktów życia nie wystarczy.

Walka początkowo obejmuje osłony, kadłub, zasięg, prędkość pocisku, namierzanie i odwrót. Później dochodzą wieżyczki, podsystemy, rakiety, hangary i abordaż. Zniszczenie transportowca usuwa jego ładunek lub zamienia część w odzyskiwalny wrak; nie dostarcza towaru do celu.

## 7. Symulacja i wydajność — propozycja techniczna

- Stan świata niezależny od grafiki i UI.
- Stały krok symulacji; rysowanie może działać z inną częstotliwością.
- Bliskie otoczenie: dokładny ruch, pociski i kolizje.
- Dalekie sektory: uproszczone podróże, produkcja i rozstrzyganie walk, ale te same zapasy, jednostki i kolejki.
- Zmiana sektora albo otwarcie mapy nie tworzy ponownie statków i nie zeruje gospodarki.
- Jeden identyfikator jednostki we wszystkich trybach symulacji.
- Ograniczona liczba drogich decyzji AI w jednej aktualizacji; rozłożenie ich w czasie.
- Zapis zawiera wersję formatu, stan losowości, ładunki, zlecenia, rezerwacje, produkcję, relacje i postęp budowy.

Dokumentacja producenta potwierdza istnienie walki w trybie low attention w X4 (S10), ale powyższy podział i implementacja są własną rekomendacją. Należy sprawdzić, czy przejście między trybami nie daje przewagi w walce ani nie duplikuje towarów.

Jeśli wybierzemy przeglądarkę, potrzebne będzie jawne zachowanie po ukryciu karty: pauza albo kontrolowane nadrabianie symulacji. Nie zakładamy automatycznie niezawodnej pracy gry w tle telefonu.

## 8. Pierwsza grywalna wersja — proponowany zakres

Cel: pełny mały obieg gospodarczy połączony z pilotażem i walką.

| Element | Zakres początkowy |
|---|---|
| Świat | 4 sektory z bramami |
| Strony | 2 frakcje gospodarcze i piraci |
| Infrastruktura | Około 10–12 stacji obsługujących kompletny łańcuch |
| Towary | 8 opisanych wyżej |
| Jednostki | 4 role, około 30–50 aktywów NPC jako punkt startowy do pomiarów |
| Własność | Kilka statków gracza, przełączanie kontroli, piloci AI |
| Stacja gracza | Dok, magazyn i jeden moduł produkcyjny |
| Zlecenia | Dostawa, rozpoznanie, ochrona |
| Interfejs | HUD, mapa, handel, majątek, rozkazy i przyczyny blokad |
| Zapis | Zapis i odczyt całego świata |

Limity jednostek są hipotezą zakresu, a nie gwarancją wydajności. Weryfikujemy je na docelowym urządzeniu.

## 9. Kolejność prac i kryteria ukończenia

1. Symulacja gospodarcza bez oprawy: wszystkie etapy produkcji działają, zapasy nigdy nie są ujemne.
2. Pilotaż i dokowanie: gracz przewozi rzeczywisty ładunek pomiędzy stacjami.
3. Własna druga jednostka: samodzielnie realizuje cykl zarobkowy, gdy gracz robi coś innego.
4. Walka i ryzyko: utrata dostawy wpływa na produkcję; eskorta może temu zapobiec.
5. Stacja gracza: buduje się z dostaw i wznawia produkcję po usunięciu braków.
6. Mapa i raporty: wszystkie jednostki mają czytelny status i dają się kontrolować.
7. Zapis, stabilność i oprawa: wczytanie zachowuje rezerwacje, kolejki oraz własność; rozgrywka przechodzi dłuższą próbę bez zablokowania podstawowego obiegu.

Scenariusz odbiorczy: gracz dostarcza części, stocznia buduje kupionego górnika, górnik zarabia na rudzie, gracz buduje fabrykę, piraci zakłócają dostawę, a eskorta przywraca logistykę. Ten scenariusz musi działać także podczas przebywania gracza w innym sektorze.

## 10. Rozwój po prototypie

Modułowe kompleksy → większe floty i zaopatrzenie → przejmowanie sektorów → abordaż i ratownictwo → dyplomacja → więcej frakcji, technologii i fabuły. Chodzenie po pokładach i stacjach można dołożyć po potwierdzeniu głównej pętli. Rozbudowane wnętrza, multiplayer i duża proceduralna galaktyka nie są częścią pierwszego zakresu.

Największe ryzyka: zakleszczenia logistyki, nasycenie gospodarki, nieczytelne AI, różnice walki bliskiej i dalekiej, zbyt szybka dominacja jednej frakcji oraz rosnący koszt symulacji. Potrzebny jest panel diagnostyczny przepływów, braków, strat i czasu decyzji AI.

## 11. Źródła

Sprawdzone 13.10.2026. Komunikaty Egosoft są źródłami producenta; podręcznik jest społecznościową dokumentacją hostowaną przez Egosoft.

- S1 — [X4: Foundations — oficjalny opis](https://www.egosoft.com/games/x4/info_en.php)
- S2 — [Trading And Mining](https://wiki.egosoft.com/X4%20Foundations%20Wiki/Manual%20and%20Guides/X4%3A%20Foundations%20Manual/Trading%20And%20Mining/)
- S3 — [Orders, Behaviours and Assignments](https://wiki.egosoft.com/X4%20Foundations%20Wiki/Manual%20and%20Guides/Objects%20in%20the%20Game%20Universe/Orders%2C%20Behaviours%20and%20Assignments/)
- S4 — [Station Building And Management](https://wiki.egosoft.com/X4%20Foundations%20Wiki/Manual%20and%20Guides/X4%3A%20Foundations%20Manual/Station%20Building%20And%20Management/)
- S5 — [Combat And Weapons](https://wiki.egosoft.com/X4%20Foundations%20Wiki/Manual%20and%20Guides/X4%3A%20Foundations%20Manual/Combat%20And%20Weapons/)
- S6 — [Capturing Ships And Sectors](https://wiki.egosoft.com/X4%20Foundations%20Wiki/Manual%20and%20Guides/X4%3A%20Foundations%20Manual/Capturing%20Ships%20And%20Sectors/)
- S7 — [Piloting And Travel](https://wiki.egosoft.com/X4%20Foundations%20Wiki/Manual%20and%20Guides/X4%3A%20Foundations%20Manual/Piloting%20And%20Travel/)
- S8 — [Empire Update 9.00 — wydanie i zmiany](https://www.egosoft.com/news/archive/2026June_en.php)
- S9 — [Diplomacy Update — opis systemu](https://www.egosoft.com/news/archive/2025July_en.php)
- S10 — [Egosoft — zapowiedź ulepszeń symulacji low attention](https://www.egosoft.com/news/archive/2022December_en.php)

## 12. Najbliższa decyzja

Wybrać perspektywę i platformę: 3D z kokpitem czy 2D z widokiem z góry; komputer czy również telefon. Rdzeń gospodarczy może pozostać podobny, ale sterowanie, interfejs i budżet wydajności będą różne. Nie rozpoczęto implementacji i nie wykonano testów gry.
