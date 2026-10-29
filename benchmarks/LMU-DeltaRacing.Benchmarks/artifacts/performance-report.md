# Rapport de performance

- Date UTC : 2026-10-29T21:10:30.3031311+00:00
- Runtime : 10.0.14
- Système : Microsoft Windows NT 10.0.19045.0
- Processeurs logiques : 8

## Parsing de XML synthétiques en mémoire

Simulation : 10 courses/jour × 3 phases × 365 jours × 10 ans = 109,500 sessions.
Les XML sont conservés dans des streams en mémoire ; chaque résultat analysé est libéré avant le suivant.
Chaque XML synthétique contient 10 pilotes, leurs métadonnées, les tours et trois secteurs par tour (Practice : 3 tours, Qualifying : 2, Race : 8).

| Durée | Débit | Allocations cumulées | Pic mémoire observé* |
|---:|---:|---:|---:|
| 00:00:09.664 | 11,331 sessions/s | 5,357.3 MiB | 68.0 MiB |

* Pic du working set échantillonné toutes les 300 sessions.

## Lots générés sur disque

Les fichiers sont écrits dans un répertoire temporaire, analysés puis supprimés.

| Fichiers | Génération | Parsing | Débit parsing |
|---:|---:|---:|---:|
| 10 | 00:00:00.006 | 00:00:00.002 | 4,202 fichiers/s |
| 100 | 00:00:00.039 | 00:00:00.016 | 6,139 fichiers/s |
| 500 | 00:00:00.190 | 00:00:00.075 | 6,620 fichiers/s |
| 1000 | 00:00:00.375 | 00:00:00.169 | 5,895 fichiers/s |
| 2000 | 00:00:00.758 | 00:00:00.305 | 6,549 fichiers/s |
| 5000 | 00:00:01.932 | 00:00:00.729 | 6,850 fichiers/s |

## Opérations de la vue Mes courses

Mesures sur des historiques synthétiques de 20 circuits et cinq catégories, avec une distribution uniforme des événements. Le jeu de données est généré avant les mesures.
Chaque opération est préchauffée 2 fois ; les résultats sont la médiane de 7 exécutions. Les allocations mesurées sont celles du thread courant.

| Courses | Opération | Médiane | Allocations médianes | Éléments traités |
|---:|---|---:|---:|---:|
| 1,000 | Tri par date décroissante | 0.588 ms | 36,344 B | 1,000 |
| 1,000 | Groupement circuit / catégorie | 0.372 ms | 57,648 B | 20 |
| 1,000 | Filtre par circuit | 0.022 ms | 512 B | 50 |
| 1,000 | Filtre par catégorie | 0.015 ms | 1,712 B | 200 |
| 1,000 | Filtres circuit + catégorie | 0.021 ms | 88 B | 0 |
| 10,000 | Tri par date décroissante | 1.835 ms | 360,344 B | 10,000 |
| 10,000 | Groupement circuit / catégorie | 2.259 ms | 490,448 B | 20 |
| 10,000 | Filtre par circuit | 0.135 ms | 4,112 B | 500 |
| 10,000 | Filtre par catégorie | 0.105 ms | 16,112 B | 2,000 |
| 10,000 | Filtres circuit + catégorie | 0.134 ms | 88 B | 0 |
| 50,000 | Tri par date décroissante | 12.129 ms | 1,800,344 B | 50,000 |
| 50,000 | Groupement circuit / catégorie | 6.530 ms | 2,918,768 B | 20 |
| 50,000 | Filtre par circuit | 1.594 ms | 20,112 B | 2,500 |
| 50,000 | Filtre par catégorie | 1.104 ms | 80,112 B | 10,000 |
| 50,000 | Filtres circuit + catégorie | 1.069 ms | 88 B | 0 |

## Chaîne d'import sur les fichiers réels

