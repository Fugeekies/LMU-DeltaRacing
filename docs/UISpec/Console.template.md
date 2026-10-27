## 7. 🖥️ Console Spectre (`LMU-DeltaRacing.Console`)

> Spécification alignée sur le code au 27/10/2026 (`ConsoleApplication.cs`, `Views/*.cs`).

### Description
Outil en ligne de commande (Spectre.Console) qui reprend trois vues du dashboard : **Accueil**, **Mes courses** et **Options**. L'interface est volontairement simple : un bandeau d'état, un menu principal, une vue affichée à la fois. Les anciennes vues « Par circuit » et « Par circuit et voiture » et leurs graphiques sont supprimées.

Les règles métier sont **celles du dashboard**, calculées par le même code partagé :
* Accueil : [`Home.template.md`](Home.template.md) ;
* Mes courses : [`MyRaces.template.md`](MyRaces.template.md) ;
* Options : [`Settings.template.md`](Settings.template.md), sauf les écarts listés plus bas.

Seul le rendu change : pas de tuiles ni de barres empilées, des tableaux et des panneaux Spectre.

### Spécifications générales
* **Pilote :** le pilote principal (`LMU:UserName`), fusions de pilotes appliquées. Au premier démarrage sans pilote configuré, le plus fréquent est détecté et enregistré à la fin de l'import initial, comme dans le dashboard.
* **Essais :** toujours ignorés à l'import (`SessionImportStatus.SkippedPractice`), jamais stockés ni affichés. Pas d'option pour les charger.
* **Aucun argument en ligne de commande :** comme le dashboard, la console charge toujours le dossier configuré dans `ResultsDirectory` (ou détecté automatiquement). Les arguments éventuels sont ignorés ; le mode actuel « fichiers en argument, statistiques puis sortie » est supprimé.
* **Configuration :** section `LMU` du `appsettings.json` placé à côté de l'exécutable de la console, avec les mêmes clés que le dashboard (`ResultsDirectory`, `UserName`, `WatchResultsOnStartup`, `DriverMerges`) plus `LogFilePath` et `FileMinimumLogLevel`. Ce fichier est distinct de celui du dashboard.
* **Logs :** aucun log dans le terminal ; JSON Lines dans le fichier configuré, comme aujourd'hui.
* **Largeur visée :** 120 colonnes. En dessous, Spectre replie les colonnes de texte ; les tableaux restent lisibles à partir de 100 colonnes.
* **Couleurs (markup Spectre) :** catégories et pastilles avec les codes hexadécimaux de [`Home.template.md`](Home.template.md) ; gain de temps en vert, perte en rouge ; texte secondaire en `grey`.

### Démarrage
1. Titre `LMU DeltaRacing` (règle Spectre, alignée à gauche).
2. Barre de progression « Chargement des résultats » (nom du fichier en cours en gris), effacée à la fin.
3. Résumé sur une ligne : `N session(s) importée(s) · N essai(s) ignoré(s) · N échec(s)`, puis une ligne par fichier en échec (nom du fichier + message).
4. Démarrage de la surveillance si `WatchResultsOnStartup` est vrai.
5. Bandeau d'état puis menu principal.

Dossier illisible : message d'erreur en rouge, puis **menu principal quand même**, pour pouvoir corriger le dossier dans Options. Seuls des réglages de log invalides arrêtent la console.

### Bandeau d'état
Affiché au-dessus du menu principal et en tête de chaque vue :

```text
 LMU DeltaRacing ─ Driver Tom ─ 🟢 Surveillance active ─ 170 sessions ─ 0 échec
```

* Pilote : nom, ou « Pilote non défini ».
* État : 🔴 « Résultats indisponibles » (dossier illisible), 🟢 « Surveillance active », 🟡 « Surveillance inactive ».
* Échecs en rouge si > 0.

