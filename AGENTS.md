# AGENTS.md — LMU-DeltaRacing

Application locale .NET 10 d'analyse des fichiers XML de résultats de *Le Mans Ultimate* (LMU) : dashboard Blazor Server (application principale, distribuée via installeur) et console Spectre.Console (version simplifiée : Accueil, Mes courses, Options). Persistance **en mémoire uniquement**. La documentation et l'UI sont en **français**, le code et les commentaires XML en **anglais**.

Ce fichier est la **source unique** des instructions pour les agents IA : GitHub Copilot le lit directement, Claude Code via l'import `@AGENTS.md` de `CLAUDE.md`. Ne pas recréer de `.github/copilot-instructions.md`, qui dupliquerait ces règles dans Copilot.

## Commandes

```powershell
dotnet build LMU-DeltaRacing.slnx
dotnet test tests/LMU-DeltaRacing.Tests/LMU-DeltaRacing.Tests.csproj
dotnet test tests/LMU-DeltaRacing.Tests/LMU-DeltaRacing.Tests.csproj --filter "FullyQualifiedName~MyRacesPresentationTests"
dotnet run --project src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj        # ouvre le navigateur sur 127.0.0.1:<port aléatoire>
dotnet run --project src/LMU-DeltaRacing.Console/LMU-DeltaRacing.Console.csproj
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release -p:BenchmarkYears=2   # benchmarks (long, éviter sauf demande)
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release --filter "TestCategory=Views" --logger "console;verbosity=detailed"   # calculs des vues seuls (TestCategory=Import : chaîne d'import)
dotnet publish src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj -c Release -p:PublishProfile=win-x64 -o publish_output
```

`dotnet test` sans argument lance aussi les benchmarks : cibler le projet de tests.

## Architecture

