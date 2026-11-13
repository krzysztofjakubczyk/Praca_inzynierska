# Trening sterowania sygnalizacją

Projekt używa Unity ML-Agents 2.3.0-exp.3 (pakiet Unity) i `mlagents` 0.30.0 (Python).

## Przygotowanie środowiska treningowego

1. Otwórz projekt w Unity 2022.3.21f1 i poczekaj, aż Package Manager pobierze pakiet `com.unity.ml-agents`.
2. W środowisku Python 3.10 zainstaluj zależności:

   ```powershell
   python -m pip install -r Training/requirements-mlagents.txt
   ```

3. W Unity otwórz scenę `Assets/Scenes/Sczanieckiej/Dane oszacowane/Oszacowane Logika rozmyta.unity`.
4. Zaznacz `GlobalTrafficLightsSystem` i w `Main Traffic Controller` ustaw `Current Choosed System Type` na `Machine Learning`.
5. Uruchom trening:

   ```powershell
   mlagents-learn Training/traffic_signal.yaml --run-id=traffic_signal
   ```

6. Wróć do Unity i uruchom scenę przyciskiem Play. Trener odbierze obserwacje i decyzje agenta.

Wytrenowany model ONNX będzie zapisany w `results/traffic_signal/TrafficSignal.onnx`. Zaimportuj go do Unity, a następnie przypisz do pola `Trained Policy Model` w komponencie `Main Traffic Controller`. Gdy pole jest puste, agent oczekuje połączenia z procesem treningowym.

## Przestrzeń decyzji agenta

- Obserwacje: dla każdej z trzech faz liczba pojazdów, długość kolejki, blokada zjazdu i informacja, czy faza jest aktualnie aktywna (łącznie 12 wartości).
- Akcje: trzy ciągłe wartości określające preferowany udział zielonego dla każdej fazy. Sterownik przelicza je na czasy, które razem wypełniają budżet cyklu 120 sekund.
- Nagroda: premia za pojazdy opuszczające skrzyżowanie; dodatkowy sygnał za zmniejszenie kolejek i kara za utrzymujące się kolejki.

Tryby statyczny i rozmyty pozostają dostępne. Tryb ML jest wybierany osobno w komponencie kontrolera.

Przy ustawieniach domyślnych cykl 120 sekund obejmuje 16,5 sekundy na światła żółte i odstępy all-red, a pozostałe 103,5 sekundy agent rozdziela między trzy fazy. Każda faza ma co najmniej 5 sekund zielonego, więc agent nie może jej pominąć.
