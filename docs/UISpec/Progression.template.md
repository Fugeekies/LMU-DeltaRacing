## 5. 📈 Menu : Progression (`Progression.razor`)

> Spécification du jalon « UI : Menu "Progression" », arrêtée le 26/10/2026. La page n'est pas encore implémentée.

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
  * **Une courbe par voiture** : le sélecteur « Session » choisit Qualification **ou** Course (pas les deux). Une courbe par voiture, chacune de sa couleur. La bande min-max n'est dessinée que lorsqu'une seule courbe est visible, pour garder le graphique lisible.
* **Légende :** sous le graphique, une entrée par courbe (couleur, nom, nombre de sessions). Un clic sur une entrée masque ou réaffiche la courbe.
* **Axe des dates (X) :** à l'échelle réelle du temps, de la première à la dernière période ayant une mesure. Graduations adaptées à la granularité (jours, semaines, mois ou années).
* **Axe des temps (Y) :** temps au tour au format `m:ss.fff`, bornes calculées sur les données visibles avec une marge. Plus bas = plus rapide.
* **Périodes sans session :** la courbe garde la dernière valeur connue par un **palier horizontal en pointillé** jusqu'à la période suivante qui a une mesure, puis un trait vertical en pointillé la ramène à cette valeur. Ce palier ne compte pas comme une mesure : pas de point, pas d'info-bulle, pas de bande. Deux périodes consécutives avec mesures sont reliées par un trait plein.
* **Info-bulle d'un point :** période, type de session, voiture (en mode par voiture), valeur tracée, min, max, moyenne et nombre de sessions.
* **Rendu :** SVG généré en C# par un composant Razor, sans bibliothèque JavaScript ni dépendance. Les calculs (périodes, agrégats, paliers, échelles) se font dans `Models/ProgressionPresentation.cs`, testé en MSTest.
* **Couleurs des courbes :** palette proposée, à valider et à reporter dans `UI_DESIGN_SPEC.md` :
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
* Une icône 📈 (bouton avec `aria-label` « Voir la progression sur ce circuit ») est ajoutée dans l'en-tête de chaque bloc circuit, à côté du badge « N résultats ».
* Elle ouvre `/progression` avec le lieu, le tracé et la dernière catégorie roulée sur ce circuit passés dans l'URL (`?venue=…&course=…&category=…`).

### Wireframe ASCII
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   PROGRESSION   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS   Driver Tom   🟢 ACTIVE  │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  HISTORIQUE & PROGRESSION                                                                                                        │
│  Progression                                                                                                                     │
│  Évolution de vos meilleurs tours sur un circuit et dans une catégorie.                                                          │
│                                                                                                                                  │
│  CIRCUIT [ Circuit de Spa-Francorchamps ▼ ]   CATÉGORIE [ LMGT3 ▼ ]                                                              │
│  PÉRIODE [ Jour | Semaine | ■Mois | Année ]   VALEUR TRACÉE [ Min | ■Moyenne | Max ]                                             │
│  AFFICHAGE [ ■Voitures mélangées | Une courbe par voiture ]   SESSION [ Qualification ▼ ] (mode par voiture uniquement)          │
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