| Projet | Rôle |
|---|---|
| `src/LMU-DeltaRacing.Domain` | Entités immuables `Session`, `Driver`, `Lap`, `Sector`, sans dépendance. `Session.Kind` (essais/qualif/course) et `Driver.BestLap` calculés à la création. |
| `src/LMU-DeltaRacing.Application` | Contrats (`IResultXmlParser`, `ISessionRepository`, `ISessionImportService`), `SessionImportService` (retries sur fichier verrouillé), télémétrie (`ApplicationTelemetry`). |
| `src/LMU-DeltaRacing.Infrastructure` | `XmlResultParser` (en flux, `XmlReader`), `InMemorySessionRepository`, `ResultsDirectoryWatcher` (`FileSystemWatcher` + `Channel`, déduplication par chemin). |
| `src/LMU-DeltaRacing.Presentation` | Code partagé par l'UI et la console. `Services/DashboardDataService` (hosted service : import initial, watcher, état partagé via `StateChanged`), `DashboardSettingsStore` (écriture des réglages), `ResultsDirectoryResolver`. Calculs des vues dans `Models/*Presentation.cs`. |
| `src/LMU-DeltaRacing.UI` | Blazor Server : composants Razor, `WindowsFolderPicker`, géométrie du graphique Progression (`ProgressionChartLayout`), vérification de la dernière release (`UpdateCheckService`, version dans `ApplicationVersion`). |
| `src/LMU-DeltaRacing.Console` | Spectre.Console : `ConsoleApplication` (menu, imports en file d'attente) et `Views/` (Accueil, Mes courses, Options) sur `DashboardDataService`. OpenTelemetry, réglages du log via `IOptionsMonitor<LmuApplicationOptions>`. Aucun argument en ligne de commande. |
| `tests/LMU-DeltaRacing.Tests` | MSTest + FluentAssertions, organisés par projet (`Application/`, `Infrastructure/`, `Presentation/`, `Console/`, `UI/`). |
| `benchmarks/LMU-DeltaRacing.Benchmarks` | Parser (synthétique et fichiers réels), chargement complet, calculs de chaque page ×1/×10/×50 ; rapport dans `artifacts/performance-report.md`. |
| `scripts/` | `installer.iss` (Inno Setup), `Prepare-PackageAssets.ps1` (anonymisation des fixtures), `Test-PublishedApp.ps1`. |
| `docs/` | `ARCHITECTURE.md`, `ROADMAP.md`, `UI_DESIGN_SPEC.md`, maquettes `UISpec/*.template.md`, `benchmarks/`. |

Points d'attention :
- UI et console partagent les calculs, les formats (`DisplayFormat`) et le service de données du projet `Presentation` : une règle métier se modifie à un seul endroit, mais son rendu existe deux fois (pages Razor et `Console/Views`). Chacune garde sa composition et son logging (UI : `JsonFileLoggerProvider` ; console : OpenTelemetry + `FileLogExporter`).
- `appsettings.json` est lu et écrit **à côté de l'exécutable** (`AppContext.BaseDirectory`), section `LMU`.
- Les fusions de pilotes (`LMU:DriverMerges`, `Presentation/Models/DriverMerges.cs`) sont appliquées **en mémoire** par `DashboardDataService` : le dépôt garde les noms d'origine, ce qui permet d'annuler une fusion et de détecter les pilotes qui se sont croisés.
- Nature d'une session : lire `Session.Kind`, jamais `SessionType.Contains(...)`. Rang en classe, nom de voiture et catégorie d'un pilote : `DriverResults` (`ClassRank()`, `CarName()`, `Category()`). Pages Razor de résultats : dériver de `DashboardPage` et envelopper le contenu dans `ResultsGate`.
- `UpdateCheckService` envoie la **seule requête sortante** de l'application (dernier tag GitHub, désactivable par `LMU:CheckForUpdates`). Aucune autre donnée ne quitte le PC : aucune donnée n'est collectée. La console n'a pas cette vérification. La version vient de `-p:Version` à la publication ; un build local reste en `0.0.0-dev`.
- Le parser est verrouillé par l'empreinte SHA-256 des 342 fixtures (`XmlResultParserTests`) : ne mettre à jour la référence qu'après avoir vérifié et justifié la différence.
- `src/Directory.Build.props` traite les avertissements comme des erreurs et exige la documentation `///` des membres publics.

## Faits vérifiés sur le format XML LMU

Vérifiés sur `assets/Results` ; s'appuyer dessus plutôt que les redéduire :
- Racine `rFactorXML/RaceResults` avec un DOCTYPE interne (garder `DtdProcessing.Ignore`, sans `XmlResolver`).
- Une phase par fichier : `Practice1`, `Qualify` ou `Race`. La console **ignore les sessions Practice à l'import** (`SessionImportStatus.SkippedPractice`) ; le dashboard les importe si l'option `LMU:LoadPracticeSessions` est active (décochée par défaut, lue à chaque import) et ne les affiche que dans l'onglet Essais de « Détail des courses » (séances d'un même événement fusionnées). Dans un même événement, essais, qualif et course partagent le même `RaceResults/DateTime`.
- `RaceResults/DateTime` est un timestamp Unix (UTC). `TimeString` est l'heure locale. Afficher avec `ToLocalTime()`.
- `<Lap num=".." s1 s2 s3>` : secteurs **partiels** (s1+s2+s3 = temps du tour). Un tour invalide vaut `--.----` et un secteur peut manquer.
- Valeurs de `CarClass` présentes : `Hyper`, `LMP2`, `LMP3`, `GTE`, `GT3`. Des sessions sont **multiclasses**.
- Plusieurs courses peuvent avoir lieu le même jour sur le même circuit.
- Pilotes des fixtures anonymisés avec des prénoms (`Driver Tom`, `Driver Lucas`…), alias stables entre fichiers. Pour enrichir les historiques, les pilotes n'ayant fait qu'une course ont été fusionnés avec des pilotes qu'ils n'ont jamais croisés (`Prepare-PackageAssets.ps1 -MergeSingleRaceDrivers`) : un alias ne désigne donc pas toujours une seule personne réelle. Pilote principal des fixtures : `Driver Tom` (jamais fusionné). Pour la démonstration, ses résultats ont été retouchés à la main dans 5 sessions : l'identité de Tom (de `<Name>` à `<ServerScored>` : nom, voiture, équipe) y est échangée avec celle du premier de sa classe, ce qui lui donne 3 victoires (courses `2025_08_14_19_57_19-69R1`, `2025_09_03_21_56_26-90R1`, `2026_10_02_20_54_31-58R1`) et 2 poles de plus (qualifs `2025_09_03_21_31_12-71Q1`, `2026_10_02_21_13_54-84Q1`). Une régénération par `Prepare-PackageAssets.ps1` depuis les résultats d'origine perdrait ces retouches.
- Les noms présents dans les événements du `Stream` (incidents, pénalités…) des fixtures actuelles ne correspondent pas aux pilotes listés : la correspondance a été perdue lors d'une anonymisation antérieure.

