## 3. ⏱️ Menu : Détail des Courses (`RaceDetails.razor`)

> Spécification alignée sur le code au 08/11/2026 (`RaceDetails.razor`, `RaceEventPresentation.cs`, `RaceDetailsPresentation.cs`, `LapAnalysisPresentation.cs`).

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
* **Structure du tableau** (11 colonnes) : hormis POS et CLASSE, sur une ligne, chaque cellule **empile** une valeur principale et, en petit dessous, son complément ou son écart. L'en-tête de chaque colonne indique les deux lignes (par exemple « SECTEUR 1 / ÉCART »).

  | Colonne | Ligne principale | Ligne secondaire |
  |---|---|---|
  | POS | Position générale (`01`, `02`… ou `—`) | — |
  | CLASSE | Badge de catégorie (voir « Mes courses »), sans la position en classe, qui doublait POS | — |
  | PILOTE / VOITURE | Nom, suivi du tag **VOUS** pour le pilote sélectionné | `CarType`, à défaut `#<CarNumber>`, à défaut `—` (tronqué avec « … » si trop long) |
  | SECTEUR 1 / 2 / 3 / ÉCART | Meilleur secteur personnel, coloré selon les règles WEC (`38.120s`) | Écart au meilleur secteur de la classe (`+0.130s` / `0.000s`) |
  | TOUR IDÉAL / ÉCART | Somme des meilleurs secteurs (`m:ss.fff`) | Écart au meilleur tour idéal de la classe (`+2.309s`) |
  | MEILLEUR TOUR / ÉCART | Meilleur tour du pilote (`m:ss.fff`) | Écart au meilleur tour de la classe (`+0.469s`) |
  | TEMPS TOTAL / ÉCART AU 1ER | En course : temps total du pilote (`FinishTime`, `h:mm:ss.fff` si ≥ 1 h sinon `m:ss.fff`) ; `—` en qualification, pour un pilote DNF/DQ ou sans temps | Écart au leader de la classe : `LEADER` (vert), `+12.500s`, `+1 tour`, `DNF` (rouge) |
  | CONTACTS / OBSTACLES | Contacts avec une voiture | Contacts avec un obstacle. Les deux lignes forment un bouton qui ouvre les incidents du pilote |
  | LIMITES / TOURS INVALIDÉS | Avertissements de limites de piste | Tours invalidés |

* **Cote pilote (onglet Course) :** au-dessus du tableau, la force du plateau de chaque classe notée (badge, cote moyenne avant la course, nombre de pilotes), la cote du pilote sélectionné avant → après la course, sa variation et sa place attendue, et un lien « Cote pilote → ». Sans classe notée : « Course non notée par la cote pilote : aucune classe n'a de pilote arrivé au drapeau à damier. ». La cote d'avant-course de chaque pilote noté suit son nom, en gris, avec la variation au survol. Règles dans [`DriverRating.template.md`](DriverRating.template.md).
* **Incidents :** règles de comptage dans [`DrivingProfile.template.md`](DrivingProfile.template.md). Une session sans aucun événement n'a pas de journal : ses deux colonnes affichent `—`, pas `0`. Le bouton ouvre, sous le tableau, le fil chronologique du pilote (temps écoulé, tour s'il est connu, description) ; il se referme par le même bouton ou par « Fermer ».
* **Déroulé de la course :** sous le tableau, en course, pour le pilote sélectionné par défaut, ou pour celui dont on a cliqué les chiffres. Un résumé (contacts, temps perdu, places perdues, tour de référence), puis un graphique SVG des positions de **tout le plateau** (`RacePositionsPresentation`, `RaceStoryChartLayout`) :
  * Une ligne par voiture ayant au moins une position de tour : position sur la grille (tour 0), puis position à la fin de chaque tour (`p`), comparées tour par tour : un retardataire finit son tour plus tard que le leader, mais sa position à la fin de ce tour reste sa place.
  * Le pilote suivi en blanc épais, dessiné au-dessus des autres ; le pilote sélectionné (s'il n'est pas le pilote suivi) en rouge ; les autres voitures en gris fin, en bleu au survol. Survoler une ligne donne « P3 Driver X » ; cliquer une ligne suit son pilote (graphique, résumé et fil des incidents). Un abandon ou une disqualification termine sa ligne par une croix rouge, « (abandon au tour N) » au survol.
  * Les noms (« P4 Driver Tom », position finale) sont écrits à droite des lignes jusqu'à 12 voitures affichées, écartés pour ne jamais se chevaucher ; au-delà, seuls le pilote suivi et le pilote sélectionné sont nommés.
  * **Positions :** « En classe » (par défaut) ou « Au général », boutons affichés seulement en course multiclasse. En classe, seules les voitures de la classe du pilote suivi sont tracées, classées entre elles à partir de leurs positions au général.
  * **Voitures :** « Tout le plateau » (« Toute la classe » en multiclasse, en classe ; par défaut) ou « Autour de *pilote* » : le pilote suivi, les N voitures juste devant lui et les N juste derrière dans le **classement final** (position officielle au général, ou rang en classe ; abandons à la place que LMU leur donne), soit 2N + 1 lignes au plus. Le classement final plutôt qu'un tour quelconque : avec « proche à un moment », le départ et les arrêts gardaient la moitié de la grille dès ± 1. **Écart à l'arrivée :** boutons − et + autour de « ± N places » (3 par défaut, de 1 à la taille du plateau moins 1). Le premier clic sur − ou + depuis « Tout le plateau » passe en « Autour de » avec l'écart affiché ; les suivants le changent. À droite : « N voitures affichées sur M ».
  * L'échelle des positions suit les voitures affichées, et la hauteur du graphique le nombre de positions (16 px par position, entre 230 et 620 px).
  * Les incidents du pilote suivi sont des points sur sa ligne, à la couleur du plus grave : dégâts (rouge), contact avec une voiture (orange), obstacle (jaune), pénalité (violet), limites de piste (gris). Ses arrêts au stand sont des traits pointillés. Survoler un point donne le détail du tour. Règles de calcul dans [`DrivingProfile.template.md`](DrivingProfile.template.md).
