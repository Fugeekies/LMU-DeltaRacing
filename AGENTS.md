# AGENTS.md — LMU-DeltaRacing

Application locale .NET 10 d'analyse des fichiers XML de résultats de *Le Mans Ultimate* (LMU) : dashboard Blazor Server, distribué via installeur. La console Spectre.Console a été supprimée le 10/11/2026 : ne pas la recréer ni ajouter d'autre interface sans décision. Persistance **en mémoire uniquement** pour les sessions, relues dans les XML à chaque lancement ; seules les données saisies par le joueur sont écrites sur disque (`appsettings.json`, `race-notes.json`). La documentation est en **français**, l'interface du dashboard en cinq langues (anglais, français, allemand, italien, espagnol ; le **français** fait référence), le code et les commentaires XML en **anglais**.

Ce fichier est la **source unique** des instructions pour les agents IA : GitHub Copilot le lit directement, Claude Code via l'import `@AGENTS.md` de `CLAUDE.md`. Ne pas recréer de `.github/copilot-instructions.md`, qui dupliquerait ces règles dans Copilot.

## Commandes

```powershell
dotnet build LMU-DeltaRacing.slnx
dotnet test tests/LMU-DeltaRacing.Tests/LMU-DeltaRacing.Tests.csproj
dotnet test tests/LMU-DeltaRacing.Tests/LMU-DeltaRacing.Tests.csproj --filter "FullyQualifiedName~MyRacesPresentationTests"
dotnet run --project src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj        # ouvre le navigateur sur deltaracing.localhost:<port aléatoire> (serveur limité à 127.0.0.1)
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release -p:BenchmarkYears=2   # benchmarks (long, éviter sauf demande)
dotnet test benchmarks/LMU-DeltaRacing.Benchmarks/LMU-DeltaRacing.Benchmarks.csproj -c Release --filter "TestCategory=Views" --logger "console;verbosity=detailed"   # calculs des vues seuls (TestCategory=Import : chaîne d'import)
dotnet publish src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj -c Release -p:PublishProfile=win-x64 -o publish_output
```

`dotnet test` sans argument lance aussi les benchmarks : cibler le projet de tests.

## Architecture

| Projet | Rôle |
|---|---|
| `src/LMU-DeltaRacing.Domain` | Entités immuables `Session`, `Driver`, `Lap`, `Sector`, `SessionEvent` (contacts, limites de piste et pénalités du `Stream`), sans dépendance. `Session.Kind` (essais/qualif/course) et `Driver.BestLap` calculés à la création. |
| `src/LMU-DeltaRacing.Application` | Contrats (`IResultXmlParser`, `ISessionRepository`, `ISessionImportService`), `SessionImportService` (retries sur fichier verrouillé). |
| `src/LMU-DeltaRacing.Infrastructure` | `XmlResultParser` (en flux, `XmlReader`), `InMemorySessionRepository`, `ResultsDirectoryWatcher` (`FileSystemWatcher` + `Channel`, déduplication par chemin). |
| `src/LMU-DeltaRacing.Presentation` | Code métier des vues, séparé des composants Razor pour être testé sans l'UI. `Services/DashboardDataService` (hosted service : import initial, watcher, état partagé via `StateChanged`), `DashboardSettingsStore` (écriture des réglages), `RaceNotesStore` (notes de course), `ResultsDirectoryResolver`. Calculs des vues dans `Models/*Presentation.cs`. |
| `src/LMU-DeltaRacing.UI` | Blazor Server : composants Razor, `WindowsFolderPicker`, géométrie du graphique Progression (`ProgressionChartLayout`), vérification de la dernière release (`UpdateCheckService`, version dans `ApplicationVersion`). |
| `tests/LMU-DeltaRacing.Tests` | MSTest + FluentAssertions, organisés par projet (`Application/`, `Infrastructure/`, `Presentation/`, `UI/`), plus `Documentation/` (cohérence du guide utilisateur). |
| `benchmarks/LMU-DeltaRacing.Benchmarks` | Parser (synthétique et fichiers réels), chargement complet, calculs de chaque page ×1/×10/×50 ; rapport dans `artifacts/performance-report.md`. |
| `scripts/` | `installer.iss` (Inno Setup), `Prepare-PackageAssets.ps1` (anonymisation des fixtures), `Repair-StreamNames.ps1` (noms du `Stream` des fixtures), `Test-PublishedApp.ps1`. |
| `docs/` | Guide utilisateur `guide/<langue>/` (en, fr, de, it, es), `COTE_PILOTE.md`, `ARCHITECTURE.md`, `ROADMAP.md`, `UI_DESIGN_SPEC.md`, maquettes `UISpec/*.template.md`, `benchmarks/`. |
| `site/` | Page de présentation publiée sur GitHub Pages (`fugeekies.github.io/LMU-DeltaRacing`) par `.github/workflows/pages.yml`, en anglais : seul ce dossier est publié. Captures du tableau de bord sur les fixtures anonymisées, image d'aperçu `social-preview.png` (1280×640), `sitemap.xml`. |