## Règles Git & PR

- **Jamais de commit direct sur `main`** : branche dédiée (`feature/…`, `fix/…`), puis PR.
- Commit ou push uniquement sur demande explicite.
- **PR créées en brouillon** (`gh pr create --draft`) : la CI (`ci.yml`) ignore les brouillons, ce qui évite un run à chaque commit pendant le travail. Passer la PR en « Ready for review » (`gh pr ready`) quand elle est terminée, ce qui lance la CI, puis attendre qu'elle soit verte avant d'annoncer la PR prête.
- Toute PR touchant `src/`, `tests/`, la configuration ou les fonctionnalités met à jour `CHANGELOG.md` sous `## [Unreleased]`, catégories Keep a Changelog : `Added`, `Changed`, `Fixed`, `Performance`, `Security`. Entrées en français, concises, orientées utilisateur/développeur.
- Toute PR modifiant le parser ou le moteur d'import committe un rapport de benchmark Markdown à jour.

## Méthode de travail

1. **Zéro invention.** Ne jamais extrapoler une balise XML, une règle métier, un comportement UI ou un choix d'architecture non documenté. En cas d'ambiguïté (spec contradictoire, cas limite XML, gestion d'erreur), **s'arrêter et poser la question** avant de coder. Les specs de `docs/` se contredisent parfois : signaler la contradiction au lieu de choisir silencieusement.
2. **TDD.** Écrire d'abord le test (MSTest + FluentAssertions), le voir échouer, puis implémenter. Préférer les vrais XML de `assets/Results/` (ne pas les copier dans `tests/`) aux données synthétiques pour les règles métier.
3. **Après chaque modification de code :** `dotnet build` sans avertissement et tests verts avant d'annoncer que c'est terminé.
4. **Garder la doc synchronisée** : `README.md` (guide utilisateur), `docs/DEVELOPMENT.md` (guide développeur), `docs/ARCHITECTURE.md` et `docs/ROADMAP.md` (cocher un jalon seulement quand il est réellement implémenté).

## Conventions de code

- Commentaires XML `///` sur chaque type et membre public. Commentaires inline pour expliquer le **pourquoi** (raison métier ou technique), pas le comment.
- Style existant : constructeurs qui valident (`ArgumentNullException.ThrowIfNull`, …), entités immuables, `CultureInfo.InvariantCulture` pour parser et formater les temps, `StringComparison.OrdinalIgnoreCase` pour les noms de pilotes.
- Logs via `Microsoft.Extensions.Logging` uniquement, messages structurés (`{SourceFile}`, jamais d'interpolation). Niveaux :
  - `Trace`/`Debug` : détails internes, boucles, valeurs brutes lues dans le XML.
  - `Information` : événements métier normaux (session importée, watcher démarré).
  - `Warning` : anomalie non bloquante (fichier verrouillé, nouvelle tentative).
  - `Error`/`Critical` : échec d'import, XML illisible, exception non gérée.
- Ne logger que le **nom** du fichier XML, jamais son chemin absolu (données personnelles).
- Logs fichier en **JSON Lines** (un objet par ligne). Aucun log dans le terminal de la console : il est réservé à Spectre.Console.
- UI : respecter `docs/UI_DESIGN_SPEC.md` (palette, couleurs WEC violet/vert/jaune/rouge, badges de catégorie) et les maquettes `docs/UISpec/*.template.md`.
