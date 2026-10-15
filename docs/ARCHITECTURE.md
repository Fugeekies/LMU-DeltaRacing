# Architecture Technique - LMU-DeltaRacing

## Couches du projet
- `Domain` : Entités métier (`Session`, `Driver`, `Lap`, `Sector`) sans dépendance externe.
- `Application` : Contrats du parser XML, du dépôt de sessions, des services d'import, contrats de métriques et traces.
- `Infrastructure` : Parser XML LMU, service de surveillance de fichiers (Watcher) et dépôt en mémoire.
- `Console` : Application console Spectre.Console, composition des dépendances, export OpenTelemetry et suivi des imports.
- `UI` : Interface Blazor prévue pour l'affichage des dashboards.
- `Tests` : Tests unitaires/d'intégration MSTest + FluentAssertions, et benchmarks de charge.

## Flux de données
1. Le parser consomme un `Stream` XML LMU via la couche `Application` pour garantir l'indépendance du système de fichiers.
2. Le parser transforme le XML en entité `Domain.Session`.
3. Le dépôt en mémoire enregistre et restitue les sessions de manière thread-safe.

## Monitoring, Logging & OpenTelemetry

L'application utilise **OpenTelemetry** couplé au système de logging standard de .NET :
- **Format des logs :** Les journaux écrits sur disque sont sérialisés en **JSON structuré avec indentation (Human-Readable JSON)**.
- **Traces & Métriques :** Exportées via OpenTelemetry vers la sortie Debug et instrumentées au niveau du runtime .NET.
- **Masquage des données sensibles :** Seul le nom du fichier XML (et non son chemin local absolu) est consigné dans les traces.
- Les logs structurés dans `lmu-deltaracing.log`, à côté de l'exécutable. Le fichier est recréé à chaque lancement pour ne garder que l'exécution courante. Les traces et métriques sont envoyées à la sortie Debug plutôt qu'au terminal. L'instrumentation runtime publie également des métriques du processus. Seul le nom du fichier, et non son chemin local complet, est ajouté aux traces.

## Stratégie de Performance & Tests de Charge

Afin de garantir la fluidité sur de gros volumes d'historiques (ex: des centaines de fichiers XML de course) :
- Des tests de charge automatisés (via BenchmarkDotNet ou suite MSTest dédiée) valident le comportement de l'application sous pression.
- Un rapport de performance (`PERFORMANCE_REPORT.md`) est systématiquement généré et joint à chaque Pull Request modifiant le moteur d'import ou le parser.
