# Rapport de charge — Import dynamique des résultats

## Scénario

Le test MSTest `ResultsDirectoryWatcherTests.StartAsync_imports_fifty_concurrently_created_result_files` démarre le watcher sur un dossier temporaire, puis y copie simultanément 50 fichiers XML réels de la fixture LMU. Il attend la fin de chaque import et vérifie que les 50 sessions sont présentes dans le dépôt, sans échec.

Le test inclut aussi des cas ciblés pour les fichiers verrouillés, les événements de renommage, les notifications répétées, les XML invalides et les fichiers déjà importés lors du chargement initial.

## Résultat mesuré

- Date : 2026-10-16
- Plateforme : Windows, .NET SDK 10.0.402
- Fichiers injectés : 50
- Imports réussis : 50
- Imports échoués : 0
- Durées d'injection et d'import observées sur deux exécutions : **645 ms** et **839 ms**
- Délai maximal du test avant échec : 30 secondes

Les durées correspondent à deux exécutions locales du test et ne sont pas une garantie de performance sur d'autres machines ou systèmes de fichiers.

## Reproduction

```powershell
dotnet test tests\LMU-DeltaRacing.Tests\LMU-DeltaRacing.Tests.csproj --filter "FullyQualifiedName~ResultsDirectoryWatcherTests" --logger "console;verbosity=detailed"
```
