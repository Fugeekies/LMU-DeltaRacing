# Spécification Visuelle & Design UI - LMU-DeltaRacing

Ce document définit l'identité visuelle, la charte graphique et l'ergonomie de l'application **LMU-DeltaRacing**. L'interface est conçue pour offrir une expérience immersive sombre (*Dark Racing Theme*), rappelant l'univers de *Le Mans Ultimate* et les logiciels de télémétrie d'ingénierie de piste du FIA WEC.

---

## 🎨 Identity & Charte Graphique

### 1. Palette de Couleurs & Ambiance
L'arrière-plan de l'application utilise un dégradé subtil passant d'un bleu nuit sombre (coin inférieur gauche) à un rouge bordeaux profond (coin supérieur droit), évoquant la charte officielle LMU :

* **Fond Principal (Gradient Global) :** `linear-gradient(135deg, #0d1b2a 0%, #0e1116 50%, #3a0d18 100%)`
* **Conteneurs / Cartes / Panneaux :** `rgba(14, 17, 22, 0.85)` avec flou d'arrière-plan (`backdrop-filter: blur(6px)`) et bordures fines `#30363d`.
* **Header / Navigation :** `rgba(10, 12, 16, 0.95)` fixé en haut de page (`sticky top-0`).
* **Texte Principal :** `#f0f6fc` (Blanc cassé haute lisibilité).
* **Texte Secondaire :** `#8b949e` (Gris pour libellés et sous-titres).
* **Accent Rouge LMU / WEC :** `#e11d48` (Utilisé pour le sous-lignage des onglets actifs et les boutons d'action).

---

### 2. Typographies
* **Interface & Données :** `Inter`, `Roboto` ou police sans-serif système pour une lisibilité parfaite des chronos et données textuelles.
* **Header & Onglets de Navigation :** `Oswald`, `Impact` ou police condensée en majuscules avec espacement des lettres (`letter-spacing: 0.05em`) pour reproduire le style officiel du site LMU.

---

### 3. Code Couleur Officiel du Chronométrage WEC (Secteurs & Deltas)
Ces règles de coloration doivent obligatoirement s'appliquer aux chronos, aux secteurs et aux écarts :
* 🟣 **Violet (`#b066ff`)** : Meilleur secteur de la **classe** dans la session (*Class Best*). En multiclasse, chaque classe a ses propres références.
* 🟢 **Vert (`#22c55e`)** : Amélioration de delta (n-1) et écart nul ou négatif au meilleur temps de la classe. Le vert n'est pas utilisé pour colorer les secteurs.
* 🟡 **Jaune (`#eab308`)** : Secteur standard (plus lent que le meilleur de la classe).
* 🔴 **Rouge (`#ef4444`)** : Secteur en retard / perte de temps ou régression de delta (n-1).

---

### 4. Badges de Catégories WEC
* **Hypercar :** Fond bordeaux / rouge foncé (`#881337`), texte blanc.
* **LMP2 :** Fond bleu vif (`#1d4ed8`), texte blanc.
* **LMGT3 :** Fond orange / ambre (`#d97706`), texte blanc ou noir.
* **LMP3** et **LMGTE** : couleurs à définir.

> État actuel : toutes les catégories utilisent le même badge neutre (bordure `#30363d`) ; les couleurs ci-dessus ne sont pas encore appliquées.

---

## 📂 Navigation & Templates des Vues (`UISpec/`)

La structure de navigation est désormais **horizontale** et positionnée en haut de page. 

Les maquettes visuelles ASCII détaillées ainsi que les règles métiers spécifiques de chaque menu sont formalisées dans les fichiers de spécifications dédiés situés dans le dossier `UISpec/` :

```text
UISpec/
├── MyRaces.template.md        --> Spécification de la vue "Mes Courses"
├── DriversRanking.template.md --> Spécification de la vue "Classement des Coureurs"
├── RaceDetails.template.md    --> Spécification de la vue "Détail des Courses"
└── Settings.template.md       --> Spécification de la vue "Options / Paramètres"
```

### Récapitulatif des 4 Menus Principaux :

1. **🏎️ Mes Courses (`/my-races`)** *(Voir template [`UISpec/MyRaces.template.md`](UISpec/MyRaces.template.md))*
   * Historique et progression personnelle groupés par circuit, une ligne par paire qualification + course.
   * Filtres par circuit et par catégorie (`Hypercar`, `LMP2`, `LMP3`, `LMGTE`, `LMGT3`).
   * Calcul automatique du delta par rapport à la ligne précédente (n-1) à circuit et catégorie identiques, quelle que soit la voiture.

2. **🏆 Classement des Coureurs (`/drivers-ranking`)** *(Voir template [`UISpec/DriversRanking.template.md`](UISpec/DriversRanking.template.md))*
   * Tableau récapitulatif de la rivalité Head-to-Head (H2H) avec les pilotes affrontés en course, dans la même classe.
   * Nombre de courses affrontées, victoires, défaites et pourcentage de victoire avec barre visuelle.

3. **⏱️ Détail des Courses (`/race-details`)** *(Voir template [`UISpec/RaceDetails.template.md`](UISpec/RaceDetails.template.md))*
   * Analyse d'un événement sélectionné via une liste déroulante, avec des sous-onglets Course, Qualification et Essais.
   * Meilleurs secteurs personnels colorés selon les règles WEC (🟣 meilleur de la classe, 🟡 standard), tour idéal et écart au leader de la classe (DIFF 1ER).

4. **⚙️ Options (`/settings`)** *(Voir template [`UISpec/Settings.template.md`](UISpec/Settings.template.md))*
   * Configuration du dossier des résultats XML du jeu (`UserData/Log/Results`).
   * Sélecteur de dossier, indicateur d'état du `FileSystemWatcher` et options de démarrage.

---

## 🛠️ Instructions d'implémentation Blazor & CSS

1. **Conformité aux Templates :** L'agent AI et les développeurs doivent suivre rigoureusement les wireframes ASCII et la disposition des colonnes définis dans le dossier `UISpec/`.
2. **Performance des Grilles :** Utiliser la virtualisation Blazor (`<Virtualize>`) ou un défilement optimisé pour les grilles de temps comportant de nombreux pilotes (> 40 voitures).
3. **Réactivité :** Les filtres et les sélecteurs doivent mettre à jour les tableaux sans rechargement complet de la page.
4. **Traçabilité :** Chaque changement de vue ou de filtre doit émettre des logs structurés (OpenTelemetry/Serilog).
