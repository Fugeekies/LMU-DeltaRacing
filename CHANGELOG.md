# Changelog - LMU-DeltaRacing

Toutes les modifications notables apportées au projet **LMU-DeltaRacing** sont documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

---

## [Unreleased]

### Added
- Nouvelle page **Progression** (`/progression`), après « Mes courses » : graphique SVG de l'évolution des meilleurs tours du pilote sur un circuit et dans une catégorie, regroupés par jour, semaine ISO, mois ou année (valeur tracée : min, moyenne ou max, avec bande min-max). Courbes Course et Qualification, ou une courbe par voiture ; les périodes sans session sont traversées en trait plein et signalées sur l'axe des dates ; ligne du record, résumé chiffré (record, début, aujourd'hui, écart), panneau de détail au survol, légende cliquable et tableau des périodes. Réglages enregistrés dans `LMU:Progression`.
- Mes courses : icône 📈 dans l'en-tête de chaque circuit, qui ouvre la Progression sur ce circuit et sa dernière catégorie roulée.
- Nouvelle page **Accueil** (`/`), premier onglet : tableau de bord de la carrière du pilote en tuiles (pilote avec ses dates de première et dernière course et ses favoris, forme récente, palmarès, qualification & départ, abandons et leurs causes, roulage), chaque total étant détaillé par catégorie (barre colorée et légende). Les résultats comptent la position en classe officielle ; une course non classée (`None`) compte dans le total mais pas dans les résultats.
- Parser : lecture de la position de départ en classe (`ClassGridPos`), de la cause d'abandon (`DNFReason`) et de la longueur du circuit (`TrackLength`).
- Détail des courses : le paramètre `?event=` ouvre directement un événement (lien « Voir l'événement » de l'Accueil).
- README réécrit pour les utilisateurs : installation, premier lancement, pages, options et questions fréquentes. La documentation technique de l'ancien README est déplacée dans `docs/DEVELOPMENT.md`.
- Options : bloc **Fusion de pilotes** pour regrouper deux noms d'un même pilote (changement de pseudo) sous le nom conservé, en mémoire et sans modifier les fichiers de résultats (`LMU:DriverMerges`). Un avertissement signale les pilotes qui ont participé à une même session ; chaque fusion peut être annulée.
- Ajout d'une page Options pour choisir, valider, enregistrer et recharger le dossier des résultats, avec sélecteur de dossiers Windows natif et préférence de démarrage du watcher.
- Déplacement de l'état d'import, des erreurs et de l'historique du dashboard sous les options, sans tableau de classement des pilotes.
- Ajout du shell LMU avec navigation horizontale supérieure, fond dégradé, surfaces sombres translucides et état du watcher en direct.
- Ajout dans la console Spectre de vues statistiques par circuit et par classe/équipe, avec deltas chronologiques des meilleurs tours valides et graphiques des mesures récentes.
- Ajout au menu de lancement du choix des tableaux statistiques à afficher et exclusion des sessions Practice des statistiques.
- Surveillance dynamique du dossier des résultats, déduplication des chemins pendant l'exécution, notification des imports et échecs, et retries sur les fichiers verrouillés.
- Ajout d'un dashboard Blazor local avec historique des sessions, classement des pilotes, progression du chargement et actualisation en direct des imports.
- Ajout d'un profil de publication Windows x64 autonome pour lancer le dashboard sans installer le runtime .NET.
- Ajout d'un installeur Windows Inno Setup, de fixtures de démonstration anonymisées et d'un test de démarrage de l'application publiée sans DLL externes.
- Ajout de `AGENTS.md`, source unique des instructions pour GitHub Copilot et Claude Code (via l'import `@AGENTS.md` de `CLAUDE.md`) : commandes, architecture, faits vérifiés sur le format XML LMU et règles de contribution.

### Fixed
- Remplacement de `.github/copilot-instruction.md`, ignoré par Copilot à cause de son nom, par `AGENTS.md` ; correction du chemin du projet UI et de l'organisation de la solution.
- Renommage de `CHANGELOG.MD` en `CHANGELOG.md`, nom attendu par les instructions de contribution.
- Correction des références documentaires vers des fichiers inexistants : maquettes `docs/UISpec/*.template.md`, rapport `performance-report.md`, script `Test-PackageAssets.ps1` supprimé, `IResultXmlParser`, `docs/ROADMAP.md` et `LMU-DeltaRacing.slnx`.
- Mes courses : plusieurs courses disputées le même jour sur un même circuit ne sont plus fusionnées en une seule ligne ; chaque course est associée à la qualification qui la précède (sur les fixtures : 89 lignes au lieu de 37).
- Mes courses : les classes LMP3 et GTE (LMGTE) ne sont plus masquées, et une classe inconnue reste affichée sous son nom brut.
- Mes courses : la colonne TOURS affiche les tours du pilote et non ceux du vainqueur.
- Classement des coureurs : seules les courses comptent, et uniquement contre les pilotes de la même classe.
- Détail des courses : les couleurs jaune et violette des secteurs s'affichent réellement (elles étaient écrasées par la couleur du texte).
- Dates de l'état des imports affichées en heure locale au lieu de l'heure UTC.

### Changed
- L'application s'ouvre sur l'Accueil : « Mes courses » ne répond plus que sur `/my-races` et le logo renvoie vers `/`. L'ancienne page `/dashboard` est retirée, l'état des imports restant affiché dans Options.
- Détail des courses : tableau compacté de 14 à 8 colonnes, sans défilement horizontal à partir de 1 280 px — chaque temps porte son écart en dessous, la voiture passe sous le nom du pilote et la catégorie sous la position ; la ligne du pilote sélectionné est surlignée. Le tour idéal affiche aussi son écart au meilleur tour idéal de la catégorie, et la colonne DIFF 1ER devient TEMPS TOTAL (temps de course complet, écart au premier en dessous).
- Détail des courses : le sélecteur propose des **événements** (essais, qualification et course disputés à la suite sur un circuit) et des sous-onglets **Course**, **Qualification** et **Essais** affichent le classement de chaque session ; plusieurs séances d'essais d'un même événement sont fusionnées en un seul classement.
- Options : nouvelle case « Charger les sessions d'essais » (`LMU:LoadPracticeSessions`, décochée par défaut) ; la cocher ou la décocher enregistre le choix et recharge les résultats. Les essais chargés ne s'affichent que dans l'onglet Essais du détail des courses ; la console continue de les ignorer.
- Fixtures : alias lisibles à prénom (`Driver Tom`, `Driver Lucas`…) au lieu de `Driver NNNN`. Le pilote principal devient `Driver Tom`.
- Fixtures : les pilotes n'ayant fait qu'une course sont fusionnés avec des pilotes qu'ils n'ont jamais croisés (`Prepare-PackageAssets.ps1 -MergeSingleRaceDrivers`) ; 5 599 noms donnent 239 alias, dont 237 avec au moins 5 courses, ce qui enrichit le classement des coureurs. Le pilote principal n'est jamais fusionné, et aucune session ne contient deux fois le même alias.
- Détail des courses : meilleurs secteurs personnels sur les tours chronométrés, colonne TOUR IDÉAL, comparaisons par classe (règle WEC), position en classe, colonne DIFF 1ER (écart au leader de la classe, tours de retard, DNF/DQ), temps au tour au format `m:ss.fff`, virtualisation au-delà de 40 pilotes.
- Classement des coureurs : barre visuelle victoires/défaites dans la colonne % de victoire.
- Options : un bouton unique « Appliquer » valide, enregistre et recharge le dossier des résultats ; la préférence de démarrage est enregistrée dès qu'on la coche.
- Le parser lit la position en classe, le nombre de tours et le statut d'arrivée de chaque pilote.
- Specs `docs/UISpec/`, `UI_DESIGN_SPEC.md` et ROADMAP mises à jour selon les règles arbitrées (jalon « Écarts UI à traiter »).
- Import automatique des résultats au démarrage sans demander de confirmation; le choix d'une vue statistique l'affiche immédiatement au lieu de revenir au menu.
- Affichage d'un titre dès le démarrage et remplacement du tableau des imports par une barre de progression et un résumé des résultats.
- Publication du dashboard en exécutable autonome, fichier unique et élagué; l'installeur sélectionne les résultats LMU présents ou les fixtures anonymisées et le workflow de release est déclenché par tag.
- Centralisation du jeu XML anonymisé dans `assets/Results`, partagé par les tests, l'application et la publication, avec suppression des copies brutes sous `tests/testAssets`.

### Performance
- Test de charge de l'import dynamique sur 50 fichiers XML injectés simultanément; résultats consignés dans `docs/benchmarks/dynamic-import-load-report.md`.

### Security
- Anonymisation complète des fixtures `assets/Results` : les noms réels restaient dans les changements de pilote (`Swap`), l'attribut `Driver` des événements (`TrackLimits`, `Penalty`) et les textes du `Stream` (contacts, pénalités, scoring, secteurs, chat). `Prepare-PackageAssets.ps1` remplace désormais tous ces noms par les mêmes alias en **conservant tous les événements de course** ; seul le contenu des messages de chat est retiré (l'alias de l'expéditeur est gardé). Le script vérifie qu'aucun nom réel ne subsiste et peut être rejoué sur des fixtures déjà anonymisées sans changer les alias existants.

### 🏗️ Architecture & Core (Clean Architecture)
- **Domain Layer (`src/LMU-DeltaRacing.Domain/`) :**
  - Création des entités métiers pures : `Session`, `Driver`, `Lap`, et `Sector`.
  - Isolation totale du modèle domaine vis-à-vis des dépendances externes et du format XML.
- **Application Layer (`src/LMU-DeltaRacing.Application/`) :**
  - Définition des contrats d'interfaces `IResultXmlParser` et `ISessionRepository`.
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
- **Feuille de Route (`docs/ROADMAP.md`) :** Découpage en micro-jalons atomiques (Configuration `appsettings.json`, File Locking, FileWatcher, Benchmarking, UI Blazor).
- **Architecture (`docs/ARCHITECTURE.md`) :** Mise à jour des documents de cadrage technique.

---

## [0.1.0-alpha] - 2026-10-16

### Added
- Initialisation de la solution .NET 10 `LMU-DeltaRacing.slnx`.
- Structure initiale des projets Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Console`, `UI`, `Tests`).
