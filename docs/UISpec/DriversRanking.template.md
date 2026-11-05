## 2. 🏆 Menu : Classement des Coureurs (`DriversRanking.razor`)

> Spécification alignée sur le code au 05/11/2026 (`DriversRanking.razor`, `DriversRankingPresentation.cs`).

### Description
Vue récapitulative de la rivalité Head-to-Head (H2H) entre le pilote sélectionné et l'ensemble des pilotes croisés dans les sessions importées (route `/drivers-ranking`).

### Spécifications fonctionnelles
* **Sessions prises en compte :** uniquement les **courses** (type contenant `Race`) dans lesquelles le pilote sélectionné est classé. Les qualifications sont ignorées.
* **Règle métier H2H :** pour chaque course retenue et chaque adversaire classé **de la même classe** (`CarClass`) que le pilote :
  * les pilotes des autres classes sont ignorés (en multiclasse, devancer une voiture d'une classe plus lente n'est pas une victoire) ;
  * l'adversaire n'est compté qu'une fois par course (dédoublonnage par nom, insensible à la casse) ;
  * « Courses affrontées » +1 ;
  * position en classe (`ClassPosition`, à défaut position générale) du pilote < celle de l'adversaire → **Victoire** ;
  * position du pilote > position de l'adversaire → **Défaite** ;
  * deux pilotes d'une même course n'ont jamais la même position : victoires + défaites = courses affrontées ;
  * `% de victoire` = victoires / courses affrontées × 100.
* **Données complémentaires** (liste arrêtée le 05/11/2026), calculées sur les mêmes courses affrontées :
  * **Cote :** cote pilote actuelle de l'adversaire, lue dans `DriverRatingHistory.FindDriver` (jamais recalculée, règles dans [`DriverRating.template.md`](DriverRating.template.md)) ; `—` s'il n'est pas noté.
  * **Écart moyen :** moyenne, sur les courses affrontées, de (position en classe de l'adversaire − position en classe du pilote). Positif quand le pilote finit devant (vert), négatif quand il finit derrière (rouge).
  * **Contacts :** contacts entre les deux voitures, comptés avec `DrivingConductPresentation` (regroupement à 5 s, contact signalé par l'une ou l'autre voiture). `—` si aucune des courses affrontées n'a de journal `Stream` ; une course sans journal n'ajoute rien.
  * **Dernière course :** date (heure locale) et lieu de la course affrontée la plus récente (`Session.StartedAt`).
* **Recherche :** champ « Nom du pilote… » filtrant en direct à chaque frappe (« contient », insensible à la casse).
* **Tri :**
  * Courses affrontées (décroissant) — **par défaut** ; égalité départagée par nom (A → Z).
  * Victoires (décroissant) ; égalité départagée par courses affrontées puis nom.
  * % de victoire (décroissant) ; égalité départagée par courses affrontées puis nom.
  * Nom (A à Z).
* **Structure du tableau :**

  | Colonne | Contenu | Format |
  |---|---|---|
  | NOM DU PILOTE | Nom de l'adversaire | texte |
  | COTE | Cote pilote actuelle de l'adversaire | `1 568`, `—` sans cote |
  | COURSES AFFRONTÉES | Courses partagées et classées, dans la même classe | entier |
  | VICTOIRES | Courses où le pilote finit devant en classe | entier |
  | DÉFAITES | Courses où le pilote finit derrière en classe | entier |
  | % DE VICTOIRE | Barre proportionnelle (vert = victoires, rouge = défaites) suivie du pourcentage | `66.7%` (une décimale, point) |
  | ÉCART MOYEN | Places d'avance moyennes en classe | `+1.5` (vert), `−0.7` (rouge), `0.0` |
  | CONTACTS | Contacts entre les deux voitures | entier, `—` sans journal |
  | DERNIÈRE COURSE / CIRCUIT | Date de la dernière course affrontée et, dessous, son lieu | `dd/MM/yyyy` |

* **Colonnes ajustables :** une poignée (deux traits fins) au bord droit de chaque en-tête, rouge LMU au survol avec un curseur de redimensionnement ; la faire glisser change la largeur de sa colonne ; un double-clic sur ce bord rend au tableau ses largeurs automatiques. Les largeurs ne sont pas mémorisées : un rechargement de la page les rétablit (même règle dans Mes courses et Détail des courses, `wwwroot/resizable-columns.js`).

* Un badge dans l'en-tête du panneau indique le nombre d'adversaires après filtrage.
* **États de la page :**
  * Chargement initial sans session : spinner « Chargement de l'historique des courses… ».
  * Erreur de chargement sans session : encadré d'erreur renvoyant vers Options.
  * Aucun adversaire : « Aucun affrontement disponible ».
  * Recherche sans résultat : « Aucun pilote ne correspond à cette recherche ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS   Driver Tom   🟢 SURVEILLANCE ACTIVE                 │
├────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ RIVALITÉ & HISTORIQUE DES ADVERSAIRES                                                                                                          │
│ Classement des coureurs                                                                                                                        │
│ Vos résultats face aux pilotes rencontrés dans les sessions importées.                                                                         │
│                                                                                                                                                │
│ HISTORIQUE HEAD-TO-HEAD                                                                                                                [ 6 ]   │
│ Adversaires rencontrés                                                                                                                         │
│ RECHERCHER UN PILOTE [ Nom du pilote…            ]   TRIER PAR [ Courses affrontées (décroissant) ▼ ]                                          │
│                                                                                                                                                │
│ ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────── │
│ NOM DU PILOTE   COTE    COURSES AFFRONTÉES   VICTOIRES   DÉFAITES   % DE VICTOIRE            ÉCART MOYEN   CONTACTS   DERNIÈRE COURSE          │
│                                                                                                                       CIRCUIT                  │
│ ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────── │
│ Driver Iris     1 612   18                   12          6          [🟩🟩🟩🟩🟥🟥] 66.7%     +1.4          3          02/10/2026               │
│                                                                                                                       Spa-Francorchamps        │
│ Driver Axel     1 703   14                   5           9          [🟩🟩🟥🟥🟥🟥] 35.7%     −0.9          —          28/09/2026               │
│                                                                                                                       Monza                    │
│ Driver Paul     1 488   12                   8           4          [🟩🟩🟩🟩🟥🟥] 66.7%     +0.8          1          21/09/2026               │
│                                                                                                                       Le Mans                  │
│ Driver Lina     —       9                    9           0          [🟩🟩🟩🟩🟩🟩] 100.0%    +3.2          0          14/09/2026               │
│                                                                                                                       Imola                    │
│                                                                                                                                                │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
