# Rapport de performance

- Date UTC : 2026-11-10T20:28:48.9472201+00:00
- Runtime : 10.0.15
- Système : Microsoft Windows NT 10.0.19045.0
- Processeurs logiques : 8

## Parsing de XML synthétiques en mémoire

Simulation : 10 courses/jour × 3 phases × 365 jours × 2 ans = 21,900 sessions.
Les XML sont conservés dans des streams en mémoire ; chaque résultat analysé est libéré avant le suivant.
Chaque XML synthétique contient 10 pilotes, leurs métadonnées, les tours et trois secteurs par tour (Practice : 3 tours, Qualifying : 2, Race : 8).

| Durée | Débit | Allocations cumulées | Pic mémoire observé* |
|---:|---:|---:|---:|
| 00:00:03.203 | 6,837 sessions/s | 1,331.0 MiB | 68.6 MiB |

* Pic du working set échantillonné toutes les 300 sessions.

## Lots générés sur disque

Les fichiers sont écrits dans un répertoire temporaire, analysés puis supprimés.

| Fichiers | Génération | Parsing | Débit parsing |
|---:|---:|---:|---:|
| 10 | 00:00:00.006 | 00:00:00.002 | 3,683 fichiers/s |
| 100 | 00:00:00.038 | 00:00:00.018 | 5,533 fichiers/s |
| 500 | 00:00:00.192 | 00:00:00.089 | 5,615 fichiers/s |
| 1000 | 00:00:00.386 | 00:00:00.170 | 5,851 fichiers/s |
| 2000 | 00:00:00.748 | 00:00:00.344 | 5,801 fichiers/s |
| 5000 | 00:00:01.825 | 00:00:00.844 | 5,919 fichiers/s |

## Opérations de la vue Mes courses

Mesures sur des historiques synthétiques de 20 circuits et cinq catégories, avec une distribution uniforme des événements. Le jeu de données est généré avant les mesures.
Chaque opération est préchauffée 2 fois ; les résultats sont la médiane de 7 exécutions. Les allocations mesurées sont celles du thread courant.

| Courses | Opération | Médiane | Allocations médianes | Éléments traités |
|---:|---|---:|---:|---:|
| 1,000 | Tri par date décroissante | 0.201 ms | 36,344 B | 1,000 |
| 1,000 | Groupement circuit / catégorie | 0.293 ms | 57,648 B | 20 |
| 1,000 | Filtre par circuit | 0.014 ms | 512 B | 50 |
| 1,000 | Filtre par catégorie | 0.010 ms | 1,712 B | 200 |
| 1,000 | Filtres circuit + catégorie | 0.013 ms | 88 B | 0 |
| 10,000 | Tri par date décroissante | 1.958 ms | 360,344 B | 10,000 |
| 10,000 | Groupement circuit / catégorie | 2.834 ms | 490,448 B | 20 |
| 10,000 | Filtre par circuit | 0.189 ms | 4,112 B | 500 |
| 10,000 | Filtre par catégorie | 0.095 ms | 16,112 B | 2,000 |
| 10,000 | Filtres circuit + catégorie | 0.131 ms | 88 B | 0 |
| 50,000 | Tri par date décroissante | 4.584 ms | 1,800,344 B | 50,000 |
| 50,000 | Groupement circuit / catégorie | 8.261 ms | 2,918,768 B | 20 |
| 50,000 | Filtre par circuit | 1.600 ms | 20,112 B | 2,500 |
| 50,000 | Filtre par catégorie | 1.159 ms | 80,112 B | 10,000 |
| 50,000 | Filtres circuit + catégorie | 1.208 ms | 88 B | 0 |

## Chaîne d'import sur les fichiers réels

