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

## Jalon — Écarts UI à traiter [Terminé]
Écarts relevés lors de la resynchronisation des specs `docs/UISpec/` avec le code. Les règles métier ont été arbitrées le 25/10/2026. Mettre à jour la spec `docs/UISpec/` concernée en même temps que chaque correction.

### Prérequis : données
- [x] **Vie privée des fixtures :** noms anonymisés avec les mêmes alias dans `Name`, `Swap`, l'attribut `Driver` des événements et les textes du `Stream` ; tous les événements de course sont conservés, seul le contenu du chat est retiré. Fixtures régénérées en place (PR #28). Les résultats LMU d'origine n'étant pas disponibles, les alias utilisés dans les événements des fixtures actuelles ne correspondent pas à ceux des `<Driver>`.
- [x] **Parser :** lire `ClassPosition`, `Laps` (tours du pilote) et `FinishStatus`.
- [x] **Tours invalides :** vérification faite sur les fixtures. LMU efface déjà le temps (`--.----`) des tours visés par « Invalid Lap Cut Track » (35/35 cas vérifiés ; l'attribut `Lap` de l'événement est en base 0). Cet événement n'apparaît qu'en essais et en qualification. Règle retenue : un tour valide est un tour chronométré ; les secteurs d'un tour sans temps sont exclus des meilleurs secteurs.

### Mes courses (`MyRaces.razor`)
- [x] **Une ligne = une paire qualif + course.** Chaque course est associée à la dernière qualification du même circuit (lieu + tracé + événement) qui la précède, sans autre course entre les deux. Une qualif sans course, ou une course sans qualif, donne une ligne partielle. Fin de la fusion par jour : sur les fixtures, les 87 courses du pilote principal donnaient 37 lignes, elles en donnent maintenant 89 (87 courses + 2 qualifs isolées).
- [x] Catégorie et voiture d'une ligne : celles de la course, à défaut celles de la qualif.
- [x] **Catégories :** Hypercar, LMP2, LMP3, LMGTE, LMGT3, chacune avec son badge et son filtre. Une classe inconnue reste affichée sous son nom brut au lieu d'être exclue.
- [x] **Delta n-1 :** inchangé (même lieu + même tracé + même catégorie, quelle que soit la voiture). Corriger `UI_DESIGN_SPEC.md`, qui exige à tort une voiture identique.
- [x] Colonne TOURS : tours effectués par le pilote (`Laps`) et non `MostLapsCompleted`.
- [x] Afficher toutes les dates en heure locale.

### Classement des coureurs (`DriversRanking.razor`)
- [x] **Courses uniquement :** seules les sessions Race comptent dans le H2H.
- [x] **Par classe :** en session multiclasse, seuls les adversaires de la même classe que le pilote comptent, comparés sur `ClassPosition`.
- [x] Ajouter la barre visuelle victoires/défaites (vert/rouge, proportionnelle) dans la colonne % de victoire.
- [x] Positions égales : sans objet, deux pilotes d'une même session n'ont jamais la même position.

### Détail des courses (`RaceDetails.razor`)
- [x] **Multiclasse :** meilleurs secteurs et meilleur tour calculés par classe (règle WEC). Afficher la position en classe.
- [x] **Meilleurs secteurs individuels :** chaque colonne secteur affiche le meilleur secteur du pilote, tous tours valides confondus. Une colonne « TOUR IDÉAL » affiche leur somme.
- [x] **Couleurs des secteurs :** violet = meilleur secteur de la classe, jaune sinon. Le vert n'est plus utilisé pour les secteurs ; il reste utilisé pour les deltas.
- [x] **Ajouter la colonne DIFF 1ER** en conservant les deltas actuels :
  - en course : écart de temps total (`FinishTime`) au premier de la classe, ou « +N tours » si le pilote a au moins un tour de retard ;
  - en qualif : écart au meilleur tour de la classe.
- [x] Afficher les temps au tour au format `m:ss.fff`, comme dans « Mes courses ».
- [x] Virtualiser les grandes grilles (> 40 voitures), comme demandé par `UI_DESIGN_SPEC.md`. Jusqu'à 40 voitures, les lignes sont rendues normalement (et pré-rendues). Le rendu virtualisé reste à vérifier dans un navigateur, par exemple sur la course du 16/09/2026 au Mans (54 voitures).

### Options (`Settings.razor`)
- [x] **Bouton unique « Appliquer » :** il valide le dossier, enregistre `ResultsDirectory`, puis recharge les résultats et le watcher. Il remplace « Tester la connexion & recharger » et « Enregistrer ».
- [x] La case « démarrage automatique de la surveillance » est enregistrée dès qu'on la coche, comme le pilote principal.
- [x] Afficher en heure locale les dates du tableau des sessions récentes (aujourd'hui en UTC).
- [x] Renvoyer vers la page Options plutôt que vers `appsettings.json` dans l'encadré d'erreur de l'état des imports.

## Jalon — Détail des courses par événement
- [x] Regrouper les sessions en événements (essais → qualification → course sur un même circuit, à moins de 2 heures d'écart), séances d'essais fusionnées en un seul classement.
- [x] Sélecteur d'événement et sous-onglets **Course**, **Qualification**, **Essais** sous le sélecteur (Course par défaut).
- [x] Option « Charger les sessions d'essais » dans Options (`LMU:LoadPracticeSessions`, décochée par défaut, rechargement immédiat) ; la console ignore toujours les essais. Ils n'apparaissent que dans l'onglet Essais.
- [ ] Optimisation : quand l'option est décochée, les fichiers d'essais sont encore lus et analysés avant d'être ignorés. Lire seulement l'en-tête du XML (type de session) permettrait de les écarter plus tôt.

## Liste d'idées à prioriser
- [ ] Colorer les badges de catégorie selon `UI_DESIGN_SPEC.md` (badges neutres aujourd'hui ; couleurs LMP3 et LMGTE à définir).
- [ ] Multi langue.
- [ ] Reprise du readme pour la doc d'utilisation 
- [ ] Exporter les résultats et statistiques en CSV.
- [ ] Ajouter une fiche pilote avec ses statistiques cumulées.
- [ ] Comparer plusieurs sessions sur un même circuit et une même voiture.
- [ ] Afficher les incidents et avertissements de limites de piste lorsqu'ils sont présents dans le XML.
- [x] Permettre de choisir le dossier de résultats depuis l'interface (page Options).
- [x] Ajouter un indicateur de progression et un résumé des derniers imports (page Options).
