# Architecture Technique - LMU-DeltaRacing

## Couches du projet
- `Domain` : entités métier (`Session`, `Driver`, `Lap`, `Sector`) sans dépendance externe.
- `Application` : contrats du parser XML et du dépôt de sessions.
- `Infrastructure` : parser XML LMU et dépôt en mémoire.
- `UI` : interface Blazor prévue pour l'affichage des dashboards.
- `Tests` : tests unitaires xUnit du parser et du dépôt, avec FluentAssertions.

## Flux de données (jalon 1)
1. Le parser lit les métadonnées de session, les résultats des pilotes, leurs tours et secteurs depuis le XML LMU.
2. Le parser retourne une entité `Domain.Session` indépendante du format XML.
3. Le dépôt en mémoire enregistre et restitue les sessions pendant l'exécution du processus.

Le parser consomme un `Stream` via le contrat de la couche `Application`, ce qui le rend indépendant du système de fichiers. Le suivi de répertoire et l'interface utilisateur restent à implémenter dans les jalons suivants.