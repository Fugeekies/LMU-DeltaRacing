## 2. 🏆 Menu : Classement des Coureurs (DriversRanking.razor)

### Description
Vue récapitulative de la rivalité Head-to-Head (H2H) avec l'ensemble des pilotes croisés dans l'historique des sessions importées.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU DELTARACING]   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS                  │
├───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                           │
│  🥊 RIVALITÉ & HISTORIQUE DES ADVERSAIRES                                                         │
│  Rechercher un pilote : [ 🔍 Nom du pilote...              ]  Tri : [ Nb de courses (Décroiss.) ▼ ] │
│                                                                                                           │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  NOM DU PILOTE             COURSES AFFRONTÉES    VICTOIRES       DÉFAITES       % DE VICTOIRE             │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  Berat Toktas              18                    12              6              66.7%  [🟩🟩🟩🟥]         │
│  Ellie Wattson             14                    5               9              35.7%  [🟩🟥🟥🟥]         │
│  Krzysztof Pietruszka      12                    8               4              66.7%  [🟩🟩🟩🟥]         │
│  Christian Rootman         9                     9               0              100.0% [🟩🟩🟩🟩]         │
│  Apex Overtake             7                     2               5              28.6%  [🟩🟥🟥🟥]         │
│  John Veneris              4                     3               1              75.0%  [🟩🟩🟩🟥]         │
│                                                                                                           │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────┘
