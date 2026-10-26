## 5. 📈 Menu : Progression (`Progression.razor`)

> Spécification alignée sur le code au 26/10/2026 (`Progression.razor`, `ProgressionPresentation.cs`, `ProgressionChartLayout.cs`).

### Description
Onglet `/progression`, placé juste après « Mes courses » dans la barre de navigation. Il trace l'évolution dans le temps des meilleurs tours du pilote sélectionné (voir Options), pour un circuit et une catégorie donnés. Les temps sont regroupés par période (jour, semaine, mois ou année) ; chaque période affiche le min, le max et la moyenne de ses meilleurs tours.

### Spécifications fonctionnelles
* **Pilote :** uniquement le pilote sélectionné dans Options (fusions de pilotes appliquées, comme dans les autres vues). Les autres pilotes des sessions ne sont pas tracés.
* **Sessions prises en compte :** qualifications et courses du pilote sélectionné, avec les mêmes règles de type que « Mes courses ». Les essais ne sont pas tracés.
* **Circuit :** couple lieu (`TrackVenue`) + tracé (`TrackCourse`), comme pour le delta n-1 de « Mes courses ».
* **Catégorie :** même règle de déduction que « Mes courses » (`CarCategory.cs`). La liste ne propose que les catégories roulées par le pilote sur le circuit choisi.
* **Mesure source :** une session donne une mesure, le **meilleur tour valide** du pilote dans cette session (plus petit temps chronométré). Une session sans tour chronométré ne donne aucune mesure.
* **Voiture :** `CarType` exact. Une variante (ex. « Ferrari 296 LMGT3 Evo ») est une voiture distincte de son modèle de base.
* **Périodes (granularité) :** jour, semaine, mois (par défaut) ou année, calculés sur la date de la session en heure locale. La semaine est la semaine ISO (lundi → dimanche). Le point d'une période est placé au début de cette période.
* **Agrégats d'une période :** min, max et moyenne des mesures de la période. Le sélecteur « Valeur tracée » choisit celle que porte la ligne (moyenne par défaut) ; la bande semi-transparente va toujours du min au max.
* **Modes d'affichage :**
  * **Voitures mélangées** (par défaut) : deux courbes, **Qualification** et **Course**, toutes voitures confondues. Chaque courbe a sa bande min-max.
  * **Une courbe par voiture** : le sélecteur « Session » choisit Course (par défaut) **ou** Qualification (pas les deux). Une courbe par voiture, chacune de sa couleur. La bande min-max n'est dessinée que lorsqu'une seule courbe est visible, pour garder le graphique lisible.
* **Légende :** sous le graphique, une entrée par courbe (couleur, nom, nombre de sessions). Un clic sur une entrée masque ou réaffiche la courbe.
* **Axe des dates (X) :** à l'échelle réelle du temps, de la première à la dernière période ayant une mesure. Graduations adaptées à la granularité (jours, semaines, mois ou années).
* **Axe des temps (Y) :** temps au tour au format `m:ss.fff`, bornes calculées sur les données visibles avec une marge. Plus bas = plus rapide.
* **Périodes sans session :** la courbe garde la dernière valeur connue par un **palier horizontal en pointillé** jusqu'à la période suivante qui a une mesure, puis un trait vertical en pointillé la ramène à cette valeur. Ce palier ne compte pas comme une mesure : pas de point, pas d'info-bulle, pas de bande. Deux périodes consécutives avec mesures sont reliées par un trait plein.
* **Réglages :** boutons segmentés PÉRIODE, VALEUR TRACÉE, AFFICHAGE, et SESSION en mode par voiture ; circuit et catégorie en listes déroulantes.
* **Résumé chiffré** (au-dessus du graphique) : une ligne par courbe visible, à sa couleur : **RECORD** (meilleur tour et date de la session), **DÉBUT** (valeur tracée de la première période), **AUJOURD'HUI** (valeur tracée de la dernière période) et l'**écart** Aujourd'hui − Début (vert si plus rapide, rouge sinon, « une seule période » sinon).
* **Ligne du record :** un trait fin en tirets par courbe visible, à sa couleur, à la hauteur de son record.
* **Panneau de détail** (à droite du graphique, en dessous sur écran étroit) : survoler, focaliser ou cliquer un point affiche sa période, sa courbe, son min, sa moyenne, son max (la valeur tracée est mise en évidence) et son nombre de sessions. Sans point choisi, le panneau montre la dernière période de chaque courbe visible.
* **Tableau des périodes** (replié par défaut, bouton « Afficher le détail des périodes ») : une ligne par période et par courbe visible, de la plus récente à la plus ancienne : période, courbe, min, moyenne, max, sessions.
* **Rendu :** SVG généré en C# par un composant Razor, sans bibliothèque JavaScript ni dépendance. Les calculs (périodes, agrégats, paliers) se font dans `Models/ProgressionPresentation.cs`, la géométrie (échelles, graduations, tracés) dans `Models/ProgressionChartLayout.cs`, tous deux testés en MSTest.
* **Couleurs des courbes** (reportées dans `UI_DESIGN_SPEC.md`) :
  * mode voitures mélangées : Qualification cyan `#38bdf8`, Course rouge LMU `#e11d48` ;
  * mode par voiture : `#38bdf8`, `#f97316`, `#a3e635`, `#f472b6`, `#facc15`, `#2dd4bf`, `#c084fc`, `#94a3b8`, attribuées par ordre alphabétique de voiture puis réutilisées en boucle.
  * Le violet `#b066ff`, le vert `#22c55e` et le rouge `#ef4444` gardent leur sens WEC (meilleur de la classe, gain, perte) et ne servent pas aux courbes.
