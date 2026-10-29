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
- [ ] Optimisation : quand l'option est décochée, les fichiers d'essais sont encore lus et analysés avant d'être ignorés. Lire seulement l'en-tête du XML (type de session) permettrait de les écarter plus tôt. Depuis le parser en flux (audit d'octobre 2026), un fichier d'essais médian se lit en 0,6 ms : le gain restant est estimé à 25 ms sur les 342 fixtures.

## Jalon — Fusion de pilotes [Terminé]
- [x] Bloc « Fusion de pilotes » dans Options : déclarer qu'un ancien nom et un nom conservé désignent le même pilote (changement de pseudo), sans modifier les fichiers de résultats.
- [x] Fusion appliquée en mémoire à toutes les vues, enregistrée dans `LMU:DriverMerges`, annulable.
- [x] Avertissement quand les deux pilotes ont participé à une même session (probablement deux personnes différentes), avant la fusion et dans la liste des fusions.

## Jalon — UI : Menu "Progression" (`Progression.razor`) [Terminé]
Spécification : [`UISpec/Progression.template.md`](UISpec/Progression.template.md).
- [x] Nouvel onglet **Progression** (`/progression`), placé juste après « Mes courses » dans la barre de navigation.
- [x] Sélecteurs **Circuit** (lieu + tracé, comme le delta n-1) et **Catégorie**. À l'ouverture directe de l'onglet, ils prennent le circuit et la catégorie de la course la plus récente.
- [x] Ne tracer que les temps du pilote principal. Un point source = le meilleur tour valide d'une session.
- [x] **Granularité** réglable : jour, semaine (ISO, du lundi au dimanche), mois (par défaut), année. Les périodes sont calculées en heure locale.
- [x] **Agrégat de la ligne** réglable : min, max, moyenne (par défaut). Une bande semi-transparente va du min au max de la période.
- [x] **Mode voitures mélangées** (par défaut) : une courbe Qualification et une courbe Course.
- [x] **Mode une courbe par voiture** (`CarType` exact, une variante Evo compte comme une voiture distincte) : on choisit Course (par défaut) ou Qualification, et chaque voiture a sa couleur.
- [x] Axe des dates à l'échelle réelle. La ligne et la bande min-max traversent les périodes sans session en trait plein ; ces périodes sont marquées par un repère grisé sur l'axe des dates.
- [x] Graphique en SVG généré en C# (composant Razor, aucune dépendance). Survoler ou cliquer un point affiche ses valeurs (min, moyenne, max, sessions) dans un panneau à droite du graphique ; par défaut, le panneau montre la dernière période de chaque courbe.
- [x] Réglages (granularité, agrégat, mode, type de session) enregistrés dans `appsettings.json` (section `LMU:Progression`).
- [x] Icône 📈 dans l'en-tête de chaque circuit de « Mes courses » : elle ouvre Progression sur ce circuit, avec la dernière catégorie roulée sur ce circuit.
- [x] Gérer les états vides : aucune session pour le couple circuit + catégorie, ou une seule période.
- [x] **Résumé chiffré** au-dessus du graphique, une ligne par courbe : record (avec sa date), valeur de la première et de la dernière période, écart (vert si plus rapide).
- [x] **Ligne du record** en tirets, à la couleur de chaque courbe.
- [x] **Tableau des périodes**, replié par défaut : min, moyenne, max et sessions de chaque période et de chaque courbe.
- [x] Légende cliquable pour masquer ou réafficher une courbe ; réglages en boutons segmentés.

## Jalon — UI : Menu "Accueil" (`Home.razor`) [Terminé]
Spécification : [`UISpec/Home.template.md`](UISpec/Home.template.md).
- [x] **Parser :** lire `ClassGridPos` et `DNFReason` du pilote, et `TrackLength` de la session.
- [x] Nouvel onglet **Accueil** sur `/`, devant « Mes courses » (qui ne garde que `/my-races`). L'ancienne page `/dashboard` est retirée, l'aperçu des imports restant dans Options ; le logo renvoie vers `/`. Mettre à jour `MyRaces.template.md`.
- [x] Tableau de bord du pilote sélectionné en tuiles groupées par sections. Chaque compteur affiche son total puis sa ventilation par catégorie (badges).
- [x] **Palmarès** (courses uniquement, position en classe officielle, statut `None` exclu) : courses, victoires, podiums, top 5, top 10 avec leurs taux, position moyenne.
- [x] **Qualification & départ :** poles (P1 de classe en qualif) et leur taux, position moyenne en qualif, places gagnées (moyenne par course et total).
- [x] **Abandons :** DNF, DQ et non classés (`None`) comptés séparément, avec leurs taux ; répartition des abandons par cause (`DNFReason`).
- [x] **Roulage :** tours bouclés, distance (km), temps en piste (tours chronométrés).
- [x] **Forme récente :** pastilles des 10 dernières courses (position en classe, DNF, DQ, NC) et résumé de la dernière course, avec un lien vers son événement dans « Détail des courses » (sélection de l'événement par l'URL à ajouter).
- [x] **Préférences :** circuit et voiture les plus roulés avec leur palmarès, dans la tuile Pilote.
- [x] Gérer les états vides : aucune course, valeur non calculable (`—`).

## Jalon — Refonte de la console Spectre [Terminé]
Spécification : [`UISpec/Console.template.md`](UISpec/Console.template.md).
Reprendre dans la console les vues Accueil, Mes courses et Options du dashboard, avec une interface simplifiée : un menu principal court, une vue affichée à la fois.
- [x] **Calculs partagés :** calculs des vues, service de données, réglages et détection du dossier déplacés du projet UI vers le projet commun `LMU-DeltaRacing.Presentation`, sans changer le comportement du dashboard.
- [x] **Suppression des anciennes vues :** retirer « Par circuit », « Par circuit et voiture », leurs graphiques et `SessionStatistics`, pour ne garder qu'une seule logique de delta.
- [x] **Sans argument :** supprimer le mode « fichiers en argument » ; la console charge toujours le dossier configuré dans `appsettings.json` (ou détecté automatiquement), comme le dashboard.
- [x] **Menu principal :** Accueil, Mes courses, Options, Quitter. L'import initial affiche une barre de progression puis un résumé sur une ligne (réussites, échecs avec leur détail).
- [x] **Accueil :** mêmes sections et mêmes règles que `Home.razor` (palmarès, qualification & départ, abandons, roulage, forme récente, préférences), ventilées par catégorie.
- [x] **Mes courses :** mêmes lignes qualif + course, delta n-1 et coloration (vert/rouge) que `MyRaces.razor`, groupées par circuit, avec les filtres Circuit et Catégorie.
- [x] **Options :** mêmes réglages que `Settings.razor`, enregistrés dans la section `LMU` du `appsettings.json` de la console (à côté de son exécutable, distinct de celui de l'UI) : dossier des résultats (validation, enregistrement, puis rechargement des résultats et du watcher), pilote principal (trié par pourcentage d'apparition), fusion de pilotes (avec l'avertissement des pilotes qui se sont croisés), démarrage automatique de la surveillance. Les réglages du fichier de log (chemin, niveau minimal) restent propres à la console.
- [x] Pas de liens vers « Détail des courses » ni « Progression », absents de la console.
- [x] **Essais :** la console ignore toujours les sessions d'essais ; pas d'option « Charger les sessions d'essais ».
- [x] **Imports pendant la surveillance :** ne plus écrire dans le terminal pendant qu'un menu est affiché ; signaler les nouveaux imports entre deux menus.
- [x] Tester le rendu des vues avec `Spectre.Console.Testing`.

## Jalon — UI : Écran de chargement au démarrage [Terminé]
Spécification : [`UI_DESIGN_SPEC.md`](UI_DESIGN_SPEC.md) (§ 6). Ne plus voir le dashboard se remplir au fil de l'eau (compteurs à 0) pendant l'import initial.
- [x] **État partagé :** indicateur `DashboardState.IsStartupImportRunning`, levé uniquement pendant l'import au démarrage et baissé une fois toutes les données en place (succès, erreur ou arrêt).
- [x] **Écran plein page :** logo de l'installeur (le `favicon.ico` existant, identique à `assets/icons`, sans nouvelle copie) et roue qui roule sur une route en guise de barre de progression (« N / M fichiers traités », pourcentage), masquant toute l'application, header compris.
- [x] Un rechargement depuis Options et les imports du watcher n'affichent pas l'écran.
- [x] Tester la visibilité, le pourcentage et le libellé (`StartupLoadingPresentation`) ainsi que le cycle de vie de l'indicateur dans `DashboardDataService`.

## Jalon — Signature de l'installeur (SignPath Foundation)
Supprimer l'avertissement Microsoft Defender SmartScreen (« Windows a protégé votre ordinateur ») affiché au lancement de l'installeur non signé. La signature gratuite de SignPath Foundation est réservée aux projets open source ; vérifier leurs conditions à jour (https://signpath.org/terms) avant la demande.
- [ ] **Licence :** remettre le texte GPL-3.0 intact dans `LICENSE` (GitHub le détecte aujourd'hui comme « Other » à cause de la ligne de copyright ajoutée en tête) et déplacer le copyright dans le README.
- [ ] **README :** ajouter une section « Code signing policy » : mention « Free code signing provided by SignPath.io, certificate by SignPath Foundation », rôles (auteurs, relecteurs, approbateurs) et déclaration de confidentialité (aucune donnée envoyée sur le réseau, serveur local limité à `127.0.0.1`).
- [ ] Activer la double authentification sur le compte GitHub.
- [ ] Déposer la demande sur https://signpath.org/apply, puis configurer le projet sur app.signpath.io (connexion GitHub, signing policy `release-signing`, artifact configuration, secret `SIGNPATH_API_TOKEN`).
- [ ] **`release.yml` :** signer l'artefact avec `signpath/github-action-submit-signing-request` avant de publier la release ; signer l'exécutable de l'application avant de générer le setup Inno, puis le setup lui-même.
- [x] **En attendant :** le README explique comment passer l'avertissement (« Informations complémentaires » → « Exécuter quand même »), dans la section Installation.

## Jalon — Mise en avant du classement dans « Mes courses » [Terminé]
Spécification : [`UISpec/MyRaces.template.md`](UISpec/MyRaces.template.md).
- [x] Colonne **CLASSEMENT** : position en classe de la course (`ClassPosition`) au format `1er`, `2e`…, `DNF`, `DQ` ou `NC` selon le statut d'arrivée, `—` sans course.
- [x] Ligne encadrée et position colorée : or pour la 1re place, argent pour la 2e, bronze pour la 3e (bordure sur toute la ligne plutôt qu'un fond coloré).
- [x] Bleu pour les 4e et 5e places, et le même bleu plus léger pour les 6e à 10e, pour différencier les lignes (couleurs ajoutées à `UI_DESIGN_SPEC.md`).
- [x] Cas particuliers : un abandon, une disqualification, une course non classée ou une ligne sans course gardent la ligne actuelle, sans bordure.
- [x] Console : colonne CLASS., la mise en avant porte sur la cellule de la position (Spectre.Console ne colore pas une ligne entière).

## Liste d'idées à prioriser
- [ ] Colorer les badges de catégorie selon `UI_DESIGN_SPEC.md` (badges neutres aujourd'hui ; couleurs LMP3 et LMGTE à définir).
- [ ] Multi langue.
- [x] Reprise du readme pour la doc d'utilisation (README utilisateur, guide développeur déplacé dans `docs/DEVELOPMENT.md`).
- [ ] Exporter les résultats et statistiques en CSV.
- [ ] Ajouter une fiche pilote avec ses statistiques cumulées.
- [ ] Comparer plusieurs sessions sur un même circuit et une même voiture.
- [ ] Afficher les incidents et avertissements de limites de piste lorsqu'ils sont présents dans le XML.
- [x] Permettre de choisir le dossier de résultats depuis l'interface (page Options).
- [x] Ajouter un indicateur de progression et un résumé des derniers imports (page Options).
