## Menu : Cote pilote (`DriverRating.razor`)

> Spécification alignée sur le code au 03/11/2026 (`DriverRating.razor`, `RatingChart.razor`, `DriverRatings.cs`, `DriverRatingPresentation.cs`, `RatingChartLayout.cs`). Décisions et simulation sur les fixtures : [`propositions/cote-pilote.md`](../propositions/cote-pilote.md). Guide utilisateur des calculs : [`COTE_PILOTE.md`](../COTE_PILOTE.md), à garder synchronisé avec cette spécification.

### Description
Page `/rating`, onglet placé après « Profil de conduite ». Elle donne au **pilote sélectionné** (voir Options) une cote Elo multijoueur, calculée sur ses fichiers de résultats pour **chaque pilote croisé en course** : sa cote, son rang et son palier parmi ces pilotes, le prochain pilote à dépasser, l'évolution course après course, ses exploits et ses revers. Le « Classement des coureurs » compte des face-à-face, sans tenir compte du niveau des adversaires ; la cote, elle, pèse chaque résultat selon la force du plateau.

### Spécifications fonctionnelles
* **Courses notées :** sessions `Race` uniquement, dans l'ordre chronologique (`StartedAt`, puis `Id`), après fusion de pilotes. Qualifications et essais ne comptent pas.
* **Classe par classe :** chaque classe d'une course est notée séparément (règle du « Classement des coureurs ») ; une voiture seule dans sa classe n'est pas notée.
* **Classe sans arrivée :** une classe dont aucun pilote n'a un statut d'arrivée (autre que `DNF`, `DQ` et `None`) n'est pas notée. Sur les fixtures, quand le pilote quitte la course au départ, LMU écrit le fichier avec tous les autres pilotes en `None` (encore en piste, sans résultat) : leur ordre n'est pas un résultat. 19 des 107 courses des fixtures sont ainsi écartées.
* **Positions :** position officielle en classe (`DriverResults.ClassRank()`), y compris pour les abandons, les disqualifications et les pilotes `None` d'une classe notée, que LMU classe à la distance parcourue. Un pilote listé deux fois dans une session (deux noms fusionnés) garde sa meilleure position.
* **Calcul (Elo multijoueur) :** chaque course est une série de duels entre les pilotes d'une classe.
  * Chance de finir devant un adversaire : `1 / (1 + 10^((cote adverse − cote) / 400))`.
  * Variation : `K × Σ (résultat du duel − chance) / (pilotes − 1)`, où un duel gagné vaut 1, perdu 0, égalité 0,5. Toutes les variations d'une course sont calculées sur les cotes d'avant la course, puis appliquées ensemble.
  * Cote de départ : **1 500**. `K = 64` pendant les **5 premières courses notées** (calibration, cote **provisoire**), puis `K = 32`. Une course fait donc varier une cote de moins de K points, quelle que soit la taille de la grille.
  * **Place attendue :** `1 + Σ chances de chaque adversaire de finir devant`. **Force du plateau :** cote moyenne de la classe avant la course, pilote compris.
* **Classement et paliers :** seuls les pilotes sortis de calibration (5 courses notées) sont classés, de la meilleure à la moins bonne cote. Palier d'après la part des pilotes classés : **Platine** jusqu'à 10 %, **Or** jusqu'à 30 %, **Argent** jusqu'à 60 %, **Bronze** au-delà (rang × 100 ≤ pilotes × part). Les noms reprennent la catégorisation FIA des pilotes, connue en endurance ; les parts sont un choix de l'application. Le seuil d'un palier est la cote du dernier pilote qui y figure.
* **Bloc « Votre cote » :**
  * Cote, badge du palier, « `9e` sur `228` pilotes classés · top `4` % » (part arrondie au supérieur), record (cote la plus haute après une course) et sa date.
  * Cote provisoire : libellé « COTE PROVISOIRE », pas de badge ni de rang, « Encore N courses de calibration avant d'être classé. », pas d'objectifs.
  * **Prochains objectifs :** « +N pour dépasser *pilote*, *rang* » (le pilote classé juste devant ; N = écart arrondi au supérieur, au moins 1) ou « Vous êtes en tête du classement : à vous de garder la place. » ; puis « +N pour entrer en *palier* » (seuil du palier suivant) ou, en Platine, « Palier Platine : vous êtes dans le top 10 % des pilotes classés. ».
  * **Chiffres :** variation sur les 10 dernières courses notées (verte si positive, rouge si négative), force moyenne des plateaux, « N / M courses finies devant la place attendue », courses notées.
