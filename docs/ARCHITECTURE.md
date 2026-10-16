# Architecture Technique - LMU-DeltaRacing

## Couches du projet
- `Domain` : Entités métier (`Session`, `Driver`, `Lap`, `Sector`) sans dépendance externe.
- `Application` : Contrats du parser XML, du dépôt de sessions, des services d'import, contrats de métriques et traces.
- `Infrastructure` : parser XML LMU, dépôt en mémoire et watcher du dossier de résultats avec traitement séquentiel, retries sur les fichiers verrouillés et déduplication par chemin.
- `Console` : application console Spectre.Console, composition des dépendances, configuration rechargée à chaud, export OpenTelemetry, progression des imports et statistiques de meilleurs tours groupées par circuit puis par classe/équipe.
- `UI` : Interface Blazor prévue pour l'affichage des dashboards.
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
- Un rapport de performance (`PERFORMANCE_REPORT.md`) joint aux Pull Requests qui modifient le moteur d'import ou le parser.

## Configuration

La console charge `appsettings.json` depuis le dossier de l'exécutable et lie la section `LMU` à `LmuApplicationOptions` via `IOptionsMonitor<T>`. Les valeurs modifiées dans le menu Spectre sont validées puis sauvegardées atomiquement dans ce fichier ; le changement est rechargé sans redémarrer l'application.

Sans argument CLI, l'application affiche son titre au démarrage puis importe automatiquement les XML configurés avec une barre de progression. À la fin, elle affiche un résumé du nombre de réussites et d'échecs (avec le détail des erreurs), puis propose les vues statistiques Spectre.Console : par lieu/tracé, par lieu/tracé/classe/équipe, ou les deux. La surveillance continue en arrière-plan jusqu'au choix `Quitter`; les vues peuvent être redemandées pour intégrer les nouveaux imports. La configuration du dossier modifiée dans ce menu s'applique au prochain démarrage. Les sessions Practice sont exclues des statistiques, sans être retirées des imports. Le meilleur tour valide de chaque autre session est comparé chronologiquement à la dernière mesure valide du même groupe; le delta est le temps courant moins le temps précédent. Les graphiques représentent le meilleur tour valide le plus récent pour chaque groupe affiché.

Après le chargement initial, le processus interactif surveille le même dossier avec `FileSystemWatcher` jusqu'au choix `Quitter`. Le service d'import ouvre exclusivement chaque fichier et effectue jusqu'à six tentatives espacées (délai maximal de 1 seconde entre tentatives) lorsqu'il est verrouillé. Il importe les fichiers XML nouveaux ou renommés et maintient un registre des chemins déjà vus en mémoire afin de neutraliser les notifications répétées. Le registre est réinitialisé à l'arrêt; au lancement suivant, le dossier est chargé à nouveau dans le dépôt en mémoire. Les échecs d'import sont signalés dans la console et ne stoppent pas la surveillance. Les statistiques sont recalculées à la demande depuis les sessions alors présentes en mémoire. Les fichiers déjà présents au lancement sont marqués comme traités lors du démarrage du watcher pour éviter un second import.

Par défaut, `ResultsDirectory` est vide et résout le dossier de fixtures `tests/testAssets/Results`, `LogFilePath` est `lmu-deltaracing.log` relatif au dossier de l'exécutable et `FileMinimumLogLevel` est `Debug`. Un chemin de résultats ou de log relatif est résolu à partir du dossier de l'exécutable. Les chemins configurés et le niveau de log sont validés avant le démarrage. Aucun log n'est affiché dans le terminal ; le seuil s'applique au fichier JSON Lines.

## Benchmarks

Le projet MSTest dédié `benchmarks/LMU-DeltaRacing.Benchmarks` mesure le parser sur 109 500 sessions XML en mémoire par défaut (10 courses/jour × 3 phases × 365 jours × 10 ans). Chaque session analysée est libérée avant la suivante pour mesurer le coût du parsing sans faire croître artificiellement le dépôt en mémoire. Les entrées de charge disque (10, 100 et 500 fichiers) sont créées dans le répertoire temporaire du système et supprimées à la fin.

Lancer `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release --logger "console;verbosity=detailed"` pour générer le rapport Markdown, qui indique les durées, débits, allocations cumulées et mémoire observée. Le fichier est écrit dans `benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md` et son contenu est affiché dans la sortie détaillée du test. Si l'environnement est limité en mémoire, utiliser `-p:BenchmarkYears=5` ou `-p:BenchmarkYears=2`.
