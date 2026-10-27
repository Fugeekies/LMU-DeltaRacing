## 1. 🏎️ Menu : Mes Courses (`MyRaces.razor`)

> Spécification alignée sur le code au 25/10/2026 (`MyRaces.razor`, `MyRacesPresentation.cs`).

### Description
Page `/my-races`, deuxième onglet de la barre de navigation, après l'Accueil. Elle montre la progression personnelle du pilote sélectionné (voir Options), groupée par circuit et filtrable par circuit et par catégorie. Chaque ligne associe une course à la qualification qui la précède ; ses meilleurs tours sont comparés à ceux de la ligne précédente ($n-1$) sur le même circuit et dans la même catégorie, quel que soit le véhicule.

### Spécifications fonctionnelles
* **Sessions prises en compte :** sessions de qualification (type contenant `Qual`) et de course (type contenant `Race`) dans lesquelles figure le pilote sélectionné. Les sessions d'essais sont importées mais ne s'affichent que dans l'onglet Essais de « Détail des courses ».
* **Catégorie :** déduite de la `CarClass` du pilote (`CarCategory.cs`) :
  * contient `Hyper` → **Hypercar** ;
  * contient `LMP2` ou vaut `P2` → **LMP2** ;
  * contient `LMP3` ou vaut `P3` → **LMP3** ;
  * contient `GTE` → **LMGTE** ;
  * contient `GT3` → **LMGT3** ;
  * toute autre classe : affichée sous son nom brut (jamais exclue) ; classe vide : `—`.
* **Une ligne = une paire qualif + course :**
  * Les sessions du pilote sont parcourues chronologiquement, circuit par circuit (même lieu `TrackVenue`, même tracé `TrackCourse`, même événement `TrackEvent`).
  * Chaque course est associée à la **dernière qualification qui la précède sur ce circuit, sans autre course entre les deux**.
  * Une qualification suivie d'une autre qualification, ou une course sans qualification avant elle, donne une **ligne partielle** (colonnes de l'autre session à `—`).
  * Plusieurs courses le même jour sur le même circuit donnent donc plusieurs lignes.
  * Date / heure : début de la qualification, à défaut de la course (heure locale).
  * Catégorie et voiture : celles de la course, à défaut celles de la qualification.
  * Meilleur tour : plus petit temps de tour chronométré de la session. LMU efface le temps des tours invalidés pour limites de piste, qui sont donc exclus.
  * Grille, tours et temps total : ceux de la course.
* **Voiture :** `CarType` ; à défaut `#<CarNumber>` ; à défaut `—`.
* **Filtres :**
  * Circuit : « Tous les circuits » ou un lieu parmi ceux présents dans les lignes (tri alphabétique). Le filtre porte sur `TrackVenue`.
  * Catégorie : « Toutes les catégories », Hypercar, LMP2, LMP3, LMGTE, LMGT3, puis les éventuelles classes inconnues présentes dans les lignes.
  * Les filtres s'appliquent sans rechargement et sont conservés lors des mises à jour en direct.
* **Groupement à l'affichage :** un bloc par couple lieu + tracé, triés par nom de lieu. L'en-tête affiche « CIRCUIT », le lieu, le tracé s'il diffère du lieu et un badge « N résultats ». Les lignes d'un bloc sont triées de la plus récente à la plus ancienne.
* **Structure du tableau :**

  | Colonne | Contenu | Format |
  |---|---|---|
  | DATE / HEURE | Début de la qualif, à défaut de la course (heure locale) | `dd/MM/yyyy HH:mm` |
  | CATÉGORIE | Badge de catégorie | — |
  | VOITURE | Modèle du véhicule | texte gras |
  | CLASSEMENT | Position en classe de la course (`ClassPosition`) ; `DNF`, `DQ` ou `NC` selon `FinishStatus` ; `—` sans course | `1er`, `2e`, `3e`… |
  | GRILLE | Position de départ en course (`GridPos`) | entier |
  | MEILLEUR TOUR COURSE | Meilleur tour en course | `m:ss.fff` |
  | DELTA COURSE (n-1) | Écart avec la course précédente | voir règle delta |
  | TOURS | Tours effectués par le pilote en course (`Laps`) | entier |
  | TEMPS TOTAL COURSE | `FinishTime` du pilote | `h:mm:ss.fff` si ≥ 1 h, sinon `m:ss.fff` |
  | MEILLEUR TOUR QUALIF | Meilleur tour en qualification | `m:ss.fff` |
  | DELTA QUALIF (n-1) | Écart avec la qualif précédente | voir règle delta |

  Toute valeur absente s'affiche `—`. Le classement ouvrant le tableau, les colonnes de la course précèdent celles de la qualification.