Points d'attention :
- Les calculs, les formats (`DisplayFormat`) et le service de données sont dans `Presentation`, les pages Razor ne font que les afficher. Logging : `JsonFileLoggerProvider`.
- `appsettings.json` est lu et écrit **à côté de l'exécutable** (`AppContext.BaseDirectory`), section `LMU`.
- Notes de course : `race-notes.json`, à côté d'`appsettings.json`, lu et écrit par `RaceNotesStore` seul. Une note par session, rattachée à `Session.Id` (stable entre deux lancements) ; une note vide est supprimée. Le fichier n'est pas publié, donc ni écrasé par une mise à jour ni supprimé à la désinstallation. Un fichier illisible n'est jamais réécrit (`IsUnreadable`, enregistrement refusé) pour ne pas perdre de notes.
- Les fusions de pilotes (`LMU:DriverMerges`, `Presentation/Models/DriverMerges.cs`) sont appliquées **en mémoire** par `DashboardDataService` : le dépôt garde les noms d'origine, ce qui permet d'annuler une fusion et de détecter les pilotes qui se sont croisés.
- Un `Incident` n'est pas un contact : compter avec `DrivingConductPresentation` (regroupement à 5 s, chaque pilote impliqué) ; la page Profil de conduite est centrée sur le pilote sélectionné (`DrivingProfilePresentation`), et le coût d'un contact se mesure contre le tour médian de la course (`RaceStoryPresentation`). Une nouvelle copie de `Session` doit reporter `Events`.
- Cote pilote : une seule source, `DriverRatings.Calculate` (Elo multijoueur sur les courses, par classe, classe sans aucun pilote arrivé écartée). Ne pas recalculer une cote ailleurs : lire `DriverRatingHistory` (`FindRace`, `FindDriver`). Règles et paramètres dans `docs/UISpec/DriverRating.template.md`.
- Nature d'une session : lire `Session.Kind`, jamais `SessionType.Contains(...)`. Rang en classe, nom de voiture et catégorie d'un pilote : `DriverResults` (`ClassRank()`, `CarName()`, `Category()`). Pages Razor de résultats : dériver de `DashboardPage` et envelopper le contenu dans `ResultsGate`.
- `UpdateCheckService` envoie la **seule requête sortante** de l'application (dernier tag GitHub, désactivable par `LMU:CheckForUpdates`). Aucune autre donnée ne quitte le PC : aucune donnée n'est collectée. La version vient de `-p:Version` à la publication ; un build local reste en `0.0.0-dev`.
- Le parser est verrouillé par l'empreinte SHA-256 des 342 fixtures (`XmlResultParserTests`) : ne mettre à jour la référence qu'après avoir vérifié et justifié la différence.
- `src/Directory.Build.props` traite les avertissements comme des erreurs et exige la documentation `///` des membres publics.

## Faits vérifiés sur le format XML LMU

