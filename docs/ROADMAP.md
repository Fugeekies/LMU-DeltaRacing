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
- [x] Configurer l'export des logs structurés en JSON Lines (un objet JSON par ligne).
- [x] Écrire les statistiques agrégées de mémoire et de durée/débit d'import dans un fichier dédié par exécution.
- [x] Ajouter une console de debug (Spectre.Console) pour suivre l'activité de l'application.
- [x] Afficher la progression du chargement des imports dans la console.

## Jalon — Gestion de la configuration
- [x] Configurer la gestion dynamique des paramètres applicatifs (`IOptionsMonitor<T>` / `appsettings.json`)
- [x] Permettre la configuration du chemin local du dossier de résultats LMU -> assets anonymisés par défaut
- [x] Configurer le chemin local du log -> fichier à côté de l'exécutable par défaut
- [x] Configurer le niveau minimal du fichier de log JSON via la configuration -> `Debug` par défaut (aucun log affiché dans la console)
- [x] Valider les chemins et le niveau de log au démarrage de l'application
- [x] Injecter les options dans les services concernés via le conteneur de dépendances
- [x] Modifier et sauvegarder les valeurs depuis le menu Spectre dans `appsettings.json`, à côté de l'exécutable

## Jalon — Quality Gates & Benchmarks
- [x] Configurer un projet MSTest dédié aux benchmarks pour valider le parsing en masse de fichiers XML.
- [x] Mesurer des lots réalistes de 10/100/500 fichiers générés localement (fichiers temporaires non commités).
- [x] Mesurer le parsing en mémoire des sessions Practice, Qualifying et Race : 10 courses par jour pendant 10 ans, soit 10 × 3 × 365 × 10 = 109 500 sessions.
- [x] Générer facilement un rapport de performance Markdown, consultable dans la sortie détaillée du test et dans le dépôt.
- [x] Permettre de réduire la charge à 5 ans ou 2 ans si les ressources mémoire sont limitées (`-p:BenchmarkYears=5` ou `-p:BenchmarkYears=2`).

## Jalon — Statistiques, Graphiques
- [x] Ajouter une vue statistique groupée par circuit.
- [x] Ajouter une vue statistique groupée par circuit et par voiture (classe + équipe).
- [x] Ajouter le delta du meilleur tour valide par rapport à la session précédente du même circuit, ordonnée par date.
- [x] Afficher des graphiques Spectre.Console des meilleurs tours valides les plus récents.
- [x] Permettre de choisir dans le menu de lancement les vues statistiques à afficher et exclure les sessions Practice des statistiques.

## Jalon — Import dynamique des résultats
- [x] Surveiller l'arrivée de nouveaux fichiers XML (`FileSystemWatcher`) avec gestion du verrouillage de fichier.
- [x] Éviter les imports en double et gérer le registre des fichiers traités en mémoire pendant l'exécution.
- [x] Signaler les fichiers invalides sans interrompre le flux d'importation.
- [x] Exécuter un test de charge sur la surveillance de dossier avec injection simultanée de 50 fichiers XML.
- [x] Générer le compte rendu du test de charge dans `docs/benchmarks/dynamic-import-load-report.md`.

## Jalon — Première interface Blazor
- [x] Mettre en place l'hébergement Blazor local, ouvert dans le navigateur par défaut et limité à `127.0.0.1`.
- [x] Afficher la liste des sessions importées.
- [x] Gérer les états de chargement, d'erreur et les listes vides.

## Jalon — Packaging, Déploiement & Installeur Autonome
- [x] Configurer le profil de publication .NET en `Self-Contained` et `Single-File` dans le projet UI.
- [x] Activer le `Trimming` pour réduire les dépendances distribuées.
- [x] Configurer un script **Inno Setup** pour générer l'installeur `LMU-DeltaRacing-Setup.exe`.
- [x] Détecter les dossiers de résultats LMU usuels à l'installation et sélectionner les fixtures si aucun résultat LMU n'est présent.
- [x] Anonymiser les noms des pilotes dans les fixtures livrées en conservant un alias cohérent entre les fichiers.
- [x] Ajouter un test automatisé vérifiant qu'aucune DLL externe n'est requise au lancement de l'exécutable publié.
- [x] Déclencher le build et la publication de l'installeur `.exe` depuis le workflow GitHub Actions lors de la création d'un tag de version.

## Jalon — UI : Shell & Layout LMU
- [x] Mettre en place le fond dégradé officiel LMU (`#0d1b2a` vers `#3a0d18`).
- [x] Créer la barre de navigation supérieure horizontale (`sticky top-0`) avec typographie condensée (`Oswald`).
- [x] Intégrer l'indicateur d'état du `FileSystemWatcher` dans le Header.
- [x] Appliquer le style des cartes sombres semi-transparentes avec effet de flou arrière-plan (`backdrop-filter: blur`).

## Jalon — UI : Menu "Options & Paramètres" (`Settings.razor`)
- [x] Implémenter la vue de configuration du chemin des résultats XML (`UserData/Log/Results`).
- [x] Ajouter le bouton de parcours d'explorer et l'indicateur visuel de validité du dossier.
- [x] Permettre de tester et recharger la surveillance `FileSystemWatcher` depuis l'interface.
- [x] Déplacer le dashboard avec les imports réussi/échec ainsi que les session récente en dessous des options.