* **Mise en avant du classement :** la ligne d'une course classée reçoit un fond teinté et une barre à gauche selon sa position en classe (couleurs de `UI_DESIGN_SPEC.md`, section 7) :
  * P1 : or ; P2 : argent ; P3 : bronze (la position est aussi écrite dans cette couleur) ;
  * P4-P5 : bleu (position en bleu) ;
  * P6-P10 : même bleu, plus léger (position en blanc).
  * Au-delà de P10, en `DNF`, `DQ`, `NC` (même avec une position en classe) et sur une ligne sans course : ligne normale.
* **Règle métier delta ($n-1$) :**
  * Historique = lignes de même lieu + même tracé + même catégorie, triées chronologiquement. Le véhicule n'est pas pris en compte.
  * Qualif et course sont calculées séparément : delta = temps courant − dernier temps non vide précédent du même type.
  * Première mesure de l'historique : `—`.
  * Gain de temps (delta < 0) : texte vert `🟢 -0.245s`.
  * Perte de temps (delta > 0) : texte rouge `🔴 +0.102s`.
  * Delta nul : `0.000s` sans couleur.
  * Trois décimales, séparateur point.
* **États de la page :**
  * Chargement initial sans session : spinner « Chargement de l'historique des courses… ».
  * Erreur de chargement sans session : encadré d'erreur avec le message et un lien vers Options.
  * Aucune ligne : « Aucune course disponible ».
  * Filtres sans résultat : « Aucune course ne correspond à ces filtres ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS        Driver Tom   🟢 SURVEILLANCE ACTIVE │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  HISTORIQUE & PROGRESSION                                                                                                        │
│  Mes courses                                                                                                                     │
│  Vos meilleurs temps, comparés à la session précédente sur le même circuit et dans la même catégorie.                           │
│                                                                                                                                  │
│  PERFORMANCES ENREGISTRÉES                                                                                                [ 5 ]  │
│  Progression par circuit                                                                                                         │
│  CIRCUIT [ Tous les circuits ▼ ]   CATÉGORIE [ Toutes les catégories ▼ ]                                                        │
│                                                                                                                                  │
│  CIRCUIT                                                                                                                         │
│  Circuit de Spa-Francorchamps                                                                                      [ 3 résultats ] │
│  ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  DATE / HEURE      CATÉGORIE  VOITURE       CLASS.  GRILLE MT COURSE  DELTA COURSE  TOURS  TEMPS TOTAL MT QUALIF  DELTA QUALIF   │
│  ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  16/10/2026 21:00  Hypercar   Ferrari 499P  2e      3      2:03.110   🟢 -0.120s    12     25:01.432   2:02.451   🟢 -0.245s     │
│  16/10/2026 20:20  Hypercar   Ferrari 499P  5e      5      2:03.230   🔴 +0.450s    12     25:03.876   2:02.696   🔴 +0.102s     │
│  16/10/2026 19:40  Hypercar   Ferrari 499P  —       —      —          —             —      —           2:02.594   —              │
│  12/10/2026 14:00  LMGTE      Porsche 911   7e      8      2:18.780   —             11     25:40.120   2:17.594   —              │
│                                                                                                                                  │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  LE MANS ULTIMATE                                                                         RÉSULTATS LOCAUX · DONNÉES NON TRANSMISES │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
