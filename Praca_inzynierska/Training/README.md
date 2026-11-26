# Trening sterowania sygnalizacją

Projekt używa Unity ML-Agents 2.3.0-exp.3 (pakiet Unity) i `mlagents` 0.30.0 (Python 3.9 lub 3.10, venv `.venv-mlagents39`).

## Jak działa agent

Schemat **„przedłuż / przełącz”**. Kolejność faz jest stała, a sterownik sam realizuje czerwone+żółte (1,5 s), żółte (2 s) i all-red (2 s).

| Element | Ustawienie | Dlaczego |
|---|---|---|
| Decyzja | co 5 s zielonego, po `minGreen` 8 s; `maxGreen` 60 s | krótki krok, więc szybkie przypisanie skutku do decyzji; maski uniemożliwiają niebezpieczne akcje |
| Akcje | `0` przedłuż, `1` następna faza | 2 akcje uczą się dużo szybciej niż ciągłe czasy faz |
| Pomijanie pustych faz | `skipEmptyPhases = true` | zmiana na fazę, na której nikt nie czeka, jest pomijana; gdy nikt nie czeka nigdzie indziej, przełączenie jest ignorowane, więc agent nie traci czasu na żółte i all-red |
| Obserwacje (27) | dla każdej z 4 faz: zatrzymane, nadjeżdżające, na czujnikach, blokada wyjazdu, aktywna, **czas od ostatniego zielonego**; globalnie: czas bieżącego zielonego, kolejka przed wjazdem, wszystkie zatrzymane | „czas od ostatniego zielonego” mówi agentowi, jak długo czeka dana faza (zapobiega jej „głodzeniu”) |
| Nagroda | `-(zatrzymane + czekające na wjazd) × czas / (50 × 5 s)`, czyli łączny czas oczekiwania; kara `0.02` za przełączenie | wprost minimalizuje KPI z pracy; skala ~ −0,1…−1,5 na krok jest dobra dla PPO |
| Epizod | 20 min symulacji, losowa godzina (07/12/15/21) i natężenie ×0,7–1,3 | jeden model dla wszystkich profili ruchu, bez przeuczenia na jedną godzinę |
| Koniec epizodu | `EpisodeInterrupted()` | limit czasu to ucięcie, a nie stan końcowy |
| Heurystyka | sterowanie akomodacyjne (actuated) | punkt odniesienia; służy też do demonstracji |

## Szybka ścieżka (zalecana)

### 0. Ustawienia sceny

Scena `Assets/Scenes/Sczanieckiej/Dane oszacowane/Oszacowane Logika rozmyta.unity`:

- `GlobalTrafficLightsSystem` → `Main Traffic Controller` → `Current Choosed System Type = Machine Learning`, pole `Trained Policy Model` puste.
- `TimeManager` → zaznacz `Repeat Hour For Ml Training`.

### 1. Baseline'y (ok. 15 min)

Uruchom tryby `Static`, `Fuzzy` i `Actuated System` z zaznaczonym `Repeat Hour For Ml Training` i przyspieszeniem 10× (klawisz Q). Każda godzina dopisuje wiersz do `Results/kpi_runs.csv`. Zapisz `avg_waiting_per_vehicle_s` dla każdej metody: to poziom, który agent ma pobić.

### 2. Demonstracje heurystyki (ok. 10 min, opcjonalne, ale mocno przyspiesza)

1. W `Main Traffic Controller` zaznacz `Record Demonstrations` (domyślnie 5000 kroków).
2. Naciśnij Play **bez** uruchomionego `mlagents-learn`. Agent jedzie heurystyką actuated, a nagranie trafia do `Assets/Demonstrations/TrafficSignal.demo`. Q przyspiesza.
3. Odznacz `Record Demonstrations`.

### 3. Trening próbny (20–30 min)

```powershell
.\.venv-mlagents39\Scripts\mlagents-learn Training/traffic_signal_bc.yaml --run-id=ks_test --time-scale=20
```

Bez demonstracji użyj `Training/traffic_signal.yaml`. Naciśnij Play w Unity i obserwuj TensorBoard:

```powershell
.\.venv-mlagents39\Scripts\tensorboard --logdir results
```

Na co patrzeć:

- `Environment/Cumulative Reward` powinien rosnąć (z BC startuje od razu blisko heurystyki).
- `Policy/Entropy` powinna powoli spadać.
- `Losses/Value Loss` powinien się stabilizować.
- W `Results/kpi_runs.csv` (wiersze z `mode = training`) kolumna `avg_waiting_per_vehicle_s` powinna maleć.

**Płaski wykres po ~30 tys. kroków → przerwij** i daj znać, zanim zainwestujesz więcej czasu.

### 4. Pełny trening (build, 4 środowiska)

1. `File > Build Settings` → zbuduj scenę do `Builds/Traffic/Traffic.exe`.
2. Uruchom:

   ```powershell
   .\.venv-mlagents39\Scripts\mlagents-learn Training/traffic_signal_bc.yaml --run-id=ks_v1 --env=Builds/Traffic/Traffic.exe --num-envs=4 --no-graphics --time-scale=20
   ```

To ok. 40–50 tys. decyzji na godzinę, więc 300 tys. kroków zajmie ok. 6–7 h. Przerwij wcześniej, jeśli przez ~50 tys. kroków nagroda się nie poprawia (checkpointy co 20 tys.). Wznowienie: dodaj `--resume`.

## Użycie wytrenowanego modelu

Model zapisuje się w `results/<run-id>/TrafficSignal.onnx`. Skopiuj go do `Assets/` i przypisz do `Trained Policy Model`. Modele z wcześniejszych wersji agenta (12 lub 23 obserwacje) nie pasują do obecnego (27).

## Wyniki (KPI)

`Results/kpi_runs.csv` zawiera jeden wiersz na przebieg (epizod) i jest liczony po 400 s rozgrzewki. Kolumny:

- `avg_waiting_per_vehicle_s`, `total_waiting_veh_s`: czas oczekiwania,
- `vehicles_crossed`: przepustowość,
- `max_backlog`: kolejka przed wjazdem do symulacji,
- `collisions` (powinno być 0) i `gridlock_events` (zakleszczenia rozwiązane automatycznie),
- `mode`: `training`, `inference`, `heuristic` albo `rule-based`.

Do porównania w pracy: dla każdej metody co najmniej 5 przebiegów godziny z różnym `Random Seed`, wynik jako średnia ± odchylenie.

## Parametry do ewentualnego strojenia

| Gdzie | Parametr | Kiedy zmienić |
|---|---|---|
| `Main Traffic Controller` | `rewardNormalizationVehicles` (50) | jeśli nagroda na krok w TensorBoard jest dużo poniżej −2, zwiększ (np. 100) |
| `Main Traffic Controller` | `minGreen` / `maxGreen` (8 / 60 s) | wymagania przepisów lub inna geometria |
| `traffic_signal*.yaml` | `beta` (3e-3) | entropia spada do zera w <20 tys. kroków: zwiększ do 5e-3; nie spada wcale: zmniejsz do 1e-3 |
| `traffic_signal*.yaml` | `learning_rate` (3e-4) | nagroda „skacze” i się nie stabilizuje: 1e-4 |
| `traffic_signal*.yaml` | `episode_length` (1200 s) | ocena na pełnej godzinie: 3620 (tylko do ewaluacji) |
