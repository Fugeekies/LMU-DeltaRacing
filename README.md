# LMU-DeltaRacing

Application d'analyse de résultats de course pour Le Mans Ultimate.

La [feuille de route](docs/ROADMAP.md) présente les jalons envisagés et une liste d'idées à prioriser.

## État actuel

Le socle métier et le jalon de monitoring sont en place : le parser transforme les fichiers XML LMU en entités métier (`Session`, `Driver`, `Lap`, `Sector`), le dépôt conserve les sessions en mémoire, et la console Spectre affiche les résultats d'import avec OpenTelemetry. Les tests MSTest s'appuient sur des fichiers XML réels de `tests/testAssets/Results`.

## Projets

- `src/LMU-DeltaRacing.Domain` : entités métier sans dépendance externe.
- `src/LMU-DeltaRacing.Application` : contrats du parser et du dépôt.
- `src/LMU-DeltaRacing.Infrastructure` : parser XML et dépôt en mémoire.
- `src/LMU-DeltaRacing.Console` : import XML ponctuel, tableau Spectre.Console et diagnostic OpenTelemetry.
- `tests/LMU-DeltaRacing.Tests` : tests unitaires MSTest et FluentAssertions du parsing, de l'import et de la console.

## Commandes

- `dotnet build`
- `dotnet test`
- `dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj -- "chemin\vers\resultat.xml"`

Sans argument, la console demande le chemin du dossier de résultats en configuration Release. En configuration Debug, elle utilise automatiquement `tests/testAssets/Results`. Les chemins de fichiers XML passés en arguments sont importés directement.

Les logs OpenTelemetry sont écrits dans `lmu-deltaracing.log`, à côté de l'exécutable, sous forme d'un tableau JSON indenté contenant des enregistrements structurés. Ce fichier unique est recréé à chaque lancement ; les logs de lancements précédents ne sont pas conservés. Les traces et métriques ne polluent pas le terminal.