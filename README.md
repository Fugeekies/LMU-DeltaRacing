# LMU-DeltaRacing

Application d'analyse de résultats de course pour Le Mans Ultimate.

La [feuille de route](docs/ROADMAP.md) présente les jalons envisagés et une liste d'idées à prioriser.

## État actuel

Le socle du premier jalon est en place : le parser XML transforme un fichier de résultats LMU en entités métier (`Session`, `Driver`, `Lap`, `Sector`), et un dépôt en mémoire stocke les sessions. Les tests utilisent un fichier XML réel de `tests/testAssets/Results`.

## Projets

- `src/LMU-DeltaRacing.Domain` : entités métier sans dépendance externe.
- `src/LMU-DeltaRacing.Application` : contrats du parser et du dépôt.
- `src/LMU-DeltaRacing.Infrastructure` : parser XML et dépôt en mémoire.
- `src/LMU-DeltaRacing.Console` : import XML ponctuel, tableau Spectre.Console et diagnostic OpenTelemetry.
- `tests/LMU-DeltaRacing.Tests` : tests unitaires du parsing, de l'import et de la console.

## Commandes

- `dotnet build`
- `dotnet test`
- `dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj -- "chemin\vers\resultat.xml"`

Sans argument, la console demande le chemin du dossier de résultats en configuration Release. En configuration Debug, elle utilise automatiquement `tests/testAssets/Results`. Les chemins de fichiers XML passés en arguments sont importés directement.