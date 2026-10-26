## 6. 🏠 Menu : Accueil (`Home.razor`)

> Spécification alignée sur le code au 26/10/2026 (`Home.razor`, `HomePresentation.cs`).

### Description
Nouvelle page d'arrivée de l'application (route `/`), premier onglet de la barre de navigation, devant « Mes courses ». C'est un tableau de bord de la carrière du pilote sélectionné (voir Options) : une grille de tuiles regroupées en sections, chaque compteur affichant son total puis sa ventilation par catégorie.

« Mes courses » ne répond plus que sur `/my-races`. L'ancienne page `/dashboard` (aperçu des imports, déjà affiché dans Options) est remplacée par cette page ; le logo de la barre de navigation renvoie vers `/`.

### Spécifications fonctionnelles
* **Pilote :** uniquement le pilote sélectionné dans Options (fusions de pilotes appliquées).
* **Sessions prises en compte :** courses (type contenant `Race`) pour le palmarès, les abandons et la forme ; qualifications (type contenant `Qual`) pour les poles et la position moyenne en qualif ; courses et qualifications pour le volume de roulage. Les essais ne sont pas comptés.
* **Catégorie :** même règle de déduction que « Mes courses » (`CarCategory.cs`).
* **Position retenue :** la **position en classe** (`ClassPosition`), comme le H2H. Une victoire en LMGT3 dans une course multiclasse compte comme une victoire.
* **Statut d'arrivée (`FinishStatus`) d'une course :**
  * `Finished Normally` : arrivé ;
  * `DNF` : abandon ;
  * `DQ` : disqualifié, compté à part des abandons ;
  * `None` : **non classé**, compté dans le total de courses avec son propre compteur, mais jamais dans victoires, podiums, tops, abandons ni positions moyennes.
* **Position officielle :** victoire, podium, top 5 et top 10 utilisent la `ClassPosition` écrite par LMU, quel que soit le statut (un abandon classé P3 compte comme un podium). Seules les courses non classées (`None`) en sont exclues.
* **Ventilation par catégorie :** sous chaque compteur, une ligne par catégorie ayant au moins une course, avec son badge, dans l'ordre Hypercar, LMP2, LMP3, LMGTE, LMGT3, puis les classes inconnues. Le total est la somme des lignes.
* **Ordre des sections :** sous le titre « Accueil », une première ligne avec la tuile **Pilote** à gauche et la section **Préférences** à droite (l'une sous l'autre en dessous de 1 120 px), puis, chacune sur toute la largeur : Forme récente, Palmarès, Roulage, Qualification & départ, Abandons.

#### Section « Pilote »
* Nom du pilote sélectionné.
* **Première course** et **dernière course** du pilote (date locale `dd/MM/yyyy`), masquées s'il n'a disputé aucune course.
* La tuile prend la hauteur des tuiles Préférences.

#### Section « Palmarès »
| Tuile | Valeur | Détail |
|---|---|---|
| COURSES | nombre de courses | ventilation par catégorie |
| VICTOIRES | courses en P1 de classe | ventilation + taux (victoires ÷ courses) |
| PODIUMS | courses en P1 à P3 de classe | ventilation + taux |
| TOP 5 | courses en P1 à P5 de classe | ventilation + taux |
| TOP 10 | courses en P1 à P10 de classe | ventilation + taux |
| POSITION MOYENNE | moyenne des `ClassPosition` des courses classées | ventilation, une décimale |

#### Section « Qualification & départ »
| Tuile | Valeur | Détail |
|---|---|---|
| POLES | qualifications terminées en P1 de classe | ventilation + taux (poles ÷ qualifications) |
| POSITION MOYENNE EN QUALIF | moyenne des `ClassPosition` en qualification | ventilation, une décimale |
| PLACES GAGNÉES | moyenne de `ClassGridPos − ClassPosition` par course | ventilation ; total cumulé ; signe `+` vert (gain), `−` rouge (perte) |

Les places gagnées ne comptent que les courses classées ayant une position de départ en classe.

