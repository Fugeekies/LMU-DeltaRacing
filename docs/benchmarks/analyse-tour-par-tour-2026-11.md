# Analyse de course tour par tour — novembre 2026

Effet sur la chaîne d'import de la lecture des nouveaux attributs de `<Lap>` (`topspeed`, `fuel`, `fuelUsed`, `ve`, `veUsed`, `twfl/twfr/twrl/twrr`, `fcompound`, `rcompound`) et de `<Pitstops>` (PR #69). « Avant » est mesuré sur `main` (commit `c5be22f`), « Après » sur le commit du parser (`9550510`), l'un après l'autre le 08/11/2026 sur la même machine (Windows 10, .NET 10, configuration Release), avec `-p:BenchmarkYears=2` et le filtre `TestCategory=Import`. Visual Studio et l'application tournaient pendant les deux mesures : les temps sont plus élevés que dans les rapports précédents, seule la comparaison entre les deux colonnes compte.

| Mesure | Avant (médiane · meilleur) | Après (médiane · meilleur) | Allocations avant → après |
|---|---:|---:|---:|
| Parsing d'un fichier Course médian (147 Ko) | 2,9 · 2,8 ms | 3,5 · 3,3 ms | 408 → 521 Kio |
| Parsing du plus gros fichier Course (272 Ko) | 5,4 · 4,6 ms | 5,5 · 4,9 ms | 662 → 761 Kio |
| Parsing des 342 fichiers en mémoire | 183,2 · 180,5 ms | 207,0 · 202,0 ms | 65,5 → 79,6 Mio |
| Chargement complet, 342 fichiers, sans essais | 162,9 · 145,9 ms | 207,1 · 204,0 ms | 67,5 → 81,2 Mio |
| Chargement complet, 342 fichiers, avec essais | 165,0 · 144,3 ms | 254,0 · 208,5 ms | 67,8 → 81,3 Mio |
| Chargement complet, 1 710 fichiers, sans essais | 730,2 · 726,8 ms | 1 012,3 · 944,9 ms | 336,4 → 405,7 Mio |
| Chargement complet, 1 710 fichiers, avec essais | 853,1 · 806,1 ms | 1 009,3 · 997,7 ms | 336,7 → 406,2 Mio |

**Lecture :** les allocations, stables d'une exécution à l'autre, augmentent d'environ 21 % : onze attributs de plus par tour (24 317 tours dans les fixtures), lus en chaînes puis convertis, et une `LapTelemetry` par tour. Le parsing seul prend 13 % de plus (meilleur temps). Les écarts du chargement complet (+25 à +40 %) dépassent celui du parsing, ce qui montre le bruit de la machine pendant la mesure ; ils restent sous la seconde pour 1 710 fichiers. Coût jugé acceptable pour une lecture faite une fois au démarrage : aucune optimisation n'est entreprise.

## Reproduction

```powershell
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release -p:BenchmarkYears=2 --filter "TestCategory=Import" --logger "console;verbosity=detailed"
```
