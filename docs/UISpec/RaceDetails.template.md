## 3. ⏱️ Menu : Détail des Courses (`RaceDetails.razor`)

> Spécification alignée sur le code au 25/10/2026 (`RaceDetails.razor`, `RaceEventPresentation.cs`, `RaceDetailsPresentation.cs`).

### Description
Classement détaillé d'un événement choisi par l'utilisateur (route `/race-details`). Un événement regroupe les essais, la qualification et la course disputés à la suite sur un circuit ; des sous-onglets sous le sélecteur affichent le classement de chaque session. Pour chaque pilote, la page affiche ses meilleurs secteurs, son tour idéal, son meilleur tour et son écart au leader. Toutes les comparaisons se font **au sein de la classe du pilote**, comme dans le chronométrage WEC.

### Spécifications fonctionnelles
* **Regroupement en événements** (`RaceEventPresentation.cs`) :
  * Les sessions d'un même circuit (lieu + tracé + événement) sont parcourues chronologiquement.
  * Une session rejoint l'événement en cours si elle respecte l'ordre **essais → qualification → course** et démarre au plus **2 heures** après la session précédente. Sinon, un nouvel événement commence. Deux qualifications ou deux courses ne sont jamais dans le même événement ; plusieurs séances d'essais peuvent l'être.
  * Ces seuils viennent des fixtures : les sessions d'un événement se suivent à moins de 30 minutes, deux événements sont séparés de plus de 30 minutes après une course.
  * **Essais multiples :** les séances d'essais d'un événement sont fusionnées en un seul classement. Chaque pilote garde tous ses tours (renumérotés), ses tours effectués sont additionnés, et les pilotes sont classés par meilleur tour chronométré (position générale et position en classe recalculées).
  * Seuls les événements où le pilote sélectionné a participé à au moins une session sont proposés.
* **Sélecteur d'événement :**
  * Liste les événements du plus récent au plus ancien.
  * Libellé : `dd/MM/yyyy HH:mm - <Lieu> (<Sessions présentes>) - <Catégorie du pilote>`, par exemple `02/10/2026 21:00 - Circuit de Spa-Francorchamps (Essais · Qualif · Course) - LMGT3` (heure locale du début de la première session ; la catégorie est omise si la classe est vide).
  * Par défaut, l'événement le plus récent est sélectionné. S'il disparaît (rechargement), le plus récent est resélectionné.
  * Le paramètre d'URL `?event=<identifiant>` (lien « Voir l'événement » de l'Accueil) sélectionne cet événement sur l'onglet Course. Un identifiant inconnu est ignoré. L'identifiant est celui de la première session de l'événement : il change à chaque rechargement des résultats.
* **Métadonnées** à côté du sélecteur : lieu, tracé s'il diffère du lieu.
* **Sous-onglets** sous le sélecteur, dans cet ordre : **COURSE**, **QUALIFICATION**, **ESSAIS** (« ESSAIS (N séances) » si plusieurs séances ont été fusionnées). Seuls les onglets des sessions présentes dans l'événement sont affichés ; l'onglet Essais n'existe que si l'option « Charger les sessions d'essais » est cochée dans Options (décochée par défaut). Par défaut, l'onglet Course (le résultat qui compte) ; l'onglet choisi est conservé en changeant d'événement s'il existe dans le nouvel événement, sinon le premier onglet disponible est affiché.
* **Ordre du classement :** position générale croissante, pilotes sans position en fin de liste, égalité départagée par nom.
* **Tours pris en compte :** uniquement les tours chronométrés. LMU efface le temps des tours invalidés pour limites de piste et des tours incomplets ; leurs secteurs sont donc exclus.
* **Calculs par pilote :**
  * **Classe** : `CarClass` du pilote. Les références « de la classe » ci-dessous ne portent que sur les pilotes de cette classe.
  * **Secteurs S1 à S3** : meilleur temps personnel de chaque secteur, tous tours chronométrés confondus (pas nécessairement sur le même tour).
  * **Tour idéal** : somme des trois meilleurs secteurs personnels, si les trois sont connus.
  * **Meilleur tour** : tour chronométré le plus rapide du pilote.
  * **Delta secteur** = meilleur secteur du pilote − meilleur secteur de la classe.
  * **Delta tour** = meilleur tour du pilote − meilleur tour de la classe.
  * **Leader de la classe** : pilote de plus petite position en classe (`ClassPosition`, à défaut position générale).
* **Écart au 1er** (ligne secondaire de la colonne TEMPS TOTAL, écart au leader de la classe) :
  * Leader de la classe : `LEADER`.
  * En course, dans cet ordre :
    1. pilote `DNF` ou `DQ` (`FinishStatus`) : le statut est affiché ;
    2. pilote avec moins de tours que le leader : `+1 tour` / `+N tours` ;
    3. sinon : temps total du pilote − temps total du leader (`FinishTime`), par exemple `+12.500s` ;
    4. donnée manquante : `—`.
  * En qualification : meilleur tour du pilote − meilleur tour de la classe.
* **Règles de couleurs WEC (secteurs) :**
  * Meilleur secteur de la classe : fond violet / texte `#b066ff`.
  * Sinon : texte jaune `#eab308`.
  * Secteur absent : `—`.
