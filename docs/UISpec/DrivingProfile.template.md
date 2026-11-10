## Menu : Profil de conduite (`DrivingProfile.razor`)

> Spécification alignée sur le code au 02/11/2026 (`DrivingProfile.razor`, `DrivingProfilePresentation.cs`, `DrivingConductPresentation.cs`).

### Description
Page `/driving-profile`, onglet placé après « Progression ». Elle montre comment roule le **pilote sélectionné** (voir Options) : ses contacts, limites de piste et pénalités, comparés aux autres pilotes de ses sessions, et où il peut progresser.

### Spécifications fonctionnelles
* **Sessions prises en compte :** qualifications et courses du pilote sélectionné, après fusion de pilotes. Les essais sont ignorés, comme dans les autres vues de résultats : ils sont optionnels, et un pilote y essaie des choses.
* **Autres pilotes :** tous les autres pilotes de ces mêmes sessions, toutes classes confondues : ils ont eu les mêmes circuits, grilles et conditions. Leurs incidents et leurs tours sont additionnés, ce qui donne une valeur pour 100 tours du groupe.
* **Contact :** les événements `Incident` ne sont pas des contacts, car chaque voiture signale le sien, souvent plusieurs fois.
  * Un contact avec une voiture compte **une fois pour chaque pilote impliqué**, quelle que soit la voiture qui l'a signalé.
  * Pour un pilote, deux signalements avec une même cible à moins de **5 secondes** comptent pour un seul contact. Sur les fixtures, 89 % des signalements d'une même paire de pilotes se suivent à moins de 5 s, et 2 % entre 5 et 10 s.
  * Cibles : **Contacts** = `another vehicle` ; **Obstacles** = `Immovable`, `Post`, `Sign`, `Cone`.
  * `Wing` et `Wheel` sont listés dans le fil des incidents de « Détail des courses », mais comptés nulle part : leur sens n'est pas établi. La valeur entre parenthèses des événements n'est pas lue.
* **Limites de piste :** chaque événement compte.
  * **Avertissements** : `TrackLimits` dont le texte est `Warning`.
  * **Tours invalidés** : `TrackLimits` dont le texte est `Invalid Lap Cut Track`. LMU n'en signale aucun en course : leur valeur pour 100 tours est calculée sur les tours de qualification.
  * `No Further Action` est ignoré.
