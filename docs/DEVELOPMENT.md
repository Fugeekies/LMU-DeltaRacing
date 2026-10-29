# LMU-DeltaRacing — Guide développeur

Documentation technique du projet (structure, commandes, packaging). Le guide d'utilisation est dans le [README](../README.md).

La [feuille de route](ROADMAP.md) présente les jalons envisagés et une liste d'idées à prioriser.

## État actuel

Le socle métier, le monitoring et la console sont en place : le parser transforme les fichiers XML LMU en entités métier (`Session`, `Driver`, `Lap`, `Sector`), le dépôt conserve les sessions en mémoire, et la console Spectre reprend l'Accueil, Mes courses et les Options du dashboard avec les mêmes calculs (projet `Presentation`). Les tests MSTest et l'application utilisent le jeu de résultats anonymisés unique de `assets/Results`.

## Projets

- `src/LMU-DeltaRacing.Domain` : entités métier sans dépendance externe.
- `src/LMU-DeltaRacing.Application` : contrats du parser et du dépôt.
- `src/LMU-DeltaRacing.Infrastructure` : parser XML, dépôt en mémoire et surveillance dynamique du dossier de résultats.
- `src/LMU-DeltaRacing.Console` : console Spectre.Console (Accueil, Mes courses, Options) sur les services partagés du projet `Presentation`, avec surveillance dynamique des XML et diagnostic OpenTelemetry.
- `src/LMU-DeltaRacing.Presentation` : calculs des vues, service de données, réglages et détection du dossier de résultats, partagés par l'UI et la console.
- `src/LMU-DeltaRacing.UI` : dashboard Blazor local avec historique des sessions, classements et surveillance dynamique des nouveaux résultats.
- `src/LMU-DeltaRacing.Console/appsettings.json` : valeurs initiales pour la configuration de l'application.
- `tests/LMU-DeltaRacing.Tests` : tests unitaires MSTest et FluentAssertions du parsing, de l'import et de la console.
- `docs/benchmarks/dynamic-import-load-report.md` : résultats du test de charge de la surveillance sur 50 fichiers XML simultanés.
- `benchmarks/LMU-DeltaRacing.Benchmarks` : projet MSTest dédié aux benchmarks : parsing de XML synthétiques et de lots temporaires, chaîne d'import sur les fichiers réels et calculs de chaque page.
- `Directory.Build.props` (racine) : framework cible, usings implicites et types nullables communs. `src/Directory.Build.props` : documentation XML obligatoire et avertissements traités comme des erreurs pour les projets de l'application.

## Commandes

