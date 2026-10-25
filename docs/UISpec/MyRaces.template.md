## 1. 🏎️ Menu : Mes Courses (`MyRaces.razor`)

> Spécification alignée sur le code au 25/10/2026 (`MyRaces.razor`, `MyRacesPresentation.cs`).

### Description
Page d'accueil de l'application (routes `/` et `/my-races`). Elle montre la progression personnelle du pilote sélectionné (voir Options), groupée par circuit et filtrable par circuit et par catégorie. Pour chaque circuit, deux tableaux se suivent : **Course** d'abord (le résultat qui compte), puis **Qualification**. Chaque ligne est une session ; son meilleur tour est comparé à celui de la session précédente du même type ($n-1$) sur le même circuit et dans la même catégorie, quel que soit le véhicule.

### Spécifications fonctionnelles
* **Sessions prises en compte :** sessions de course (type contenant `Race`) et de qualification (type contenant `Qual`) dans lesquelles figure le pilote sélectionné. Les sessions d'essais sont importées mais n'apparaissent pas ici : elles se consultent dans « Détail des courses ».
* **Catégorie :** déduite de la `CarClass` du pilote (`CarCategory.cs`) :
  * contient `Hyper` → **Hypercar** ;
  * contient `LMP2` ou vaut `P2` → **LMP2** ;
  * contient `LMP3` ou vaut `P3` → **LMP3** ;
  * contient `GTE` → **LMGTE** ;
  * contient `GT3` → **LMGT3** ;
  * toute autre classe : affichée sous son nom brut (jamais exclue) ; classe vide : `—`.
* **Une ligne = une session.** Plusieurs courses le même jour sur le même circuit donnent plusieurs lignes.
  * Meilleur tour : plus petit temps de tour chronométré de la session. LMU efface le temps des tours invalidés pour limites de piste, qui sont donc exclus.
  * Date / heure : début de l'événement (`RaceResults/DateTime`, commun aux essais, à la qualif et à la course d'un même événement), en heure locale.
* **Voiture :** `CarType` ; à défaut `#<CarNumber>` ; à défaut `—`.
* **Filtres :**
  * Circuit : « Tous les circuits » ou un lieu parmi ceux présents dans les lignes (tri alphabétique). Le filtre porte sur `TrackVenue`.
  * Catégorie : « Toutes les catégories », Hypercar, LMP2, LMP3, LMGTE, LMGT3, puis les éventuelles classes inconnues présentes dans les lignes.
  * Les filtres s'appliquent aux deux tableaux, sans rechargement, et sont conservés lors des mises à jour en direct.
* **Groupement à l'affichage :** un bloc par couple lieu + tracé, triés par nom de lieu. L'en-tête affiche « CIRCUIT », le lieu, le tracé s'il diffère du lieu et un badge « N sessions ». Dans le bloc, le titre « COURSE N » précède le tableau des courses, puis « QUALIFICATION N » celui des qualifications. Un type sans session affiche « Aucune course sur ce circuit. » ou « Aucune qualification sur ce circuit. ». Les lignes sont triées de la plus récente à la plus ancienne.
* **Tableau Course :**

  | Colonne | Contenu | Format |
  |---|---|---|
  | DATE / HEURE | Début de l'événement (heure locale) | `dd/MM/yyyy HH:mm` |
  | CATÉGORIE | Badge de catégorie | — |
  | VOITURE | Modèle du véhicule | texte gras |
  | GRILLE | Position de départ (`GridPos`) | entier |
  | MEILLEUR TOUR | Meilleur tour en course | `m:ss.fff` |
  | DELTA (n-1) | Écart avec la course précédente | voir règle delta |
  | TOURS | Tours effectués par le pilote (`Laps`) | entier |
  | TEMPS TOTAL | `FinishTime` du pilote | `h:mm:ss.fff` si ≥ 1 h, sinon `m:ss.fff` |

* **Tableau Qualification :** DATE / HEURE, CATÉGORIE, VOITURE, MEILLEUR TOUR, DELTA (n-1), TOURS (mêmes formats).
* Toute valeur absente s'affiche `—`.
* **Règle métier delta ($n-1$) :**
  * Historique = sessions du même type (course ou qualification), même lieu + même tracé + même catégorie, triées chronologiquement. Le véhicule n'est pas pris en compte.
  * Delta = meilleur tour courant − dernier meilleur tour non vide précédent de l'historique.
  * Première mesure de l'historique : `—`.
  * Gain de temps (delta < 0) : texte vert `🟢 -0.245s`.
  * Perte de temps (delta > 0) : texte rouge `🔴 +0.102s`.
  * Delta nul : `0.000s` sans couleur.
  * Trois décimales, séparateur point.
* **États de la page :**
  * Chargement initial sans session : spinner « Chargement de l'historique des courses… ».
  * Erreur de chargement sans session : encadré d'erreur avec le message et un lien vers Options.
  * Aucune session : « Aucune course disponible ».
  * Filtres sans résultat : « Aucune course ne correspond à ces filtres ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS   Driver Tom   🟢 SURVEILLANCE ACTIVE │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  HISTORIQUE & PROGRESSION                                                                                        │
│  Mes courses                                                                                                     │
│  Vos meilleurs temps, comparés à la session précédente sur le même circuit et dans la même catégorie.            │
│                                                                                                                  │
│  PERFORMANCES ENREGISTRÉES                                                                               [ 170 ] │
│  Progression par circuit                                                                                         │
│  CIRCUIT [ Tous les circuits ▼ ]   CATÉGORIE [ Toutes les catégories ▼ ]                                         │
│                                                                                                                  │
│  CIRCUIT                                                                                                         │
│  Algarve International Circuit                                                                    [ 22 sessions ] │
│                                                                                                                  │
│  COURSE 11                                                                                                       │
│  DATE / HEURE      CATÉGORIE  VOITURE              GRILLE  MEILLEUR TOUR  DELTA (n-1)   TOURS  TEMPS TOTAL       │
│  24/01/2026 17:01  LMGT3      Ford Mustang LMGT3   4       1:48.242       🟢 -0.772s    11     20:32.043         │
│  24/01/2026 16:21  LMGT3      Mercedes-AMG LMGT3   17      1:49.014       🔴 +0.701s    12     22:14.337         │
│  17/10/2025 18:20  LMP2       Oreca 07             16      1:41.811       —             13     22:44.958         │
│                                                                                                                  │
│  QUALIFICATION 11                                                                                                │
│  DATE / HEURE      CATÉGORIE  VOITURE              MEILLEUR TOUR  DELTA (n-1)   TOURS                            │
│  24/01/2026 17:01  LMGT3      Ford Mustang LMGT3   1:48.424       🟢 -0.949s    3                                │
│  24/01/2026 16:21  LMGT3      Mercedes-AMG LMGT3   1:49.373       🔴 +1.468s    3                                │
│                                                                                                                  │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  LE MANS ULTIMATE                                                         RÉSULTATS LOCAUX · DONNÉES NON TRANSMISES │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
