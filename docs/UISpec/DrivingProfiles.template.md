## Menu : Profils de conduite (`DrivingProfiles.razor`)

> Spécification alignée sur le code au 02/11/2026 (`DrivingProfiles.razor`, `DrivingConductPresentation.cs`).

### Description
Page `/driving-profiles`, onglet placé après « Classement des coureurs ». Elle montre comment chaque pilote roule : contacts, limites de piste et pénalités, sur toutes les sessions importées. Dashboard uniquement : la console attend la décision du jalon « Avenir de la console Spectre ».

### Spécifications fonctionnelles
* **Sessions prises en compte :** qualifications et courses. Les essais sont ignorés, comme dans les autres vues de résultats : ils sont optionnels, et un pilote y essaie des choses.
* **Pilotes :** tous ceux des sessions retenues, regroupés par nom (insensible à la casse), après fusion de pilotes.
* **Contact :** les événements `Incident` ne sont pas des contacts, car chaque voiture signale le sien, souvent plusieurs fois.
  * Un contact avec une voiture compte **une fois pour chaque pilote impliqué**, quelle que soit la voiture qui l'a signalé.
  * Pour un pilote, deux signalements avec une même cible à moins de **5 secondes** comptent pour un seul contact. Sur les fixtures, 89 % des signalements d'une même paire de pilotes se suivent à moins de 5 s, et 2 % entre 5 et 10 s.
  * Cibles : **Contacts** = `another vehicle` ; **Obstacles** = `Immovable`, `Post`, `Sign`, `Cone`.
  * `Wing` et `Wheel` sont listés dans le fil des incidents de « Détail des courses », mais comptés nulle part : leur sens n'est pas établi. La valeur entre parenthèses des événements n'est pas lue.
* **Limites de piste :** chaque événement compte.
  * **Avertissements** : `TrackLimits` dont le texte est `Warning`.
  * **Tours invalidés** : `TrackLimits` dont le texte est `Invalid Lap Cut Track`. LMU n'en signale aucun en course : cette colonne vient donc des qualifications, et une note sous le tableau le rappelle.
  * `No Further Action` est ignoré.
* **Pénalités :** événements `Penalty` reçus (`received…`) et disqualifications (`Disqualify`). Les pénalités purgées (`served…`) ne comptent pas, et celles des `TrackLimits` non plus, car elles sont répétées par les événements `Penalty`.
* **Pour 100 tours :** (contacts + obstacles) × 100 / tours, pour comparer des pilotes aux historiques différents. Sans tour : `—`.
* **Recherche :** champ « Nom du pilote… » filtrant en direct (« contient », insensible à la casse).
* **Tri :** sessions (par défaut), contacts pour 100 tours, avertissements, tours invalidés, pénalités, tous décroissants ; nom (A à Z). Égalité départagée par nom.
* **Structure du tableau :**

  | Colonne | Contenu | Format |
  |---|---|---|
  | NOM DU PILOTE | Nom, suivi du tag **VOUS** pour le pilote sélectionné | texte |
  | SESSIONS | Qualifications et courses | entier |
  | TOURS | Tours effectués (`Laps`, à défaut le nombre de tours listés) | entier |
  | CONTACTS | Contacts avec une voiture | entier |
  | OBSTACLES | Contacts avec un obstacle | entier |
  | POUR 100 TOURS | Contacts et obstacles pour 100 tours | `28.5` ou `—` |
  | AVERTISSEMENTS | Avertissements de limites de piste | entier |
  | TOURS INVALIDÉS | Tours invalidés | entier |
  | PÉNALITÉS | Pénalités reçues | entier |

* **Ligne du pilote sélectionné :** surlignée, comme dans « Détail des courses ».
* Un badge dans l'en-tête du panneau indique le nombre de pilotes après filtrage.
* **États de la page :**
  * Chargement initial sans session : spinner.
  * Erreur de chargement sans session : encadré d'erreur renvoyant vers Options.
  * Aucune qualification ni course : « Aucun profil disponible ».
  * Recherche sans résultat : « Aucun pilote ne correspond à cette recherche ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  PROPRETÉ & DISCIPLINE EN PISTE                                                                           │
│  Profils de conduite                                                                                      │
│                                                                                                           │
│  QUALIFICATIONS & COURSES                                                                         [ 238 ] │
│  Pilotes rencontrés                                                                                       │
│  RECHERCHER UN PILOTE [ Nom du pilote…     ]   TRIER PAR [ Sessions (décroissant) ▼ ]                     │
│                                                                                                           │
│  NOM DU PILOTE      SESSIONS  TOURS  CONTACTS  OBSTACLES  POUR 100 TOURS  AVERTISS.  TOURS INV.  PÉNALITÉS │
│  Driver Tom VOUS    170       1079   180       127        28.5            111        31          6         │
│  Driver Lucas       38        207    50        45         45.9            0          0           6         │
│                                                                                                           │
│  Un contact compte une fois pour chaque pilote impliqué · En course, LMU ne signale pas de tours invalidés │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