#### Section « Abandons »
| Tuile | Valeur | Détail |
|---|---|---|
| ABANDONS | courses en `DNF` | ventilation + taux (abandons ÷ courses) |
| DISQUALIFICATIONS | courses en `DQ` | ventilation + taux |
| NON CLASSÉ | courses en `None` | ventilation |
| CAUSES D'ABANDON | répartition des abandons par `DNFReason` | barre horizontale empilée + liste « cause : nombre (%) » |

Libellés des causes : `Accident` → Accident, `Engine` → Moteur, `Suspension` → Suspension, `Fuel` → Carburant, `DNF` ou absent → Non précisée. Une autre valeur s'affiche sous son nom brut.

#### Section « Roulage »
| Tuile | Valeur | Détail |
|---|---|---|
| TOURS BOUCLÉS | somme des `Laps` du pilote | ventilation |
| DISTANCE | somme de `Laps × TrackLength` | en km, sans décimale, séparateur de milliers ; ventilation |
| TEMPS EN PISTE | somme des temps de tours chronométrés | format `h:mm`, ventilation. Les tours sans temps (invalidés) ne sont pas comptés |

#### Section « Forme récente »
* Les **10 dernières courses**, de la plus récente à la plus ancienne, en pastilles : `P1`…`Pn` (position en classe), `DNF`, `DQ` ou `NC`.
* Couleur des pastilles : violet `#b066ff` pour une victoire, vert `#22c55e` pour un podium, neutre sinon, rouge `#ef4444` pour DNF / DQ, gris `#8b949e` pour NC.
* Au survol : date, circuit, catégorie et voiture.
* Sous les pastilles, la **dernière course** : date, circuit, voiture, position en classe, statut, meilleur tour, et un lien vers son événement dans « Détail des courses ».

#### Section « Préférences »
| Tuile | Valeur | Détail |
|---|---|---|
| CIRCUIT FAVORI | circuit (lieu + tracé) avec le plus de courses | nombre de courses, victoires et podiums sur ce circuit |
| VOITURE FAVORITE | `CarType` avec le plus de courses | nombre de courses, victoires et podiums avec cette voiture |
| RÉPARTITION PAR CATÉGORIE | part des courses par catégorie | barre horizontale empilée, couleurs des badges de catégorie |

En cas d'égalité, le plus récemment roulé l'emporte.

Couleurs de la répartition (voir `UI_DESIGN_SPEC.md`) : Hypercar `#881337`, LMP2 `#1d4ed8`, LMP3 `#7c3aed`, LMGTE `#15803d`, LMGT3 `#d97706`, classe inconnue en gris `#8b949e`. Les causes d'abandon utilisent, dans l'ordre de fréquence, `#f97316`, `#38bdf8`, `#facc15`, `#f472b6` et `#94a3b8`.

#### États de la page
* Chargement initial sans session : spinner « Chargement de l'historique des courses… ».
* Erreur de chargement sans session : encadré d'erreur avec le message et un lien vers Options.
* Aucune session du pilote : « Aucune course disponible ».
* Valeur non calculable (aucune qualification, aucun abandon…) : `—`.
* La page se met à jour automatiquement à chaque nouvel import.

