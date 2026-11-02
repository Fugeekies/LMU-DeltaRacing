# Identité et chronologie des sessions — novembre 2026

Effet sur la chaîne d'import de la lecture de la date de phase, de l'identifiant calculé et du refus des doublons (PR #52). « Avant » vient du rapport du 29/10/2026, « Après » du second run du 02/11/2026, sur la même machine (Windows 10, 8 processeurs logiques, .NET 10.0.14, configuration Release), avec `-p:BenchmarkYears=2` et le filtre `TestCategory=Import`.

| Mesure | Avant (médiane · meilleur) | Après (médiane · meilleur) | Allocations avant → après |
|---|---:|---:|---:|
| Parsing des 342 fichiers en mémoire | 143,6 · 134,7 ms | 165,8 · 147,5 ms | 31,9 → 32,1 Mo |
| Chargement complet, 342 fichiers, sans essais | 98,5 · 95,1 ms | 112,4 · 94,4 ms | 32,6 → 32,7 Mo |
| Chargement complet, 342 fichiers, avec essais | 111,4 · 87,7 ms | 131,2 · 129,2 ms | 32,6 → 32,8 Mo |
| Chargement complet, 1 710 fichiers, sans essais | 505,5 · 488,1 ms | 505,3 · 501,8 ms | 162,7 → 163,2 Mo |
| Chargement complet, 1 710 fichiers, avec essais | 625,3 · 501,5 ms | 555,9 · 529,1 ms | 163,0 → 163,4 Mo |

**Lecture :** les allocations, stables d'une exécution à l'autre, augmentent de moins de 1 %. Les temps varient surtout d'un run à l'autre : le chargement de 342 fichiers sans essais valait 131,5 ms au premier run du 02/11 et 112,4 ms au second, et les écarts changent de sens selon les lignes. Aucune régression n'est donc mesurable au-dessus de ce bruit. Le coût théorique se limite à un SHA-256 d'une courte chaîne par session.

**Changement de méthode :** au-delà de 342 fichiers, les benchmarks copient les fixtures. Une copie exacte étant désormais un doublon, chaque copie reçoit un lieu qui lui est propre. Le nombre de sessions stockées reste celui d'avant (1 040 sans essais, 1 710 avec), donc les mesures restent comparables, mais chaque copie est maintenant réécrite pour préparer le test, hors de la mesure.

## Reproduction

```powershell
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release -p:BenchmarkYears=2 --filter "TestCategory=Import" --logger "console;verbosity=detailed"
```
