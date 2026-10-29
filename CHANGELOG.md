# Changelog - LMU-DeltaRacing

Toutes les modifications notables apportées au projet **LMU-DeltaRacing** sont documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

---

## [Unreleased]

### Added
- Mes courses : colonne **CLASSEMENT** (position en classe de la course : `1er`, `2e`…, ou `DNF`, `DQ`, `NC`) et mise en avant des lignes selon le résultat (ligne encadrée et position colorée) : or, argent ou bronze pour le podium, bleu pour les 4e et 5e places, bleu plus léger pour les 6e à 10e. Dans la console, la colonne CLASS. porte la même mise en avant sur la cellule de la position. Les colonnes de la course passent avant celles de la qualification, et la catégorie avant la date : dans chaque circuit, les lignes sont groupées par catégorie (ordre du filtre) puis triées de la plus récente à la plus ancienne.
- Dashboard : écran de chargement au démarrage. Pendant l'import initial, le logo Delta Racing et une roue qui roule sur une route (barre de progression « N / M fichiers traités ») remplacent toute l'application ; le dashboard s'affiche une fois toutes les données chargées, au lieu de se remplir au fil de l'eau. Un rechargement depuis Options garde l'affichage habituel.
- Benchmarks sur les fichiers réels de `assets/Results` : parsing par type de session, import disque, chargement complet du service (avec essais, avec fusions, avec journal JSON), puis calculs de chaque page (Accueil, Mes courses, Classement, Détail des courses, Progression, Options) sur l'historique réel répété ×1, ×10 et ×50. Catégories `Import` et `Views` pour les lancer séparément ; comparaison avant/après dans `docs/benchmarks/audit-performance-2026-10.md`.
- Tests : empreinte SHA-256 de toutes les valeurs lues dans les 342 fixtures (verrou de non-régression du parser), cas limites du parser, entités du domaine, catégories et statuts d'arrivée, ordre de l'import parallèle, robustesse du watcher et du chargement.
- Console Spectre refondue sur les données du dashboard : bandeau d'état (pilote, surveillance, sessions, échecs) et menu **Accueil**, **Mes courses**, **Options**, **Quitter**. L'Accueil et Mes courses reprennent les calculs du dashboard (ventilation par catégorie en colonnes, filtres Circuit et Catégorie) ; les Options permettent de changer le dossier des résultats (rechargement immédiat), le pilote principal, les fusions de pilotes, le démarrage automatique de la surveillance et les réglages du log. Les nouveaux imports sont signalés au retour au menu, sans casser l'affichage des menus.

