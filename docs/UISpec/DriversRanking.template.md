## 2. 🏆 Menu : Classement des Coureurs (`DriversRanking.razor`)

> Spécification alignée sur le code au 25/10/2026 (`DriversRanking.razor`, `DriversRankingPresentation.cs`).

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
  | COURSES AFFRONTÉES | Courses partagées et classées, dans la même classe | entier |
  | VICTOIRES | Courses où le pilote finit devant en classe | entier |
  | DÉFAITES | Courses où le pilote finit derrière en classe | entier |
  | % DE VICTOIRE | Barre proportionnelle (vert = victoires, rouge = défaites) suivie du pourcentage | `66.7%` (une décimale, point) |

* Un badge dans l'en-tête du panneau indique le nombre d'adversaires après filtrage.
* **États de la page :**
  * Chargement initial sans session : spinner « Chargement de l'historique des courses… ».
  * Erreur de chargement sans session : encadré d'erreur renvoyant vers Options.
  * Aucun adversaire : « Aucun affrontement disponible ».
  * Recherche sans résultat : « Aucun pilote ne correspond à cette recherche ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS   Driver Tom   🟢 SURVEILLANCE ACTIVE │
├───────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  RIVALITÉ & HISTORIQUE DES ADVERSAIRES                                                                        │
│  Classement des coureurs                                                                                      │
│  Vos résultats face aux pilotes rencontrés dans les sessions importées.                                       │
│                                                                                                               │
│  HISTORIQUE HEAD-TO-HEAD                                                                              [ 6 ]   │
│  Adversaires rencontrés                                                                                       │
│  RECHERCHER UN PILOTE [ Nom du pilote…            ]   TRIER PAR [ Courses affrontées (décroissant) ▼ ]       │
│                                                                                                               │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  NOM DU PILOTE             COURSES AFFRONTÉES    VICTOIRES       DÉFAITES       % DE VICTOIRE                  │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  Driver Iris               18                    12              6              [🟩🟩🟩🟩🟥🟥] 66.7%            │
│  Driver Axel               14                    5               9              [🟩🟩🟥🟥🟥🟥] 35.7%            │
│  Driver Paul               12                    8               4              [🟩🟩🟩🟩🟥🟥] 66.7%            │
│  Driver Lina               9                     9               0              [🟩🟩🟩🟩🟩🟩] 100.0%           │
│                                                                                                               │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
