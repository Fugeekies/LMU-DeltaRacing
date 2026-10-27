# Architecture Technique - LMU-DeltaRacing

## Couches du projet
- `Domain` : Entités métier (`Session`, `Driver`, `Lap`, `Sector`) sans dépendance externe.
- `Application` : Contrats du parser XML, du dépôt de sessions, des services d'import, contrats de métriques et traces.
- `Infrastructure` : parser XML LMU, dépôt en mémoire et watcher du dossier de résultats avec traitement séquentiel, retries sur les fichiers verrouillés et déduplication par chemin.
- `Console` : application console Spectre.Console, composition des dépendances, configuration rechargée à chaud, export OpenTelemetry, progression des imports et statistiques de meilleurs tours groupées par circuit puis par classe/équipe.
- `Presentation` : code partagé par l'UI et la console : calculs des vues (`*Presentation`), service de données (`DashboardDataService` : import initial, rechargement, watcher, fusions de pilotes, détection du pilote principal), écriture des réglages (`DashboardSettingsStore`) et détection du dossier de résultats (`ResultsDirectoryResolver`).
- `UI` : Dashboard Blazor interactif local : composants Razor alimentés par `Presentation`, sélecteur de dossier Windows et géométrie du graphique SVG de Progression.
- `Tests` : tests unitaires et d'intégration MSTest + FluentAssertions ; les benchmarks de charge sont prévus au jalon Quality Gates.

## Flux de données
1. Le parser consomme un `Stream` XML LMU via la couche `Application` pour garantir l'indépendance du système de fichiers.
2. Le parser transforme le XML en entité `Domain.Session`.
3. Le dépôt en mémoire enregistre et restitue les sessions de manière thread-safe.

## Monitoring, Logging & OpenTelemetry

L'application utilise **OpenTelemetry** couplé au système de logging standard de .NET :
- **Format des logs :** Les journaux écrits dans `lmu-deltaracing.log` utilisent JSON Lines : un objet JSON structuré par ligne, sans tableau JSON englobant.
- **Fichier courant :** Les logs sont écrits dans `lmu-deltaracing.log`, à côté de l'exécutable. À l'arrêt, le fichier est fermé mais conservé pour consultation. Il est écrasé au démarrage suivant afin de ne conserver que l'exécution courante.
- **Statistiques :** `lmu-deltaracing-stats.json` contient le résumé OpenTelemetry de la dernière exécution : durée moyenne d'import, débit en fichiers par seconde, mémoire moyenne du processus (working set et heap .NET), nombre de fichiers et résultats d'import. Le fichier est fermé et conservé à l'arrêt, puis remplacé au prochain démarrage.
- **Traces et métriques :** Elles sont exportées vers la sortie Debug, et non vers le terminal. L'instrumentation runtime publie également des métriques du processus.
- **Masquage des données sensibles :** Seul le nom du fichier XML, et non son chemin local absolu, est consigné dans les traces.

## Stratégie de Performance & Tests de Charge

Pour garantir la fluidité sur de gros volumes d'historiques (par exemple des centaines de fichiers XML de course), le jalon Quality Gates prévoit :
- Des tests de charge automatisés (via BenchmarkDotNet ou une suite MSTest dédiée) pour valider le comportement de l'application sous pression.
- Un rapport de performance (`benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md`) joint aux Pull Requests qui modifient le moteur d'import ou le parser.

## Configuration

La console charge `appsettings.json` depuis le dossier de l'exécutable et lie la section `LMU` à `LmuApplicationOptions` via `IOptionsMonitor<T>`. Les valeurs modifiées dans le menu Spectre sont validées puis sauvegardées atomiquement dans ce fichier ; le changement est rechargé sans redémarrer l'application.

Sans argument CLI, l'application affiche son titre au démarrage puis importe automatiquement les XML configurés avec une barre de progression. À la fin, elle affiche un résumé du nombre de réussites, de sessions Practice ignorées et d'échecs (avec le détail des erreurs), puis propose les vues statistiques Spectre.Console : par lieu/tracé, par lieu/tracé/classe/équipe, ou les deux. La surveillance continue en arrière-plan jusqu'au choix `Quitter`; les vues peuvent être redemandées pour intégrer les nouveaux imports. La configuration du dossier modifiée dans ce menu s'applique au prochain démarrage. Les fichiers Practice sont ignorés par le service d'import commun et ne sont donc chargés dans aucun menu ni stockés dans le dépôt. Le meilleur tour valide de chaque autre session est comparé chronologiquement à la dernière mesure valide du même groupe; le delta est le temps courant moins le temps précédent. Les graphiques représentent le meilleur tour valide le plus récent pour chaque groupe affiché.

Après le chargement initial, le processus interactif surveille le même dossier avec `FileSystemWatcher` jusqu'au choix `Quitter`. Le service d'import ouvre exclusivement chaque fichier et effectue jusqu'à six tentatives espacées (délai maximal de 1 seconde entre tentatives) lorsqu'il est verrouillé. Il importe les fichiers XML nouveaux ou renommés et maintient un registre des chemins déjà vus en mémoire afin de neutraliser les notifications répétées. Le registre est réinitialisé à l'arrêt; au lancement suivant, le dossier est chargé à nouveau dans le dépôt en mémoire. Les échecs d'import sont signalés dans la console et ne stoppent pas la surveillance. Les statistiques sont recalculées à la demande depuis les sessions alors présentes en mémoire. Les fichiers déjà présents au lancement sont marqués comme traités lors du démarrage du watcher pour éviter un second import.