### Imports pendant la surveillance
* Le watcher n'écrit **jamais** dans le terminal pendant qu'un menu est affiché (aujourd'hui, ses messages cassent le `SelectionPrompt`).
* Les résultats sont mis en file d'attente puis affichés au prochain retour au menu principal, sous le bandeau :

  ```text
   + 2 nouveaux résultats : Spa — Race · Spa — Qualify
   ✖ Échec : 2026_10_27_21_04_11-55R1.xml — XML illisible
  ```
* Une vue affichée après un import utilise les sessions alors présentes en mémoire. Il n'y a pas de rafraîchissement automatique d'une vue déjà affichée.

### Menu principal
`SelectionPrompt` :

```text
 Que voulez-vous afficher ?
 > Accueil
   Mes courses
   Options
   Quitter
```

* Chaque vue efface l'écran (`AnsiConsole.Clear()`), affiche le bandeau puis son contenu.
* Une vue se termine par son propre menu (« Retour » en dernier choix). Échap n'est pas géré par Spectre : on revient par ce choix.
* « Quitter » arrête la surveillance et termine le processus (code 1 s'il y a eu des échecs d'import, comme aujourd'hui).

### Vue « Accueil »
Mêmes données et mêmes règles que `Home.razor`. Différences de rendu :
* **Ventilation par catégorie en colonnes :** chaque section est un tableau, une ligne par indicateur, colonnes `Total`, `Taux`, puis une colonne par catégorie roulée (ordre Hypercar, LMP2, LMP3, LMGTE, LMGT3, classes inconnues), en-têtes à la couleur de la catégorie. Les catégories sans aucune course n'ont pas de colonne.
* **Pilote** et **Forme récente** : deux panneaux côte à côte (`Columns`), l'un sous l'autre si la largeur est insuffisante.
* **Forme récente :** les 10 pastilles en texte coloré (`P1` violet, podium vert, `DNF`/`DQ` rouge, `NC` gris), puis la dernière course. Pas de survol ni de lien vers « Détail des courses ».
* **Causes d'abandon :** `BreakdownChart` Spectre (couleurs de `Home.template.md`), suivi de la liste « cause : nombre (%) ».
* Valeur non calculable : `—`. Aucune course : « Aucune course disponible ».
* Menu de fin : « Retour ».

### Vue « Mes courses »
Mêmes lignes, deltas et tri que `MyRaces.razor`.
* **Filtres :** à la première ouverture de la vue, deux `SelectionPrompt` successifs, Circuit (« Tous les circuits » puis lieux triés, avec recherche) et Catégorie (« Toutes les catégories » puis les catégories). Ils sont conservés jusqu'à la fermeture de l'application ; le choix « Changer les filtres » les redemande.
* **Groupement :** une règle Spectre par couple lieu + tracé (`Circuit de Spa-Francorchamps ─ 3 résultats`), puis le tableau du circuit.
* **Colonnes abrégées** pour tenir en 120 colonnes :

  | Colonne console | Colonne dashboard |
  |---|---|
  | DATE | DATE / HEURE (`dd/MM/yyyy HH:mm`) |
  | CAT. | CATÉGORIE (texte coloré à la couleur de la catégorie) |
  | VOITURE | VOITURE |
  | QUALIF | MEILLEUR TOUR QUALIF |
  | Δ QUALIF | DELTA QUALIF (n-1) |
  | GRILLE | GRILLE |
  | COURSE | MEILLEUR TOUR COURSE |
  | Δ COURSE | DELTA COURSE (n-1) |
  | TOURS | TOURS |
  | TEMPS TOTAL | TEMPS TOTAL COURSE |

* **Deltas :** `-0.245s` vert, `+0.102s` rouge, `0.000s` sans couleur, sans les pastilles 🟢 🔴 dont la largeur varie selon les terminaux.
* Aucune ligne : « Aucune course disponible ». Filtres sans résultat : « Aucune course ne correspond à ces filtres ».
* Menu de fin : « Changer les filtres », « Retour ».

### Vue « Options »
En tête, un tableau des réglages actuels, puis l'état des imports :

* **Réglages :** dossier des résultats (valeur configurée, ou « Détection automatique », et chemin résolu), état du dossier (🟢 « Dossier valide · N fichier(s) XML » / 🔴 message), pilote principal, nombre de fusions, démarrage automatique (Oui / Non), fichier de log et niveau minimal.
* **État des imports :** une ligne `Sessions importées N · Dernière session <lieu> <date> · Fichiers en échec N`, la liste des fichiers en échec, puis le tableau **« Sessions récentes de <pilote> »** limité aux **10 plus récentes** (DATE / HEURE, CIRCUIT, ÉVÉNEMENT, SESSION, VOITURE).

Puis un menu d'actions :

```text
 Que voulez-vous modifier ?
 > Dossier des résultats
   Pilote principal
   Fusion de pilotes
   Démarrage automatique
   Fichier de log
   Retour
```

#### Dossier des résultats
* `TextPrompt` avec la valeur actuelle par défaut ; **vide = détection automatique**, dans le même ordre que le dashboard (`ResultsDirectoryResolver`). La console ne cherche aujourd'hui que dans `assets\Results`.
* Un chemin relatif est résolu depuis le dossier de l'exécutable.
* Le dossier est validé et son état affiché. S'il est invalide, rien n'est enregistré.
* S'il est valide, confirmation « Appliquer ? » (o/n), puis comme le bouton « Appliquer » du dashboard : enregistrement de `ResultsDirectory`, vidage du dépôt, réimport du dossier avec barre de progression, (re)démarrage de la surveillance. Le changement s'applique **tout de suite** ; aujourd'hui, il ne prend effet qu'au prochain démarrage.
* Pas de bouton « Parcourir… » : la console n'ouvre pas de sélecteur de dossier.

#### Pilote principal
* `SelectionPrompt` avec recherche, mêmes libellés et même tri que le dashboard (`Nom — 12,3 % (n/N fichiers)`), le pilote actuel en tête.
* Enregistrement immédiat dans `LMU:UserName`.

#### Fusion de pilotes
* Sous-menu : « Ajouter une fusion », la liste des fusions existantes (`Ancien nom → Nom conservé`, suivie de `⚠ N session(s) en commun` le cas échéant), « Retour ».
* **Ajouter :** deux `SelectionPrompt` avec recherche (Ancien nom, puis Nom conservé, sans l'ancien nom). Si les deux pilotes se sont croisés, l'avertissement du dashboard est affiché puis une confirmation est demandée. Mêmes règles de cohérence (pas de chaîne, inversion).
* **Choisir une fusion existante :** confirmation « Annuler cette fusion ? » (o/n).
* Enregistrement immédiat dans `LMU:DriverMerges`, sans recharger les fichiers.

#### Démarrage automatique
* `ConfirmationPrompt` « Lancer la surveillance au démarrage ? », valeur actuelle par défaut, enregistrée immédiatement dans `LMU:WatchResultsOnStartup`. Elle s'applique au prochain démarrage.

#### Fichier de log
* Propre à la console, comme aujourd'hui : chemin du fichier (relatif à l'exécutable ou absolu), puis niveau minimal (`SelectionPrompt`, niveau actuel en tête), validés puis enregistrés dans `LogFilePath` et `FileMinimumLogLevel`.

#### Écarts avec la page Options du dashboard
* Pas de « Parcourir… » ni d'option « Charger les sessions d'essais ».
* Réglages du fichier de log en plus.
* Sessions récentes limitées à 10 lignes.

### Wireframes ASCII
Rendu dans un terminal de 120 colonnes. Les valeurs sont des exemples.

#### Démarrage et menu principal
```text
── LMU DeltaRacing ────────────────────────────────────────────────────────────────────────────────────────────────────
170 session(s) importée(s) · 38 essai(s) ignoré(s) · 1 échec(s)
✖ 2026_09_12_20_41_03-12R1.xml — Unexpected end of file.

 LMU DeltaRacing ─ Driver Tom ─ 🟢 Surveillance active ─ 170 sessions ─ 1 échec

 Que voulez-vous afficher ?
 > Accueil
   Mes courses
   Options
   Quitter
```

#### Accueil
```text
 LMU DeltaRacing ─ Driver Tom ─ 🟢 Surveillance active ─ 170 sessions ─ 0 échec

╭─ Pilote ───────────────────────────────────────────╮ ╭─ Forme récente ───────────────────────────────────────────╮
│ Driver Tom                                         │ │ P2  P7  DNF  P1  P11  P4  NC  P3  P9  P6                  │
│ En piste du 06/08/2025 au 16/10/2026               │ │                                                           │
│                                                    │ │ Dernière course                                           │
│ Circuit favori   Circuit de la Sarthe              │ │ 16/10/2026 21:00 · Spa · BMW M4 LMGT3                     │
│                  18 courses · 2 V · 4 podiums      │ │ P2 en classe · Arrivé · 2:18.104                          │
│ Voiture favorite McLaren 720S LMGT3 Evo            │ │                                                           │
│                  31 courses · 2 V · 6 podiums      │ │                                                           │
╰────────────────────────────────────────────────────╯ ╰───────────────────────────────────────────────────────────╯

 Palmarès
┌──────────────────┬───────┬────────┬───────┬───────┬───────┐
│                  │ Total │ Taux   │ LMP2  │ LMGTE │ LMGT3 │
├──────────────────┼───────┼────────┼───────┼───────┼───────┤
│ Courses          │    87 │        │    15 │    20 │    52 │
│ Victoires        │     5 │  5.7 % │     1 │     1 │     3 │
│ Podiums          │    14 │ 16.1 % │     2 │     3 │     9 │
│ Top 5            │    27 │ 31.0 % │     4 │     7 │    16 │
│ Top 10           │    48 │ 55.2 % │     7 │    12 │    29 │
│ Position moyenne │   8.4 │        │   7.9 │   7.0 │   9.1 │
└──────────────────┴───────┴────────┴───────┴───────┴───────┘

 Roulage
┌──────────────────┬──────────┬──────────┬──────────┬──────────┐
│                  │ Total    │ LMP2     │ LMGTE    │ LMGT3    │
├──────────────────┼──────────┼──────────┼──────────┼──────────┤
│ Tours bouclés    │    1 342 │      214 │      316 │      812 │
│ Distance         │ 6 120 km │   980 km │ 1 450 km │ 3 690 km │
│ Temps en piste   │    41:37 │     6:12 │     9:40 │    25:45 │
└──────────────────┴──────────┴──────────┴──────────┴──────────┘

 Qualification & départ
┌──────────────────┬───────────┬────────┬───────┬───────┬───────┐
│                  │ Total     │ Taux   │ LMP2  │ LMGTE │ LMGT3 │
├──────────────────┼───────────┼────────┼───────┼───────┼───────┤
│ Poles            │         3 │  3.4 % │     1 │     0 │     2 │
│ Qualif moyenne   │       7.2 │        │   6.1 │   6.8 │   7.9 │
│ Places gagnées   │ +0.9 / c. │        │  +0.4 │  +0.6 │  +1.2 │
│                  │ +78 total │        │       │       │       │
└──────────────────┴───────────┴────────┴───────┴───────┴───────┘

 Abandons
┌──────────────────┬───────┬────────┬───────┬───────┬───────┐
│                  │ Total │ Taux   │ LMP2  │ LMGTE │ LMGT3 │
├──────────────────┼───────┼────────┼───────┼───────┼───────┤
│ Abandons         │    13 │ 14.9 % │     2 │     2 │     9 │
│ Disqualifications│     0 │  0.0 % │       │       │       │
│ Non classé       │     3 │  3.4 % │     0 │     1 │     2 │
└──────────────────┴───────┴────────┴───────┴───────┴───────┘

 Causes d'abandon
 ██████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒
 ■ Non précisée 7 (54 %)  ■ Suspension 5 (38 %)  ■ Moteur 1 (8 %)

 > Retour
```

#### Mes courses
```text
 LMU DeltaRacing ─ Driver Tom ─ 🟢 Surveillance active ─ 170 sessions ─ 0 échec
 Circuit : Tous les circuits · Catégorie : Toutes les catégories

── Circuit de Spa-Francorchamps ─ 3 résultats ─────────────────────────────────────────────────────────────────────────
┌──────────────────┬──────────┬──────────────┬──────────┬──────────┬────────┬──────────┬──────────┬───────┬─────────────┐
│ DATE             │ CAT.     │ VOITURE      │ QUALIF   │ Δ QUALIF │ GRILLE │ COURSE   │ Δ COURSE │ TOURS │ TEMPS TOTAL │
├──────────────────┼──────────┼──────────────┼──────────┼──────────┼────────┼──────────┼──────────┼───────┼─────────────┤
│ 16/10/2026 21:00 │ Hypercar │ Ferrari 499P │ 2:02.451 │ -0.245s  │      3 │ 2:03.110 │ -0.120s  │    12 │ 25:01.432   │
│ 16/10/2026 20:20 │ Hypercar │ Ferrari 499P │ 2:02.696 │ +0.102s  │      5 │ 2:03.230 │ +0.450s  │    12 │ 25:03.876   │
│ 16/10/2026 19:40 │ Hypercar │ Ferrari 499P │ 2:02.594 │ —        │      — │ —        │ —        │     — │ —           │
└──────────────────┴──────────┴──────────────┴──────────┴──────────┴────────┴──────────┴──────────┴───────┴─────────────┘

── Circuit de la Sarthe ─ 18 résultats ────────────────────────────────────────────────────────────────────────────────
…

 > Changer les filtres
   Retour
```

#### Options
```text
 LMU DeltaRacing ─ Driver Tom ─ 🟢 Surveillance active ─ 170 sessions ─ 0 échec

┌─────────────────────────┬───────────────────────────────────────────────────────────────────────────────────────────┐
│ Dossier des résultats   │ Détection automatique                                                                     │
│                         │ C:\Users\…\Documents\Le Mans Ultimate\UserData\Log\Results                                │
│ État du dossier         │ 🟢 Dossier valide · 342 fichier(s) XML                                                    │
│ Pilote principal        │ Driver Tom — 81,7 % (170/208 fichiers)                                                    │
│ Fusions de pilotes      │ 2 (dont 1 avec sessions en commun)                                                        │
│ Démarrage automatique   │ Oui                                                                                       │
│ Fichier de log          │ lmu-deltaracing.log · Debug                                                               │
└─────────────────────────┴───────────────────────────────────────────────────────────────────────────────────────────┘
 Sessions importées 170 · Dernière session Circuit de Spa-Francorchamps 16/10/2026 21:00 · Fichiers en échec 0

 Sessions récentes de Driver Tom
┌──────────────────┬──────────────────────────────┬──────────────────────────────┬─────────┬──────────────────────────┐
│ DATE / HEURE     │ CIRCUIT                      │ ÉVÉNEMENT                    │ SESSION │ VOITURE                  │
├──────────────────┼──────────────────────────────┼──────────────────────────────┼─────────┼──────────────────────────┤
│ 16/10/2026 21:00 │ Circuit de Spa-Francorchamps │ 6 Hours of Spa-Francorchamps │ Race    │ BMW M4 LMGT3             │
│ 16/10/2026 20:20 │ Circuit de Spa-Francorchamps │ 6 Hours of Spa-Francorchamps │ Qualify │ BMW M4 LMGT3             │
│ …                │                              │                              │         │                          │
└──────────────────┴──────────────────────────────┴──────────────────────────────┴─────────┴──────────────────────────┘

 Que voulez-vous modifier ?
 > Dossier des résultats
   Pilote principal
   Fusion de pilotes
   Démarrage automatique
   Fichier de log
   Retour
```

#### Fusion de pilotes
```text
 Fusion de pilotes
 > Ajouter une fusion
   Driver Paul → Driver Iris
   Driver Tristan → Driver Lucas  ⚠ 2 session(s) en commun
   Retour

 Ancien nom : Driver Tristan
 Nom conservé : Driver Lucas
 ⚠ Ces deux pilotes ont participé ensemble à 2 session(s) : ce sont probablement deux personnes différentes.
 Fusionner quand même ? [o/n] (n) :
```