### Prérequis de données
Le parser doit lire, en plus des champs actuels :
* `ClassGridPos` du pilote (places gagnées) ;
* `DNFReason` du pilote (causes d'abandon) ;
* `TrackLength` de la session, en mètres (distance).

### Wireframe ASCII
Les valeurs sont des exemples.
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   ACCUEIL   MES COURSES   PROGRESSION   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS   Driver Tom  🟢 │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  TABLEAU DE BORD                                                                                                                 │
│  Accueil                                                                                                                         │
│                                                                                                                                  │
│  PILOTE                   PRÉFÉRENCES                                                                                            │
│  ┌──────────────────────┐ ┌──────────────────────────┐ ┌──────────────────────────┐ ┌────────────────────────────────────────┐   │
│  │ Driver Tom           │ │ CIRCUIT FAVORI           │ │ VOITURE FAVORITE         │ │ RÉPARTITION PAR CATÉGORIE              │   │
│  │ Première : 06/08/2025│ │ Circuit de la Sarthe     │ │ McLaren 720S LMGT3 Evo   │ │ ████████████████▓▓▓▓▓▓▓░░░░░           │   │
│  │ Dernière : 16/10/2026│ │ 18 courses · 2 V · 4 pod.│ │ 31 courses · 2 V · 6 pod.│ │ LMGT3 60 % · LMGTE 23 % · LMP2 17 %    │   │
│  └──────────────────────┘ └──────────────────────────┘ └──────────────────────────┘ └────────────────────────────────────────┘   │
│                                                                                                                                  │
│  FORME RÉCENTE                                                                                                                   │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ (P2) (P7) (DNF) (P1) (P11) (P4) (NC) (P3) (P9) (P6)                                                                        │  │
│  │ DERNIÈRE COURSE  16/10/2026 21:00 · Spa · BMW M4 LMGT3                                                                     │  │
│  │ P2 en classe · Arrivé · 2:18.104   Voir l'événement →                                                                      │  │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                                                                  │
│  PALMARÈS                                                                                                                        │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐   │
│  │ COURSES          │ │ VICTOIRES        │ │ PODIUMS          │ │ TOP 5            │ │ TOP 10           │ │ POSITION MOYENNE │   │
│  │ 87               │ │ 5       5.7 %    │ │ 14     16.1 %    │ │ 27     31.0 %    │ │ 48     55.2 %    │ │ 8.4              │   │
│  │ [LMGT3]  52      │ │ [LMGT3]  3       │ │ [LMGT3]  9       │ │ [LMGT3] 16       │ │ [LMGT3] 29       │ │ [LMGT3]  9.1     │   │
│  │ [LMGTE]  20      │ │ [LMGTE]  1       │ │ [LMGTE]  3       │ │ [LMGTE]  7       │ │ [LMGTE] 12       │ │ [LMGTE]  7.0     │   │
│  │ [LMP2]   15      │ │ [LMP2]   1       │ │ [LMP2]   2       │ │ [LMP2]   4       │ │ [LMP2]   7       │ │ [LMP2]   7.9     │   │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘ └──────────────────┘ └──────────────────┘ └──────────────────┘   │
│                                                                                                                                  │
│  ROULAGE                                                                                                                         │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐                                                                  │
│  │ TOURS BOUCLÉS    │ │ DISTANCE         │ │ TEMPS EN PISTE   │                                                                  │
│  │ 1 342            │ │ 6 120 km         │ │ 41:37            │                                                                  │
│  │ [LMGT3] 812      │ │ …                │ │ …                │                                                                  │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘                                                                  │
│                                                                                                                                  │
│  QUALIFICATION & DÉPART                                                                                                          │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐                                                                  │
│  │ POLES            │ │ QUALIF MOYENNE   │ │ PLACES GAGNÉES   │                                                                  │
│  │ 3   3.4 %        │ │ 7.2              │ │ +0.9 / course    │                                                                  │
│  │ [LMGT3]  2       │ │ [LMGT3]  7.9     │ │ 🟢 +78 au total  │                                                                   │
│  │ [LMP2]   1       │ │ …                │ │ [LMGT3] +1.2     │                                                                  │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘                                                                  │
│                                                                                                                                  │
│  ABANDONS                                                                                                                        │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────────────────────────┐                         │
│  │ ABANDONS         │ │ DISQUALIFICATIONS│ │ NON CLASSÉ       │ │ CAUSES D'ABANDON                     │                         │
│  │ 13  14.9 %       │ │ 0   0.0 %        │ │ 3   3.4 %        │ │ ███████████▓▓▓▓▓▓▓░░                 │                         │
│  │ [LMGT3]  9       │ │                  │ │ [LMGT3]  2       │ │ Non précisée 7 · Suspension 5 · …    │                         │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘ └──────────────────────────────────────┘                         │
│                                                                                                                                  │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  LE MANS ULTIMATE                                                                         RÉSULTATS LOCAUX · DONNÉES NON TRANSMISES │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
