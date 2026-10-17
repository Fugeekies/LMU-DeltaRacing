## 3. ⏱️ Menu : Détail des Courses (RaceDetails.razor)

### Description
Analyse télémétrique et classement détaillé d'une course spécifique sélectionnée par l'utilisateur.

### Spécifications fonctionnelles :
* Sélecteur : Liste déroulante des sessions enregistrées.
* Structure du Tableau : POS, Nom du coureur, Voiture, Secteur 1, Secteur 2, Secteur 3, Diff 1er.
* Règles de couleurs WEC :
  * Meilleur secteur absolu de la session : Fond violet #b066ff / badge 🟣.
  * Meilleur secteur personnel du pilote : Fond vert #22c55e / badge 🟢.
  * Temps standard : Texte jaune #eab308.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU DELTARACING]   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS       🟢 ONLINE   │
├───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                           │
│  ⏱️ ANALYSE DE SESSION                                                                                     │
│  Sélectionner une course : [ 🏎️ 16/10/2026 19:00 - Spa-Francorchamps (Race) - Hypercar       ▼ ]         │
│                                                                                                           │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ POS  NOM DU COUREUR        VOITURE         SECTEUR 1     SECTEUR 2     SECTEUR 3     DIFF 1ER       │  │
│  ├─────────────────────────────────────────────────────────────────────────────────────────────────────┤  │
│  │ 01   Antonio Fuoco         Ferrari 499P    🟣 38.120s    🟢 44.210s    🟡 40.121s    LEADER         │  │
│  │ 02   Sébastien Buemi       Toyota GR010    🟢 38.250s    🟣 44.180s    🟣 40.090s    +0.199s        │  │
│  │ 03   Berat Toktas (Vous)   Ferrari 499P    🟢 38.310s    🟡 44.400s    🟡 40.210s    +0.469s        │  │
│  │ 04   Krzysztof Pietruszka  Porsche 963     🟡 38.500s    🟡 44.610s    🟡 40.350s    +1.009s        │  │
│  │ 05   Ellie Wattson         Cadillac V-R    🟡 38.720s    🟡 44.800s    🟡 40.410s    +1.479s        │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                                           │
│  Légende WEC :  🟣 Best Absolu Session   🟢 Best Personnel   🟡 Temps Standard                            │
│                                                                                                           │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────┘