## Jalon — UI : Menu "Mes Courses" (`MyRaces.razor`)
- [x] Structurer la vue avec groupement par circuit.
- [x] Ajouter la colonne `Catégorie` (Hypercar, LMP2, LMGT3) et les filtres associés (Circuit et Catégorie).
- [x] Implémenter le calcul dynamique du delta $n-1$ (session précédente du même circuit et de la même catégorie).
- [x] Appliquer la coloration conditionnelle des deltas (🟢 gain de temps, 🔴 perte de temps).

## Jalon — Déterminer l'utilisateur principal
- [x] Détecter le pilote le plus fréquent dans les fichiers de résultats et enregistrer son nom dans `LMU:UserName`.
- [x] Au premier démarrage, attendre la fin du chargement des données avant de détecter le pilote si aucun nom n'est configuré.
- [x] Afficher le nom du pilote sélectionné dans la barre de navigation.
- [x] Basculer les vues de résultats sur le pilote sélectionné.
- [x] Ignorer les fichiers qui ne contiennent pas le pilote sélectionné.
- [x] Permettre de sélectionner un pilote dans Options, trié par pourcentage d'apparition global, et enregistrer ce choix dans `appsettings.json`.

## Jalon — UI : Menu "Classement des Coureurs" (`DriversRanking.razor`)
- [x] Implémenter le service d'agrégation d'historique Head-to-Head (H2H).
- [x] Afficher le tableau avec les colonnes : `Nom`, `Courses affrontées`, `Victoires`, `Défaites`, `% Victoire`.
- [x] Ajouter la barre de recherche par nom et le tri dynamique (par défaut : volume d'affrontements décroissant).

## Jalon — UI : Menu "Détail des Courses" (`RaceDetails.razor`)
- [x] Intégrer le sélecteur de session (dropdown) en haut de page.
- [x] Afficher le tableau de classement complet de la course sélectionnée avec les temps par secteur.
- [x] Appliquer la coloration WEC pour les secteurs (🟣 Best absolu, 🟢 Best perso, 🟡 Standard) et calculer les écarts au meilleur secteur et au meilleur tour de la session.

## Jalon — Écarts UI à traiter
Écarts relevés lors de la resynchronisation des specs `docs/UISpec/` avec le code. Chaque point est à arbitrer (règle métier) avant correction.

### Mes courses (`MyRaces.razor`)
- [ ] Ne plus fusionner les courses d'un même jour sur un même circuit : définir l'unité d'une ligne (session, paire qualif/course…). Sur les fixtures, 87 courses du pilote principal donnent au plus 37 lignes.
- [ ] Regrouper par date locale et non UTC (une course vers minuit peut changer de jour).
- [ ] Prendre en charge les classes `GTE` et `LMP3` présentes dans les résultats LMU, aujourd'hui exclues sans message (environ 41 % des sessions du pilote principal des fixtures).
- [ ] Colonne TOURS : afficher les tours du pilote et non `MostLapsCompleted` (tours du vainqueur).
- [ ] Rendre déterministes la catégorie et la voiture d'une ligne quand le pilote change de voiture.
- [ ] Définir ce qu'est un « tour valide » (limites de piste) pour le calcul du meilleur tour.
- [ ] Trancher la règle du delta n-1 : même véhicule ou non (`UI_DESIGN_SPEC.md` et `MyRaces.template.md` se contredisent ; le code ignore le véhicule).

### Classement des coureurs (`DriversRanking.razor`)
- [ ] Exclure les qualifications du décompte des « courses affrontées », ou les signaler distinctement.
- [ ] Comparer les positions par classe en session multiclasse.
- [ ] Définir le traitement des positions égales (comptées comme affrontées sans victoire ni défaite).
- [ ] Ajouter la barre visuelle victoires/défaites (🟩🟥) de la maquette d'origine, ou la retirer définitivement de la spec.

### Détail des courses (`RaceDetails.razor`)
- [ ] Ajouter la colonne « DIFF 1ER » (écart au leader) prévue par la maquette d'origine, ou acter son remplacement par l'écart au meilleur tour.
- [ ] Calculer les meilleurs secteurs et tours par classe en session multiclasse (règle WEC).
- [ ] Décider s'il faut afficher les meilleurs secteurs réalisés hors du meilleur tour (aujourd'hui, seuls les secteurs du meilleur tour sont affichés).
- [ ] Afficher le temps au tour au format `m:ss.fff`, comme dans « Mes courses ».
- [ ] Virtualiser les grandes grilles (> 40 voitures), comme demandé par `UI_DESIGN_SPEC.md`.

### Options (`Settings.razor`)
- [ ] Simplifier le changement de dossier : « Tester & recharger » n'enregistre pas et « Enregistrer » ne recharge pas.
- [ ] Afficher en heure locale les dates du tableau des sessions récentes (aujourd'hui en UTC).
- [ ] Renvoyer vers la page Options plutôt que vers `appsettings.json` dans l'encadré d'erreur de l'état des imports.

## Liste d'idées à prioriser
- [ ] Multi langue.
- [ ] Reprise du readme pour la doc d'utilisation 
- [ ] Exporter les résultats et statistiques en CSV.
- [ ] Ajouter une fiche pilote avec ses statistiques cumulées.
- [ ] Comparer plusieurs sessions sur un même circuit et une même voiture.
- [ ] Afficher les incidents et avertissements de limites de piste lorsqu'ils sont présents dans le XML.
- [x] Permettre de choisir le dossier de résultats depuis l'interface (page Options).
- [x] Ajouter un indicateur de progression et un résumé des derniers imports (page Options).
