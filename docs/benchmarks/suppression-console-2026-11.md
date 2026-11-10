# Suppression de la console et de la télémétrie d'import — novembre 2026

Effet sur la chaîne d'import du retrait d'`ApplicationTelemetry` (PR #71) : `SessionImportService` ne crée plus d'activité, n'incrémente plus de compteur et ne chronomètre plus chaque fichier. Seule la console lisait ces mesures ; sans elle, plus rien ne les écoutait.

« Avant » est la branche `main` mesurée le 10/11/2026, « Après » la branche de la PR, mesurée deux fois le même soir, sur la même machine (Windows 10, 8 processeurs logiques, .NET 10.0.15, configuration Release), avec `-p:BenchmarkYears=2` et le filtre `TestCategory=Load`.

| Mesure | Avant (médiane · meilleur) | Après, run 1 | Après, run 2 | Allocations avant → après |
|---|---:|---:|---:|---:|
| Import disque, 342 fichiers, sans essais | 352,6 · 308,3 ms | 333,6 · 306,9 ms | 320,8 · 311,7 ms | 79,9 → 79,8 Mo |
| Chargement complet, 342 fichiers, sans essais | 198,2 · 197,3 ms | 202,0 · 198,3 ms | 203,3 · 173,4 ms | 81,4 → 81,1 Mo |
| Chargement complet, 342 fichiers, avec essais | 221,3 · 201,0 ms | 203,7 · 167,5 ms | 220,9 · 199,0 ms | 81,4 → 81,4 Mo |
| Import disque, 1 710 fichiers, sans essais | 1 728,4 · 1 710,4 ms | 1 637,8 · 1 613,4 ms | 1 673,8 · 1 661,8 ms | 399,2 → 399,2 Mo |
| Chargement complet, 1 710 fichiers, sans essais | 925,7 · 925,6 ms | 890,2 · 875,4 ms | 930,9 · 901,9 ms | 405,9 → 406,3 Mo |
| Chargement complet, 1 710 fichiers, avec essais | 1 006,4 · 961,8 ms | 956,0 · 945,9 ms | 991,7 · 978,0 ms | 406,4 → 406,3 Mo |

**Lecture :** les deux runs « Après » s'écartent l'un de l'autre autant que de « Avant » (jusqu'à 17 ms sur 342 fichiers avec essais) : aucun gain n'est mesurable au-dessus du bruit, et les allocations ne changent pas. C'était attendu : sans écouteur, `ActivitySource.StartActivity` renvoie `null` et un compteur sans abonné ne fait presque rien. Le retrait simplifie le code sans effet sur les performances.

Le rapport complet du run 2 est dans [`performance-report.md`](../../benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md).

## Reproduction

```powershell
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release -p:BenchmarkYears=2 --filter "TestCategory=Load"
```
