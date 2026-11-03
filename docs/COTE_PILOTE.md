# La Cote pilote, expliquée

Ce guide détaille, règle par règle, comment LMU DeltaRacing calcule la **Cote pilote** (onglet « Cote pilote » du tableau de bord). Aucune connaissance en mathématiques n'est nécessaire : chaque règle est illustrée par un exemple chiffré.

← Retour au [guide d'utilisation](../README.md#cote-pilote)

## Sommaire

1. [L'idée en une minute](#1-lidée-en-une-minute)
2. [Quelles courses comptent](#2-quelles-courses-comptent)
3. [La chance de battre un adversaire](#3-la-chance-de-battre-un-adversaire)
4. [La place attendue](#4-la-place-attendue)
5. [Combien de points on gagne ou on perd](#5-combien-de-points-on-gagne-ou-on-perd)
6. [Un exemple complet](#6-un-exemple-complet)
7. [Les débuts : la calibration](#7-les-débuts--la-calibration)
8. [Le classement et les paliers](#8-le-classement-et-les-paliers)
9. [Ce qu'affiche chaque bloc de la page](#9-ce-quaffiche-chaque-bloc-de-la-page)
10. [Questions fréquentes](#10-questions-fréquentes)

---

## 1. L'idée en une minute

La Cote pilote reprend le principe du **classement Elo** des échecs, adapté aux courses à plusieurs voitures.

- Tout le monde part de **1 500 points**.
- Une course est vue comme une série de **duels** : finir devant un pilote, c'est gagner son duel contre lui ; finir derrière, c'est le perdre.
- Avant la course, les cotes **prédisent** une place pour chaque pilote. Après la course, on compare la place obtenue à cette place attendue :
  - **mieux que prévu** : on gagne des points ;
  - **moins bien que prévu** : on en perd ;
  - **comme prévu** : la cote ne bouge presque pas.
- Les points gagnés par les uns sont, en gros, perdus par les autres.

Conséquence : une **5e place dans un plateau très relevé** peut rapporter des points, alors qu'une **5e place dans un plateau débutant** peut en coûter. C'est ce qui distingue la cote des statistiques de l'Accueil et du « Classement des coureurs », qui comptent les résultats sans tenir compte du niveau des adversaires.

Tout est calculé sur votre ordinateur, à partir de vos seuls fichiers de résultats LMU. Rien n'est envoyé en ligne.

## 2. Quelles courses comptent

| Règle | Détail |
|---|---|
| **Courses uniquement** | Les qualifications et les essais ne comptent pas : ce ne sont pas des résultats de course. |
| **Ordre chronologique** | Les courses sont rejouées de la plus ancienne à la plus récente. Chaque course part donc des cotes laissées par la précédente. |
| **Catégorie par catégorie** | Dans une course multiclasse, chaque catégorie est notée à part. Une Hypercar ne « bat » pas une GT3 : les deux ne sont jamais comparées. |
| **Au moins deux pilotes** | Une voiture seule dans sa catégorie n'a personne à battre : sa course n'est pas notée. |
| **Au moins un pilote arrivé** | Si **aucun** pilote de la catégorie n'a reçu le drapeau à damier, la catégorie n'est pas notée (voir ci-dessous). |
| **Position officielle en classe** | On utilise la position en classe écrite par LMU, celle de « Mes courses ». Les abandons (DNF), les disqualifiés (DQ) et les non-classés (NC) gardent la place que LMU leur donne, en général en fin de classement : un abandon est donc un duel perdu contre tous ceux qui finissent devant. |
| **Fusions de pilotes** | Les fusions déclarées dans Options sont appliquées : un pilote qui a changé de pseudo garde une seule cote. Si un même pilote apparaît deux fois dans une course après une fusion, seule sa meilleure place compte. |

**Pourquoi écarter les catégories sans arrivée ?** Quand vous quittez une course avant la fin (au départ, par exemple), LMU enregistre le fichier tout de suite. Les autres pilotes y figurent encore « en piste », sans résultat (statut `None`), simplement rangés dans l'ordre où ils roulaient à cet instant, souvent l'ordre de la grille. Ce n'est pas un vrai résultat : la noter ferait gagner ou perdre des points au hasard. Dans les fichiers de démonstration, 19 courses sur 107 sont ainsi écartées.

En revanche, dès qu'au moins un pilote est arrivé, la catégorie est notée **en entier** : un pilote encore en piste à l'enregistrement du fichier (souvent un retardataire à un tour) garde la place que LMU lui donne.

## 3. La chance de battre un adversaire

Avant chaque course, on calcule pour chaque duel la **chance** de finir devant l'adversaire. Elle ne dépend que de l'**écart de cote** entre les deux pilotes :

```
chance = 1 / (1 + 10^(écart / 400))      avec écart = cote de l'adversaire − votre cote
```

Concrètement :

| Votre cote face à l'adversaire | Votre chance de finir devant |
|---|---|
| Égales | 50 % |
| 50 points de plus | 57 % |
| 100 points de plus | 64 % |
| 200 points de plus | 76 % |
| 400 points de plus | 91 % (10 contre 1) |
| 100 points de moins | 36 % |
| 200 points de moins | 24 % |

Les chances de deux adversaires l'un contre l'autre font toujours 100 % : si vous avez 64 % de chances de battre un pilote, il a 36 % de chances de vous battre.

## 4. La place attendue

La **place attendue** est la place que les cotes prévoyaient avant la course :

```
place attendue = 1 + somme, pour chaque adversaire, de sa chance de finir devant vous
```

On part de la 1re place, et chaque adversaire vous « recule » de sa chance de vous battre.

- 20 pilotes tous de même cote : chaque adversaire a 50 % de chances de vous battre, donc place attendue = 1 + 19 × 0,5 = **10,5** (le milieu de la grille).
- Si vous êtes bien mieux coté que le plateau, la place attendue se rapproche de 1 ; si vous êtes moins bien coté, elle se rapproche de la dernière place.

Dans la page, c'est la colonne **ATTENDU** de l'historique, affichée en vert quand vous avez fini devant.

## 5. Combien de points on gagne ou on perd

Toute la règle tient en une ligne :

```
variation = K × (place attendue − place obtenue) / nombre d'adversaires
```

- **K** vaut **32** en temps normal, et **64** pendant vos 5 premières courses (voir [calibration](#7-les-débuts--la-calibration)).
- Le **nombre d'adversaires** est le nombre de pilotes notés de votre catégorie, moins vous.

Quelques conséquences directes :

- **Finir devant la place attendue rapporte**, finir derrière coûte. Plus l'écart est grand, plus la variation est forte.
- **Le vainqueur gagne toujours des points**, et **le dernier en perd toujours** : la place attendue est toujours entre la 1re et la dernière place.
- **Une course fait varier la cote de moins de K points** (32, ou 64 en calibration), quelle que soit la taille de la grille. Grâce à la division par le nombre d'adversaires, une course à 40 voitures ne pèse pas plus lourd qu'une course à 10.
- **Toutes les variations d'une course sont calculées sur les cotes d'avant la course**, puis appliquées en même temps. L'ordre des pilotes dans le fichier ne change rien.

> **Pour les curieux :** c'est exactement la règle Elo classique, appliquée à chaque duel puis divisée par le nombre d'adversaires : `variation = K × somme(résultat du duel − chance) / adversaires`, où un duel gagné vaut 1, un duel perdu 0 et une égalité 0,5. La somme des « résultat − chance » est toujours égale à « place attendue − place obtenue », d'où la formule simplifiée ci-dessus.

**Exemples réels** (tirés des fichiers de démonstration) :

| Course | Résultat | Attendu | Pilotes | K | Calcul | Variation |
|---|---|---|---|---|---|---|
| Spa, 14/08/2025 | P1 | 11,0 | 21 | 64 | 64 × (11,0 − 1) / 20 | **+32** |
| Monza, 06/08/2025 | P3 | 10,9 | 21 | 64 | 64 × (10,9 − 3) / 20 | **+25** |
| Spa, 02/10/2026 | P4 | 8,8 | 20 | 32 | 32 × (8,8 − 4) / 19 | **+8** |
| Fuji, 27/07/2026 | P18 | 8,6 | 19 | 32 | 32 × (8,6 − 18) / 18 | **−17** |

(La place attendue est affichée arrondie au dixième : refaire le calcul à la main peut donner un point d'écart.)

## 6. Un exemple complet

Une course en catégorie LMGT3, 4 pilotes, tous sortis de calibration (K = 32) :

| Pilote | Cote avant la course |
|---|---|
| Vous | 1 600 |
| Alex | 1 700 |
| Bruno | 1 500 |
| Chris | 1 400 |

**Étape 1 : la force du plateau.** C'est la cote moyenne de la catégorie avant la course, vous compris : (1 600 + 1 700 + 1 500 + 1 400) / 4 = **1 550**.

**Étape 2 : vos chances dans chaque duel.**

| Adversaire | Écart | Votre chance de finir devant | Sa chance de finir devant vous |
|---|---|---|---|
| Alex (1 700) | +100 | 36 % | 64 % |
| Bruno (1 500) | −100 | 64 % | 36 % |
| Chris (1 400) | −200 | 76 % | 24 % |

**Étape 3 : votre place attendue.** 1 + 0,64 + 0,36 + 0,24 = **2,24**. Les cotes vous voyaient un peu derrière la 2e place.

**Étape 4 : la variation selon votre résultat.**

| Votre place | Calcul | Variation | Nouvelle cote |
|---|---|---|---|
| 1re (devant Alex) | 32 × (2,24 − 1) / 3 | **+13** | 1 613 |
| 2e | 32 × (2,24 − 2) / 3 | **+3** | 1 603 |
| 3e | 32 × (2,24 − 3) / 3 | **−8** | 1 592 |
| 4e (dernier) | 32 × (2,24 − 4) / 3 | **−19** | 1 581 |

Battre Alex, mieux coté, rapporte gros ; finir derrière Chris, bien moins coté, coûte cher.

## 7. Les débuts : la calibration

- Un nouveau pilote part de **1 500**, qu'il soit débutant ou champion : la cote ne sait encore rien de lui.
- Pendant ses **5 premières courses notées**, K vaut **64** au lieu de 32 : la cote bouge deux fois plus vite pour trouver rapidement son niveau. C'est la pratique des fédérations d'échecs pour les nouveaux joueurs.
- Pendant ces 5 courses, la cote est **provisoire** : le pilote n'est **pas classé** et n'a pas de palier. La page affiche « COTE PROVISOIRE » et le nombre de courses restantes.
- À partir de la 6e course, K revient à 32 et le pilote entre dans le classement.

Il n'y a pas de baisse liée à l'inactivité : une cote ne bouge que lorsque son pilote court.

## 8. Le classement et les paliers

**Le classement** réunit tous les pilotes sortis de calibration (au moins 5 courses notées), de la meilleure à la moins bonne cote. Ce sont les pilotes **que vous avez croisés**, puisque la cote ne connaît que vos fichiers.

**Le palier** dépend de votre place dans ce classement, et non d'un nombre de points fixe : une même cote peut donc changer de palier selon les pilotes présents dans vos fichiers.

| Palier | Part du classement | Exemple avec 228 pilotes classés |
|---|---|---|
| **Platine** | top 10 % | rangs 1 à 22 |
| **Or** | jusqu'à 30 % | rangs 23 à 68 |
| **Argent** | jusqu'à 60 % | rangs 69 à 136 |
| **Bronze** | au-delà | rangs 137 à 228 |

Les noms reprennent la catégorisation des pilotes de la FIA, familière en endurance (Platinum, Gold, Silver, Bronze). Les parts sont propres à LMU DeltaRacing.

**Le « top N % »** affiché sous votre cote est votre rang divisé par le nombre de pilotes classés, arrondi au-dessus : le 9e sur 228 est dans le **top 4 %** (9 / 228 = 3,9 %).

**Le seuil d'un palier** est la cote du dernier pilote qui y figure aujourd'hui. C'est la ligne en pointillés du graphique (« Platine 1 549 »).

## 9. Ce qu'affiche chaque bloc de la page

### Votre cote

| Élément | Calcul |
|---|---|
| Cote | Votre cote après votre dernière course notée, arrondie au point. |
| Badge | Votre palier (absent pendant la calibration). |
| « 9e sur 228 pilotes classés · top 4 % » | Votre rang dans le classement et sa part, arrondie au-dessus. |
| Record | La cote la plus haute atteinte après une course, et la date de cette course. |

### Prochains objectifs

| Objectif | Calcul |
|---|---|
| « +6 pour dépasser Driver Warren, 8e » | Écart avec la cote du pilote classé juste devant vous, arrondi au-dessus, avec au moins 1 point. En tête : « Vous êtes en tête du classement ». |
| « +19 pour entrer en Or » | Écart avec le seuil du palier suivant. En Platine, la page rappelle que vous êtes dans le top 10 %. |

Ces écarts sont calculés sur la situation d'aujourd'hui : les autres pilotes courent aussi, et les seuils bougent avec eux.

### Les quatre chiffres

| Chiffre | Calcul |
|---|---|
| « +55 sur vos 10 dernières courses » | Somme des variations de vos 10 dernières courses notées (vert si positive, rouge si négative). |
| Force moyenne de vos plateaux | Moyenne des forces de plateau de toutes vos courses notées. Plus elle est haute, plus vous affrontez des pilotes bien cotés. |
| « 36 / 72 courses finies devant la place attendue » | Courses où votre place obtenue est meilleure que la place attendue, sur le total. |
| Courses notées | Nombre de vos courses prises en compte. |

### Évolution

- Un point par course notée, à **intervalle régulier** et dans l'ordre chronologique : une soirée de 4 courses prend autant de place qu'un mois de 4 courses.
- La ligne part de votre cote d'avant la première course (1 500).
- Point **vert** : la course a rapporté des points ; **rouge** : elle en a coûté ; **gris** : moins d'un demi-point.
- Lignes en pointillés : seuils actuels des paliers.
- Survoler un point affiche la date, le circuit, la place obtenue et attendue, et la variation.

### Vos exploits et vos revers

Les 3 courses qui vous ont **rapporté** le plus de points, et les 3 qui vous en ont **coûté** le plus, avec la place obtenue, la place attendue et la force du plateau. La date mène au détail de la course.

### Historique

Toutes vos courses notées, de la plus récente à la plus ancienne : résultat, place attendue, nombre de pilotes notés de votre catégorie, force du plateau, variation et cote après la course.

### Classement

Le top 10, puis vous et vos 3 voisins de chaque côté. Le bouton affiche tous les pilotes classés.

### Ailleurs dans le tableau de bord

- **Détail des courses**, onglet Course : la force du plateau de chaque catégorie notée, votre cote avant → après la course et votre place attendue. La cote d'avant-course de chaque pilote suit son nom ; la survoler montre sa variation.
- **Accueil**, tuile Pilote : votre cote, votre palier, votre rang et la variation de vos 10 dernières courses.

## 10. Questions fréquentes

**J'ai fini 5e et j'ai perdu des points. Pourquoi ?**
Les cotes prévoyaient mieux : vous étiez le mieux coté du plateau, et votre place attendue était par exemple 3,2. Finir 5e, c'est faire moins bien que prévu.

**J'ai fini 12e et j'ai gagné des points. Pourquoi ?**
Le plateau était plus fort que vous, avec une place attendue de 14 par exemple. Finir 12e, c'est faire mieux que prévu.

**Une course n'apparaît pas dans mon historique.**
C'est une qualification ou des essais, ou bien personne n'est arrivé dans votre catégorie (vous avez quitté la course au départ), ou vous étiez seul dans votre catégorie. Le Détail des courses l'indique : « Course non notée par la cote pilote ».

**Un abandon compte-t-il ?**
Oui, dès qu'au moins un pilote de votre catégorie est arrivé. Vous gardez la place que LMU vous donne, en général derrière tous les pilotes arrivés : c'est donc souvent une perte de points.

**Un pilote qui n'a fait que 6 courses est devant moi. Est-ce normal ?**
Oui. Pendant ses 5 courses de calibration, sa cote a bougé deux fois plus vite : quelques bons résultats suffisent à le placer haut. Sa cote se stabilisera avec ses courses suivantes.

**Pourquoi ma cote n'est-elle pas celle de LMU ?**
Ce sont deux calculs différents. La Cote pilote ne connaît que les pilotes et les courses de **vos** fichiers, alors que le classement officiel de LMU repose sur toutes les courses en ligne. Comparez votre cote avec celle des pilotes de votre classement, pas avec LMU.

**Ma cote a changé alors que je n'ai pas couru.**
La cote est recalculée à chaque lancement, à partir de tous les fichiers du dossier de résultats. Elle change si vous ajoutez ou retirez des fichiers (y compris d'anciennes courses, qui réécrivent l'histoire depuis leur date), si vous déclarez ou annulez une fusion de pilotes, ou si vous changez de dossier de résultats.

**Les chiffres affichés ne tombent pas juste au point près.**
Les calculs gardent toutes les décimales ; seul l'affichage est arrondi. « 1 560 → 1 568 (+8) » peut cacher 1 559,6 → 1 567,7 (+8,1).

**Mon pseudo a changé, j'ai deux cotes.**
Déclarez la fusion dans **Options › Fusion de pilotes** : vos deux historiques sont alors réunis et rejoués comme un seul.