Par défaut, `ResultsDirectory` est vide et résout le dossier d'assets anonymisés `assets/Results`, `LogFilePath` est `lmu-deltaracing.log` relatif au dossier de l'exécutable et `FileMinimumLogLevel` est `Debug`. Un chemin de résultats ou de log relatif est résolu à partir du dossier de l'exécutable. Les chemins configurés et le niveau de log sont validés avant le démarrage. Aucun log n'est affiché dans le terminal ; le seuil s'applique au fichier JSON Lines.

## Benchmarks

L'application Blazor compose les mêmes services de parsing, dépôt et import que la console, à une différence près : elle peut importer les sessions d'essais (option `LMU:LoadPracticeSessions`, décochée par défaut), que la page Détail des courses affiche dans un onglet dédié. Cette page regroupe les sessions en événements (essais, qualification, course) via `RaceEventPresentation`. Dans la page Options, le bouton « Appliquer » vérifie un dossier, enregistre `ResultsDirectory` dans `appsettings.json`, puis recharge les résultats et le watcher sans redémarrer ; la préférence `WatchResultsOnStartup` est enregistrée dès qu'elle est modifiée. Le sélecteur de dossier Windows natif fournit un chemin exploitable par le watcher. Les fusions de pilotes déclarées dans Options sont appliquées en mémoire aux sessions chargées, sans modifier les fichiers. Les erreurs d'import, la progression et les sessions récentes sont présentées sous le formulaire Options, sans tableau de classement des pilotes. La page d'accueil (`/`) calcule les statistiques de carrière du pilote via `HomePresentation`, sur la position en classe officielle ; le parser lit pour elle `ClassGridPos`, `DNFReason` et `TrackLength`. La page Progression (`/progression`) regroupe les meilleurs tours du pilote par période locale via `ProgressionPresentation`, puis `ProgressionChartLayout` calcule la géométrie du graphique SVG (échelles, graduations, tracés) sans bibliothèque JavaScript ; ses réglages sont enregistrés dans `LMU:Progression`. Lors d'un rechargement, le dépôt en mémoire est vidé avant l'import du nouveau dossier pour éviter de mélanger les sessions de deux sources. L'application écoute sur une adresse loopback avec un port dynamique et ouvre le navigateur par défaut. Les logs restent en JSON Lines dans `lmu-deltaracing.log`; le profil de publication `win-x64` est autonome et ne nécessite pas d'installation du runtime .NET.

Le projet MSTest dédié `benchmarks/LMU-DeltaRacing.Benchmarks` mesure le parser sur 109 500 sessions XML en mémoire par défaut (10 courses/jour × 3 phases × 365 jours × 10 ans). Chaque session analysée est libérée avant la suivante pour mesurer le coût du parsing sans faire croître artificiellement le dépôt en mémoire. Les entrées de charge disque (10, 100 et 500 fichiers) sont créées dans le répertoire temporaire du système et supprimées à la fin.

Lancer `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release --logger "console;verbosity=detailed"` pour générer le rapport Markdown, qui indique les durées, débits, allocations cumulées et mémoire observée. Le fichier est écrit dans `benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md` et son contenu est affiché dans la sortie détaillée du test. Si l'environnement est limité en mémoire, utiliser `-p:BenchmarkYears=5` ou `-p:BenchmarkYears=2`.

## Packaging Windows

Le profil `win-x64` publie l'UI en mode autonome, fichier unique et avec trimming partiel. L'unique jeu de résultats de démonstration anonymisés du dépôt est versionné dans `assets/Results` et sert aussi aux tests. `scripts/Prepare-PackageAssets.ps1` permet de régénérer ces fichiers depuis un dossier de résultats fourni par l'utilisateur; le workflow de release copie les XML versionnés vers la publication, sous `TestAssets/Results`, et n'anonymise ni ne transforme les données. Les noms des pilotes sont remplacés par des alias à prénom (`Driver Tom`, `Driver Lucas`…) cohérents sur l'ensemble du lot, y compris dans les événements du `Stream`, sans altérer les résultats LMU de l'utilisateur. Avec `-MergeSingleRaceDrivers`, les pilotes n'ayant fait qu'une course partagent l'alias d'un pilote qu'ils n'ont jamais croisé, pour des historiques de démonstration plus riches. `scripts/Test-PublishedApp.ps1` vérifie l'absence de DLL externes, démarre l'exécutable et valide une réponse HTTP locale.

`scripts/installer.iss` génère l'installeur Inno Setup. Lors d'une première installation, il cherche des XML dans les dossiers LMU courants (Documents et installation Steam) et configure `LMU:ResultsDirectory` vers le premier dossier trouvé; sinon, il utilise les fixtures anonymisées installées avec l'application. Les mises à niveau préservent le `appsettings.json` existant. Le workflow `.github/workflows/release.yml` s'exécute sur les tags `v*` et attache l'installeur à la release correspondante.
