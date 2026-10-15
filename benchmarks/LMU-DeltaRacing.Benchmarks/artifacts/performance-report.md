# Rapport de performance — parsing XML

- Date UTC : 2026-10-15T21:45:09.9581278+00:00
- Runtime : 10.0.13
- Système : Microsoft Windows NT 10.0.19045.0
- Processeurs logiques : 8

## Charge en mémoire

Simulation : 10 courses/jour × 3 phases × 365 jours × 10 ans = 109,500 sessions.
Les XML sont conservés dans des streams en mémoire ; chaque résultat analysé est libéré avant le suivant.
Chaque XML synthétique contient 10 pilotes, leurs métadonnées, les tours et trois secteurs par tour (Practice : 3 tours, Qualifying : 2, Race : 8).

| Durée | Débit | Allocations cumulées | Pic mémoire observé* |
|---:|---:|---:|---:|
| 00:00:14.525 | 7,539 sessions/s | 7,626.0 MiB | 59.7 MiB |

* Pic du working set échantillonné toutes les 300 sessions.

## Lots générés sur disque

Les fichiers sont écrits dans un répertoire temporaire, analysés puis supprimés.

| Fichiers | Génération | Parsing | Débit parsing |
|---:|---:|---:|---:|
| 10 | 00:00:00.006 | 00:00:00.003 | 3,249 fichiers/s |
| 100 | 00:00:00.037 | 00:00:00.020 | 4,866 fichiers/s |
| 500 | 00:00:00.205 | 00:00:00.105 | 4,761 fichiers/s |
| 1000 | 00:00:00.408 | 00:00:00.189 | 5,271 fichiers/s |
| 2000 | 00:00:00.788 | 00:00:00.385 | 5,187 fichiers/s |
| 5000 | 00:00:01.979 | 00:00:00.971 | 5,145 fichiers/s |