* **Pénalités :** événements `Penalty` reçus (`received…`) et disqualifications (`Disqualify`). Les pénalités purgées (`served…`) ne comptent pas, et celles des `TrackLimits` non plus, car elles sont répétées par les événements `Penalty`.
* **Ce que vos incidents vous coûtent :** premier bloc de la page, en course uniquement, affiché dès le premier contact.
  * **Temps perdu :** pour chaque tour avec un contact (voiture ou obstacle), écart avec le tour médian de la même course, sans valeur négative. Le tour médian exclut le premier tour et les tours au stand, et ces deux types de tours ne comptent pas de temps perdu, car leur temps a d'autres causes : c'est donc un minimum. Format `8 min 51 s`, suivi de la moyenne par course.
  * **Phrases :** part des contacts au départ ou au premier tour (dès 5 contacts), coût médian d'un tour avec contact, circuit le plus coûteux, contacts avec dégâts, abandons avec un contact dans leur dernier tour. Une phrase sans donnée n'apparaît pas.
  * **Chiffres :** contacts, contacts au départ ou au 1er tour (les contacts d'avant le premier tour, sur la grille, lui sont rattachés), places perdues sur les tours avec contact (les gains ne compensent pas), contacts avec dégâts (dégât signalé entre 1 s avant et 3 s après le contact).
  * **Course la plus coûteuse :** lien vers son événement dans « Détail des courses ».
* **Tuiles (bilan) :** CONTACTS, OBSTACLES, AVERTISSEMENTS, TOURS INVALIDÉS, PÉNALITÉS. Chacune affiche la valeur pour 100 tours du pilote, verte si elle est inférieure ou égale à celle des autres pilotes, rouge sinon, puis « Autres pilotes : N · N au total ». Sans tour : `—`, en gris.
* **« Où progresser » :** un tableau groupé en trois parties, Session (Course, Qualification), Catégorie et Circuit (lieu, suivi du tracé s'il diffère). Dans chaque partie, les lignes vont de la plus roulée à la moins roulée.

  | Colonne | Contenu | Format |
  |---|---|---|
  | SESSIONS | Nombre de sessions | entier |
  | TOURS | Tours effectués (`Laps`, à défaut le nombre de tours listés) | entier |
  | CONTACTS / 100 T. | Contacts avec une voiture pour 100 tours | `16.7` |
  | OBSTACLES / 100 T. | Contacts avec un obstacle pour 100 tours | `11.8` |
  | AVERTISSEMENTS / 100 T. | Avertissements pour 100 tours | `10.3` |
  | PÉNALITÉS | Pénalités reçues | entier |
  | TEMPS PERDU | Temps perdu sur les tours avec contact, en course | `76.8 s` ou `—` |
  | ZONE À RISQUE | Secteur qui concentre au moins la moitié des contacts hors premier tour, dès 3 contacts | `Secteur 3` ou `—` |

  Sous 20 tours, une ligne affiche `—` à la place des valeurs pour 100 tours : un contact sur un tour se lirait comme 100 contacts pour 100 tours. Une note sous le tableau le rappelle.
* **Forme récente :** les 10 dernières courses, de la plus récente à la plus ancienne : date (lien vers l'événement dans « Détail des courses »), circuit, catégorie, contacts, obstacles, avertissements, pénalités, temps perdu.
* **Listes :** les 5 pilotes les plus touchés, les obstacles touchés et les motifs de pénalité, du plus au moins fréquent. Les motifs restent en anglais, tels qu'écrits par LMU. Une liste vide affiche « Aucun contact avec une voiture. », « Aucun contact avec un obstacle. » ou « Aucune pénalité. ».
* **États de la page :**
  * Chargement initial sans session : spinner.
  * Erreur de chargement sans session : encadré d'erreur renvoyant vers Options.
  * Aucun pilote sélectionné, ou aucune qualification ni course de ce pilote : « Aucun profil disponible ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  PROPRETÉ & DISCIPLINE EN PISTE                                                                           │
│  Profil de conduite                                                                                       │
│  Driver Tom · 170 sessions · 1079 tours, comparés aux autres pilotes des mêmes sessions.                  │
│                                                                                                           │
│  CE QUE VOS INCIDENTS VOUS COÛTENT                                                                        │
│  8 min 51 s                       • 43 % de vos contacts arrivent au départ ou au premier tour.           │
│  perdues sur les tours avec       • Un tour avec contact vous coûte 2.9 s en médiane.                     │
│  contact, soit 6.6 s par course   • 7 de vos 7 abandons ont eu un contact dans leur dernier tour.         │
│  258 contacts · 112 au départ · 231 places perdues · 41 avec dégâts                                       │
│                                                                                                           │
│  CONTACTS        OBSTACLES       AVERTISSEMENTS    TOURS INVALIDÉS   PÉNALITÉS                            │
│  16.7            11.8            10.3              11.4              0.6                                  │
│  Autres : 23.0   Autres : 13.4   Autres : 13.4     Autres : 13.3     Autres : 1.7                         │
│                                                                                                           │
│  OÙ PROGRESSER                                                                                            │
│                        SESSIONS  TOURS  CONTACTS/100  OBSTACLES/100  AVERTISSEMENTS/100  PÉNALITÉS         │
│  SESSION                                                                                                  │
│  Course                87        807    22.3          12.0           13.8                6                 │
│  Qualification         83        272    0.0           11.0           0.0                 0                 │
│  CATÉGORIE                                                                                                │
│  LMGT3                 85        517    16.1          13.3           7.5                 4                 │
│  Hypercar              3         1      —             —              —                   0                 │
│  CIRCUIT                                                                                                  │
│  Circuit de la Sarthe  16        47     57.4          68.1           10.6                2                 │
│                                                                                                           │
│  FORME RÉCENTE : vos 10 dernières courses                                                                 │
│  02/10/2026 21:14  Circuit de Spa-Francorchamps  LMGT3   0  1  1  0                                       │
│                                                                                                           │
│  PILOTES QUE VOUS TOUCHEZ LE PLUS    OBSTACLES                       MOTIFS DE PÉNALITÉ                    │
│  Driver Capucine               6     Contact avec un obstacle fixe 104   Speeding                   3      │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
