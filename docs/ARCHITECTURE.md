# Architecture Technique - LMU-DeltaRacing

## Couches du projet
- `Domain` : entités métier (`Session`, `Driver`, `Lap`, `Sector`) sans dépendance externe.
- `Application` : contrats du parser XML, du dépôt de sessions et du service d'import, avec traces et métriques OpenTelemetry.
- `Infrastructure` : parser XML LMU et dépôt en mémoire.
- `Console` : application console Spectre.Console, composition des dépendances, export OpenTelemetry et tableau des imports.
- `UI` : interface Blazor prévue pour l'affichage des dashboards.
- `Tests` : tests unitaires xUnit du parser et du dépôt, avec FluentAssertions.

## Flux de données (jalon 1)
1. Le parser lit les métadonnées de session, les résultats des pilotes, leurs tours et secteurs depuis le XML LMU.
2. Le parser retourne une entité `Domain.Session` indépendante du format XML.
3. Le dépôt en mémoire enregistre et restitue les sessions pendant l'exécution du processus.

Le parser consomme un `Stream` via le contrat de la couche `Application`, ce qui le rend indépendant du système de fichiers. Le suivi de répertoire et l'interface utilisateur restent à implémenter dans les jalons suivants.

## Monitoring et console

Le projet `LMU-DeltaRacing.Console` accepte des chemins XML en arguments ou, sans argument, demande le dossier des résultats en configuration Release. En configuration Debug, il sélectionne automatiquement `tests/testAssets/Results`. Il importe les fichiers XML du dossier sans surveillance continue, puis affiche un tableau avec l'état, le circuit, le type de session et le nombre de pilotes. Les erreurs de lecture ou de parsing restent visibles dans le tableau et dans les logs.

OpenTelemetry écrit les logs structurés dans `lmu-deltaracing.log`, à côté de l'exécutable. Le fichier est recréé à chaque lancement pour ne garder que l'exécution courante. Les traces et métriques sont envoyées à la sortie Debug plutôt qu'au terminal. L'instrumentation runtime publie également des métriques du processus. Seul le nom du fichier, et non son chemin local complet, est ajouté aux traces.

Exemple : `dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj -- "chemin\vers\resultat.xml"`.