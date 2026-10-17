## 1. 🏎️ Menu : Mes Courses (`MyRaces.razor`)

### Description
Vue axée sur la progression personnelle de l'utilisateur, groupée par circuit et filtrable par catégorie. Elle calcule dynamiquement les écarts de temps par rapport à la session immédiatement précédente ($n-1$) sur le même circuit et dans la même catégorie, sans tenir compte du véhicule.

### Spécifications fonctionnelles :
* Filtres : Circuit (Tous / Filtre spécifique) et Catégorie (Toutes / Hypercar / LMP2 / LMGT3).
* Structure du Tableau : Date / Heure, Catégorie, Voiture, Temps Qualif, Delta Qualif (n-1), Temps Course, Delta Course (n-1).
* Règle Métier Delta ($n-1$) :
  * Comparaison effectuée entre la session courante et la session précédente du même Circuit + même Catégorie.
  * Si le temps est meilleur (gain de temps) : Texte vert 🟢 -X.XXXs.
  * Si le temps est moins bon (perte de temps) : Texte rouge 🔴 +X.XXXs.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU DELTARACING]   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS                  │
├───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                           │
│  📊 HISTORIQUE & PROGRESSION PERSONNELLE                                                                  │
│  Filtres : [ Circuit : Tous ▼ ]   [ Catégorie : Toutes (Hypercar, LMP2, LMGT3) ▼ ]                         │
│                                                                                                           │
│  🏁 CIRCUIT DE SPA-FRANCORCHAMPS (3 sessions enregistrées)                                                │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  DATE / HEURE      CATÉGORIE   VOITURE        TEMPS QUALIF   DELTA (n-1)     TEMPS COURSE   DELTA (n-1)     │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  16/10/2026 19:00  Hypercar    Ferrari 499P   2:02.451       🟢 -0.245s      2:03.110       🟢 -0.120s      │
│  15/10/2026 21:15  Hypercar    Ferrari 499P   2:02.696       🔴 +0.102s      2:03.230       🔴 +0.450s      │
│  12/10/2026 14:00  LMGT3       BMW M4 GT3     2:17.594       ---             2:18.780       ---             │
│                                                                                                           │
│  🏁 CIRCUIT DE LA SARTHE - LE MANS (2 sessions enregistrées)                                              │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  DATE / HEURE      CATÉGORIE   VOITURE        TEMPS QUALIF   DELTA (n-1)     TEMPS COURSE   DELTA (n-1)     │
│  ───────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  14/10/2026 20:30  LMP2        Oreca 07       3:26.102       🟢 -0.510s      3:27.400       🟢 -0.310s      │
│  10/10/2026 18:00  LMP2        Oreca 07       3:26.612       ---             3:27.710       ---             │
│                                                                                                           │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────┘
