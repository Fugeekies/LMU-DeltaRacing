# Choix des sessions importées — novembre 2026

Effet sur la chaîne d'import de la lecture de `<Setting>` et de l'option `LMU:LoadRaceWeekendSessions` (décochée par défaut : les 6 courses « Race Weekend » des fixtures ne sont plus importées). « Avant » est mesuré sur `main` (commit `dca44d4`), « Après » sur le commit de l'option (`a23d9ae`), l'un après l'autre le 13/11/2026 sur la même machine (Windows 10, .NET 10, configuration Release), avec `-p:BenchmarkYears=2` et le filtre `TestCategory=Import`.

| Mesure | Avant (médiane · meilleur) | Après (médiane · meilleur) | Allocations avant → après |
|---|---:|---:|---:|
| Parsing d'un fichier Essais médian (20 Ko) | 0,74 · 0,68 ms | 0,82 · 0,75 ms | 70,3 → 70,1 Kio |
| Parsing d'un fichier Course médian (147 Ko) | 3,2 · 3,1 ms | 3,9 · 3,6 ms | 520,9 → 520,8 Kio |
| Parsing du plus gros fichier Course (272 Ko) | 5,2 · 4,7 ms | 5,4 · 5,0 ms | 761,1 → 760,9 Kio |
| Parsing des 342 fichiers en mémoire | 198,2 · 189,5 ms | 196,6 · 191,3 ms | 79,6 → 79,6 Mio |
| Chargement complet, 342 fichiers, sans essais (sessions) | 188,5 · 162,1 ms (208) | 189,1 · 169,1 ms (202) | 81,2 → 81,3 Mio |
| Chargement complet, 342 fichiers, avec essais (sessions) | 192,5 · 191,5 ms (342) | 193,1 · 177,0 ms (336) | 81,5 → 81,2 Mio |
| Chargement complet, 1 710 fichiers, sans essais | 876,4 · 875,4 ms | 867,3 · 841,2 ms | 406,1 → 406,4 Mio |
| Chargement complet, 1 710 fichiers, avec essais | 969,9 · 928,1 ms | 953,8 · 929,0 ms | 406,8 → 405,8 Mio |

**Lecture :** une seule valeur de plus par fichier (`<Setting>`) : les allocations ne bougent pas et les temps restent dans le bruit de mesure (le parsing des 342 fichiers est identique ; les écarts sur un fichier isolé, dans les deux sens, ne se retrouvent pas sur le lot). Six sessions de moins sont conservées (208 → 202 sans essais), mais leurs fichiers sont toujours lus en entier avant d'être écartés : aucun gain de temps. Écarter ces fichiers dès l'en-tête du XML est la tâche « Optimisation des essais » du jalon.

## Reproduction

```powershell
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release -p:BenchmarkYears=2 --filter "TestCategory=Import" --logger "console;verbosity=detailed"
```
