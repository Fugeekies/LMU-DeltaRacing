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
   - Les logs sur fichier doivent être sérialisés en **JSON Lines** : un objet JSON structuré par ligne, sans tableau JSON englobant. Le seuil minimal du fichier est configurable.
   - Ne pas afficher de logs dans la console interactive ; réserver le terminal au menu et aux tableaux Spectre.Console.
   - Les statistiques agrégées d'OpenTelemetry doivent être écrites séparément dans `lmu-deltaracing-stats.json`, en JSON indenté lisible. Les fichiers sont conservés après l'arrêt et remplacés au lancement suivant.
   - Les paramètres applicatifs sont configurés via `IOptionsMonitor<T>` et `appsettings.json` à côté de l'exécutable ; les changements faits dans le menu doivent être validés, sauvegardés et rechargés.

4. **Tests de Charge & Performance :**
   - Pour chaque composant critique (parser XML, file watcher, repository in-memory), prévoir des tests de charge (stress tests de parsing / volume).
   - Les comptes rendus et résultats de benchmarks doivent être générés au format Markdown et **commités dans la PR** associée.

---

## ⚠️ Directives de Comportement & Code

1. **Zéro Invention & Clarifications Obligatoires :**
   - **Règle absolue :** Ne JAMAIS inventer ou extrapoler des structures de données, des balises XML, des hypothèses métier ou des choix d'architecture non documentés.
   - Si un détail technique ou fonctionnel est ambigu, flou ou manquant (ex: cas limite dans le XML, comportement UI, gestion d'une erreur), tu DOIS t'arrêter et poser des questions explicites (aussi bien techniques que fonctionnelles) avant de coder.

2. **Commentaires Abondants :**
   - Commente abondamment le code pour expliquer le **POURQUOI** (la raison métier ou technique) et pas seulement le "comment".
   - Fournis des commentaires XML (`///`) exhaustifs sur chaque classe, interface, propriété et méthode publique.

3. **Logging Élevé & Niveaux de Log Granulaires :**
   - Abonde en logs à chaque étape clé du traitement (début/fin de parsing, accès fichier, événements watcher, ajouts en mémoire).
   - Utilise strictement et intelligemment les niveaux de log appropriés :
     - `Trace` / `Debug` : Détails internes, étapes de boucles, structures brutes lues dans le XML.
     - `Information` : Événements métier normaux (ex: *"Session 'Spa 24h' importée avec succès - 28 pilotes"*).
     - `Warning` : Situations anomales mais non bloquantes (ex: *"Pilote sans temps au tour valide dans le fichier"*, *"Fichier temporairement verrouillé, nouvelle tentative..."*).
     - `Error` / `Critical` : Échecs d'import, fichiers XML corrompus ou illisibles, exceptions non capturées.
    
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