* **Évolution :** graphique SVG, un point par course notée à intervalle régulier (ordre chronologique), la ligne partant de la cote d'avant la première course. Point vert si la course a rapporté des points, rouge si elle en a coûté, gris sous un demi-point. Lignes en tirets, à la couleur du palier, pour le seuil de chaque palier non vide, avec leur libellé à droite (« Platine 1 549 ») ; l'échelle les inclut. Six dates au plus sur l'axe (`dd/MM/yy`). Survoler un point : date, circuit, « P3 sur 21 (attendu : 10.9) », « +25 → 1 525 ».
* **Vos exploits / Vos revers :** les 3 courses qui ont rapporté le plus de points (au moins 1) et les 3 qui en ont coûté le plus. Pour chacune : date (lien vers l'événement dans « Détail des courses »), circuit, « P3 sur 21, attendu 10.9 · plateau à 1 497 », variation.
* **Historique :** les courses notées, de la plus récente à la plus ancienne, 15 par défaut, bouton « Afficher les N courses ».

  | Colonne | Contenu | Format |
  |---|---|---|
  | DATE | Date locale, lien vers l'événement | `dd/MM/yyyy HH:mm` |
  | CIRCUIT | Lieu | texte |
  | CATÉGORIE | Badge de la catégorie | badge |
  | RÉSULTAT | Pastille de l'Accueil : `P1` violet, podium vert, `DNF`/`DQ` rouge, `NC` gris | `P3` |
  | ATTENDU | Place attendue, verte si le pilote a fini devant | `10.9` |
  | PILOTES | Pilotes notés de la classe | entier |
  | FORCE DU PLATEAU | Cote moyenne de la classe avant la course | `1 497` |
  | VARIATION | Points gagnés ou perdus | `+25`, `−4`, `0` |
  | COTE | Cote après la course | `1 525` |

* **Classement :** les pilotes classés par cote. Par défaut, le top 10 puis le pilote sélectionné et ses 3 voisins de chaque côté, séparés par une ligne `⋯` ; bouton « Afficher les N pilotes ». Colonnes RANG, PILOTE (tag « VOUS » et ligne teintée pour le pilote sélectionné), PALIER (badge), COTE, COURSES (courses notées), DERNIÈRE COURSE (`dd/MM/yyyy`).
* **« Comment la cote est-elle calculée ? » :** encadré repliable en bas de page, qui reprend ces règles en français courant, dont la forme simplifiée de la variation, `K × (place attendue − place obtenue) / adversaires` (égale à la somme des duels sans égalité de position), avec un exemple, et rappelle que la cote ne mesure que les grilles des fichiers de l'utilisateur, et non le classement officiel de LMU.
* **Formats :** cotes en points entiers, séparateur de milliers fin insécable (`DisplayFormat.Rating`) ; variations arrondies avant le choix du signe (`DisplayFormat.RatingChange`) : jamais `+0`.
* **États de la page :**
  * Chargement initial sans session : spinner. Erreur de chargement sans session : encadré d'erreur renvoyant vers Options.
  * Aucun pilote sélectionné, ou aucune course notée de ce pilote : « Aucune cote disponible ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Ailleurs dans le dashboard
* **Accueil, tuile Pilote :** « COTE PILOTE » (ou « COTE PILOTE PROVISOIRE »), la cote, le badge du palier, « `9e` sur `228` pilotes classés · `+55` sur 10 courses » ; le bloc est un lien vers `/rating`.
* **Détail des courses, onglet Course :** au-dessus du tableau, « FORCE DU PLATEAU » de chaque classe notée (badge, cote moyenne, pilotes), la cote du pilote sélectionné avant → après la course avec sa variation et sa place attendue, et un lien « Cote pilote → ». Une course sans classe notée affiche « Course non notée par la cote pilote : aucune classe n'a de pilote arrivé au drapeau à damier. ». Dans le tableau, la cote d'avant-course de chaque pilote noté suit son nom, en gris ; la survoler donne « Cote avant la course : 1 532 → 1 546 (+14) ».

### Couleurs
Paliers : Platine bleu glacier `#67e8f9`, Or `#d4af37`, Argent `#c0c7d1`, Bronze `#d2733f` (les couleurs du podium de « Mes courses »). Badge : texte et bordure de la couleur, fond teinté à 12 %. Points de la courbe : vert et rouge WEC.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│  VOTRE NIVEAU FACE AUX GRILLES CROISÉES                                                                   │
│  Cote pilote                                                                                              │
│  Driver Tom · 72 courses notées · une cote Elo calculée pour chaque pilote de vos fichiers de résultats.  │
│                                                                                                           │
│  VOTRE COTE                                      PROCHAINS OBJECTIFS                                      │
│  1 568  [PLATINE]                                • +6 pour dépasser Driver Warren, 8e                     │
│  9e sur 228 pilotes classés · top 4 %            • Palier Platine : vous êtes dans le top 10 %…           │
│  Record : 1 579, le 03/09/2025                                                                            │
│  +55 sur vos 10 dernières · 1 503 force moyenne · 36 / 72 devant la place attendue · 72 courses notées    │
│                                                                                                           │
│  ÉVOLUTION : votre cote course après course                                                               │
│  1 580 ┤      ●●                                                                    ●                     │
│  1 560 ┤   ●●   ●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●                                    ●                       │
│  1 549 ┤- - - - - - - - - - - - - - - - - - - - - - ●●●●●- - - - - - - - - - - -●- - - - - Platine 1 549 │
│  1 522 ┤- -●- - - - - - - - - - - - - - - - - - - - - - - - - - - ●●●●●●- -●●- - - - - - - Or 1 522     │
│        06/08/25        17/09/25        07/10/25        14/05/26        28/07/26        16/09/26           │
│                                                                                                           │
│  VOS EXPLOITS                                     VOS REVERS                                              │
│  14/08/2025 · Spa-Francorchamps          +32      06/08/2025 · Monza                          −18         │
│  P1 sur 21, attendu 11.0 · plateau 1 507          P16 sur 21, attendu 10.4 · plateau 1 505                │
│                                                                                                           │
│  HISTORIQUE                                                              [Afficher les 72 courses]        │
│  DATE              CIRCUIT        CATÉGORIE  RÉSULTAT  ATTENDU  PILOTES  FORCE  VARIATION  COTE           │
│  02/10/2026 21:14  Spa            LMGT3      (P4)      8.8      20       1 498  +8         1 568          │
│                                                                                                           │
│  CLASSEMENT                                                              [Afficher les 228 pilotes]       │
│  RANG  PILOTE              PALIER     COTE   COURSES  DERNIÈRE COURSE                                     │
│  1     Driver Lisa         [PLATINE]  1 615  8        30/07/2026                                          │
│  9     Driver Tom  VOUS    [PLATINE]  1 568  72       02/10/2026                                          │
│                                                                                                           │
│  ▸ Comment la cote est-elle calculée ?                                                                    │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