- `dotnet build`
- `dotnet test`
- `dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj`
- `dotnet run --project src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj`
- `dotnet publish src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj -c Release -p:PublishProfile=win-x64`
- `pwsh -File scripts/Prepare-PackageAssets.ps1 -SourceDirectory "$env:USERPROFILE\Documents\Le Mans Ultimate\UserData\Log\Results" -OutputDirectory assets/Results -MergeSingleRaceDrivers`
- `Copy-Item assets/Results -Destination publish_output/TestAssets -Recurse`
- `pwsh -File scripts/Test-PublishedApp.ps1 -PublishDirectory publish_output`
- `iscc /DAppVersion=1.0.0 scripts/installer.iss`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release --logger "console;verbosity=detailed"`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release -p:BenchmarkYears=5 --logger "console;verbosity=detailed"`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release --filter "TestCategory=Import" --logger "console;verbosity=detailed"`
- `dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj --configuration Release --filter "TestCategory=Views" --logger "console;verbosity=detailed"`

La console ne prend aucun argument : elle charge le dossier configuré dans son `appsettings.json` (ou détecté automatiquement) et reste ouverte jusqu'au choix `Quitter`.

Au démarrage, la console importe les XML du dossier configuré avec une barre de progression, affiche un résumé du chargement (les échecs sont détaillés), puis le menu principal : Accueil, Mes courses, Options, Quitter. Les nouveaux XML sont importés automatiquement si la surveillance est active; les fichiers verrouillés sont réessayés, les doublons ignorés, et les imports sont signalés au retour au menu principal. Les vues sont recalculées à chaque ouverture depuis les sessions en mémoire. Changer le dossier des résultats dans Options recharge immédiatement les résultats et la surveillance. Les sessions d'essais ne sont jamais importées. Les tests de rendu utilisent `Spectre.Console.Testing` (`TestConsole`).

Les logs OpenTelemetry sont écrits dans `lmu-deltaracing.log`, à côté de l'exécutable, sous forme de JSON Lines : chaque ligne est un objet JSON structuré. Le fichier reste disponible après la fermeture de l'application et est écrasé au démarrage suivant.

Les statistiques agrégées de la dernière exécution sont écrites dans `lmu-deltaracing-stats.json`, au même endroit. Le fichier indique les imports réussis/échoués, la durée moyenne par fichier, le débit moyen d'import, ainsi que les moyennes du working set et du heap .NET. Il est lui aussi conservé après l'arrêt et réinitialisé au prochain lancement. Les traces et métriques OpenTelemetry ne polluent pas le terminal.

Les réglages sont enregistrés dans `appsettings.json` à côté de l'exécutable et rechargés à chaud. Par défaut, le dossier des XML est découvert dans `assets/Results`, le log se nomme `lmu-deltaracing.log` à côté de l'exécutable et son niveau minimal est `Debug`. Le terminal n'affiche pas les logs ; le niveau configuré ne concerne que le fichier.

Le dashboard Blazor s'ouvre sur la page Accueil (`/`), tableau de bord de la carrière du pilote sélectionné. Au premier démarrage, il sélectionne automatiquement le pilote le plus fréquent dans les résultats importés et enregistre son nom dans `LMU:UserName`; le pilote peut ensuite être changé dans Options, où les candidats sont triés par fréquence d'apparition. Les vues de résultats n'affichent que les sessions contenant le pilote sélectionné. La page Options permet aussi de saisir ou choisir le dossier de résultats avec le sélecteur Windows, de le valider, de le sauvegarder et de recharger son watcher. L'état des imports et les erreurs y sont affichés sous la configuration. La préférence `LMU:WatchResultsOnStartup` permet de désactiver la surveillance automatique au lancement. L'application s'ouvre dans le navigateur par défaut et n'écoute que sur l'interface locale (`127.0.0.1`) avec un port attribué automatiquement.

Le profil `win-x64` produit une publication autonome et à fichier unique. L'élagage est désactivé afin de préserver la sérialisation d'état utilisée par les composants Blazor interactifs. Le jeu unique de résultats de démonstration anonymisés est versionné dans `assets/Results`; le workflow le copie vers la destination d'installation `TestAssets/Results` sans le régénérer. Pour renouveler ce jeu de données depuis un autre dossier de résultats LMU, exécutez `Prepare-PackageAssets.ps1` vers `assets/Results` et incluez les fichiers XML mis à jour dans la modification. Pour construire et vérifier manuellement l'installeur, publiez d'abord l'UI avec ce profil dans `publish_output`, copiez les fixtures vers `publish_output/TestAssets`, exécutez `Test-PublishedApp.ps1`, puis compilez `scripts/installer.iss` avec Inno Setup. Les fixtures utilisent des alias de pilotes stables entre fichiers; les noms des résultats LMU de l'utilisateur ne sont pas modifiés. L'installeur choisit un dossier de résultats LMU détecté ou, en l'absence de résultats, les fixtures anonymisées installées avec l'application. La configuration existante est conservée lors des mises à niveau.

Le workflow `.github/workflows/ci.yml` compile la solution en Release (les avertissements de `src/` sont des erreurs) et lance le projet de tests sur chaque PR et sur `main` après fusion ; en cas d'échec, les résultats de tests (`.trx`) sont joints au run. Les benchmarks n'y sont pas lancés.

Le workflow manuel `.github/workflows/release.yml` construit et vérifie l'application, lance les tests unitaires, puis publie `LMU-DeltaRacing-Setup.exe` dans une release GitHub. Depuis l'onglet Actions, choisissez « Build and release Windows installer (Manual) », la branche `main` (seule autorisée), puis le type de version : **Majeur**, **Mineur** ou **Patch** (par défaut). Le workflow incrémente le dernier tag `vX.Y.Z` (par exemple `v0.0.8` donne `v1.0.0`, `v0.1.0` ou `v0.0.9`), crée le tag sur la révision construite et nomme la release « LMU-DeltaRacing vX.Y.Z ». Les notes de la release listent une ligne par PR fusionnée depuis le tag précédent (titre et numéro de la PR), suivies du lien de comparaison entre les deux tags : un titre de PR clair sert donc directement de changelog. `CHANGELOG.md` n'est pas modifié par le workflow.

Le projet de benchmarks génère des entrées XML synthétiques et les analyse depuis des streams en mémoire, sans conserver les sessions analysées. Par défaut, il simule 10 courses par jour pendant 10 ans, avec Practice, Qualifying et Race (109 500 analyses). La charge peut être réduite à 5 ou 2 ans avec `-p:BenchmarkYears=5` ou `-p:BenchmarkYears=2`. Il génère aussi temporairement des lots de 10 à 5 000 fichiers sur disque puis les supprime. Il mesure ensuite la chaîne d'import sur les fichiers réels de `assets/Results` (parsing par type de session, import disque, chargement complet de `DashboardDataService` avec ou sans essais, fusions et journal JSON) et les calculs de chaque page sur l'historique réel répété ×1, ×10 et ×50. Chaque mesure est préchauffée et le rapport donne la médiane, le meilleur temps et les allocations. Le rapport Markdown est enregistré dans `benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md` par le test de catégorie `Load` ; les catégories `Import` et `Views` affichent une partie seule dans la sortie détaillée, sans écrire le rapport.

Le [dernier rapport de benchmark](../benchmarks/LMU-DeltaRacing.Benchmarks/artifacts/performance-report.md) est disponible dans le dépôt. Ses mesures reflètent l'environnement et la charge de cette exécution.

Le [rapport de charge de l'import dynamique](benchmarks/dynamic-import-load-report.md) décrit le test d'injection simultanée de 50 fichiers XML.

L'[audit de performance d'octobre 2026](benchmarks/audit-performance-2026-10.md) compare les mesures avant et après le parser en flux et l'import parallèle.

Le parser est verrouillé par un test qui compare l'empreinte SHA-256 de toutes les valeurs lues dans les 342 fixtures à une valeur de référence (`XmlResultParserTests.Parse_reads_every_fixture_as_the_reference_snapshot`). Si une évolution voulue du parser change ces valeurs (nouveau champ lu, par exemple), le message d'échec donne la nouvelle empreinte à reporter dans le test, après avoir vérifié la différence.
