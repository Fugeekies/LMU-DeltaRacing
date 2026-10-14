# Instructions du Projet - LMU-DeltaRacing

## 🎯 Vision du Projet
**LMU-DeltaRacing** est une application Desktop .NET 10 Blazor d'analyse de télémétrie, de statistiques et d'historique de courses pour le jeu *Le Mans Ultimate* (LMU).
- **Architecture :** Clean Architecture (`Domain`, `Application`, `Infrastructure`, `UI`, `Tests`).
- **Persistance initiale :** In-Memory uniquement (pas de BDD pour le moment).
- **Source de données :** Fichiers XML de résultats LMU situés en local (`UserData/Log/Results/`).

---

## 🛠️ Règles de Développement Strictes

1. **Test-Driven Development (TDD) Obligatoire**
   - Écris TOUJOURS les tests unitaires (`xUnit` + `FluentAssertions`) avant le code métier.
   - Aucun Use Case ou Service ne doit être écrit sans son test unitaire associé.
   - Utilise les fichiers XML réels présents dans `./tests/TestAssets/ResultsXML/` pour valider le parsing.

2. **Clean Architecture & Découpage**
   - `LMU-DeltaRacing.Domain` : Entités pures (Session, Lap, Driver, Sector...), aucune dépendance externe.
   - `LMU-DeltaRacing.Application` : Interfaces, Use Cases, DTOs.
   - `LMU-DeltaRacing.Infrastructure` : Implémentations du parser XML, FileSystemWatcher, Repositories In-Memory.
   - `LMU-DeltaRacing.UI` : Interface Blazor (Blazor Hybrid / Server local).

3. **Qualité & Conventions**
   - Activer le C# Nullable Context (`<Nullable>enable</Nullable>`).
   - Documenter toutes les classes et méthodes publiques avec des commentaires XML `///`.
   - Variables et méthodes en Anglais, documentation et commits en Français ou Anglais.

---

## 🚀 Commandes Utiles

- **Build de la solution :** `dotnet build`
- **Lancer tous les tests :** `dotnet test`
- **Lancer un test spécifique :** `dotnet test --filter "Name=NomDuTest"`
- **Lancer l'application UI :** `dotnet run --project src/LMU-DeltaRacing.UI/LMU-DeltaRacing.UI.csproj`

---

## 📂 Organisation de la Solution

- `src/LMU-DeltaRacing.Domain/`
- `src/LMU-DeltaRacing.Application/`
- `src/LMU-DeltaRacing.Infrastructure/`
- `ui/LMU-DeltaRacing.UI/`
- `tests/LMU-DeltaRacing.Tests/` 
- `tests/testAssets/Results/`
- `docs/` (`ARCHITECTURE.md`)