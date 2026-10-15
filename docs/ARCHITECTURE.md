# Architecture Technique - LMU-DeltaRacing

## Couches du projet
- `Domain` : Entités métier (`Session`, `Driver`, `Lap`, `Sector`) sans dépendance externe.
- `Application` : Contrats du parser XML, du dépôt de sessions, des services d'import, contrats de métriques et traces.
- `Infrastructure` : parser XML LMU et dépôt en mémoire. Le watcher de fichiers est prévu au jalon d'import dynamique, mais n'est pas encore implémenté.
- `Console` : Application console Spectre.Console, composition des dépendances, export OpenTelemetry et suivi des imports.
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
