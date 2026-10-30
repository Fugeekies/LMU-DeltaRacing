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

### 4. Couleurs officielles des catégories
Chaque catégorie prend la couleur de fond de son numéro de course, en aplat, sans dégradé, sauf LMGT3 (voir ci-dessous). LMP2 et LMP3 sont mesurées sur les visuels de la [page Catégories de l'ELMS](https://www.europeanlemansseries.com/fr/page/categories).
* **Hypercar :** Rouge (`#c8102e`), texte blanc. Teinte choisie : la catégorie n'existe pas en ELMS.
* **LMP2 :** Bleu (`#144a94`), texte blanc.
* **LMP3 :** Violet (`#623078`), texte blanc.
* **LMGTE :** Vert (`#15803d`), texte blanc.
* **LMGT3 :** Orange (`#d97706`), texte blanc. Son numéro officiel est vert, comme celui de LMGTE : l'orange évite de confondre les deux catégories.
* **Classe inconnue :** Gris (`#8b949e`), texte sombre.

Ces couleurs s'appliquent aux badges (Mes courses, Détail des courses), aux barres et légendes de l'Accueil, et aux séparateurs de catégorie de Mes courses. Dans la console, la catégorie est un badge sur la même couleur : le bleu et le violet sont illisibles en texte sur un terminal noir.

---

### 5. Courbes du graphique de Progression
* **Voitures mélangées :** Course en rouge LMU (`#e11d48`), Qualification en cyan (`#38bdf8`).
* **Une courbe par voiture :** `#38bdf8`, `#f97316`, `#a3e635`, `#f472b6`, `#facc15`, `#2dd4bf`, `#c084fc`, `#94a3b8`, attribuées par ordre alphabétique de voiture puis réutilisées en boucle.
* Le violet, le vert et le rouge WEC ci-dessus gardent leur sens (meilleur de la classe, gain, perte) et ne servent pas aux courbes. Seul l'écart du résumé chiffré utilise le vert (plus rapide) ou le rouge (plus lent).

### 6. Écran de chargement au démarrage
* Pendant l'import au démarrage, un écran plein page remplace toute l'application (header compris) : logo Delta Racing (icône de l'installeur `assets/icons/Logo-Delta-Racing-rouge-et-blanc-1.ico`, déjà servie comme `wwwroot/favicon.ico`), « IMPORT INITIAL » en rouge LMU, titre « Chargement de vos résultats ».
* La barre de progression est une route : une roue (pneu sombre, jante rouge `#e11d48`) tourne en continu et avance de gauche à droite selon les fichiers traités / total ; la portion parcourue est rouge. Sous la route : « N / M fichiers traités » (ou « Recherche des fichiers de résultats… » tant que le total est inconnu) et le pourcentage.
* Sous le pourcentage : numéro de version de l'application (`v0.1.2`) en gris discret. L'import étant très court en Release, la version reste consultable dans Options.
* L'écran disparaît quand l'import au démarrage est terminé, y compris en erreur (l'erreur s'affiche alors dans les pages). Un rechargement depuis Options et les imports du watcher ne l'affichent pas.
* Avec `prefers-reduced-motion`, la roue avance sans tourner.
* **Fenêtre de nouvelle version :** quand une version plus récente est publiée, un encart fixe en bas à droite (fond `--surface-raised`, liseré rouge LMU) affiche « Nouvelle version disponible : vX.Y.Z » et « Voir dans Options », lien vers `/settings`. Il apparaît une fois l'écran de chargement passé, reste 3 secondes et ne revient pas avant le prochain lancement, même si la page est rechargée.

### 7. Classement dans « Mes courses »
Couleurs choisies pour rester lisibles sur le fond sombre, sans réutiliser le violet, le vert, le jaune et le rouge WEC. Chaque ligne est encadrée d'une bordure de 1 px de sa couleur et la position (colonne CLASSEMENT) est écrite dans cette couleur ; le fond de la ligne n'en garde qu'une trace : 10 % pour le podium, pour que sa couleur se reconnaisse d'un coup d'œil, 5 % pour le top 10.
* **P1 :** or `#d4af37`.
* **P2 :** argent `#c0c7d1`.
* **P3 :** bronze cuivré `#d2733f` : plus rouge que l'or, pour ne pas confondre P1 et P3. L'or garde son ton mat, un or plus vif se confondrait avec le jaune WEC des secteurs.
* **Coupe du podium :** de P1 à P3, une petite coupe (SVG de 14 px, dessinée avec `currentColor`) précède la position, dans la couleur du rang : or, argent ou bronze. Elle n'apparaît pas sur un `DNF`, un `DQ` ou un `NC`, même avec une position en classe.
* **P4-P5 :** bleu `#60a5fa`.
* **P6-P10 :** même bleu, plus léger : bordure `#60a5fa` à 45 %, position mélangée à 50 % avec le blanc du texte.

---

## 📂 Navigation & Templates des Vues (`UISpec/`)

La structure de navigation est désormais **horizontale** et positionnée en haut de page. 

Les maquettes visuelles ASCII détaillées ainsi que les règles métiers spécifiques de chaque menu sont formalisées dans les fichiers de spécifications dédiés situés dans le dossier `UISpec/` :

```text
UISpec/
├── Home.template.md           --> Spécification de la vue "Accueil"
├── MyRaces.template.md        --> Spécification de la vue "Mes Courses"
├── Progression.template.md    --> Spécification de la vue "Progression"
├── DriversRanking.template.md --> Spécification de la vue "Classement des Coureurs"
├── RaceDetails.template.md    --> Spécification de la vue "Détail des Courses"
├── Settings.template.md       --> Spécification de la vue "Options / Paramètres"
└── Console.template.md        --> Spécification de la console Spectre (Accueil, Mes courses, Options)
```

### Récapitulatif des Menus Principaux :

1. **🏠 Accueil (`/`)** *(Voir template [`UISpec/Home.template.md`](UISpec/Home.template.md))*
   * Tableau de bord de la carrière du pilote : bandeau Pilote (favoris inclus) et Forme récente, puis tuiles Palmarès, Roulage, Qualification & départ et Abandons.
   * Chaque compteur affiche son total et sa ventilation par catégorie.

2. **🏎️ Mes Courses (`/my-races`)** *(Voir template [`UISpec/MyRaces.template.md`](UISpec/MyRaces.template.md))*
   * Historique et progression personnelle groupés par circuit, une ligne par paire qualification + course.
   * Filtres par circuit et par catégorie (`Hypercar`, `LMP2`, `LMP3`, `LMGTE`, `LMGT3`).
   * Calcul automatique du delta par rapport à la ligne précédente (n-1) à circuit et catégorie identiques, quelle que soit la voiture.

3. **📈 Progression (`/progression`)** *(Voir template [`UISpec/Progression.template.md`](UISpec/Progression.template.md))*
   * Évolution des meilleurs tours du pilote sur un circuit et une catégorie, agrégés par jour, semaine, mois ou année (min, max, moyenne).
   * Courbes Qualification et Course toutes voitures confondues, ou une courbe par voiture ; accessible depuis l'icône 📈 des blocs circuit de « Mes courses ».

4. **🏆 Classement des Coureurs (`/drivers-ranking`)** *(Voir template [`UISpec/DriversRanking.template.md`](UISpec/DriversRanking.template.md))*
   * Tableau récapitulatif de la rivalité Head-to-Head (H2H) avec les pilotes affrontés en course, dans la même classe.
   * Nombre de courses affrontées, victoires, défaites et pourcentage de victoire avec barre visuelle.

5. **⏱️ Détail des Courses (`/race-details`)** *(Voir template [`UISpec/RaceDetails.template.md`](UISpec/RaceDetails.template.md))*
   * Analyse d'un événement sélectionné via une liste déroulante, avec des sous-onglets Course, Qualification et Essais.
   * Meilleurs secteurs personnels colorés selon les règles WEC (🟣 meilleur de la classe, 🟡 standard), tour idéal, temps total et écart au leader de la classe.

6. **⚙️ Options (`/settings`)** *(Voir template [`UISpec/Settings.template.md`](UISpec/Settings.template.md))*
   * Configuration du dossier des résultats XML du jeu (`UserData/Log/Results`).
   * Sélecteur de dossier, indicateur d'état du `FileSystemWatcher` et options de démarrage.

---

## 🛠️ Instructions d'implémentation Blazor & CSS

1. **Conformité aux Templates :** L'agent AI et les développeurs doivent suivre rigoureusement les wireframes ASCII et la disposition des colonnes définis dans le dossier `UISpec/`.
2. **Performance des Grilles :** Utiliser la virtualisation Blazor (`<Virtualize>`) ou un défilement optimisé pour les grilles de temps comportant de nombreux pilotes (> 40 voitures).
3. **Réactivité :** Les filtres et les sélecteurs doivent mettre à jour les tableaux sans rechargement complet de la page.
4. **Traçabilité :** Chaque changement de vue ou de filtre doit émettre des logs structurés (OpenTelemetry/Serilog).
