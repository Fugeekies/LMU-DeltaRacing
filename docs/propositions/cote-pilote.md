# Proposition — Cote pilote (Elo multijoueur)

> 03/11/2026. Fonctionnalité livrée avec ce document : [`UISpec/DriverRating.template.md`](../UISpec/DriverRating.template.md).

## Le manque

Les pages existantes mesurent le pilote contre lui-même (Progression, Mes courses) ou comptent des résultats bruts (Accueil, Classement des coureurs). Une 5e place derrière quatre pilotes très rapides et une 5e place dans un plateau débutant y pèsent pareil. Aucune page ne répond à « quel est mon niveau face aux pilotes que je croise, et est-ce que je progresse ? ».

## La réponse : une cote Elo multijoueur

Chaque course est lue comme une série de duels entre les pilotes d'une classe. Battre un pilote mieux coté rapporte plus de points, perdre contre un pilote moins bien coté en coûte plus. Les fichiers de résultats contiennent **tous** les pilotes de chaque course : la cote de chaque adversaire se construit avec les mêmes fichiers, sans aucune donnée extérieure ni requête réseau.

## Décisions

| Sujet | Décision | Raison |
|---|---|---|
| Sessions | Courses seulement | Comme le Classement des coureurs : une qualification n'est pas un résultat. |
| Comparaison | Par classe | Règle déjà en place dans le H2H : battre une GT3 en Hypercar ne dit rien. |
| Statut `None` | Classe non notée si **aucun** pilote n'est arrivé ; sinon tous les pilotes à leur position officielle | Voir la mesure ci-dessous. Garder les positions officielles évite d'afficher une place différente de « Mes courses ». |
| Départ | 1 500 | Convention Elo. |
| K | 64 pendant 5 courses (calibration), puis 32 | Pratique de la FIDE (K plus fort pour un nouveau joueur), adaptée à des historiques courts : beaucoup de pilotes des fixtures n'ont qu'une dizaine de courses. |
| Normalisation | Variation divisée par (pilotes − 1) | Une course de 40 voitures ne doit pas peser 20 fois plus qu'un duel. |
| Classement | Pilotes sortis de calibration seulement | Une cote sur 1 ou 2 courses n'a pas de sens. |
| Paliers | Platine 10 %, Or 30 %, Argent 60 %, Bronze au-delà, en part du classement | Des seuils fixes n'auraient aucun sens : l'échelle dépend des fichiers de chacun. Les parts s'adaptent à n'importe quel historique. Noms de la catégorisation FIA des pilotes. |
| Console | Pas de vue | En attente du jalon « Avenir de la console Spectre ». |

## Mesures sur les 342 fixtures

**Statut `None` :** 572 lignes sur 2 303 en course. Il désigne les voitures encore en piste à l'écriture du fichier. 66 courses en ont quelques-unes (souvent des retardataires à un tour du vainqueur, classées derrière lui par LMU) ; dans les courses que Driver Tom quitte au premier tour, **tout** le plateau est en `None`, dans l'ordre de la grille. Exclure seulement les pilotes `None` laissait ces courses notées sur 2 ou 3 abandons à 0 tour (Tom gagnait +9 points en finissant 18e). Avec la règle retenue, 19 courses sur 107 ne sont pas notées et les autres gardent leurs positions officielles.

**Réglage de K** (cote de Driver Tom et écart des cotes classées) :

| K calibration / K ensuite | Cote de Tom | Rang | Cote max / min | Seuil Platine |
|---|---|---|---|---|
| 40 / 20 (10 courses) | 1 558 | 7 / 228 | 1 586 / 1 421 | 1 537 |
| **64 / 32 (5 courses)** | **1 568** | **9 / 228** | **1 615 / 1 403** | **1 549** |
| 80 / 40 (8 courses) | 1 576 | 13 / 228 | 1 637 / 1 368 | 1 569 |

64 / 32 donne des variations lisibles (de ±1 à ±32 points par course) sans que quelques courses heureuses ne propulsent un pilote en tête. L'écart reste resserré sur les fixtures, car la plupart des pilotes n'y ont que 6 à 20 courses : il s'élargit avec l'historique.

**Coût :** 7 ms pour les 208 qualifications et courses des fixtures, 187 ms à ×50 (10 400 sessions), moins que le Profil de conduite (471 ms). Mesuré par les benchmarks de la catégorie `Views`.

## Limites assumées

- La cote ne mesure que les grilles des fichiers de l'utilisateur : ce n'est pas le Driver Rating officiel de LMU, et l'encadré de la page le dit.
- Un alias des fixtures peut désigner plusieurs personnes (fusion des pilotes à course unique) : leur cote mélange ces personnes.
- Les égalités de position n'existent pas dans les fixtures ; elles valent un demi-duel si elles apparaissent.
