# LMU-DeltaRacing

Application d'analyse de résultats de course pour Le Mans Ultimate.

La [feuille de route](docs/ROADMAP.md) présente les jalons envisagés et une liste d'idées à prioriser.

## État actuel

Le socle métier, le monitoring et les statistiques de la console sont en place : le parser transforme les fichiers XML LMU en entités métier (`Session`, `Driver`, `Lap`, `Sector`), le dépôt conserve les sessions en mémoire, et la console Spectre affiche les résultats d'import ainsi que deux vues de statistiques par circuit et par voiture. Les vues comparent les meilleurs tours valides dans l'ordre chronologique et affichent leur delta avec la session précédente du même circuit; des graphiques présentent les mesures les plus récentes. Les tests MSTest s'appuient sur des fichiers XML réels de `tests/testAssets/Results`.

## Projets

- `src/LMU-DeltaRacing.Domain` : entités métier sans dépendance externe.
- `src/LMU-DeltaRacing.Application` : contrats du parser et du dépôt.
- `src/LMU-DeltaRacing.Infrastructure` : parser XML et dépôt en mémoire.
- `src/LMU-DeltaRacing.Console` : import XML ponctuel, tableaux et graphiques Spectre.Console des statistiques par circuit/voiture, et diagnostic OpenTelemetry.
- `src/LMU-DeltaRacing.Console/appsettings.json` : valeurs initiales pour la configuration de l'application.
- `tests/LMU-DeltaRacing.Tests` : tests unitaires MSTest et FluentAssertions du parsing, de l'import et de la console.
- `benchmarks/LMU-DeltaRacing.Benchmarks` : projet MSTest dédié aux benchmarks du parsing en mémoire et de lots XML temporaires.

## Commandes

- `dotnet build`
- `dotnet test`
- `dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj -- "chemin\vers\resultat.xml"`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release --logger "console;verbosity=detailed"`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release -p:BenchmarkYears=5 --logger "console;verbosity=detailed"`

Sans argument, la console propose d'importer les résultats, de modifier la configuration ou de quitter. Les chemins de fichiers XML passés en arguments sont importés directement.

Sans argument, la console importe automatiquement les XML du dossier configuré au démarrage, puis affiche le récapitulatif des imports et propose directement les vues statistiques : par circuit, par circuit et voiture (classe + équipe), ou les deux. La sélection affiche la vue immédiatement au lieu de reboucler dans le menu. La modification du dossier de résultats depuis le menu s'applique au prochain démarrage. Les sessions Practice restent importées et visibles dans le récapitulatif des imports, mais sont exclues des statistiques. Un delta négatif indique une amélioration par rapport à la session mesurée précédente sur le même lieu et tracé; un delta positif indique un temps plus lent. Les sessions sans tour valide restent visibles mais ne remplacent pas la dernière mesure exploitable.

Les logs OpenTelemetry sont écrits dans `lmu-deltaracing.log`, à côté de l'exécutable, sous forme de JSON Lines : chaque ligne est un objet JSON structuré. Le fichier reste disponible après la fermeture de l'application et est écrasé au démarrage suivant.

Les statistiques agrégées de la dernière exécution sont écrites dans `lmu-deltaracing-stats.json`, au même endroit. Le fichier indique les imports réussis/échoués, la durée moyenne par fichier, le débit moyen d'import, ainsi que les moyennes du working set et du heap .NET. Il est lui aussi conservé après l'arrêt et réinitialisé au prochain lancement. Les traces et métriques OpenTelemetry ne polluent pas le terminal.

Les réglages sont enregistrés dans `appsettings.json` à côté de l'exécutable et rechargés à chaud. Par défaut, le dossier des XML est découvert dans `tests/testAssets/Results`, le log se nomme `lmu-deltaracing.log` à côté de l'exécutable et son niveau minimal est `Debug`. Le terminal n'affiche pas les logs ; le niveau configuré ne concerne que le fichier.

Le projet de test génère les entrées XML et les analyse depuis des streams en mémoire, sans conserver les sessions analysées. Par défaut, il simule 10 courses par jour pendant 10 ans, avec Practice, Qualifying et Race (109 500 analyses). La charge peut être réduite à 5 ou 2 ans avec `-p:BenchmarkYears=5` ou `-p:BenchmarkYears=2`. Il génère aussi temporairement des lots de 10, 100 et 500 fichiers sur disque puis les supprime. Le rapport Markdown contient la durée, le débit, les allocations et la mémoire observée ; il est enregistré dans `benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md` et son contenu est affiché dans la sortie détaillée du test.

Le [dernier rapport de benchmark](benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md) est disponible dans le dépôt. Ses mesures reflètent l'environnement et la charge de cette exécution.