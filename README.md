# LMU DeltaRacing

**Suivez votre progression sur Le Mans Ultimate, course après course.**

LMU DeltaRacing lit les fichiers de résultats que *Le Mans Ultimate* enregistre après chaque session et en tire un tableau de bord personnel :

- vos meilleurs tours en qualification et en course, comparés à votre session précédente sur le même circuit ;
- votre bilan face à chaque adversaire rencontré ;
- le classement détaillé de chaque course, secteur par secteur, comme sur les écrans de chronométrage du WEC.

Tout se passe sur votre PC : l'application ne se connecte à aucun service en ligne et **aucune donnée n'est envoyée**.

---

## Sommaire

1. [Installation](#installation)
2. [Premier lancement](#premier-lancement)
3. [Les pages](#les-pages)
4. [Les options](#les-options)
5. [Questions fréquentes](#questions-fréquentes)

---

## Installation

**Configuration requise :** Windows 64 bits. Rien d'autre à installer.

1. Téléchargez `LMU-DeltaRacing-Setup.exe` depuis la page [Releases](https://github.com/Fugeekies/LMU-DeltaRacing/releases) du projet.
2. Lancez-le. L'installation se fait dans votre profil Windows, sans droits administrateur.
   > Windows peut afficher un avertissement SmartScreen, car l'installeur n'est pas signé. Cliquez sur **Informations complémentaires**, puis **Exécuter quand même**.
3. Cochez si vous le souhaitez **Créer une icône sur le bureau**, puis terminez l'installation.

L'installeur cherche automatiquement le dossier où *Le Mans Ultimate* enregistre vos résultats, dans vos Documents et dans le dossier d'installation Steam du jeu. S'il ne trouve aucun résultat, il configure des **données de démonstration** pour que vous puissiez découvrir l'application. Vous pourrez choisir le bon dossier ensuite (voir [Options](#les-options)).

**Mise à jour :** installez simplement la nouvelle version par-dessus l'ancienne. Vos réglages sont conservés.

**Désinstallation :** depuis *Paramètres Windows › Applications*, comme n'importe quel programme.

---

## Premier lancement

Lancez **LMU-DeltaRacing** depuis le menu Démarrer ou l'icône du bureau.

- Le tableau de bord s'ouvre **dans votre navigateur web**.
- Une **fenêtre noire** s'ouvre aussi : c'est l'application elle-même. Laissez-la ouverte tant que vous utilisez le tableau de bord ; **la fermer arrête l'application**. Fermer seulement l'onglet du navigateur ne suffit pas.
- Au démarrage, l'application lit tous vos fichiers de résultats. Une barre de progression s'affiche dans **Options** ; l'indicateur en haut à droite passe de **IMPORT INITIAL** à **SURVEILLANCE ACTIVE**.
- **Votre pilote est détecté automatiquement :** c'est le nom qui apparaît dans le plus grand nombre de vos fichiers de résultats. Il s'affiche en haut à droite. S'il est incorrect, changez-le dans **Options › Pilote principal**.

Ensuite, **laissez l'application ouverte pendant que vous jouez** : chaque nouvelle session terminée dans *Le Mans Ultimate* apparaît automatiquement dans le tableau de bord, sans rien recharger.

---

## Les pages

Le menu en haut de l'écran donne accès à quatre pages.

### Mes courses

Votre historique, **circuit par circuit**, pour voir si vous progressez.

- Chaque ligne associe une **course** à la **qualification** qui la précède.
- Pour chacune, vous voyez votre meilleur tour, votre place sur la grille, le nombre de tours et votre temps total de course.
- La colonne **DELTA (n-1)** compare votre meilleur tour à celui de votre session précédente sur le même circuit, dans la même catégorie :
  - 🟢 **en vert, négatif** : vous avez été plus rapide ;
  - 🔴 **en rouge, positif** : vous avez été plus lent.

  La comparaison se fait même si vous avez changé de voiture, tant que la catégorie est la même.
- Filtrez par **circuit** et par **catégorie** : Hypercar, LMP2, LMP3, LMGTE, LMGT3.

> Les tours invalidés par le jeu (sortie des limites de piste) ne comptent pas : *Le Mans Ultimate* efface leur temps.

### Classement des coureurs

Votre bilan **face à face** contre chaque pilote croisé en course.

- **Courses affrontées :** nombre de courses disputées contre ce pilote.
- **Victoires / Défaites :** nombre de fois où vous avez terminé devant / derrière lui.
- **% de victoire**, avec une barre verte (victoires) et rouge (défaites).

Seules les **courses** comptent (pas les qualifications), et seulement contre les pilotes de **votre catégorie** : battre une GT3 au volant d'une Hypercar ne compte pas.

Recherchez un pilote par son nom et triez par nombre de courses, victoires, pourcentage ou nom.

### Détail des courses

Le classement complet d'un événement.

1. Choisissez l'événement dans la liste. Chaque événement regroupe les essais, la qualification et la course disputés à la suite sur un circuit, par exemple « 02/10/2026 21:00 - Circuit de Spa-Francorchamps (Qualif · Course) ».
2. Passez d'une session à l'autre avec les onglets **COURSE**, **QUALIFICATION** et **ESSAIS**.

Pour chaque pilote :

| Colonne | Signification |
|---|---|
| **POS / CLASSE** | Position au général, catégorie et position dans la catégorie |
| **SECTEUR 1, 2, 3** | Meilleur temps du pilote sur chaque secteur. 🟣 **Violet** : meilleur de la catégorie. 🟡 **Jaune** : temps standard |
| **DELTA S1, S2, S3** | Écart avec le meilleur temps de la catégorie sur ce secteur |
| **TOUR IDÉAL** | Somme des trois meilleurs secteurs : le tour que le pilote aurait pu faire |
| **MEILLEUR TOUR / DELTA TOUR** | Meilleur tour réel et écart avec le meilleur tour de la catégorie |
| **DIFF 1ER** | Écart avec le premier de la catégorie : **LEADER**, un temps, **+1 tour** ou **DNF** |

Votre ligne est repérée par la mention **VOUS**. Toutes les comparaisons se font **au sein de chaque catégorie**, comme en WEC.

### Options

Les réglages, détaillés ci-dessous. Sous les réglages, un **état des imports** montre la progression du chargement, les fichiers qui n'ont pas pu être lus et vos sessions récentes.

---

## Les options

| Réglage | À quoi ça sert |
|---|---|
| **Dossier des résultats XML** | Où *Le Mans Ultimate* enregistre vos résultats, en général `Documents\Le Mans Ultimate\UserData\Log\Results` ou `…\steamapps\common\Le Mans Ultimate\UserData\Log\Results`. Choisissez-le avec **Parcourir…**, puis cliquez sur **Appliquer** : le dossier est enregistré et vos résultats sont rechargés. Laissez le champ vide pour une détection automatique. |
| **Pilote principal** | Le pilote dont vous suivez les résultats (vous, en principe). La liste est triée par nombre d'apparitions. Le choix est enregistré tout de suite. |
| **Fusion de pilotes** | Vous avez changé de pseudo ? Choisissez l'**ancien nom** et le **nom conservé**, puis **Fusionner** : les deux historiques sont regroupés. Vos fichiers de résultats ne sont pas modifiés, et chaque fusion peut être annulée. Si les deux noms ont couru ensemble dans une même session, un **avertissement** s'affiche : il s'agit sans doute de deux personnes différentes. |
| **Démarrage automatique** | Surveiller les nouveaux résultats dès le lancement de l'application (activé par défaut). |
| **Sessions d'essais** | Charger aussi les essais pour l'onglet **ESSAIS** du détail des courses. Désactivé par défaut, car les essais sont souvent vides et allongent le chargement. |

Les réglages sont enregistrés dans le fichier `appsettings.json` du dossier d'installation (`%LOCALAPPDATA%\Programs\LMU-DeltaRacing`).

---

## Questions fréquentes

**Je ne vois aucune course.**
Vérifiez dans **Options** que le dossier des résultats est le bon (l'indicateur doit afficher « Dossier valide » avec un nombre de fichiers), puis que le **pilote principal** est bien votre nom de pilote dans le jeu.

**Je vois des pilotes « Driver Tom », « Driver Lucas »…**
Ce sont les **données de démonstration** installées avec l'application, utilisées quand aucun résultat du jeu n'a été trouvé. Choisissez votre dossier de résultats dans **Options**, puis **Appliquer**.

**Une session que je viens de terminer n'apparaît pas.**
Le jeu écrit le fichier de résultats à la **fin** de la session. Vérifiez que l'indicateur en haut à droite affiche **SURVEILLANCE ACTIVE**. Sinon, cliquez sur **Appliquer** dans Options pour relancer la surveillance.

**Mon historique est coupé en deux depuis que j'ai changé de pseudo.**
Utilisez **Options › Fusion de pilotes**.

**Pourquoi ma voiture n'est-elle pas comparée à toutes les autres ?**
Les écarts, les secteurs violets et le classement face à face se calculent **par catégorie**, comme en championnat d'endurance : une GT3 est comparée aux GT3.

**J'ai fermé le navigateur, comment rouvrir le tableau de bord ?**
Si la fenêtre noire de l'application est encore ouverte, fermez-la, puis relancez **LMU-DeltaRacing** depuis le menu Démarrer : le navigateur se rouvrira.

**Où trouver le journal en cas de problème ?**
Dans le fichier `lmu-deltaracing.log` du dossier d'installation. Il est remplacé à chaque lancement.

**Mes données sont-elles envoyées quelque part ?**
Non. L'application fonctionne uniquement sur votre PC et le tableau de bord n'est accessible que depuis celui-ci.

---

*Développeurs : la documentation technique est dans [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) et [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).*