* **Sélection initiale :**
  * Ouverture directe de l'onglet : circuit et catégorie de la **course la plus récente** du pilote sélectionné (à défaut, de sa session la plus récente).
  * Depuis l'icône 📈 de « Mes courses » : le circuit du bloc et la **dernière catégorie roulée** par le pilote sur ce circuit.
* **Réglages enregistrés :** granularité, valeur tracée, mode d'affichage et session du mode par voiture sont enregistrés dans `appsettings.json` (section `LMU:Progression`) dès qu'ils changent, puis restaurés au lancement suivant. Le circuit et la catégorie ne sont pas enregistrés.
* **États de la page :**
  * Chargement initial sans session : spinner « Chargement de l'historique des courses… ».
  * Erreur de chargement sans session : encadré d'erreur avec le message et un lien vers Options.
  * Aucune session du pilote : « Aucune course disponible ».
  * Aucune mesure pour le circuit et la catégorie choisis : « Aucun tour chronométré pour ce circuit et cette catégorie ».
  * Une seule période : le point est affiché seul, sans ligne, avec sa bande min-max réduite à un trait vertical.
  * La page se met à jour automatiquement à chaque nouvel import, en conservant les sélections.

### Accès depuis « Mes courses »
* Une icône 📈 (lien avec `aria-label` « Voir la progression sur ce circuit ») est ajoutée dans l'en-tête de chaque bloc circuit, à côté du badge « N résultats ».
* Elle ouvre `/progression` avec le lieu, le tracé et la dernière catégorie roulée sur ce circuit passés dans l'URL (`?venue=…&course=…&category=…`).

### Wireframe ASCII
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   ACCUEIL   MES COURSES   PROGRESSION   CLASSEMENT   DÉTAIL DES COURSES   OPTIONS     Driver Tom   🟢 ACTIVE  │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  HISTORIQUE & PROGRESSION                                                                                                        │
│  Progression                                                                                                                     │
│  Évolution de vos meilleurs tours sur un circuit et dans une catégorie.                                                          │
│                                                                                                                                  │
│  CIRCUIT [ Circuit de Spa-Francorchamps ▼ ]   CATÉGORIE [ LMGT3 ▼ ]                                                              │
│  PÉRIODE [ Jour | Semaine | ■Mois | Année ]   VALEUR TRACÉE [ Min | ■Moyenne | Max ]                                             │
│  AFFICHAGE [ ■Voitures mélangées | Une courbe par voiture ]   SESSION [ ■Course | Qualification ] (mode par voiture)          │
│                                                                                                                                  │
│  ■ Course         RECORD 2:17.104 (14/03/2026)   DÉBUT 2:19.820 (novembre 2025)   AUJOURD'HUI 2:17.902 (avril 2026)   −1.918 s   │
│  ■ Qualification  RECORD 2:16.511 (02/04/2026)   DÉBUT 2:18.402 (novembre 2025)   AUJOURD'HUI 2:16.890 (avril 2026)   −1.512 s   │
│                                                                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 2:20.000 ┤                                                                                                                 │  │
│  │          │  ●━━━━━━━●                                                                                                      │  │
│  │ 2:19.000 ┤  ░░░░░░░░░╲                                                                                                     │  │
│  │          │            ●┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐                                       ┌──────────────────────────┐               │  │
│  │ 2:18.000 ┤  ○━━━━━━━○  ░░░░░░░░░░░░░░░░┆                                       │ Mars 2026 · Course        │               │  │
│  │          │            ╲                ●━━━━━━━━━━●                            │ Moyenne    2:17.412       │               │  │
│  │ 2:17.000 ┤             ○┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐ ░░░░░░░░░░ ╲                          │ Min 2:16.980 Max 2:17.844 │               │  │
│  │          │                             ○━━━━━━━━━━○  ●                         │ 4 sessions                │               │  │
│  │ 2:16.000 ┤                                           ○                         └──────────────────────────┘               │  │
│  │          └────────┬──────────┬──────────┬──────────┬──────────┬──────────┬───────                                          │  │
│  │                 nov.       déc.       janv.      févr.      mars       avr.                                                │  │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│  ● Course (12 sessions)   ○ Qualification (9 sessions)   ━ mesure   ┄ période sans session   ░ min-max                         │
│                                                                                                                                  │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  LE MANS ULTIMATE                                                                         RÉSULTATS LOCAUX · DONNÉES NON TRANSMISES │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

Mode « Une courbe par voiture » (session Course), légende :
```text
│  ● McLaren 720S LMGT3 Evo (8)   ● BMW M4 LMGT3 (3)   ● Ferrari 296 LMGT3 (2)   ● Ferrari 296 LMGT3 Evo (1)                       │
```

En-tête de bloc circuit dans « Mes courses » :
```text
│  CIRCUIT                                                                                                                         │
│  Circuit de Spa-Francorchamps                                                                         📈   [ 3 résultats ]       │
```
