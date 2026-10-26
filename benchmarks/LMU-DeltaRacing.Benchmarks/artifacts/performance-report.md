# Rapport de performance — parsing XML

- Date UTC : 2026-10-26T19:52:37.5705321+00:00
- Runtime : 10.0.14
- Système : Microsoft Windows NT 10.0.19045.0
- Processeurs logiques : 8

## Charge en mémoire

Simulation : 10 courses/jour × 3 phases × 365 jours × 10 ans = 109,500 sessions.
Les XML sont conservés dans des streams en mémoire ; chaque résultat analysé est libéré avant le suivant.
Chaque XML synthétique contient 10 pilotes, leurs métadonnées, les tours et trois secteurs par tour (Practice : 3 tours, Qualifying : 2, Race : 8).

| Durée | Débit | Allocations cumulées | Pic mémoire observé* |
|---:|---:|---:|---:|
| 00:00:12.744 | 8,592 sessions/s | 7,920.3 MiB | 67.3 MiB |

* Pic du working set échantillonné toutes les 300 sessions.

## Lots générés sur disque

Les fichiers sont écrits dans un répertoire temporaire, analysés puis supprimés.

| Fichiers | Génération | Parsing | Débit parsing |
|---:|---:|---:|---:|
| 10 | 00:00:00.007 | 00:00:00.014 | 695 fichiers/s |
| 100 | 00:00:00.038 | 00:00:00.018 | 5,313 fichiers/s |
| 500 | 00:00:00.189 | 00:00:00.106 | 4,692 fichiers/s |
| 1000 | 00:00:00.406 | 00:00:00.176 | 5,670 fichiers/s |
| 2000 | 00:00:00.749 | 00:00:00.398 | 5,014 fichiers/s |
| 5000 | 00:00:01.946 | 00:00:00.917 | 5,452 fichiers/s |

## Opérations de la vue Mes courses

Mesures sur des historiques synthétiques de 20 circuits et cinq catégories, avec une distribution uniforme des événements. Le jeu de données est généré avant les mesures.
Chaque opération est préchauffée 2 fois ; les résultats sont la médiane de 7 exécutions. Les allocations mesurées sont celles du thread courant.

| Courses | Opération | Médiane | Allocations médianes | Éléments traités |
|---:|---|---:|---:|---:|
| 1,000 | Tri par date décroissante | 0.140 ms | 36,344 B | 1,000 |
| 1,000 | Groupement circuit / catégorie | 0.506 ms | 57,648 B | 20 |
| 1,000 | Filtre par circuit | 0.033 ms | 512 B | 50 |
| 1,000 | Filtre par catégorie | 0.043 ms | 1,712 B | 200 |
| 1,000 | Filtres circuit + catégorie | 0.033 ms | 88 B | 0 |
| 10,000 | Tri par date décroissante | 1.842 ms | 360,344 B | 10,000 |
| 10,000 | Groupement circuit / catégorie | 3.740 ms | 490,448 B | 20 |
| 10,000 | Filtre par circuit | 0.233 ms | 4,112 B | 500 |
| 10,000 | Filtre par catégorie | 0.163 ms | 16,112 B | 2,000 |
| 10,000 | Filtres circuit + catégorie | 0.242 ms | 88 B | 0 |
| 50,000 | Tri par date décroissante | 12.003 ms | 1,800,344 B | 50,000 |
| 50,000 | Groupement circuit / catégorie | 9.013 ms | 2,918,768 B | 20 |
| 50,000 | Filtre par circuit | 1.452 ms | 20,112 B | 2,500 |
| 50,000 | Filtre par catégorie | 1.114 ms | 80,112 B | 10,000 |
| 50,000 | Filtres circuit + catégorie | 1.014 ms | 88 B | 0 |