Mesures sur les 342 fichiers XML anonymisés de `assets/Results` (essais, qualifications et courses, jusqu'à 280 Ko par fichier). Chaque opération est préchauffée, puis la médiane et le meilleur temps de plusieurs exécutions sont retenus. Les allocations comptent tous les threads.

### Parsing XML

| Fichier | Opération | Médiane | Meilleur | Allocations (médiane) | Éléments |
|---:|---|---:|---:|---:|---:|
| 20 Ko | Parsing d'un fichier Essais médian | 0.148 ms | 0.144 ms | 55.3 KiB | 22 |
| 222 Ko | Parsing du plus gros fichier Essais | 0.989 ms | 0.977 ms | 168.5 KiB | 28 |
| 65 Ko | Parsing d'un fichier Qualification médian | 0.318 ms | 0.315 ms | 93.7 KiB | 20 |
| 129 Ko | Parsing du plus gros fichier Qualification | 0.606 ms | 0.602 ms | 134.8 KiB | 42 |
| 147 Ko | Parsing d'un fichier Course médian | 0.696 ms | 0.691 ms | 161.9 KiB | 22 |
| 272 Ko | Parsing du plus gros fichier Course | 1.232 ms | 1.199 ms | 222.1 KiB | 42 |
| 25.8 Mo | Parsing des 342 fichiers en mémoire (séquentiel) | 143.6 ms | 134.7 ms | 31.9 MiB | 7,253 |

### Import depuis le disque

« Chargement complet » mesure `DashboardDataService` du démarrage jusqu'à la fin de l'import initial (liste des fichiers, import, fusions, fréquences des pilotes), sans surveillance. Au-delà d'une copie, les fichiers réels sont dupliqués sous d'autres noms pour simuler un historique plus long.

| Fichiers | Opération | Médiane | Meilleur | Allocations (médiane) | Éléments |
|---:|---|---:|---:|---:|---:|
| 342 | Import disque fichier par fichier, sans essais | 213.5 ms | 207.5 ms | 32.2 MiB | 208 |
| 342 | Chargement complet du service, sans essais | 98.5 ms | 95.1 ms | 32.6 MiB | 208 |
| 342 | Chargement complet du service, avec essais | 111.4 ms | 87.7 ms | 32.6 MiB | 342 |
| 342 | Chargement complet du service, sans essais, 2 fusions | 110.0 ms | 91.6 ms | 32.7 MiB | 208 |
| 342 | Chargement complet du service, sans essais, journal JSON (Debug) | 127.5 ms | 114.8 ms | 41.6 MiB | 208 |
| 1,710 | Import disque fichier par fichier, sans essais | 1,134.9 ms | 1,110.1 ms | 160.9 MiB | 1,040 |
| 1,710 | Chargement complet du service, sans essais | 505.5 ms | 488.1 ms | 162.7 MiB | 1,040 |
| 1,710 | Chargement complet du service, avec essais | 625.3 ms | 501.5 ms | 163.0 MiB | 1,710 |
| 1,710 | Chargement complet du service, sans essais, 2 fusions | 586.8 ms | 575.8 ms | 163.2 MiB | 1,040 |
| 1,710 | Chargement complet du service, sans essais, journal JSON (Debug) | 589.7 ms | 559.8 ms | 207.5 MiB | 1,040 |

## Calculs des vues

L'historique réel (208 qualifications et courses, pilote « Driver Tom ») est répété ×1, ×10 et ×50, chaque copie décalée dans le temps. Ces calculs sont refaits par la page affichée à chaque changement d'état (import, fusion, changement de pilote).

| Sessions | Opération | Médiane | Meilleur | Allocations (médiane) | Éléments |
|---:|---|---:|---:|---:|---:|
| 208 (×1) | Fusions de pilotes appliquées (2 fusions) | 0.267 ms | 0.266 ms | 53.6 KiB | 208 |
| 208 (×1) | Fréquences des pilotes (Options) | 0.812 ms | 0.571 ms | 50.8 KiB | 238 |
| 208 (×1) | Sessions en commun de deux pilotes (Options) | 0.327 ms | 0.323 ms | 13.7 KiB | 26 |
| 208 (×1) | Accueil | 1.387 ms | 1.385 ms | 251.1 KiB | 10 |
| 208 (×1) | Mes courses : lignes | 0.300 ms | 0.297 ms | 124.6 KiB | 89 |
| 208 (×1) | Mes courses : filtres et tri par circuit | 0.082 ms | 0.078 ms | 19.8 KiB | 96 |
| 208 (×1) | Classement des coureurs | 0.968 ms | 0.937 ms | 274.5 KiB | 236 |
| 208 (×1) | Détail des courses : événements | 0.248 ms | 0.239 ms | 98.0 KiB | 89 |
| 208 (×1) | Progression : circuits | 0.391 ms | 0.269 ms | 48.9 KiB | 11 |
| 208 (×1) | Progression : courbes du circuit par défaut | 0.171 ms | 0.169 ms | 41.0 KiB | 2 |
| 208 (×1) | Progression : géométrie SVG | 0.051 ms | 0.049 ms | 11.2 KiB | 2 |
| 2,080 (×10) | Fusions de pilotes appliquées (2 fusions) | 2.885 ms | 2.725 ms | 531.1 KiB | 2,080 |
| 2,080 (×10) | Fréquences des pilotes (Options) | 4.916 ms | 4.688 ms | 124.0 KiB | 238 |
| 2,080 (×10) | Sessions en commun de deux pilotes (Options) | 3.508 ms | 3.313 ms | 120.0 KiB | 260 |
| 2,080 (×10) | Accueil | 14.2 ms | 13.5 ms | 2.0 MiB | 10 |
| 2,080 (×10) | Mes courses : lignes | 3.487 ms | 2.524 ms | 1.1 MiB | 890 |
| 2,080 (×10) | Mes courses : filtres et tri par circuit | 1.627 ms | 1.591 ms | 73.7 KiB | 897 |
| 2,080 (×10) | Classement des coureurs | 8.429 ms | 7.965 ms | 2.3 MiB | 236 |
| 2,080 (×10) | Détail des courses : événements | 4.778 ms | 3.304 ms | 919.0 KiB | 890 |
| 2,080 (×10) | Progression : circuits | 3.808 ms | 2.639 ms | 383.6 KiB | 11 |
| 2,080 (×10) | Progression : courbes du circuit par défaut | 1.518 ms | 1.375 ms | 381.3 KiB | 2 |
| 2,080 (×10) | Progression : géométrie SVG | 0.495 ms | 0.464 ms | 71.1 KiB | 2 |
| 10,400 (×50) | Fusions de pilotes appliquées (2 fusions) | 21.6 ms | 15.7 ms | 2.6 MiB | 10,400 |
| 10,400 (×50) | Fréquences des pilotes (Options) | 26.4 ms | 24.6 ms | 449.0 KiB | 238 |
| 10,400 (×50) | Sessions en commun de deux pilotes (Options) | 16.0 ms | 14.9 ms | 592.5 KiB | 1,300 |
| 10,400 (×50) | Accueil | 33.2 ms | 29.3 ms | 9.9 MiB | 10 |
| 10,400 (×50) | Mes courses : lignes | 12.0 ms | 9.294 ms | 5.6 MiB | 4,450 |
| 10,400 (×50) | Mes courses : filtres et tri par circuit | 1.576 ms | 1.544 ms | 339.0 KiB | 4,457 |
| 10,400 (×50) | Classement des coureurs | 24.7 ms | 23.5 ms | 11.4 MiB | 236 |
| 10,400 (×50) | Détail des courses : événements | 7.719 ms | 5.621 ms | 4.1 MiB | 4,450 |
| 10,400 (×50) | Progression : circuits | 7.372 ms | 6.611 ms | 1.9 MiB | 11 |
| 10,400 (×50) | Progression : courbes du circuit par défaut | 5.776 ms | 5.030 ms | 1.8 MiB | 2 |
| 10,400 (×50) | Progression : géométrie SVG | 1.712 ms | 1.629 ms | 328.3 KiB | 2 |
| 1 (21 pilotes) | Détail des courses : classement de la plus grosse course | 0.180 ms | 0.173 ms | 78.2 KiB | 21 |