* **Analyse tour par tour** (`LapAnalysisPresentation`, `LapAnalysisPanel.razor`, `LapTimesChartLayout`) : sous le déroulé, en course, pour le même pilote suivi (celui de la ligne sélectionnée, par défaut le pilote sélectionné dans Options). Titre « Analyse tour par tour de *pilote* ».
  * **Tours représentatifs :** tours chronométrés, hors premier tour et hors tours `pit="1"`, et pas plus lents que **107 %** de la médiane de ces tours (un tête-à-queue, un drapeau jaune ou un contact ne disent rien du rythme). Ils servent au rythme, à la régularité, aux relais et à la consommation.
  * **Résumé :** arrêts au stand (`<Pitstops>`, `—` si absent), **régularité** = écart-type des tours représentatifs (`±0.412s`, au moins 3 tours, sinon « Régularité non calculée… ») avec le rang dans la classe (1 = le plus régulier, parmi les pilotes de la classe ayant une régularité), **rythme** = médiane des tours représentatifs, **écart moyen au meilleur tour** = moyenne des tours représentatifs − meilleur tour du pilote, **vitesse de pointe** (plus haute `topspeed` du pilote, en km/h) et celle de la classe. Une note rappelle le nombre de tours représentatifs et de tours écartés.
  * **Graphique des temps au tour** (SVG, 260 px de tracé) : un point par tour chronométré, reliés tant que les tours se suivent (un tour sans temps coupe la ligne). Le pilote suivi en blanc, le pilote sélectionné en rouge s'il n'est pas le pilote suivi. Plus bas = plus rapide, comme sur Progression. L'échelle va du tour le plus rapide à **112 %** du rythme du pilote suivi : les tours plus lents sont dessinés sur le bord haut. Points pleins pour les tours représentatifs, creux gris pour les autres, creux jaunes pour les tours `pit="1"` ; traits pointillés verticaux aux tours `pit="1"` du pilote suivi ; ligne horizontale en tirets cyan au rythme. Survoler un point donne pilote, tour, temps, et « voie des stands » ou « tour non représentatif ».
  * **Relais :** un relais se termine au **premier tour de chaque suite de tours `pit="1"`** (le tour d'entrée), un départ depuis la voie des stands (suite de tours `pit="1"` dès le tour 1) n'en termine pas. Tableau : numéro, tours (« 3 à 11 (9 tours) »), composés de pneus du relais (nom seul si avant et arrière sont identiques, sinon « Hard / Soft »), rythme (médiane des tours représentatifs du relais), meilleur tour, puis, si la course en contient : carburant et énergie consommés par tour (moyenne des tours représentatifs, en % du réservoir) et usure de chaque pneu pendant le relais (gomme du premier tour connu − gomme du dernier, AVG · AVD · ARG · ARD).
  * **Consommation et usure :** carburant par tour et nombre de tours tenables avec un plein (1 ÷ consommation), de même pour l'énergie virtuelle (GT3 et Hyper), usure moyenne de chaque pneu par tour (entre deux tours consécutifs dont le second est représentatif et le premier n'est pas un tour au stand). Valeurs en pourcentage : le XML ne donne pas la capacité du réservoir. Sans aucune de ces données (serveur qui ne les partage pas, cas de toutes les sessions depuis juillet 2026 dans les fixtures) : « Carburant, énergie et usure des pneus non partagés par le serveur de cette course. »
  * **Détail des tours** (replié, bouton « Afficher le détail des tours ») : tour, temps, position au général (`P3`), vitesse de pointe, puis carburant restant, énergie restante et gomme restante des 4 pneus si la course en contient, composés et repère `●` des tours `pit="1"`. Les tours non représentatifs sont grisés.
