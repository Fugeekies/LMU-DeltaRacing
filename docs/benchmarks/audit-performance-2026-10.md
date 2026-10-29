# Audit de performance — octobre 2026

Comparaison des mesures avant et après l'audit du code (parser en flux, import parallèle, calculs des vues). Les deux séries ont été mesurées sur la même machine (Windows 10, 8 processeurs logiques, .NET 10.0.14, configuration Release) avec les benchmarks de `benchmarks/LMU-DeltaRacing.Benchmarks` ; le rapport complet est dans [performance-report.md](../../benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md).

Les durées sont des médianes. Elles reflètent cette machine et ne garantissent pas les performances ailleurs.

## Chargement des résultats

Les 342 fichiers XML de `assets/Results` (essais, qualifications et courses, 25,8 Mo). « Chargement complet » mesure `DashboardDataService` du démarrage à la fin de l'import initial, sans surveillance. Au-delà de 342 fichiers, les fichiers réels sont dupliqués.

| Mesure | Avant | Après | Gain |
|---|---:|---:|---:|
| Parsing des 342 fichiers en mémoire | 294 ms · 165 Mo alloués | 145 ms · 32 Mo alloués | ×2,0 · ×5,2 |
| Parsing du plus gros fichier de course (272 Ko) | 6,3 ms · 1,6 Mo | 4,8 ms · 0,23 Mo | ×1,3 · ×7 |
| Chargement complet, 342 fichiers | 413 ms | 96 ms | ×4,3 |
| Chargement complet, 342 fichiers, 2 fusions de pilotes | 477 ms | 93 ms | ×5,1 |
| Chargement complet, 1 710 fichiers | 2 059 ms | 516 ms | ×4,0 |
| Chargement complet, 1 710 fichiers, 2 fusions de pilotes | 4 508 ms | 525 ms | ×8,6 |

Origine des gains :

- **Parser en flux** (`XmlReader` au lieu de `XDocument`) : le journal d'événements de la session (`Stream` : incidents, scores, limites de piste), qui fait l'essentiel d'un fichier de course, est lu sans être chargé en mémoire. Les valeurs lues sont identiques : un test compare l'empreinte SHA-256 de toutes les valeurs des 342 fixtures à celle de l'ancien parser.
- **Import parallèle** : les fichiers sont analysés sur plusieurs cœurs (un cœur reste libre pour l'affichage) ; leurs résultats sont pris dans l'ordre des fichiers, donc la progression et la liste des échecs restent identiques à un import séquentiel.
- **Fin du coût quadratique** : les fusions de pilotes et les fréquences étaient recalculées sur toutes les sessions après chaque fichier ; elles le sont une fois, en fin de chargement.

Le journal JSON du dashboard au niveau `Debug` (par défaut) ajoute environ 20 ms aux 342 fichiers.

## Calculs des vues

Historique réel répété ×50 (10 400 qualifications et courses). Chaque page refait ces calculs à chaque changement d'état.

| Calcul | Avant | Après |
|---|---:|---:|
| Fréquences des pilotes (Options, fin de chargement) | 49 ms · 16,4 Mo | 32 ms · 0,45 Mo |
| Accueil | 38 ms | 26 ms |
| Mes courses | 16 ms | 12 ms |
| Classement des coureurs | 27 ms | 23 ms |
| Détail des courses : événements | 9,6 ms | 7,9 ms |
| Progression : courbes | 6,4 ms | 5,8 ms |

Le meilleur tour d'un pilote est calculé une fois, à la création du pilote (`Driver.BestLap`), au lieu d'être recalculé par chaque vue. La page Progression ne recalcule plus son graphique au survol des points, et la page Options ne recompte plus les sessions communes des fusions à chaque affichage.

## Reproduction

```powershell
# Rapport complet (environ 1 minute), écrit dans benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release --filter "TestCategory=Load"

# Chaîne d'import ou calculs des vues seuls, affichés dans la sortie détaillée
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release --filter "TestCategory=Import" --logger "console;verbosity=detailed"
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release --filter "TestCategory=Views" --logger "console;verbosity=detailed"
```
