# LMU-DeltaRacing

Application d'analyse de résultats de course pour Le Mans Ultimate.

La [feuille de route](docs/ROADMAP.md) présente les jalons envisagés et une liste d'idées à prioriser.

## État actuel

Le socle métier, le monitoring et les statistiques de la console sont en place : le parser transforme les fichiers XML LMU en entités métier (`Session`, `Driver`, `Lap`, `Sector`), le dépôt conserve les sessions en mémoire, et la console Spectre affiche une progression pendant l'import ainsi que deux vues de statistiques par circuit et par voiture. Les vues comparent les meilleurs tours valides dans l'ordre chronologique et affichent leur delta avec la session précédente du même circuit; des graphiques présentent les mesures les plus récentes. Les tests MSTest s'appuient sur des fichiers XML réels de `tests/testAssets/Results`.

## Projets

- `src/LMU-DeltaRacing.Domain` : entités métier sans dépendance externe.
- `src/LMU-DeltaRacing.Application` : contrats du parser et du dépôt.
- `src/LMU-DeltaRacing.Infrastructure` : parser XML, dépôt en mémoire et surveillance dynamique du dossier de résultats.
- `src/LMU-DeltaRacing.Console` : import initial et surveillance dynamique des XML, tableaux et graphiques Spectre.Console des statistiques par circuit/voiture, et diagnostic OpenTelemetry.
- `src/LMU-DeltaRacing.UI` : dashboard Blazor local avec historique des sessions, classements et surveillance dynamique des nouveaux résultats.
- `src/LMU-DeltaRacing.Console/appsettings.json` : valeurs initiales pour la configuration de l'application.
- `tests/LMU-DeltaRacing.Tests` : tests unitaires MSTest et FluentAssertions du parsing, de l'import et de la console.
- `docs/benchmarks/dynamic-import-load-report.md` : résultats du test de charge de la surveillance sur 50 fichiers XML simultanés.
- `benchmarks/LMU-DeltaRacing.Benchmarks` : projet MSTest dédié aux benchmarks du parsing en mémoire et de lots XML temporaires.

## Commandes

- `dotnet build`
- `dotnet test`
- `dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj -- "chemin\vers\resultat.xml"`
- `dotnet run --project src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj`
- `dotnet publish src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj -c Release -p:PublishProfile=win-x64`
- `pwsh -File scripts/Prepare-PackageAssets.ps1 -SourceDirectory tests/testAssets/Results -OutputDirectory packaging/TestAssets/Results`
- `pwsh -File scripts/Test-PackageAssets.ps1`
- `Copy-Item packaging/TestAssets/Results -Destination publish_output/TestAssets -Recurse`
- `pwsh -File scripts/Test-PublishedApp.ps1 -PublishDirectory publish_output`
- `iscc /DAppVersion=1.0.0 scripts/installer.iss`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release --logger "console;verbosity=detailed"`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release -p:BenchmarkYears=5 --logger "console;verbosity=detailed"`

Sans argument, la console charge le dossier configuré et reste en surveillance jusqu'au choix `Quitter`. Les chemins de fichiers XML passés en arguments sont importés en mode ponctuel, sans démarrer le watcher.

Au démarrage, la console affiche son titre puis importe automatiquement les XML du dossier configuré avec une barre de progression et un résumé du chargement (les échecs sont détaillés). Elle surveille ensuite ce dossier jusqu'au choix `Quitter` dans le menu. Les nouveaux XML sont importés automatiquement; les fichiers verrouillés sont réessayés, les doublons ignorés, et les erreurs affichées sans interrompre la surveillance. Le registre des chemins traités est en mémoire et repart de zéro au prochain démarrage, où les fichiers du dossier sont chargés à nouveau. Les vues statistiques peuvent être réaffichées depuis le menu et incluent les sessions importées depuis le démarrage. La modification du dossier de résultats depuis le menu s'applique au prochain démarrage. Les sessions Practice restent importées mais sont exclues des statistiques. Un delta négatif indique une amélioration par rapport à la session mesurée précédente sur le même lieu et tracé; un delta positif indique un temps plus lent. Les sessions sans tour valide restent visibles mais ne remplacent pas la dernière mesure exploitable.

