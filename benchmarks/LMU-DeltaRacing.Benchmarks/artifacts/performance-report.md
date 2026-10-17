# Rapport de performance — parsing XML

- Date UTC : 2026-10-17T18:58:07.1985295+00:00
- Runtime : 10.0.13
- Système : Microsoft Windows NT 10.0.19045.0
- Processeurs logiques : 8

## Charge en mémoire

Simulation : 10 courses/jour × 3 phases × 365 jours × 10 ans = 109,500 sessions.
Les XML sont conservés dans des streams en mémoire ; chaque résultat analysé est libéré avant le suivant.
Chaque XML synthétique contient 10 pilotes, leurs métadonnées, les tours et trois secteurs par tour (Practice : 3 tours, Qualifying : 2, Race : 8).

| Durée | Débit | Allocations cumulées | Pic mémoire observé* |
|---:|---:|---:|---:|
| 00:00:16.159 | 6,776 sessions/s | 7,790.3 MiB | 67.2 MiB |

* Pic du working set échantillonné toutes les 300 sessions.

## Lots générés sur disque

Les fichiers sont écrits dans un répertoire temporaire, analysés puis supprimés.

| Fichiers | Génération | Parsing | Débit parsing |
|---:|---:|---:|---:|
| 10 | 00:00:00.023 | 00:00:00.005 | 1,707 fichiers/s |
| 100 | 00:00:00.042 | 00:00:00.020 | 4,765 fichiers/s |
| 500 | 00:00:00.236 | 00:00:00.105 | 4,733 fichiers/s |
| 1000 | 00:00:00.434 | 00:00:00.219 | 4,551 fichiers/s |
| 2000 | 00:00:00.859 | 00:00:00.421 | 4,742 fichiers/s |
| 5000 | 00:00:02.129 | 00:00:01.090 | 4,585 fichiers/s |

## Opérations de la vue Mes courses

Mesures sur des historiques synthétiques de 20 circuits et trois catégories, avec une distribution uniforme des événements. Le jeu de données est généré avant les mesures.
Chaque opération est préchauffée 2 fois ; les résultats sont la médiane de 7 exécutions. Les allocations mesurées sont celles du thread courant.

| Courses | Opération | Médiane | Allocations médianes | Éléments traités |
|---:|---|---:|---:|---:|
| 1,000 | Tri par date décroissante | 0.149 ms | 36,344 B | 1,000 |
| 1,000 | Groupement circuit / catégorie | 0.304 ms | 70,176 B | 60 |
| 1,000 | Filtre par circuit | 0.017 ms | 512 B | 50 |
| 1,000 | Filtre par catégorie | 0.010 ms | 2,776 B | 333 |
| 1,000 | Filtres circuit + catégorie | 0.015 ms | 248 B | 17 |
| 10,000 | Tri par date décroissante | 2.083 ms | 360,344 B | 10,000 |
| 10,000 | Groupement circuit / catégorie | 2.709 ms | 583,136 B | 60 |
| 10,000 | Filtre par circuit | 0.144 ms | 4,112 B | 500 |
| 10,000 | Filtre par catégorie | 0.173 ms | 26,776 B | 3,333 |
| 10,000 | Filtres circuit + catégorie | 0.265 ms | 1,448 B | 167 |
| 50,000 | Tri par date décroissante | 13.319 ms | 1,800,344 B | 50,000 |
| 50,000 | Groupement circuit / catégorie | 10.415 ms | 2,603,296 B | 60 |
| 50,000 | Filtre par circuit | 1.809 ms | 20,112 B | 2,500 |
| 50,000 | Filtre par catégorie | 1.511 ms | 133,440 B | 16,666 |
| 50,000 | Filtres circuit + catégorie | 2.043 ms | 6,776 B | 833 |