* **Ligne du pilote sélectionné :** légèrement surlignée (fond rouge LMU translucide), en plus du tag **VOUS**.
* Le tableau garde sa zone de défilement **vertical** (hauteur maximale 680 px, en-tête fixe).
* **Colonnes ajustables :** une poignée (deux traits fins) au bord droit de chaque en-tête, rouge LMU au survol avec un curseur de redimensionnement ; la faire glisser change la largeur de sa colonne ; un double-clic sur ce bord rend au tableau ses largeurs automatiques. Les largeurs ne sont pas mémorisées : un rechargement de la page les rétablit.

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
│ [LMU] DELTARACING   ACCUEIL   COURSES ▾   PERFORMANCE ▾   PILOTES ▾   OPTIONS                                  Driver Tom   🟢 SURVEILLANCE ACTIVE │
├────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  ANALYSE DE SESSION                                                                                                                                │
│  Détail des courses                                                                                                                                │
│  Classement, meilleurs secteurs et écarts de la session sélectionnée, comparés au sein de chaque classe.                                          │
│                                                                                                                                                    │
│  SÉLECTIONNER UNE COURSE                                                                                                                           │
│  [ 02/10/2026 21:00 - Circuit de Spa-Francorchamps (Essais · Qualif · Course) - LMGT3 ▼ ]     Circuit de Spa-Francorchamps                      │
│  [ COURSE ]   QUALIFICATION   ESSAIS                                                                                                               │
│  ────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  POS  CLASSE     PILOTE             SECTEUR 1    SECTEUR 2    SECTEUR 3    TOUR IDÉAL   MEILLEUR TOUR   TEMPS TOTAL                                  │
│                  VOITURE            ÉCART        ÉCART        ÉCART        ÉCART        ÉCART           ÉCART AU 1ER                                 │
│  ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────    │
│  01   LMGT3      Driver Louis       🟣 41.859s   🟡 74.351s   🟣 44.395s   2:40.605     2:41.796        21:47.737                                    │
│                  BMW M4 LMGT3       0.000s       +0.370s      0.000s       0.000s       0.000s          LEADER                                       │
│  ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────    │
│  04   LMGT3      Driver Tom VOUS    🟡 42.478s   🟡 74.602s   🟡 45.835s   2:42.914     2:44.849        22:37.645      ← ligne surlignée             │
│                  Porsche 911 GT3 R  +0.619s      +0.621s      +1.439s      +2.309s      +3.054s         +49.908s                                     │
│  ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────    │
│  11   LMGT3      Driver Emma        🟡 42.411s   🟡 75.358s   🟡 45.068s   2:42.836     2:43.196        —                                            │
│                  McLaren 720S LMGT3 +0.551s      +1.378s      +0.672s      +2.231s      +1.400s         DNF                                          │
│                                                                                                                                                    │
│  🟣 Meilleur secteur de la classe   🟡 Temps standard   Secteurs : meilleur temps personnel sur les tours chronométrés · Écarts calculés par classe │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
