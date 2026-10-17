# Changelog - LMU-DeltaRacing

Toutes les modifications notables apportées au projet **LMU-DeltaRacing** sont documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

---

## [Unreleased]

### Added
- Ajout d'une page Options pour choisir, valider, enregistrer et recharger le dossier des résultats, avec sélecteur de dossiers Windows natif et préférence de démarrage du watcher.
- Déplacement de l'état d'import, des erreurs et de l'historique du dashboard sous les options, sans tableau de classement des pilotes.
- Ajout du shell LMU avec navigation horizontale supérieure, fond dégradé, surfaces sombres translucides et état du watcher en direct.
- Ajout dans la console Spectre de vues statistiques par circuit et par classe/équipe, avec deltas chronologiques des meilleurs tours valides et graphiques des mesures récentes.
- Ajout au menu de lancement du choix des tableaux statistiques à afficher et exclusion des sessions Practice des statistiques.
- Surveillance dynamique du dossier des résultats, déduplication des chemins pendant l'exécution, notification des imports et échecs, et retries sur les fichiers verrouillés.
- Ajout d'un dashboard Blazor local avec historique des sessions, classement des pilotes, progression du chargement et actualisation en direct des imports.
- Ajout d'un profil de publication Windows x64 autonome pour lancer le dashboard sans installer le runtime .NET.
- Ajout d'un installeur Windows Inno Setup, de fixtures de démonstration anonymisées et d'un test de démarrage de l'application publiée sans DLL externes.

### Changed
- Import automatique des résultats au démarrage sans demander de confirmation; le choix d'une vue statistique l'affiche immédiatement au lieu de revenir au menu.
- Affichage d'un titre dès le démarrage et remplacement du tableau des imports par une barre de progression et un résumé des résultats.
- Publication du dashboard en exécutable autonome, fichier unique et élagué; l'installeur sélectionne les résultats LMU présents ou les fixtures anonymisées et le workflow de release est déclenché par tag.
- Centralisation du jeu XML anonymisé dans `assets/Results`, partagé par les tests, l'application et la publication, avec suppression des copies brutes sous `tests/testAssets`.

### Performance
- Test de charge de l'import dynamique sur 50 fichiers XML injectés simultanément; résultats consignés dans `docs/benchmarks/dynamic-import-load-report.md`.

### 🏗️ Architecture & Core (Clean Architecture)
- **Domain Layer (`src/LMU-DeltaRacing.Domain/`) :**
  - Création des entités métiers pures : `Session`, `Driver`, `Lap`, et `Sector`.
  - Isolation totale du modèle domaine vis-à-vis des dépendances externes et du format XML.
- **Application Layer (`src/LMU-DeltaRacing.Application/`) :**
  - Définition des contrats d'interfaces `IXmlParserService` et `ISessionRepository`.
  - Intégration des abstractions de métriques et de traçabilité via OpenTelemetry (`System.Diagnostics.ActivitySource` & `Meter`).
- **Infrastructure Layer (`src/LMU-DeltaRacing.Infrastructure/`) :**
  - Implémentation du parser XML réactif consommant des `Stream` pour l'indépendance du File System.
  - Implémentation du dépôt In-Memory (`InMemorySessionRepository`) thread-safe pour le stockage temporaire des sessions.

### 📊 Observabilité, Logging & Diagnostic
- **Formatage des logs :** Configuration de l'export des journaux sur disque sous forme de fichiers **JSON structurés et indentés (Human-Readable JSON)** dans `lmu-deltaracing.log`.
- **OpenTelemetry :**
  - Envoi des traces et métriques d'exécution vers la sortie Debug.
  - Anonymisation automatique des chemins absolus locaux dans les traces (seul le nom de fichier XML est conservé).
- **Console Debug (`src/LMU-DeltaRacing.Console/`) :**
  - Application console basée sur `Spectre.Console` pour la visualisation immédiate des imports XML.
  - Sélection automatique du dossier de test (`tests/testAssets/Results`) en mode Debug ou via argument CLI.
  - Tableau récapitulatif interactif affichant le statut de l'import, le circuit, la catégorie et le nombre de pilotes.

### 🧪 Quality Assurance & Tests
- **Framework de Test :** Migration et alignement strict sur **MSTest** et **FluentAssertions**.
- **Dataset de Test :** Intégration d'échantillons XML réels du jeu Le Mans Ultimate dans `tests/testAssets/Results/`.
- **Tests d'intégration :** Couverture du parsing des sessions, de l'extraction des meilleurs tours/secteurs et de la persistance en mémoire.

### 📚 Documentation & Guidelines
- **Spécifications UI (`docs/UI_DESIGN_SPEC.md`) :** Création de la charte graphique officielle *Dark Racing WEC/LMU* (palette `#0e1116`, codes couleurs WEC pour les deltas secteur : Violet `#b066ff`, Vert `#22c55e`, Jaune `#eab308`, Rouge `#ef4444`).
- **Instructions Copilot (`.github/copilot-instructions.md`) :**
  - Imposition du workflow Git à base de Pull Requests (interdiction stricte du push direct sur `main`).
  - Règle "Zéro Invention" obligeant l'agent à poser des questions techniques/fonctionnelles en cas d'ambiguïté.
  - Exigence de commentaires XML abondants et de logs granulaires (`Trace` à `Critical`).
  - Maintien systématique du `CHANGELOG.md` par PR.
- **Feuille de Route (`docs/FeuilleDeRoute.md`) :** Découpage en micro-jalons atomiques (Configuration `appsettings.json`, File Locking, FileWatcher, Benchmarking, UI Blazor).
- **Spécifications d'Agent (`AGENTS.md`) & Architecture (`docs/ARCHITECTURE.md`) :** Mise à jour des documents de cadrage technique.

---

## [0.1.0-alpha] - 2026-10-16

### Added
- Initialisation de la solution .NET 10 `LMU-DeltaRacing.sln`.
- Structure initiale des projets Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Console`, `UI`, `Tests`).
