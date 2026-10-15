# Instructions du Projet - LMU-DeltaRacing

## 🎯 Vision du Projet
**LMU-DeltaRacing** est une application Desktop .NET 10 Blazor d'analyse de télémétrie, de statistiques et d'historique de courses pour le jeu *Le Mans Ultimate* (LMU).
- **Architecture :** Clean Architecture (`Domain`, `Application`, `Infrastructure`, `UI`, `Console`, `Tests`).
- **Persistance initiale :** In-Memory uniquement (pas de BDD pour le moment).
- **Source de données :** Fichiers XML de résultats LMU situés en local (`UserData/Log/Results/`).

---

## 🛡️ Règles Git & Workflow Stricte

1. **INTERDICTION DE PUSH SUR `main` :**
   - Tout changement doit impérativement passer par une **Pull Request (PR)** depuis une branche dédiée (ex: `feature/nom-feature`, `fix/nom-bug`).
   - Ne jamais commiter directement sur la branche principale `main`.

2. **Test-Driven Development (TDD) Obligatoire :**
   - Écris TOUJOURS les tests unitaires / d'intégration (`MSTest` + `FluentAssertions`) avant le code métier.
   - Utilise les fichiers XML réels dans `tests/testAssets/Results/` pour valider le parsing.

3. **Logging & Observabilité (OpenTelemetry & JSON) :**
   - Tous les logs applicatifs doivent être émis via les abstractions `Microsoft.Extensions.Logging` / OpenTelemetry.
   - Les logs sur fichier doivent être sérialisés au format **JSON structuré et indenté (Human-Readable JSON)** pour faciliter la lecture humaine et l'analyse automatique.

4. **Tests de Charge & Performance :**
   - Pour chaque composant critique (parser XML, file watcher, repository in-memory), prévoir des tests de charge (stress tests de parsing / volume).
   - Les comptes rendus et résultats de benchmarks doivent être générés au format Markdown et **commités dans la PR** associée.

---

## 📂 Organisation de la Solution

- `src/LMU-DeltaRacing.Domain/`
- `src/LMU-DeltaRacing.Application/`
- `src/LMU-DeltaRacing.Infrastructure/`
- `src/LMU-DeltaRacing.Console/`
- `ui/LMU-DeltaRacing.UI/`
- `tests/LMU-DeltaRacing.Tests/` 
- `tests/testAssets/Results/`
- `docs/` (`ARCHITECTURE.md`, `ROADMAP.md`, `benchmarks/`)
