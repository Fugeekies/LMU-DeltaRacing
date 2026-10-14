# Architecture Technique - LMU-DeltaRacing

## Couches du projet
- `Domain` : Contient les entités métier (Session, Driver, Lap, Sector).
- `Application` : Contient les contrats (Interfaces) et les Use Cases.
- `Infrastructure` : Contient l'implémentation du parser XML LMU et du FileWatcher.
- `UI` : Interface Blazor pour l'affichage des dashboards.
- `Tests` : Tests unitaires et d'intégration (xUnit).

## Flux de données (Milestone 1)
1. Fichier XML LMU -> `Infrastructure.XmlParser` -> Entités `Domain`.
2. Entités `Domain` -> `Infrastructure.InMemoryRepository`.