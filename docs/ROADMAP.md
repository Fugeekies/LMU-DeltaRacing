# Feuille de route

Les jalons ci-dessous décrivent une progression possible du projet. Les idées restent à préciser et à prioriser selon les besoins.

## Jalon — Socle métier et lecture XML

Le premier flux est en place : un fichier de résultats LMU peut être transformé en entités métier, puis stocké en mémoire.

- [x] Définir les entités `Session`, `Driver`, `Lap` et `Sector`.
- [x] Ajouter les contrats du parser XML et du dépôt de sessions.
- [x] Implémenter le parser XML et le dépôt en mémoire.
- [x] Ajouter des tests ciblés à partir d'un fichier XML réel.

## Jalon — Monitoring des perfs et console spectre
- [ ] Mettre en place OpenTelemetry pour instrumenter l'application.
- [ ] Ajouter une console de debug pour suivre l'activité de l'application.
- [ ] Afficher dans la console les logs et événements OpenTelemetry.
- [ ] Créer une mini-interface sous forme de tableau pour visualiser les sessions et l'état des imports.

 
## Jalon — Import des résultats

Automatiser la découverte et l'import des fichiers de résultats présents sur la machine, avec une première interface et des outils de diagnostic.

- [ ] Configurer le chemin du dossier de résultats LMU (`UserData/Log/Results/`).
- [ ] Surveiller l'arrivée de nouveaux fichiers XML et attendre la fin de leur écriture avant l'import.
- [ ] Éviter les imports en double et permettre de relancer l'import d'un fichier.
- [ ] Signaler les fichiers invalides sans interrompre le traitement des autres résultats.
- [ ] Ajouter des tests d'intégration du flux d'import.

## Jalon — Première interface

Présenter l'historique des sessions et leurs résultats dans une interface locale.

- [ ] Choisir et mettre en place l'hébergement Blazor local (Hybrid ou Server).
- [ ] Afficher la liste des sessions importées.
- [ ] Afficher le détail d'une session et le classement des pilotes.
- [ ] Ajouter les états de chargement, les listes vides et les erreurs d'import.

## Jalon — Statistiques et analyse

Construire des vues d'analyse à partir des sessions importées.

- [ ] Comparer les temps au tour et les secteurs entre pilotes.
- [ ] Mettre en évidence les meilleurs tours et les évolutions de rythme.
- [ ] Filtrer l'historique par circuit, tracé, voiture, classe et pilote.
- [ ] Ajouter des vues de progression par pilote et par session.

## Jalon — Historique durable et finition

Fiabiliser l'usage au quotidien et préparer une première version distribuable.

- [ ] Évaluer un stockage persistant pour conserver l'historique entre les lancements.
- [ ] Prévoir une stratégie de migration et de sauvegarde des données si une base est retenue.
- [ ] Ajouter des journaux utiles pour diagnostiquer les erreurs de lecture et d'import.
- [ ] Vérifier la gestion de gros historiques et le démarrage après interruption.
- [ ] Documenter l'installation, la configuration et les limites connues.

## Liste d'idées à prioriser

- [ ] Exporter les résultats et statistiques en CSV.
- [ ] Ajouter une fiche pilote avec ses statistiques cumulées.
- [ ] Comparer plusieurs sessions sur un même circuit et une même voiture.
- [ ] Afficher les incidents et avertissements de limites de piste lorsqu'ils sont présents dans le XML.
- [ ] Permettre de choisir le dossier de résultats depuis l'interface.
- [ ] Ajouter un indicateur de progression et un résumé des derniers imports.