### Changed
- Code : la nature d'une session (essais, qualification, course) est lue une fois dans le domaine (`Session.Kind`) et le meilleur tour calculé une fois (`Driver.BestLap`) ; le rang en classe, le nom de la voiture et la catégorie (`DriverResults`) sont partagés par toutes les vues, au lieu de 4 à 7 copies légèrement différentes.
- Dashboard : les pages partagent une base (`DashboardPage`, abonnement à l'état du service) et un composant d'attente (`ResultsGate`, chargement ou erreur) ; Détail des courses utilise les formats de `DisplayFormat`. Rendu des pages inchangé.
- Build : `Directory.Build.props` centralise le framework cible ; les projets de `src/` vérifient que tout membre public est documenté et traitent les avertissements comme des erreurs (hors alertes d'audit NuGet).
- Fixtures : Driver Tom remporte 3 courses et 2 poles de plus (5 sessions retouchées en échangeant son identité avec celle du premier de sa classe), pour illustrer les victoires de l'Accueil et la mise en avant dorée de « Mes courses ».
- Accueil : un abandon (DNF) ou une disqualification (DQ) ne compte plus dans les victoires, podiums, top 5 et top 10, même avec une position en classe (LMU classe P1 une voiture seule dans sa catégorie qui abandonne après 0 tour). Ils restent comptés dans le total de courses et leurs propres compteurs, comme dans « Mes courses ».
- Nouveau projet `LMU-DeltaRacing.Presentation` : les calculs des vues (`*Presentation`), le service de données (`DashboardDataService`), l'écriture des réglages (`DashboardSettingsStore`) et la détection du dossier de résultats (`ResultsDirectoryResolver`) quittent le projet UI pour être partagés avec la console. Aucun changement de comportement du dashboard.
- Console : suppression des vues « Par circuit » et « Par circuit et voiture », de leurs graphiques et de `SessionStatistics` (meilleur tour calculé sur tous les pilotes, sans pilote principal). La console ne prend plus de fichiers en argument : elle charge toujours le dossier de son `appsettings.json`, détecté automatiquement comme pour le dashboard (dossiers LMU usuels) s'il est vide. Un dossier illisible ne l'arrête plus. Les réglages du log enregistrés depuis la console ne réécrivent plus toute la section `LMU`.
- Formats d'affichage (`DisplayFormat`) et filtres de « Mes courses » (`MyRacesFilters`) partagés entre le dashboard et la console.
- Release : le workflow manuel demande le type de version (Majeur, Mineur, Patch) au lieu du numéro, incrémente le dernier tag `vX.Y.Z` et liste dans les notes de la release une ligne par PR fusionnée depuis la release précédente.

### Fixed
- Import : un fichier XML bien formé qui n'est pas un résultat LMU (sans `RaceResults`, sans `DateTime`, numéro de tour invalide…) bloquait le dashboard sur « Chargement… » au démarrage et arrêtait la surveillance du dossier. Il est désormais listé dans les échecs d'import et les autres fichiers sont chargés.
- Import : une erreur inattendue pendant l'import d'un fichier n'arrête plus la surveillance ni le chargement initial ; le fichier est compté en échec.
- Console : les statistiques d'exécution (`lmu-deltaracing-stats.json`) ne perdent plus d'imports quand plusieurs fichiers sont importés en même temps.
- Options : dans « Sessions récentes », la course d'un événement est toujours affichée avant sa qualification et ses essais, qui partagent sa date dans les fichiers LMU ; l'ordre variait d'un lancement à l'autre.
- Détail des courses et Options : les dates s'affichent toujours au format `jj/mm/aaaa hh:mm`, quelle que soit la langue de Windows.
- Release : le test de l'application publiée (`Test-PublishedApp.ps1`) attend la fin de l'écran de chargement au démarrage avant de vérifier la page d'accueil ; il échouait depuis l'ajout de cet écran, qui masque la navigation pendant l'import initial.

### Performance
- Parser XML en flux (`XmlReader`) : le journal d'événements de la session (`Stream`), qui fait l'essentiel d'un fichier de course, n'est plus chargé en mémoire. Parsing des 342 fichiers de démonstration : 294 ms → 145 ms, allocations 165 Mo → 32 Mo.
- Chargement initial : les fichiers sont analysés en parallèle (progression et échecs toujours rapportés dans l'ordre des fichiers) et les fusions de pilotes ne sont plus réappliquées après chaque fichier, ce qui rendait le chargement quadratique. Chargement des 342 fichiers : 413 ms → 96 ms ; 1 710 fichiers avec 2 fusions : 4,5 s → 0,5 s.
- Progression : le graphique n'est plus recalculé au survol des points. Options : les sessions communes des fusions sont comptées une fois par changement d'état, et non à chaque affichage. Fréquences des pilotes : 16 Mo → 0,45 Mo alloués sur 10 400 sessions.

---

## [0.0.8] - 2026-10-26

### Added
- Nouvelle page **Progression** (`/progression`), après « Mes courses » : graphique SVG de l'évolution des meilleurs tours du pilote sur un circuit et dans une catégorie, regroupés par jour, semaine ISO, mois ou année (valeur tracée : min, moyenne ou max, avec bande min-max). Courbes Course et Qualification, ou une courbe par voiture ; les périodes sans session sont traversées en trait plein et signalées sur l'axe des dates ; ligne du record, résumé chiffré (record, début, aujourd'hui, écart), panneau de détail au survol, légende cliquable et tableau des périodes. Réglages enregistrés dans `LMU:Progression`.
- Mes courses : icône 📈 dans l'en-tête de chaque circuit, qui ouvre la Progression sur ce circuit et sa dernière catégorie roulée.
- Nouvelle page **Accueil** (`/`), premier onglet : tableau de bord de la carrière du pilote en tuiles (pilote avec ses dates de première et dernière course et ses favoris, forme récente, palmarès, qualification & départ, abandons et leurs causes, roulage), chaque total étant détaillé par catégorie (barre colorée et légende). Les résultats comptent la position en classe officielle ; une course non classée (`None`) compte dans le total mais pas dans les résultats.
- Parser : lecture de la position de départ en classe (`ClassGridPos`), de la cause d'abandon (`DNFReason`) et de la longueur du circuit (`TrackLength`).
- Détail des courses : le paramètre `?event=` ouvre directement un événement (lien « Voir l'événement » de l'Accueil).
- README réécrit pour les utilisateurs : installation, premier lancement, pages, options et questions fréquentes. La documentation technique de l'ancien README est déplacée dans `docs/DEVELOPMENT.md`.
- Options : bloc **Fusion de pilotes** pour regrouper deux noms d'un même pilote (changement de pseudo) sous le nom conservé, en mémoire et sans modifier les fichiers de résultats (`LMU:DriverMerges`). Un avertissement signale les pilotes qui ont participé à une même session ; chaque fusion peut être annulée.
- Ajout d'une page Options pour choisir, valider, enregistrer et recharger le dossier des résultats, avec sélecteur de dossiers Windows natif et préférence de démarrage du watcher.
- Déplacement de l'état d'import, des erreurs et de l'historique du dashboard sous les options, sans tableau de classement des pilotes.
- Ajout du shell LMU avec navigation horizontale supérieure, fond dégradé, surfaces sombres translucides et état du watcher en direct.
- Ajout dans la console Spectre de vues statistiques par circuit et par classe/équipe, avec deltas chronologiques des meilleurs tours valides et graphiques des mesures récentes.
- Ajout au menu de lancement du choix des tableaux statistiques à afficher et exclusion des sessions Practice des statistiques.
- Surveillance dynamique du dossier des résultats, déduplication des chemins pendant l'exécution, notification des imports et échecs, et retries sur les fichiers verrouillés.
- Ajout d'un dashboard Blazor local avec historique des sessions, classement des pilotes, progression du chargement et actualisation en direct des imports.
- Ajout d'un profil de publication Windows x64 autonome pour lancer le dashboard sans installer le runtime .NET.
- Ajout d'un installeur Windows Inno Setup, de fixtures de démonstration anonymisées et d'un test de démarrage de l'application publiée sans DLL externes.
- Ajout de `AGENTS.md`, source unique des instructions pour GitHub Copilot et Claude Code (via l'import `@AGENTS.md` de `CLAUDE.md`) : commandes, architecture, faits vérifiés sur le format XML LMU et règles de contribution.

### Fixed
- Remplacement de `.github/copilot-instruction.md`, ignoré par Copilot à cause de son nom, par `AGENTS.md` ; correction du chemin du projet UI et de l'organisation de la solution.
- Renommage de `CHANGELOG.MD` en `CHANGELOG.md`, nom attendu par les instructions de contribution.
- Correction des références documentaires vers des fichiers inexistants : maquettes `docs/UISpec/*.template.md`, rapport `performance-report.md`, script `Test-PackageAssets.ps1` supprimé, `IResultXmlParser`, `docs/ROADMAP.md` et `LMU-DeltaRacing.slnx`.
- Mes courses : plusieurs courses disputées le même jour sur un même circuit ne sont plus fusionnées en une seule ligne ; chaque course est associée à la qualification qui la précède (sur les fixtures : 89 lignes au lieu de 37).
- Mes courses : les classes LMP3 et GTE (LMGTE) ne sont plus masquées, et une classe inconnue reste affichée sous son nom brut.
- Mes courses : la colonne TOURS affiche les tours du pilote et non ceux du vainqueur.
- Classement des coureurs : seules les courses comptent, et uniquement contre les pilotes de la même classe.
- Détail des courses : les couleurs jaune et violette des secteurs s'affichent réellement (elles étaient écrasées par la couleur du texte).
- Dates de l'état des imports affichées en heure locale au lieu de l'heure UTC.

### Changed
- L'application s'ouvre sur l'Accueil : « Mes courses » ne répond plus que sur `/my-races` et le logo renvoie vers `/`. L'ancienne page `/dashboard` est retirée, l'état des imports restant affiché dans Options.
- Détail des courses : tableau compacté de 14 à 8 colonnes, sans défilement horizontal à partir de 1 280 px — chaque temps porte son écart en dessous, la voiture passe sous le nom du pilote et la catégorie sous la position ; la ligne du pilote sélectionné est surlignée. Le tour idéal affiche aussi son écart au meilleur tour idéal de la catégorie, et la colonne DIFF 1ER devient TEMPS TOTAL (temps de course complet, écart au premier en dessous).
- Détail des courses : le sélecteur propose des **événements** (essais, qualification et course disputés à la suite sur un circuit) et des sous-onglets **Course**, **Qualification** et **Essais** affichent le classement de chaque session ; plusieurs séances d'essais d'un même événement sont fusionnées en un seul classement.
- Options : nouvelle case « Charger les sessions d'essais » (`LMU:LoadPracticeSessions`, décochée par défaut) ; la cocher ou la décocher enregistre le choix et recharge les résultats. Les essais chargés ne s'affichent que dans l'onglet Essais du détail des courses ; la console continue de les ignorer.
- Fixtures : alias lisibles à prénom (`Driver Tom`, `Driver Lucas`…) au lieu de `Driver NNNN`. Le pilote principal devient `Driver Tom`.
- Fixtures : les pilotes n'ayant fait qu'une course sont fusionnés avec des pilotes qu'ils n'ont jamais croisés (`Prepare-PackageAssets.ps1 -MergeSingleRaceDrivers`) ; 5 599 noms donnent 239 alias, dont 237 avec au moins 5 courses, ce qui enrichit le classement des coureurs. Le pilote principal n'est jamais fusionné, et aucune session ne contient deux fois le même alias.
- Détail des courses : meilleurs secteurs personnels sur les tours chronométrés, colonne TOUR IDÉAL, comparaisons par classe (règle WEC), position en classe, colonne DIFF 1ER (écart au leader de la classe, tours de retard, DNF/DQ), temps au tour au format `m:ss.fff`, virtualisation au-delà de 40 pilotes.
- Classement des coureurs : barre visuelle victoires/défaites dans la colonne % de victoire.
- Options : un bouton unique « Appliquer » valide, enregistre et recharge le dossier des résultats ; la préférence de démarrage est enregistrée dès qu'on la coche.
- Le parser lit la position en classe, le nombre de tours et le statut d'arrivée de chaque pilote.
- Specs `docs/UISpec/`, `UI_DESIGN_SPEC.md` et ROADMAP mises à jour selon les règles arbitrées (jalon « Écarts UI à traiter »).
- Import automatique des résultats au démarrage sans demander de confirmation; le choix d'une vue statistique l'affiche immédiatement au lieu de revenir au menu.
- Affichage d'un titre dès le démarrage et remplacement du tableau des imports par une barre de progression et un résumé des résultats.
- Publication du dashboard en exécutable autonome, fichier unique et élagué; l'installeur sélectionne les résultats LMU présents ou les fixtures anonymisées et le workflow de release est déclenché par tag.
- Centralisation du jeu XML anonymisé dans `assets/Results`, partagé par les tests, l'application et la publication, avec suppression des copies brutes sous `tests/testAssets`.

### Performance
- Test de charge de l'import dynamique sur 50 fichiers XML injectés simultanément; résultats consignés dans `docs/benchmarks/dynamic-import-load-report.md`.

### Security
- Anonymisation complète des fixtures `assets/Results` : les noms réels restaient dans les changements de pilote (`Swap`), l'attribut `Driver` des événements (`TrackLimits`, `Penalty`) et les textes du `Stream` (contacts, pénalités, scoring, secteurs, chat). `Prepare-PackageAssets.ps1` remplace désormais tous ces noms par les mêmes alias en **conservant tous les événements de course** ; seul le contenu des messages de chat est retiré (l'alias de l'expéditeur est gardé). Le script vérifie qu'aucun nom réel ne subsiste et peut être rejoué sur des fixtures déjà anonymisées sans changer les alias existants.

### 🏗️ Architecture & Core (Clean Architecture)
- **Domain Layer (`src/LMU-DeltaRacing.Domain/`) :**
  - Création des entités métiers pures : `Session`, `Driver`, `Lap`, et `Sector`.
  - Isolation totale du modèle domaine vis-à-vis des dépendances externes et du format XML.
- **Application Layer (`src/LMU-DeltaRacing.Application/`) :**
  - Définition des contrats d'interfaces `IResultXmlParser` et `ISessionRepository`.
  - Intégration des abstractions de métriques et de traçabilité via OpenTelemetry (`System.Diagnostics.ActivitySource` & `Meter`).
- **Infrastructure Layer (`src/LMU-DeltaRacing.Infrastructure/`) :**
  - Implémentation du parser XML réactif consommant des `Stream` pour l'indépendance du File System.
  - Implémentation du dépôt In-Memory (`InMemorySessionRepository`) thread-safe pour le stockage temporaire des sessions.

### 📊 Observabilité, Logging & Diagnostic
- **Formatage des logs :** Configuration de l'export des journaux sur disque sous forme de fichiers **JSON structurés et indentés (Human-Readable JSON)** dans `lmu-deltaracing.log`.
- **OpenTelemetry :**
  - Envoi des traces et métriques d'exécution vers la sortie Debug.
  - Anonymisation automatique des chemins absolus locaux dans les traces (seul le nom de fichier XML est conservé).
- **Console Debug (`src/LMU-DeltaRacing.Console/`) :**
  - Application console basée sur `Spectre.Console` pour la visualisation immédiate des imports XML.
  - Sélection automatique du dossier de test (`tests/testAssets/Results`) en mode Debug ou via argument CLI.
  - Tableau récapitulatif interactif affichant le statut de l'import, le circuit, la catégorie et le nombre de pilotes.

### 🧪 Quality Assurance & Tests
- **Framework de Test :** Migration et alignement strict sur **MSTest** et **FluentAssertions**.
- **Dataset de Test :** Intégration d'échantillons XML réels du jeu Le Mans Ultimate dans `tests/testAssets/Results/`.
- **Tests d'intégration :** Couverture du parsing des sessions, de l'extraction des meilleurs tours/secteurs et de la persistance en mémoire.

### 📚 Documentation & Guidelines
- **Spécifications UI (`docs/UI_DESIGN_SPEC.md`) :** Création de la charte graphique officielle *Dark Racing WEC/LMU* (palette `#0e1116`, codes couleurs WEC pour les deltas secteur : Violet `#b066ff`, Vert `#22c55e`, Jaune `#eab308`, Rouge `#ef4444`).
- **Instructions Copilot (`.github/copilot-instructions.md`) :**
  - Imposition du workflow Git à base de Pull Requests (interdiction stricte du push direct sur `main`).
  - Règle "Zéro Invention" obligeant l'agent à poser des questions techniques/fonctionnelles en cas d'ambiguïté.
  - Exigence de commentaires XML abondants et de logs granulaires (`Trace` à `Critical`).
  - Maintien systématique du `CHANGELOG.md` par PR.
- **Feuille de Route (`docs/ROADMAP.md`) :** Découpage en micro-jalons atomiques (Configuration `appsettings.json`, File Locking, FileWatcher, Benchmarking, UI Blazor).
- **Architecture (`docs/ARCHITECTURE.md`) :** Mise à jour des documents de cadrage technique.

---

## [0.1.0-alpha] - 2026-10-16

### Added
- Initialisation de la solution .NET 10 `LMU-DeltaRacing.slnx`.
- Structure initiale des projets Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Console`, `UI`, `Tests`).