* **Couleurs des deltas :** delta ≤ 0 en vert, delta > 0 en rouge, valeur absente en gris (`—`).
* **Structure du tableau** (8 colonnes, sans défilement horizontal à partir de 1 280 px de large) : chaque cellule **empile** une valeur principale et, en petit dessous, son complément ou son écart. L'en-tête de chaque colonne indique les deux lignes (par exemple « SECTEUR 1 / ÉCART »).

  | Colonne | Ligne principale | Ligne secondaire |
  |---|---|---|
  | POS / CLASSE | Position générale (`01`, `02`… ou `—`) | Badge de catégorie (voir « Mes courses ») + position en classe (`LMGT3 P2`) |
  | PILOTE / VOITURE | Nom, suivi du tag **VOUS** pour le pilote sélectionné | `CarType`, à défaut `#<CarNumber>`, à défaut `—` (tronqué avec « … » si trop long) |
  | SECTEUR 1 / 2 / 3 / ÉCART | Meilleur secteur personnel, coloré selon les règles WEC (`38.120s`) | Écart au meilleur secteur de la classe (`+0.130s` / `0.000s`) |
  | TOUR IDÉAL / ÉCART | Somme des meilleurs secteurs (`m:ss.fff`) | Écart au meilleur tour idéal de la classe (`+2.309s`) |
  | MEILLEUR TOUR / ÉCART | Meilleur tour du pilote (`m:ss.fff`) | Écart au meilleur tour de la classe (`+0.469s`) |
  | TEMPS TOTAL / ÉCART AU 1ER | En course : temps total du pilote (`FinishTime`, `h:mm:ss.fff` si ≥ 1 h sinon `m:ss.fff`) ; `—` en qualification, pour un pilote DNF/DQ ou sans temps | Écart au leader de la classe : `LEADER` (vert), `+12.500s`, `+1 tour`, `DNF` (rouge) |

* **Ligne du pilote sélectionné :** légèrement surlignée (fond rouge LMU translucide), en plus du tag **VOUS**.
* Le tableau garde sa zone de défilement **vertical** (hauteur maximale 680 px, en-tête fixe).

* **Grandes grilles :** au-delà de 40 pilotes, les lignes sont virtualisées (seules les lignes visibles sont rendues). Jusqu'à 40 pilotes, toutes les lignes sont rendues, y compris dans le pré-rendu serveur.
* **Légende** sous le tableau : Meilleur secteur de la classe (violet), Temps standard (jaune), et rappel que les secteurs sont des meilleurs personnels comparés par classe.
* **États de la page :**
  * Chargement initial sans session : spinner.
  * Erreur de chargement sans session : encadré d'erreur avec un lien vers Options.
  * Aucune session pour le pilote : « Aucune session disponible ».
  * Session sans pilote : « Cette session ne contient aucun résultat de pilote ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS                       Driver Tom   🟢 SURVEILLANCE ACTIVE │
├────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  ANALYSE DE SESSION                                                                                                                                │
│  Détail des courses                                                                                                                                │
│  Classement, meilleurs secteurs et écarts de la session sélectionnée, comparés au sein de chaque classe.                                          │
│                                                                                                                                                    │
│  SÉLECTIONNER UNE COURSE                                                                                                                           │
│  [ 02/10/2026 21:00 - Circuit de Spa-Francorchamps (Essais · Qualif · Course) - LMGT3 ▼ ]     Circuit de Spa-Francorchamps                      │
│  [ COURSE ]   QUALIFICATION   ESSAIS                                                                                                               │
│  ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  POS        PILOTE                  SECTEUR 1    SECTEUR 2    SECTEUR 3    TOUR IDÉAL   MEILLEUR TOUR   TEMPS TOTAL                                  │
│  CLASSE     VOITURE                 ÉCART        ÉCART        ÉCART        ÉCART        ÉCART           ÉCART AU 1ER                                 │
│  ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────    │
│  01         Driver Louis            🟣 41.859s   🟡 74.351s   🟣 44.395s   2:40.605     2:41.796        21:47.737                                    │
│  LMGT3 P1   BMW M4 LMGT3            0.000s       +0.370s      0.000s       0.000s       0.000s          LEADER                                       │
│  ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────    │
│  04         Driver Tom VOUS         🟡 42.478s   🟡 74.602s   🟡 45.835s   2:42.914     2:44.849        22:37.645      ← ligne surlignée             │
│  LMGT3 P4   Porsche 911 GT3 R       +0.619s      +0.621s      +1.439s      +2.309s      +3.054s         +49.908s                                     │
│  ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────    │
│  11         Driver Emma             🟡 42.411s   🟡 75.358s   🟡 45.068s   2:42.836     2:43.196        —                                            │
│  LMGT3 P11  McLaren 720S LMGT3      +0.551s      +1.378s      +0.672s      +2.231s      +1.400s         DNF                                          │
│                                                                                                                                                    │
│  🟣 Meilleur secteur de la classe   🟡 Temps standard   Secteurs : meilleur temps personnel sur les tours chronométrés · Écarts calculés par classe │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