Vérifiés sur `assets/Results` ; s'appuyer dessus plutôt que les redéduire :
- Racine `rFactorXML/RaceResults` avec un DOCTYPE interne (garder `DtdProcessing.Ignore`, sans `XmlResolver`).
- Une phase par fichier : `Practice1`, `Qualify` ou `Race`. Le dashboard importe les sessions Practice si l'option `LMU:LoadPracticeSessions` est active (décochée par défaut, lue à chaque import ; sinon `SessionImportStatus.SkippedPractice`) et ne les affiche que dans l'onglet Essais de « Détail des courses » (séances d'un même événement fusionnées).
- `RaceResults/Setting` (`Session.Setting`, `Session.IsRaceWeekend`) vaut `Multiplayer` (336 fichiers) ou `Race Weekend` (6, toutes des courses). Les fichiers `Race Weekend` sont des courses **hors ligne contre l'IA** : un seul pilote `<isPlayer>1`, tous les autres en `AIControl` (absent des 336 fichiers `Multiplayer`) quand des tours existent ; 3 des 6 n'ont aucun tour (course quittée, `FinishStatus` à `None` ou `DNF`). Aucune qualification ni séance d'essais ne les accompagne. `<Dedicated>` vaut `0` partout : il ne distingue rien. Le dashboard ne les importe que si l'option `LMU:LoadRaceWeekendSessions` est active (décochée par défaut, lue à chaque import ; sinon `SessionImportStatus.SkippedRaceWeekend`).
- `RaceResults/DateTime` est un timestamp Unix (UTC), commun à une série de sessions (essais, qualif et course d'un même événement, voire deux courses après un redémarrage) : il ne date pas une session. Chaque phase porte sa propre date (`<Race><DateTime>`, `<Qualify><DateTime>`), présente et unique dans les 342 fixtures : c'est `Session.StartedAt`, à défaut `RaceResults/DateTime`. `TimeString` est l'heure locale. Afficher avec `ToLocalTime()`.
- `Session.Id` est calculé à partir du lieu, du tracé, de la phase et de sa date (stable entre deux lancements). Deux fichiers de même identifiant sont une même session : le premier importé est gardé (`ISessionRepository.TryAdd`), le second est ignoré (`SessionImportStatus.SkippedDuplicate`). Dans un test, ne pas copier une même fixture pour simuler deux sessions.
- `<Lap num=".." s1 s2 s3>` : secteurs **partiels** (s1+s2+s3 = temps du tour). Un tour invalide vaut `--.----` et un secteur peut manquer. `et` est le **début** du tour (638/638 sur les courses de Tom : `et` + temps = `et` suivant), `p` la position au général à la fin du tour et `pit="1"` un passage par la voie des stands. `et` vaut `--.---` sur 3 059 tours des fichiers récents (inconnu).
- Arrêts : `pit="1"` marque le tour d'entrée et souvent celui de sortie, les drive-through et les départs depuis la voie des stands. Il diffère de `<Pitstops>` (compteur du pilote) pour 164 pilotes sur 2 303 en course : le **nombre d'arrêts** se lit dans `<Pitstops>` (`Driver.PitStops`, décision du 08/11/2026), les tours `pit="1"` ne servent qu'à repérer les passages et découper les relais.
- Autres attributs de `<Lap>` (`Lap.Telemetry`) : `topspeed` (km/h, présent partout ; `0.00` sur 358 tours d'une voiture qui n'a pas roulé : inconnu), `fcompound`/`rcompound` (`index,Nom`, présents partout ; l'index dépend de la voiture, seul le nom compte : `Soft`, `Medium`, `Hard`, `Wet`, et `N/A` sans pneu), `FL`/`FR`/`RL`/`RR` (composé par roue, fichiers récents, non lus). `fuel`, `fuelUsed`, `ve`, `veUsed` et `twfl/twfr/twrl/twrr` ne sont écrits que si le serveur partage le carburant (`<ClientFuelVisible>`) : aucun dans les 117 fichiers à `2` (tous ceux depuis juillet 2026), et pour une partie des pilotes seulement à `1` ou `3`. `fuel` et `ve` (énergie virtuelle, GT3 et Hyper seulement) sont des **fractions** du réservoir (0 à 1) à la fin du tour ; aucune capacité dans le XML, donc pas de litres. `fuelUsed` = `fuel` du tour précédent − `fuel` du tour (9 140/9 140), négatif sur le tour d'un ravitaillement ; de même pour `veUsed`. `tw..` est la gomme restante de chaque pneu (1 = neuf), qui ne remonte qu'au stand (35/35).
- Valeurs de `CarClass` présentes : `Hyper`, `LMP2`, `LMP3`, `GTE`, `GT3`. Des sessions sont **multiclasses**.
- Plusieurs courses peuvent avoir lieu le même jour sur le même circuit.
- Pilotes des fixtures anonymisés avec des prénoms (`Driver Tom`, `Driver Lucas`…), alias stables entre fichiers. Pour enrichir les historiques, les pilotes n'ayant fait qu'une course ont été fusionnés avec des pilotes qu'ils n'ont jamais croisés (`Prepare-PackageAssets.ps1 -MergeSingleRaceDrivers`) : un alias ne désigne donc pas toujours une seule personne réelle. Pilote principal des fixtures : `Driver Tom` (jamais fusionné). Pour la démonstration, ses résultats ont été retouchés à la main dans 5 sessions : l'identité de Tom (de `<Name>` à `<ServerScored>` : nom, voiture, équipe) y est échangée avec celle du premier de sa classe, ce qui lui donne 3 victoires (courses `2025_08_14_19_57_19-69R1`, `2025_09_03_21_56_26-90R1`, `2026_10_02_20_54_31-58R1`) et 2 poles de plus (qualifs `2025_09_03_21_31_12-71Q1`, `2026_10_02_21_13_54-84Q1`). Une régénération par `Prepare-PackageAssets.ps1` depuis les résultats d'origine perdrait ces retouches.
- Les noms des événements du `Stream` des fixtures correspondent aux pilotes listés : 100 % en course, 3 incidents manquants en qualification, environ 75 % en essais. Un nom qui ne correspond à personne (pilote parti, nom d'origine porté par deux pilotes) garde un alias sans pilote : ses événements sont ignorés. La correspondance, perdue lors d'une anonymisation, a été rétablie depuis les résultats d'origine par `Repair-StreamNames.ps1`, qui ne touche qu'au `Stream` : alias et retouches de Tom sont conservés, et les événements suivent les tours d'une voiture, y compris dans les 5 sessions retouchées. Ces originaux contiennent de vrais noms : ne jamais les copier dans le dépôt ni citer leur contenu. Le numéro des événements (`ID`, `Driver X(12)`) ne se relie pas à `CarNumber`, qui peut être en double dans un fichier.
- Événements du `Stream`, chacun avec son attribut `et` (temps écoulé depuis le début de la session, en secondes) :
  - `Incident` : `Nom(id) reported contact (valeur) with another vehicle Nom(id)`, ou `with Immovable`, `Post`, `Sign`, `Wing`, `Wheel`, `Cone`. Chaque voiture signale son contact, souvent plusieurs fois. La valeur entre parenthèses n'est pas lue : elle semble mesurer la violence du choc (médiane de 2 315 avec dégâts, 373 sans, sur les courses de Tom), sans que ce soit documenté.
  - `TrackLimits` : attributs `Driver`, `ID`, `Lap` (base 0), `WarningPoints`, `CurrentPoints`, `Resolution` ; texte `No Further Action`, `Warning`, `Invalid Lap Cut Track` (jamais en course), `Drive Through Penalty`, `Stop Go Penalty` ou `Disqualify`.
  - `Penalty` : attributs `Driver`, `ID`, `Time`, `Penalty`, `Reason`, `Laps` ; texte libre (`received Drive Thru penalty…`, `served…`, `finished before serving penalty…`).
  - `Score` : `Nom(id) lap=N point=P t=.. et=..`, avec `lap` en base 0 : `point=1` donne `s1`, `point=2` donne `s2`, et `point=0` clôt le tour (dernier secteur ou temps du tour, non distingué).
  - `Sector` : dégâts (`reports new suspension damage`, `engine`) et records (`set new best for sector N`) malgré son nom. Les avertissements `TrackLimits` sont datés après coup : ne pas en déduire un secteur. `Sent` : messages (`"Nom" has taken the wheel.`). `ChatMessage` : contenu retiré des fixtures.

## Règles Git & PR

- **Jamais de commit direct sur `main`** : branche dédiée (`feature/…`, `fix/…`), puis PR.
- Une modification terminée (build sans avertissement, tests verts) est committée et poussée sur sa branche sans attendre de demande, sauf si l'utilisateur précise « sans commiter ». Une nouvelle évolution part sur sa propre branche, avec une PR en brouillon.
- **PR créées en brouillon** (`gh pr create --draft`) : la CI (`ci.yml`) ignore les brouillons, ce qui évite un run à chaque commit pendant le travail. Passer la PR en « Ready for review » (`gh pr ready`) quand elle est terminée, ce qui lance la CI, puis attendre qu'elle soit verte avant d'annoncer la PR prête.
- Toute PR touchant `src/`, `tests/`, la configuration ou les fonctionnalités met à jour `CHANGELOG.md` sous `## [Unreleased]`, catégories Keep a Changelog : `Added`, `Changed`, `Fixed`, `Performance`, `Security`. Entrées en français, concises, orientées utilisateur/développeur.
- Toute PR modifiant le parser ou le moteur d'import committe un rapport de benchmark Markdown à jour.

## Méthode de travail

1. **Zéro invention.** Ne jamais extrapoler une balise XML, une règle métier, un comportement UI ou un choix d'architecture non documenté. En cas d'ambiguïté (spec contradictoire, cas limite XML, gestion d'erreur), **s'arrêter et poser la question** avant de coder. Les specs de `docs/` se contredisent parfois : signaler la contradiction au lieu de choisir silencieusement.
2. **TDD.** Écrire d'abord le test (MSTest + FluentAssertions), le voir échouer, puis implémenter. Préférer les vrais XML de `assets/Results/` (ne pas les copier dans `tests/`) aux données synthétiques pour les règles métier.
3. **Après chaque modification de code :** `dotnet build` sans avertissement et tests verts avant d'annoncer que c'est terminé.
4. **Garder la doc synchronisée** : guide utilisateur (`README.md` et `docs/guide/`), `docs/DEVELOPMENT.md` (guide développeur), `docs/ARCHITECTURE.md` et `docs/ROADMAP.md` (cocher un jalon seulement quand il est réellement implémenté).
5. **Guide utilisateur multilingue** : `README.md` est une courte page d'accueil avec une section par langue ; le guide est dans `docs/guide/<langue>/` (`en`, `fr`, `de`, `it`, `es`), une page courte par sujet, avec les mêmes noms de fichiers dans chaque langue. Toute modification d'une page se reporte **dans les cinq langues** (le français sert de référence). Chaque page commence par le sélecteur de langue vers la même page ; chaque guide cite les libellés de l'écran dans sa langue, tels que les donnent les traductions de `Texts.resx`. `UserGuideTests` vérifie les pages, le sélecteur et les liens. Les autres documents de `docs/` restent en français.

## Conventions de code

- Commentaires XML `///` sur chaque type et membre public. Commentaires inline pour expliquer le **pourquoi** (raison métier ou technique), pas le comment.
- Style existant : constructeurs qui valident (`ArgumentNullException.ThrowIfNull`, …), entités immuables, `CultureInfo.InvariantCulture` pour parser et formater les temps, `StringComparison.OrdinalIgnoreCase` pour les noms de pilotes.
- Textes affichés : jamais en dur. Le dashboard et le code partagé de `Presentation` lisent `Presentation/Resources/Texts.resx` (français, langue de référence ; clés en anglais par page et rôle, `MyRaces.Title`) : `@T["Clé"]` et `@T["Clé", valeur]` dans les pages Razor, `Texts.Get`, `Texts.Format` et `Texts.Plural` (clés `.One` / `.Other`) en C#. Pas de texte dans un champ `static readonly` : il ne suivrait pas la langue. `TextsTests` vérifie que chaque clé utilisée existe, que chaque texte sert et que chaque format est valide. Toute modification d’un texte se reporte dans les quatre traductions (`Texts.en.resx`, `Texts.de.resx`, `Texts.it.resx`, `Texts.es.resx`) : `TextsTests` vérifie qu’elles ont les mêmes clés et les mêmes paramètres `{0}`. La langue s’applique par la culture d’interface seule (`UiLanguage.Apply`, `CurrentUICulture`) : les nombres gardent la culture de Windows. Les tests forcent le français. Proposition : `docs/propositions/interface-multilingue.md`.
- Logs via `Microsoft.Extensions.Logging` uniquement, messages structurés (`{SourceFile}`, jamais d'interpolation). Niveaux :
  - `Trace`/`Debug` : détails internes, boucles, valeurs brutes lues dans le XML.
  - `Information` : événements métier normaux (session importée, watcher démarré).
  - `Warning` : anomalie non bloquante (fichier verrouillé, nouvelle tentative).
  - `Error`/`Critical` : échec d'import, XML illisible, exception non gérée.
- Ne logger que le **nom** du fichier XML, jamais son chemin absolu (données personnelles).
- Logs fichier en **JSON Lines** (un objet par ligne).
- UI : respecter `docs/UI_DESIGN_SPEC.md` (palette, couleurs WEC violet/vert/jaune/rouge, badges de catégorie) et les maquettes `docs/UISpec/*.template.md`.
