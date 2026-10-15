# Feuille de route

Les jalons ci-dessous décrivent une progression possible du projet. Les idées restent à préciser et à prioriser selon les besoins.

# Feuille de Route

## Jalon — Socle métier et lecture XML [Terminé]
- [x] Définir les entités `Session`, `Driver`, `Lap` et `Sector`.
- [x] Ajouter les contrats du parser XML et du dépôt de sessions.
- [x] Implémenter le parser XML et le dépôt en mémoire.
- [x] Ajouter des tests ciblés à partir d'un fichier XML réel (MSTest).

## Jalon — Monitoring des perfs et console Spectre [Terminé]
- [x] Mettre en place OpenTelemetry pour instrumenter l'application.
- [x] Configurer l'export des logs au format JSON structuré et indenté (Human-Readable).
- [x] Ajouter une console de debug (Spectre.Console) pour suivre l'activité de l'application.
- [x] Créer un tableau de visualisation des imports dans la console.

## Jalon — Gestion de la configuration
- [ ] Configurer la gestion dynamique des paramètres applicatifs (`IOptions<T>` / `appsettings.json`)
- [ ] Permettre la configuration du chemin local du dossier de résultats LMU (`UserData/Log/Results/`)
- [ ] Configurer le niveau de log minimal par cible (Console / Fichier JSON) via la configuration
- [ ] Ajouter la validation des chemins de configuration au démarrage de l'application
- [ ] Injecter les configurations dans les services de l'Application et de l'Infrastructure
- [ ] Modifier/Sauvegarder les valeurs depuis la console spectre

## Jalon — Quality Gates & Benchmarks
- [ ] Configurer un projet de tests de charge / benchmarks pour valider le parsing en masse de fichiers XML.
- [ ] Automatiser la création d'un rapport de performance au format Markdown à joindre aux PR.

## Jalon — Import dynamique des résultats
- [ ] Configurer le chemin du dossier de résultats LMU (`UserData/Log/Results/`).
- [ ] Surveiller l'arrivée de nouveaux fichiers XML (`FileSystemWatcher`) avec gestion du verrouillage de fichier.
- [ ] Éviter les imports en double et gérer le registre des fichiers traités.
- [ ] Signaler les fichiers invalides sans interrompre le flux d'importation.
- [ ] Exécuter un test de charge sur la surveillance de dossier (ex: injection simultanée de 50 fichiers XML).

## Jalon — Première interface Blazor
- [ ] Mettre en place l'hébergement Blazor local.
- [ ] Afficher la liste des sessions et le classement des pilotes.
- [ ] Gérer les états de chargement, d'erreur et les listes vides.

## Jalon — Statistiques, Graphiques et Historique durable
- [ ] Vues comparatives des secteurs, meileurs tours et rythmes de course.
- [ ] Persistance durable des données (évaluation BDD SQLite).

## Liste d'idées à prioriser

- [ ] Exporter les résultats et statistiques en CSV.
- [ ] Ajouter une fiche pilote avec ses statistiques cumulées.
- [ ] Comparer plusieurs sessions sur un même circuit et une même voiture.
- [ ] Afficher les incidents et avertissements de limites de piste lorsqu'ils sont présents dans le XML.
- [ ] Permettre de choisir le dossier de résultats depuis l'interface.
- [ ] Ajouter un indicateur de progression et un résumé des derniers imports.
