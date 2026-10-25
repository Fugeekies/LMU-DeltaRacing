## 1. 🏎️ Menu : Mes Courses (`MyRaces.razor`)

> Spécification alignée sur le code au 25/10/2026 (`MyRaces.razor`, `MyRacesPresentation.cs`).

### Description
Page d'accueil de l'application (routes `/` et `/my-races`). Elle montre la progression personnelle du pilote sélectionné (voir Options), groupée par circuit et filtrable par circuit et par catégorie. Pour chaque événement, elle compare les meilleurs tours en qualification et en course à ceux de l'événement précédent ($n-1$) sur le même circuit et dans la même catégorie, quel que soit le véhicule.

### Spécifications fonctionnelles
* **Sessions prises en compte :** sessions de qualification (type contenant `Qual`) et de course (type contenant `Race`) dans lesquelles figure le pilote sélectionné. Les sessions Practice ne sont jamais importées.
* **Catégorie :** déduite de la `CarClass` du pilote :
  * contient `Hyper` → **Hypercar** ;
  * contient `LMP2` ou vaut `P2` → **LMP2** ;
  * contient `GT3` → **LMGT3** ;
  * toute autre classe (`GTE`, `LMP3`…) → session **exclue** de la page.
* **Regroupement en événement (une ligne du tableau) :** toutes les sessions Q/R du pilote ayant le même lieu (`TrackVenue`), le même tracé (`TrackCourse`), le même événement (`TrackEvent`) et la même **date calendaire (UTC)** forment une seule ligne :
  * Date / heure : début de la première session du groupe, affichée en heure locale.
  * Catégorie et voiture : celles de la première session du groupe dans l'ordre du dépôt.
  * Meilleur tour qualif : plus petit temps de tour chronométré parmi les qualifications du groupe.
  * Meilleur tour course : plus petit temps de tour chronométré parmi les courses du groupe.
  * Grille, tours et temps total : première valeur disponible parmi les courses du groupe.
* **Voiture :** `CarType` ; à défaut `#<CarNumber>` ; à défaut `—`.
* **Filtres :**
  * Circuit : « Tous les circuits » ou un lieu parmi ceux présents dans les lignes (tri alphabétique). Le filtre porte sur `TrackVenue`.
  * Catégorie : « Toutes les catégories », Hypercar, LMP2, LMGT3.
  * Les filtres s'appliquent sans rechargement et sont conservés lors des mises à jour en direct.
* **Groupement à l'affichage :** un bloc par couple lieu + tracé, triés par nom de lieu. L'en-tête affiche « CIRCUIT », le lieu, le tracé s'il diffère du lieu et un badge « N résultats ». Les lignes d'un bloc sont triées de la plus récente à la plus ancienne.
* **Structure du tableau :**

  | Colonne | Contenu | Format |
  |---|---|---|
  | DATE / HEURE | Début de l'événement (heure locale) | `dd/MM/yyyy HH:mm` |
  | CATÉGORIE | Badge Hypercar / LMP2 / LMGT3 | — |
  | VOITURE | Modèle du véhicule | texte gras |
  | MEILLEUR TOUR QUALIF | Meilleur tour en qualification | `m:ss.fff` |
  | DELTA QUALIF (n-1) | Écart avec la qualif précédente | voir règle delta |
  | GRILLE | Position de départ en course (`GridPos`) | entier |
  | MEILLEUR TOUR COURSE | Meilleur tour en course | `m:ss.fff` |
  | DELTA COURSE (n-1) | Écart avec la course précédente | voir règle delta |
  | TOURS | `MostLapsCompleted` de la session de course | entier |
  | TEMPS TOTAL COURSE | `FinishTime` du pilote | `h:mm:ss.fff` si ≥ 1 h, sinon `m:ss.fff` |

  Toute valeur absente s'affiche `—`.
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
  * Erreur de chargement sans session : encadré d'erreur avec le message et « Vérifiez le dossier configuré dans Options ».
  * Aucune ligne : « Aucune course disponible » (qualifs et courses avec tours valides en Hypercar, LMP2 ou LMGT3).
  * Filtres sans résultat : « Aucune course ne correspond à ces filtres ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS        Driver 2038  🟢 SURVEILLANCE ACTIVE │
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
│  DATE / HEURE      CATÉGORIE  VOITURE       MT QUALIF  DELTA QUALIF  GRILLE  MT COURSE  DELTA COURSE  TOURS  TEMPS TOTAL         │
│  ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────  │
│  16/10/2026 19:00  Hypercar   Ferrari 499P  2:02.451   🟢 -0.245s    3       2:03.110   🟢 -0.120s    12     25:01.432           │
│  15/10/2026 21:15  Hypercar   Ferrari 499P  2:02.696   🔴 +0.102s    5       2:03.230   🔴 +0.450s    12     25:03.876           │
│  12/10/2026 14:00  LMGT3      BMW M4 GT3    2:17.594   —             8       2:18.780   —             11     25:40.120           │
│                                                                                                                                  │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  LE MANS ULTIMATE                                                                         RÉSULTATS LOCAUX · DONNÉES NON TRANSMISES │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Écarts connus
Les comportements à corriger ou à arbitrer sont suivis dans la [feuille de route](../ROADMAP.md), jalon « Écarts UI à traiter ».
