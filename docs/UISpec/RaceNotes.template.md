## 3 ter. 📝 Notes de course (Détail des courses, Mes courses, Analyse tour par tour)

> **Maquette à valider** (13/11/2026), jalon « Notes de course » de la [feuille de route](../ROADMAP.md). Stockage déjà en place : `RaceNotesStore`, fichier `race-notes.json`.

### Description
Le joueur note, pour chaque session, les conditions, le comportement de la voiture, les réglages et son ressenti. Le XML ne contient ni météo ni réglages : tout est saisi à la main. Une note appartient à une **session** (`Session.Id`), quel que soit le pilote affiché : ce sont les notes du joueur. Elle se saisit dans Détail des courses et se relit dans Mes courses et dans Comparer les sessions.

### Spécifications fonctionnelles
* **Champs**, tous facultatifs :
  * **Conditions** : boutons à bascule ☀️ Sec · 🌥️ Humide · 🌧️ Pluie · 🌦️ Variable. Cliquer de nouveau sur le bouton actif le désélectionne.
  * **Ressenti** : 5 étoiles cliquables (★★★★☆). Cliquer sur l'étoile active efface le ressenti.
  * **Voiture** : comportement (zone de texte, 3 lignes).
  * **Réglages** : texte libre (zone de texte, 3 lignes).
  * **Remarques** : texte libre (zone de texte, 3 lignes).
* **Suggestion « Pluie »** : si le pilote sélectionné (Options) a roulé au moins un tour en pneus `Wet` dans la session et qu'aucune condition n'est choisie, une ligne propose « 💧 Pneus pluie montés pendant la session. [Choisir Pluie] ». Rien n'est enregistré sans clic.
* **Enregistrement explicite** : bouton **Enregistrer**. **Annuler** revient à la note enregistrée, **Supprimer la note** vide tous les champs et enregistre (une note vide est supprimée du fichier). Message dans la zone d'état : « Note enregistrée. », « Note supprimée. » ou l'erreur.
* **Fichier illisible** (`RaceNotesStore.IsUnreadable`) : bandeau d'alerte « Le fichier des notes est illisible : vos notes ne sont pas affichées et aucune modification n'est enregistrée, pour ne pas les perdre. » ; le bouton « + Ajouter une note » est désactivé.

#### A. Détail des courses : bandeau « Notes »
* Bandeau entre les sous-onglets et le classement, pour la session de l'onglet actif (Course, Qualification ou Essais). Il reste visible sans faire défiler un classement de 30 pilotes.
* **Sans note** : une ligne « Aucune note pour cette session. [+ Ajouter une note] ».
* **Avec note, en lecture** : une ligne de résumé (icône et libellé des conditions, étoiles), puis les textes non vides, chacun précédé de son libellé ; bouton **Modifier**.
* **En modification** : le formulaire remplace le résumé. Changer d'onglet ou d'événement avec des modifications non enregistrées les abandonne (pas de fenêtre de confirmation).
* **Essais fusionnés** : la note de l'onglet Essais est rattachée à la première séance de l'événement.

#### B. Mes courses : icône 📝
* Une ligne réunit la course et la qualification d'un événement : l'icône 📝 s'affiche après la date si l'une des deux sessions a une note.
* Survol : résumé de chaque note, « Course : 🌧️ Pluie · ★★★★☆ · Réglages : aileron 6… » (réglages coupés à 60 caractères), une ligne par session.

#### C. Analyse tour par tour : colonne « Notes » de Comparer les sessions
* Dernière colonne **NOTES** : icône des conditions et étoiles, puis les réglages sur une ligne, coupés avec « … » ; le survol donne la note complète. `—` sans note.
* C'est l'usage principal : comparer les réglages et les conditions des sessions d'une même voiture sur un même circuit.

### Wireframe ASCII

