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
- `src/LMU-DeltaRacing.Console/appsettings.json` : valeurs initiales pour la configuration de l'application.
- `tests/LMU-DeltaRacing.Tests` : tests unitaires MSTest et FluentAssertions du parsing, de l'import et de la console.

## Commandes

- `dotnet build`
- `dotnet test`
- `dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj -- "chemin\vers\resultat.xml"`

Sans argument, la console propose d'importer les résultats, de modifier la configuration ou de quitter. Les chemins de fichiers XML passés en arguments sont importés directement.

Les logs OpenTelemetry sont écrits dans `lmu-deltaracing.log`, à côté de l'exécutable, sous forme de JSON Lines : chaque ligne est un objet JSON structuré. Le fichier reste disponible après la fermeture de l'application et est écrasé au démarrage suivant.

Les statistiques agrégées de la dernière exécution sont écrites dans `lmu-deltaracing-stats.json`, au même endroit. Le fichier indique les imports réussis/échoués, la durée moyenne par fichier, le débit moyen d'import, ainsi que les moyennes du working set et du heap .NET. Il est lui aussi conservé après l'arrêt et réinitialisé au prochain lancement. Les traces et métriques OpenTelemetry ne polluent pas le terminal.

Les réglages sont enregistrés dans `appsettings.json` à côté de l'exécutable et rechargés à chaud. Par défaut, le dossier des XML est découvert dans `tests/testAssets/Results`, le log se nomme `lmu-deltaracing.log` à côté de l'exécutable et son niveau minimal est `Debug`. Le terminal n'affiche pas les logs ; le niveau configuré ne concerne que le fichier.