Mesures sur les 342 fichiers XML anonymisés de `assets/Results` (essais, qualifications et courses, jusqu'à 280 Ko par fichier). Chaque opération est préchauffée, puis la médiane et le meilleur temps de plusieurs exécutions sont retenus. Les allocations comptent tous les threads.

### Parsing XML

| Fichier | Opération | Médiane | Meilleur | Allocations (médiane) | Éléments |
|---:|---|---:|---:|---:|---:|
| 20 Ko | Parsing d'un fichier Essais médian | 0.217 ms | 0.204 ms | 70.0 KiB | 22 |
| 223 Ko | Parsing du plus gros fichier Essais | 2.341 ms | 1.916 ms | 636.8 KiB | 28 |
| 64 Ko | Parsing d'un fichier Qualification médian | 0.500 ms | 0.471 ms | 173.7 KiB | 20 |
| 129 Ko | Parsing du plus gros fichier Qualification | 1.068 ms | 1.020 ms | 371.8 KiB | 42 |
| 147 Ko | Parsing d'un fichier Course médian | 1.399 ms | 1.366 ms | 513.4 KiB | 22 |
| 272 Ko | Parsing du plus gros fichier Course | 1.797 ms | 1.727 ms | 749.9 KiB | 42 |
| 25.8 Mo | Parsing des 342 fichiers en mémoire (séquentiel) | 211.9 ms | 210.7 ms | 79.6 MiB | 7,253 |

### Import depuis le disque

« Chargement complet » mesure `DashboardDataService` du démarrage jusqu'à la fin de l'import initial (liste des fichiers, import, fusions, fréquences des pilotes), sans surveillance. Au-delà d'une copie, les fichiers réels sont dupliqués, avec un lieu propre à chaque copie, pour simuler un historique plus long.

| Fichiers | Opération | Médiane | Meilleur | Allocations (médiane) | Éléments |
|---:|---|---:|---:|---:|---:|
| 342 | Import disque fichier par fichier, sans essais | 320.8 ms | 311.7 ms | 79.8 MiB | 208 |
| 342 | Chargement complet du service, sans essais | 203.3 ms | 173.4 ms | 81.1 MiB | 208 |
| 342 | Chargement complet du service, avec essais | 220.9 ms | 199.0 ms | 81.4 MiB | 342 |
| 342 | Chargement complet du service, sans essais, 2 fusions | 192.7 ms | 175.3 ms | 81.7 MiB | 208 |
| 342 | Chargement complet du service, sans essais, journal JSON (Debug) | 220.0 ms | 192.3 ms | 90.3 MiB | 208 |
| 1,710 | Import disque fichier par fichier, sans essais | 1,673.8 ms | 1,661.8 ms | 399.2 MiB | 1,040 |
| 1,710 | Chargement complet du service, sans essais | 930.9 ms | 901.9 ms | 406.3 MiB | 1,040 |
| 1,710 | Chargement complet du service, avec essais | 991.7 ms | 978.0 ms | 406.3 MiB | 1,710 |
| 1,710 | Chargement complet du service, sans essais, 2 fusions | 945.8 ms | 890.6 ms | 407.7 MiB | 1,040 |
| 1,710 | Chargement complet du service, sans essais, journal JSON (Debug) | 999.8 ms | 973.6 ms | 450.0 MiB | 1,040 |

## Calculs des vues

L'historique réel (208 qualifications et courses, pilote « Driver Tom ») est répété ×1, ×10 et ×50, chaque copie décalée dans le temps. Ces calculs sont refaits par la page affichée à chaque changement d'état (import, fusion, changement de pilote).

| Sessions | Opération | Médiane | Meilleur | Allocations (médiane) | Éléments |
|---:|---|---:|---:|---:|---:|
| 208 (×1) | Fusions de pilotes appliquées (2 fusions) | 0.705 ms | 0.699 ms | 425.6 KiB | 208 |
| 208 (×1) | Fréquences des pilotes (Options) | 0.625 ms | 0.615 ms | 50.8 KiB | 238 |
| 208 (×1) | Sessions en commun de deux pilotes (Options) | 0.361 ms | 0.359 ms | 13.7 KiB | 26 |
| 208 (×1) | Accueil | 1.404 ms | 1.364 ms | 257.0 KiB | 10 |
| 208 (×1) | Mes courses : lignes | 0.302 ms | 0.299 ms | 130.1 KiB | 89 |
| 208 (×1) | Mes courses : filtres et tri par circuit | 0.084 ms | 0.080 ms | 19.8 KiB | 96 |
| 208 (×1) | Classement des coureurs (cotes calculées à part) | 15.4 ms | 14.2 ms | 2.8 MiB | 236 |
| 208 (×1) | Profil de conduite | 24.5 ms | 22.2 ms | 5.5 MiB | 170 |
| 208 (×1) | Cote pilote : cotes de tous les pilotes | 7.421 ms | 6.017 ms | 837.5 KiB | 88 |
| 208 (×1) | Cote pilote : page du pilote | 7.532 ms | 6.497 ms | 1,001.4 KiB | 72 |
| 208 (×1) | Détail des courses : événements | 0.400 ms | 0.348 ms | 103.9 KiB | 89 |
| 208 (×1) | Progression : circuits | 0.540 ms | 0.494 ms | 48.9 KiB | 11 |
| 208 (×1) | Progression : courbes du circuit par défaut | 0.299 ms | 0.298 ms | 41.0 KiB | 2 |
| 208 (×1) | Progression : géométrie SVG | 0.070 ms | 0.068 ms | 11.2 KiB | 2 |
| 208 (×1) | Progression : records et comparaison des sessions | 0.248 ms | 0.239 ms | 57.1 KiB | 19 |
| 2,080 (×10) | Fusions de pilotes appliquées (2 fusions) | 9.258 ms | 8.111 ms | 4.2 MiB | 2,080 |
| 2,080 (×10) | Fréquences des pilotes (Options) | 5.374 ms | 4.843 ms | 124.0 KiB | 238 |
| 2,080 (×10) | Sessions en commun de deux pilotes (Options) | 3.878 ms | 3.735 ms | 120.0 KiB | 260 |
| 2,080 (×10) | Accueil | 16.4 ms | 15.3 ms | 2.0 MiB | 10 |
| 2,080 (×10) | Mes courses : lignes | 4.225 ms | 2.965 ms | 1.2 MiB | 890 |
| 2,080 (×10) | Mes courses : filtres et tri par circuit | 0.750 ms | 0.734 ms | 73.7 KiB | 897 |
| 2,080 (×10) | Classement des coureurs (cotes calculées à part) | 59.3 ms | 57.2 ms | 27.5 MiB | 236 |
| 2,080 (×10) | Profil de conduite | 100.3 ms | 94.9 ms | 49.9 MiB | 1,700 |
| 2,080 (×10) | Cote pilote : cotes de tous les pilotes | 29.7 ms | 27.8 ms | 7.5 MiB | 880 |
| 2,080 (×10) | Cote pilote : page du pilote | 34.8 ms | 32.9 ms | 8.8 MiB | 720 |
| 2,080 (×10) | Détail des courses : événements | 1.183 ms | 1.097 ms | 881.1 KiB | 890 |
| 2,080 (×10) | Progression : circuits | 1.514 ms | 1.494 ms | 253.6 KiB | 11 |
| 2,080 (×10) | Progression : courbes du circuit par défaut | 1.317 ms | 1.287 ms | 251.3 KiB | 2 |
| 2,080 (×10) | Progression : géométrie SVG | 0.544 ms | 0.490 ms | 71.1 KiB | 2 |
| 2,080 (×10) | Progression : records et comparaison des sessions | 1.464 ms | 1.433 ms | 490.2 KiB | 190 |
| 10,400 (×50) | Fusions de pilotes appliquées (2 fusions) | 91.0 ms | 69.2 ms | 20.8 MiB | 10,400 |
| 10,400 (×50) | Fréquences des pilotes (Options) | 26.3 ms | 25.5 ms | 449.0 KiB | 238 |
| 10,400 (×50) | Sessions en commun de deux pilotes (Options) | 17.6 ms | 17.1 ms | 592.5 KiB | 1,300 |
| 10,400 (×50) | Accueil | 27.6 ms | 24.9 ms | 10.1 MiB | 10 |
| 10,400 (×50) | Mes courses : lignes | 12.7 ms | 10.5 ms | 5.9 MiB | 4,450 |
| 10,400 (×50) | Mes courses : filtres et tri par circuit | 1.603 ms | 1.524 ms | 339.0 KiB | 4,457 |
| 10,400 (×50) | Classement des coureurs (cotes calculées à part) | 289.3 ms | 286.4 ms | 137.4 MiB | 236 |
| 10,400 (×50) | Profil de conduite | 507.7 ms | 484.2 ms | 249.9 MiB | 8,500 |
| 10,400 (×50) | Cote pilote : cotes de tous les pilotes | 164.3 ms | 155.3 ms | 37.3 MiB | 4,400 |
| 10,400 (×50) | Cote pilote : page du pilote | 175.0 ms | 165.2 ms | 43.5 MiB | 3,600 |
| 10,400 (×50) | Détail des courses : événements | 7.866 ms | 7.616 ms | 4.3 MiB | 4,450 |
| 10,400 (×50) | Progression : circuits | 8.151 ms | 7.733 ms | 1.9 MiB | 11 |
| 10,400 (×50) | Progression : courbes du circuit par défaut | 7.486 ms | 6.801 ms | 1.8 MiB | 2 |
| 10,400 (×50) | Progression : géométrie SVG | 1.244 ms | 1.070 ms | 328.3 KiB | 2 |
| 10,400 (×50) | Progression : records et comparaison des sessions | 8.987 ms | 8.273 ms | 2.4 MiB | 950 |
| 1 (21 pilotes) | Détail des courses : déroulé de la plus grosse course | 0.090 ms | 0.086 ms | 59.9 KiB | 19 |
| 1 (21 pilotes) | Détail des courses : analyse tour par tour de la plus grosse course | 1.493 ms | 1.379 ms | 483.2 KiB | 19 |
| 1 (21 pilotes) | Détail des courses : classement de la plus grosse course | 0.271 ms | 0.263 ms | 139.0 KiB | 21 |

