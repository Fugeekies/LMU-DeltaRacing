## 4. ⚙️ Menu : Options (`Settings.razor`)

> Spécification alignée sur le code au 25/10/2026 (`Settings.razor`, `DashboardOverview.razor`, `DashboardDataService.cs`, `ResultsDirectoryResolver.cs`).

### Description
Panneau de configuration (route `/settings`) qui permet de choisir le dossier des résultats XML, de recharger les imports, de choisir le pilote principal et de régler la surveillance au démarrage. Sous les options, il affiche l'état des imports et les sessions récentes du pilote.

### Spécifications fonctionnelles

#### Panneau « Dossier des résultats XML »
* Champ de saisie du chemin, validé à la sortie du champ. Placeholder : dossier actuellement chargé, à défaut « Détection automatique du dossier LMU ».
* Bouton **« Parcourir… »** : ouvre le sélecteur de dossiers natif Windows. Il est désactivé hors Windows (message d'aide affiché) et pendant un traitement.
* **Champ vide = détection automatique**, dans cet ordre (premier dossier contenant des `.xml`) :
  1. `Documents\Le Mans Ultimate\UserData\Log\Results`
  2. `Documents\My Games\Le Mans Ultimate\UserData\Log\Results`
  3. `<dossier de l'application>\UserData\Log\Results`
  4. `<dossier de l'application>\TestAssets\Results` (fixtures installées)
  5. `assets\Results`, en remontant les dossiers parents (développement)
* Un chemin relatif est résolu depuis le dossier de l'application.
* **Indicateur d'état :**
  * 🟢 « Dossier valide · N fichier(s) XML » + chemin résolu ;
  * 🔴 « Dossier introuvable ou illisible » + message d'erreur ;
  * ⚪ « Vérifiez ou choisissez un dossier de résultats. » si aucune vérification n'a eu lieu.
* Bouton **« Appliquer »**, en une seule action :
  1. revalide le dossier ;
  2. enregistre `LMU:ResultsDirectory` dans `appsettings.json` (écriture atomique, à côté de l'exécutable ; champ vide = détection automatique) ;
  3. vide le dépôt en mémoire, réimporte tout le dossier et (re)démarre la surveillance.

  Il affiche « Chargement… » pendant le traitement et est désactivé si le dossier est invalide. Message de retour : « Dossier enregistré et chargé : N fichier(s) traité(s). La surveillance est active. », ou le message d'erreur.

#### Panneau « Pilote principal »
* Liste déroulante des pilotes présents dans les résultats importés, triés par fréquence d'apparition décroissante puis par nom. Libellé : `Nom — 12,3 % (n/N fichiers)`.
* Le choix est **enregistré immédiatement** dans `LMU:UserName` (`appsettings.json`) et toutes les vues basculent sur ce pilote.
* Liste désactivée pendant le chargement initial ou si aucun pilote n'est disponible.
* Pilote non défini : option « Détection après le chargement… ». Au premier démarrage, le pilote le plus fréquent est sélectionné et enregistré automatiquement à la fin de l'import initial.
* Pilote configuré absent des résultats : option « <nom> (absent des résultats chargés) ».

#### Panneau « Fusion de pilotes »
* But : après un changement de nom (pseudo), déclarer que deux noms désignent le même pilote, pour ne pas couper son historique.
* Deux listes déroulantes (« Ancien nom », « Nom conservé ») proposent les pilotes des résultats chargés ; bouton **« Fusionner »**, désactivé si un nom manque ou si les deux sont identiques.
* **Fusion en mémoire uniquement :** les fichiers de résultats ne sont jamais modifiés. Dans toutes les vues (Mes courses, Classement, Détail, choix du pilote principal), l'ancien nom est remplacé par le nom conservé. Si le pilote principal est un ancien nom, le nom conservé est utilisé.
* Enregistrement immédiat dans `appsettings.json` (`LMU:DriverMerges`, liste de `{ "From", "To" }`), sans recharger les fichiers.
* Cohérence des fusions : un ancien nom pointe toujours directement vers un nom conservé (pas de chaîne) ; fusionner un nom conservé dans un de ses anciens noms inverse la fusion.
* **Avertissement :** si les deux pilotes ont participé ensemble à au moins une session (données d'origine, en tenant compte des noms déjà fusionnés), un encadré d'alerte indique le nombre de sessions communes : une même personne ne peut pas courir deux fois dans une session. La fusion reste possible.
* Liste des fusions enregistrées : « Ancien nom → Nom conservé », avec « ⚠ N session(s) en commun » si le cas se présente, et un bouton **« Annuler »** qui restaure les noms d'origine.

#### Panneau « Démarrage automatique »
* Case à cocher « Lancer la surveillance des fichiers XML automatiquement au démarrage de l'application » (`LMU:WatchResultsOnStartup`, cochée par défaut).
* Le choix est **enregistré immédiatement** dans `appsettings.json` (message « Préférence de démarrage enregistrée. ») et s'applique au prochain démarrage. Le bouton « Appliquer » active la surveillance immédiatement, quelle que soit la case.

#### Panneau « Sessions d'essais »
* Case à cocher « Charger les sessions d'essais (onglet Essais du détail des courses) » (`LMU:LoadPracticeSessions`, **décochée par défaut** : les essais sont souvent vides et ralentissent le chargement).
* Le choix est **enregistré immédiatement**, puis les résultats du dossier configuré sont **rechargés** (messages « Sessions d'essais chargées. » / « Sessions d'essais retirées. »).
* Décochée : les fichiers d'essais sont lus mais pas conservés ; l'onglet Essais du détail des courses n'apparaît plus.

* Les messages de retour de la page s'affichent sous les panneaux. Il n'y a plus de bouton « Enregistrer » : chaque réglage est enregistré par son action.

#### État des imports (sous les options, également accessible via `/dashboard`)
* Pendant l'import initial : barre de progression « N / M fichiers traités ».
* Encadré d'erreur si les résultats n'ont pas pu être chargés, avec un lien vers Options.
* Trois cartes :
  * **SESSIONS IMPORTÉES** : nombre de sessions du pilote (essais compris s'ils sont chargés) ;
  * **DERNIÈRE SESSION** : lieu et date de la plus récente ;
  * **FICHIERS EN ÉCHEC** : nombre d'échecs (en couleur d'alerte si > 0).
* Liste des fichiers en échec (nom + message) lorsqu'il y en a.
* Tableau **« Sessions récentes de <pilote> »** trié du plus récent au plus ancien : DATE / HEURE (heure locale), CIRCUIT (lieu + tracé), ÉVÉNEMENT, SESSION (badge du type), VOITURE.

#### Barre de navigation (commune à toutes les pages)
* À droite : nom du pilote sélectionné (ou « Pilote non défini ») et état de l'application :
  * libellé : RÉSULTATS INDISPONIBLES (erreur), sinon IMPORT INITIAL (chargement en cours), sinon SURVEILLANCE ACTIVE ou SURVEILLANCE INACTIVE ;
  * point : 🔴 en cas d'erreur, 🟢 si le watcher tourne (y compris pendant l'import initial), 🟡 sinon.

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS   Driver Tom   🟢 SURVEILLANCE ACTIVE │
├───────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  CONFIGURATION & PARAMÈTRES                                                                                   │
│  Options                                                                                                      │
│  Configurez la source des résultats et la surveillance des fichiers LMU.                                      │
│                                                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ EMPLACEMENT DES FICHIERS DU JEU                                                                         │  │
│  │ Dossier des résultats XML                                                                               │  │
│  │ Chemin du dossier (UserData/Log/Results)                                                                │  │
│  │ [ C:\Users\…\Documents\Le Mans Ultimate\UserData\Log\Results                    ] [ Parcourir… ]       │  │
│  │ 🟢 Dossier valide · 342 fichier(s) XML   C:\Users\…\Results                                             │  │
│  │ [ Appliquer ]  Enregistre le dossier, puis recharge les résultats et la surveillance.                   │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ PROFIL DE PILOTE                                                                                        │  │
│  │ Pilote principal                                                                                        │  │
│  │ Pilote utilisé dans les vues de résultats                                                               │  │
│  │ [ Driver Tom — 81,7 % (170/208 fichiers)                                                          ▼ ]   │  │
│  │ Les pilotes sont triés par pourcentage de fichiers importés… Le choix est enregistré automatiquement.   │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ CHANGEMENT DE NOM                                                                                       │  │
│  │ Fusion de pilotes                                                                                       │  │
│  │ Ancien nom [ Driver Tristan ▼ ]   Nom conservé [ Driver Lucas ▼ ]   [ Fusionner ]                       │  │
│  │ ! Ces deux pilotes ont participé ensemble à 2 session(s) : ce sont probablement deux personnes…         │  │
│  │ Driver Paul → Driver Iris                                                             [ Annuler ]       │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ SURVEILLANCE & DÉMARRAGE                                                                                │  │
│  │ Démarrage automatique                                                                                   │  │
│  │ ☑ Lancer la surveillance des fichiers XML automatiquement au démarrage de l'application                 │  │
│  │ Ce choix est enregistré immédiatement et s'applique au prochain démarrage…                              │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ DONNÉES CHARGÉES                                                                                        │  │
│  │ Sessions d'essais                                                                                      │  │
│  │ ☐ Charger les sessions d'essais (onglet Essais du détail des courses)                                   │  │
│  │ Les essais sont souvent vides ; les ignorer accélère le chargement…                                     │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│  Dossier enregistré et chargé : 342 fichier(s) traité(s). La surveillance est active.                         │
│                                                                                                               │
│  ESPACE INGÉNIEUR DE PISTE — Tableau de bord                                                                  │
│  [ SESSIONS IMPORTÉES 170 ]   [ DERNIÈRE SESSION  Circuit de Spa-Francorchamps ]    [ FICHIERS EN ÉCHEC 0 ]   │
│  ARCHIVES DE COURSE — Sessions récentes de Driver Tom                                                 [ 170 ]   │
│  DATE / HEURE      CIRCUIT                          ÉVÉNEMENT                       SESSION   VOITURE          │
│  02/10/2026 21:00  Circuit de Spa-Francorchamps     6 Hours of Spa-Francorchamps    Race      …                │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