#### A. Détail des courses : sans note, puis en lecture
```text
│  SÉLECTIONNER UNE COURSE                                                                                                     │
│  [ 02/10/2026 21:00 - Circuit de Spa-Francorchamps (Qualif · Course) - LMGT3 ▼ ]     Circuit de Spa-Francorchamps            │
│  [ COURSE ]   QUALIFICATION                                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │ 📝 NOTES   Aucune note pour cette session.                                                  [ + Ajouter une note ]  │   │
│  └──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│  POS  CLASSE     PILOTE             SECTEUR 1    SECTEUR 2    SECTEUR 3    TOUR IDÉAL   MEILLEUR TOUR   TEMPS TOTAL         │
│  ...                                                                                                                         │

│  [ COURSE ]   QUALIFICATION                                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │ 📝 NOTES   🌧️ Pluie · ★★★★☆                                                                          [ Modifier ]  │   │
│  │ VOITURE     Sous-vire en entrée de virage lent, très stable à Eau Rouge.                                             │   │
│  │ RÉGLAGES    Aileron 6, pressions −0,2, ABS 3.                                                                        │   │
│  │ REMARQUES   Arrêt trop tardif au tour 14.                                                                            │   │
│  └──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│  POS  CLASSE     PILOTE             SECTEUR 1    ...                                                                         │
```

#### A. Détail des courses : en modification
```text
│  ┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │ 📝 NOTES DE LA COURSE                                                                                                │   │
│  │                                                                                                                      │   │
│  │ CONDITIONS   [ ☀️ Sec ]  [ 🌥️ Humide ]  [▓🌧️ Pluie▓]  [ 🌦️ Variable ]          RESSENTI   ★ ★ ★ ★ ☆                 │   │
│  │ 💧 Pneus pluie montés pendant la session.  [ Choisir Pluie ]      ← seulement si aucune condition n'est choisie       │   │
│  │                                                                                                                      │   │
│  │ VOITURE                                  RÉGLAGES                                 REMARQUES                          │   │
│  │ ┌──────────────────────────────────┐     ┌──────────────────────────────────┐     ┌──────────────────────────────┐   │   │
│  │ │ Sous-vire en entrée de virage    │     │ Aileron 6, pressions −0,2,       │     │ Arrêt trop tardif au tour 14.│   │   │
│  │ │ lent, très stable à Eau Rouge.   │     │ ABS 3.                           │     │                              │   │   │
│  │ │                                  │     │                                  │     │                              │   │   │
│  │ └──────────────────────────────────┘     └──────────────────────────────────┘     └──────────────────────────────┘   │   │
│  │                                                                                                                      │   │
│  │ [ Enregistrer ]   [ Annuler ]                                                          [ Supprimer la note ]         │   │
│  └──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
```
Sur téléphone, les trois zones de texte passent l'une sous l'autre.

#### B. Mes courses
```text
│  CATÉGORIE  DATE / HEURE         VOITURE       CLASS.  GRILLE MT COURSE  DELTA COURSE  TOURS  TEMPS TOTAL MT QUALIF  ...      │
│  ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  LMGT3      02/10/2026 21:00 📝  Porsche 911   4e      6      2:44.849   🟢 -0.310s    8      22:37.645   2:43.902   ...      │
│                              └─ survol : Course : 🌧️ Pluie · ★★★★☆ · Réglages : Aileron 6, pressions −0,2, ABS 3.            │
│                                          Qualification : ☀️ Sec · ★★★☆☆                                                      │
│  LMGT3      25/09/2026 20:40     Porsche 911   7e      9      2:45.159   🔴 +0.120s    8      22:51.003   2:44.300   ...      │
```

#### C. Analyse tour par tour : Comparer les sessions
```text
│  COMPARER LES SESSIONS                                                                                                       │
│  DATE / HEURE      SESSION        VOITURE        CLASS.  MEILLEUR TOUR   RYTHME     RÉGULARITÉ  V. POINTE  ARRÊTS  NOTES                       │
│  ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────── │
│  02/10/2026 21:00  Course         Porsche 911    4e      2:44.849        2:46.120   ±0.412s     268 km/h   1       🌧️ ★★★★☆ Aileron 6, press… │
│                                                          +1.203s                                                                              │
│  02/10/2026 20:40  Qualification  Porsche 911    6e      2:43.902 Record —          —           271 km/h   —       ☀️ ★★★☆☆                    │
│  25/09/2026 20:40  Course         Porsche 911    7e      2:45.159        2:47.010   ±0.655s     266 km/h   1       —                          │
```

### Points à valider
1. **Place du bandeau** dans Détail des courses : entre les onglets et le classement (proposé) ou sous le classement.
2. **Enregistrement** : bouton Enregistrer (proposé) ou enregistrement automatique à chaque modification.
3. **Essais fusionnés** : note rattachée à la première séance (proposé), ou pas de note sur l'onglet Essais.
4. **Icônes des conditions** : emojis (proposé, comme 📈 de Mes courses) ou badges texte aux couleurs de `UI_DESIGN_SPEC.md`.
