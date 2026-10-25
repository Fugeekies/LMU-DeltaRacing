## 2. 🏆 Menu : Classement des Coureurs (`DriversRanking.razor`)

> Spécification alignée sur le code au 25/10/2026 (`DriversRanking.razor`, `DriversRankingPresentation.cs`).

### Description
Vue récapitulative de la rivalité Head-to-Head (H2H) entre le pilote sélectionné et l'ensemble des pilotes croisés dans les sessions importées (route `/drivers-ranking`).

### Spécifications fonctionnelles
* **Sessions prises en compte :** toutes les sessions importées (qualifications **et** courses ; les Practice ne sont jamais importées) dans lesquelles le pilote sélectionné a une position de classement.
* **Règle métier H2H :** pour chaque session retenue et chaque adversaire classé (position renseignée) :
  * l'adversaire n'est compté qu'une fois par session (dédoublonnage par nom, insensible à la casse) ;
  * « Courses affrontées » +1 ;
  * position du pilote < position de l'adversaire → **Victoire** ;
  * position du pilote > position de l'adversaire → **Défaite** ;
  * la comparaison utilise le **classement général** de la session, toutes classes confondues ;
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
  | COURSES AFFRONTÉES | Sessions partagées et classées | entier |
  | VICTOIRES | Sessions où le pilote finit devant | entier |
  | DÉFAITES | Sessions où le pilote finit derrière | entier |
  | % DE VICTOIRE | Part des victoires | `66.7%` (une décimale, point) |

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
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS   Driver 2038  🟢 SURVEILLANCE ACTIVE │
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
│  Driver 2161               18                    12              6              66.7%                          │
│  Driver 1662               14                    5               9              35.7%                          │
│  Driver 0412               12                    8               4              66.7%                          │
│  Driver 0087               9                     9               0              100.0%                         │
│                                                                                                               │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Écarts connus
Les comportements à corriger ou à arbitrer sont suivis dans la [feuille de route](../ROADMAP.md), jalon « Écarts UI à traiter ».