Les logs OpenTelemetry sont écrits dans `lmu-deltaracing.log`, à côté de l'exécutable, sous forme de JSON Lines : chaque ligne est un objet JSON structuré. Le fichier reste disponible après la fermeture de l'application et est écrasé au démarrage suivant.

Les statistiques agrégées de la dernière exécution sont écrites dans `lmu-deltaracing-stats.json`, au même endroit. Le fichier indique les imports réussis/échoués, la durée moyenne par fichier, le débit moyen d'import, ainsi que les moyennes du working set et du heap .NET. Il est lui aussi conservé après l'arrêt et réinitialisé au prochain lancement. Les traces et métriques OpenTelemetry ne polluent pas le terminal.

Les réglages sont enregistrés dans `appsettings.json` à côté de l'exécutable et rechargés à chaud. Par défaut, le dossier des XML est découvert dans `tests/testAssets/Results`, le log se nomme `lmu-deltaracing.log` à côté de l'exécutable et son niveau minimal est `Debug`. Le terminal n'affiche pas les logs ; le niveau configuré ne concerne que le fichier.

Le dashboard Blazor charge les XML au démarrage, affiche les sessions et leur classement, puis actualise les données à l'arrivée de nouveaux fichiers. Il s'ouvre dans le navigateur par défaut et n'écoute que sur l'interface locale (`127.0.0.1`) avec un port attribué automatiquement. En développement, le dossier des fixtures est découvert par défaut; pour une utilisation autonome, renseignez `LMU:ResultsDirectory` dans `src/LMU-DeltaRacing.UI/appsettings.json` ou dans celui publié à côté de l'exécutable.

Le profil `win-x64` produit une publication autonome, élaguée et à fichier unique. Les fixtures de démonstration anonymisées sont versionnées dans `packaging/TestAssets/Results`; le workflow les valide puis les copie dans la publication sans les régénérer. Pour renouveler ce jeu de données, exécutez `Prepare-PackageAssets.ps1` vers ce dossier, vérifiez-le avec `Test-PackageAssets.ps1` et incluez les fichiers XML mis à jour dans la modification. Pour construire et vérifier manuellement l'installeur, publiez d'abord l'UI avec ce profil dans `publish_output`, copiez les fixtures versionnées vers `publish_output/TestAssets`, exécutez `Test-PublishedApp.ps1`, puis compilez `scripts/installer.iss` avec Inno Setup. Les fixtures utilisent des alias de pilotes stables entre fichiers; les noms des résultats LMU de l'utilisateur ne sont pas modifiés. L'installeur choisit un dossier de résultats LMU détecté ou, en l'absence de résultats, les fixtures anonymisées installées avec l'application. La configuration existante est conservée lors des mises à niveau.

Le workflow `.github/workflows/release.yml` construit et vérifie l'application puis publie `LMU-DeltaRacing-Setup.exe` dans une release GitHub pour chaque tag `v*`.

Le projet de test génère les entrées XML et les analyse depuis des streams en mémoire, sans conserver les sessions analysées. Par défaut, il simule 10 courses par jour pendant 10 ans, avec Practice, Qualifying et Race (109 500 analyses). La charge peut être réduite à 5 ou 2 ans avec `-p:BenchmarkYears=5` ou `-p:BenchmarkYears=2`. Il génère aussi temporairement des lots de 10, 100 et 500 fichiers sur disque puis les supprime. Le rapport Markdown contient la durée, le débit, les allocations et la mémoire observée ; il est enregistré dans `benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md` et son contenu est affiché dans la sortie détaillée du test.

Le [dernier rapport de benchmark](benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md) est disponible dans le dépôt. Ses mesures reflètent l'environnement et la charge de cette exécution.

Le [rapport de charge de l'import dynamique](docs/benchmarks/dynamic-import-load-report.md) décrit le test d'injection simultanée de 50 fichiers XML.