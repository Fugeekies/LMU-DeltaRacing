# Feuille de route

Les jalons à faire sont en haut, par ordre de priorité. Les jalons terminés suivent, du plus récent au plus ancien, avec leur date de fin et la PR qui les a livrés.

## À faire

### Jalon — Signature de l'installeur (SignPath Foundation) (en parallèle)
Supprimer l'avertissement Microsoft Defender SmartScreen (« Windows a protégé votre ordinateur ») affiché au lancement de l'installeur non signé. La signature gratuite de SignPath Foundation est réservée aux projets open source ; vérifier leurs conditions à jour (https://signpath.org/terms) avant la demande. Surtout administratif : à mener en parallèle des jalons techniques.
- [x] **Licence :** texte officiel complet de la GPL-3.0 dans `LICENSE` (il était tronqué et précédé d'une ligne de copyright, d'où « Other » sur GitHub), copyright déplacé en bas du README.
- [x] **README :** ajouter une section « Code signing policy » : mention « Free code signing provided by SignPath.io, certificate by SignPath Foundation », rôles (auteurs, relecteurs, approbateurs) et déclaration de confidentialité (aucune donnée collectée, seule requête sortante : le numéro de la dernière version sur GitHub, désactivable ; serveur local limité à `127.0.0.1`). Lien « Code signing policy » au pied de la page de présentation ; nom de produit `LMU-DeltaRacing` dans les métadonnées de l'exécutable.
- [ ] Activer la double authentification sur le compte GitHub.
- [ ] Déposer la demande sur https://signpath.org/apply, puis configurer le projet sur app.signpath.io (connexion GitHub, signing policy `release-signing`, artifact configuration, secret `SIGNPATH_API_TOKEN`).
- [ ] **`release.yml` :** signer l'artefact avec `signpath/github-action-submit-signing-request` avant de publier la release ; signer l'exécutable de l'application avant de générer le setup Inno, puis le setup lui-même. Étapes ajoutées, inactives tant que la variable `SIGNPATH_ORGANIZATION_ID` n'est pas définie (configuration décrite dans `docs/DEVELOPMENT.md`) : à cocher après une première release signée.
- [x] **En attendant :** le README explique comment passer l'avertissement (« Informations complémentaires » → « Exécuter quand même »), dans la section Installation.

### Jalon — Export CSV
- [ ] Exporter les résultats et statistiques en CSV. Données et pages concernées à définir avant de coder.

### Liste d'idées à prioriser
- Écarté pour l'instant : une persistance en base de données ou un cache des sessions. Après l'audit d'octobre 2026, 342 fichiers se chargent en 100 ms environ (moins de 3 s estimées pour 10 000 fichiers) : relire les XML à chaque démarrage reste simple et suffisant.

## Terminés

### Jalon — Notes de course — 13/11/2026 · PR [#73](https://github.com/Fugeekies/LMU-DeltaRacing/pull/73)
Noter, pour chaque session, les conditions, le comportement de la voiture, les réglages et son ressenti. Le XML ne contient ni météo ni réglages (seulement `<FixedSetups>`) ; le seul indice de pluie est le composé `Wet` des tours. Première donnée saisie par le joueur : elle doit survivre à un redémarrage, contrairement aux sessions relues à chaque lancement.
- [x] **Stockage :** fichier `race-notes.json` à côté d'`appsettings.json` (décision du 13/11/2026), une note par session, rattachée à `Session.Id` (stable entre deux lancements, indépendant du nom du fichier), écrit d'un bloc comme les réglages. L'installeur ne le publie pas : une mise à jour ne l'écrase pas, une désinstallation le laisse en place. Mettre à jour la règle « persistance en mémoire uniquement » d'`AGENTS.md` et `docs/ARCHITECTURE.md`.
- [x] **Contenu** (champs facultatifs, à revoir à l'usage) : conditions (Sec, Humide, Pluie, Variable ; « Pluie » proposée si des pneus `Wet` ont été montés), comportement de la voiture, réglages (texte libre), ressenti de 1 à 5 et remarques.
- [x] **Détail des courses :** bloc « Notes » de chaque session, saisie et lecture. Bandeau entre les onglets et le classement, bouton Enregistrer, maquette [`RaceNotes.template.md`](UISpec/RaceNotes.template.md) validée le 13/11/2026.
- [x] **Mes courses :** icône sur les sessions annotées.
- [x] **Analyse tour par tour :** conditions et réglages dans le tableau « Comparer les sessions ».
- [x] Textes en cinq langues et page du guide utilisateur dans les cinq langues.

### Jalon — Choix des sessions importées — 13/11/2026 · PR [#72](https://github.com/Fugeekies/LMU-DeltaRacing/pull/72)
Laisser le joueur choisir les types de session pris en compte, comme l'option « Charger les sessions d'essais » le fait déjà pour les essais. Constat sur les fixtures : la balise `<Setting>` vaut `Multiplayer` dans 336 fichiers et `Race Weekend` dans 6 (toutes des courses), que le parser ne lisait pas.
- [x] **Format :** vérifier le sens de `<Setting>` (valeurs possibles, ce qui distingue une session « Race Weekend ») et le documenter dans `AGENTS.md` (faits vérifiés). Constat : les 6 fichiers « Race Weekend » sont des courses hors ligne contre l'IA (un seul joueur, adversaires en `AIControl`), dont 3 sans aucun tour.
- [x] **Parser :** lire `<Setting>`, justifier la mise à jour de l'empreinte SHA-256 de référence et committer un rapport de benchmark à jour. `Session.Setting` et `Session.IsRaceWeekend` ; empreinte mise à jour (seul le champ ajouté la change) ; [rapport de benchmark](benchmarks/choix-sessions-importees-2026-11.md).
- [x] **Option** pour intégrer ou non les sessions « Race Weekend », lue à chaque import comme `LMU:LoadPracticeSessions` : case « Charger les courses hors ligne contre l'IA » (`LMU:LoadRaceWeekendSessions`), décochée par défaut (décision du 13/11/2026).

### Jalon — Avenir de la console Spectre — 10/11/2026 · PR [#71](https://github.com/Fugeekies/LMU-DeltaRacing/pull/71)
Chaque règle métier était partagée (`Presentation`), mais son rendu existait deux fois : pages Razor et `Console/Views`. Chaque nouvelle vue doublait donc le coût de développement et de test, et la console n'avait déjà plus que l'Accueil, Mes courses et les Options.
- [x] Décider du rôle de la console : supprimée (décision du 10/11/2026). Un terminal rend mal les graphiques (Progression, Cote pilote, tour par tour), les tableaux larges (comparaison des sessions, relais, usure des pneus) et la navigation entre pages ; l'installeur ne distribuait que le dashboard.
- [x] Supprimer le projet `LMU-DeltaRacing.Console`, ses tests, sa spécification (`UISpec/Console.template.md`) et ce qui ne servait qu'à lui (`DashboardDataService.LiveImportCompleted`, `ApplicationTelemetry`, paquets Spectre.Console et OpenTelemetry).
- [x] Documenter la décision dans `AGENTS.md` et `docs/ARCHITECTURE.md`, et retirer la console du guide développeur et des spécifications.

### Jalon — Analyse de course tour par tour — 08/11/2026 · PR [#69](https://github.com/Fugeekies/LMU-DeltaRacing/pull/69)
Les balises `<Lap>` contenaient bien plus que les temps et les secteurs utilisés : position, vitesse de pointe, carburant, énergie, usure des pneus, composés et arrêts. Dashboard uniquement, tant que le jalon « Avenir de la console Spectre » n'est pas tranché. Nouvelle page « Analyse tour par tour » du menu Courses (`/lap-analysis`), spec [`LapAnalysis.template.md`](UISpec/LapAnalysis.template.md).
- [x] **Format :** attributs de `<Lap>` et `<Pitstops>` vérifiés sur les 342 fixtures et documentés dans `AGENTS.md`. `fuel` et `ve` sont des fractions du réservoir (pas de capacité dans le XML : pourcentages seulement) ; carburant, énergie et usure n'existent que si le serveur les partage (`<ClientFuelVisible>`), jamais dans les sessions depuis juillet 2026. `pit="1"` diffère de `<Pitstops>` pour 164 pilotes sur 2 303 : le nombre d'arrêts se lit dans `<Pitstops>` (décision du 08/11/2026).
- [x] **Parser :** vitesse de pointe, carburant, énergie, usure, composés (`Lap.Telemetry`) et `Driver.PitStops` ; empreinte SHA-256 mise à jour (seuls les champs ajoutés la changent) ; [rapport de benchmark](benchmarks/analyse-tour-par-tour-2026-11.md).
- [x] **Régularité :** écart-type des tours représentatifs (hors premier tour, tours au stand et tours plus lents que 107 % de la médiane), rang dans la classe, rythme et écart moyen au meilleur tour.
- [x] **Records personnels :** meilleur tour et vitesse de pointe par couple circuit + voiture (page Analyse tour par tour), badge « Record » sur les sessions qui battent le record précédent.
- [x] **Graphique tour par tour** (page Analyse tour par tour, lien depuis Détail des courses) : courbe des temps du pilote suivi, et du pilote sélectionné pour comparer.
- [x] **Nombre d'arrêts au stand** d'un pilote dans une course : `<Pitstops>`, affiché dans l'analyse tour par tour et dans la comparaison des sessions.
- [x] **Résumé des relais :** relais découpés au premier tour de chaque passage par la voie des stands, avec composés, rythme, meilleur tour, consommation et usure.
- [x] **Usure et consommation :** usure des 4 pneus, carburant et énergie par tour, tours tenables avec un plein, quand le serveur partage ces données.
- [x] **Comparer plusieurs sessions** sur un même circuit et dans une même catégorie : tableau « Comparer les sessions » de la page Analyse tour par tour.

### Jalon — Navigation en sous-menus — 06/11/2026 · PR [#68](https://github.com/Fugeekies/LMU-DeltaRacing/pull/68)
La barre de navigation comptait huit onglets (Accueil, Mes courses, Détail des courses, Progression, Profil de conduite, Cote pilote, Classement des coureurs, Options) : trop pour rester lisible, surtout sur un écran étroit.
- [x] Proposer le regroupement en sous-menus et le faire valider : Accueil, Courses (Mes courses, Détail des courses), Performance (Progression, Profil de conduite), Pilotes (Cote pilote, Classement des coureurs), Options. Validé sur pièce le 06/11/2026, décrit dans [`UI_DESIGN_SPEC.md`](UI_DESIGN_SPEC.md) (§ 2 ter).
- [x] Mettre en place les sous-menus, sans changer les adresses des pages : menus au survol ou au clavier, onglets simples sur téléphone.
- [x] Mettre à jour les guides utilisateur dans les cinq langues (sommaire du guide).

### Jalon — Pilote principal introuvable au lancement — 06/11/2026 · PR [#67](https://github.com/Fugeekies/LMU-DeltaRacing/pull/67)
Le pilote principal n'était détecté que si aucun nom n'était configuré (jalon « Déterminer l'utilisateur principal »). Si le nom enregistré dans `LMU:UserName` n'apparaissait dans aucun fichier de résultats (dossier changé, pseudo modifié), les vues de résultats restaient vides.
- [x] Après chaque import complet (démarrage, rechargement depuis Options), si le pilote configuré n'apparaît dans aucune session, le pilote le plus fréquent le remplace et est enregistré. Les fichiers ajoutés par la surveillance ne déclenchent pas la vérification.
- [x] Informer le joueur du changement : encadré « Pilote principal remplacé » traduit dans les cinq langues sur les pages de résultats, ligne en tête de l'Accueil de la console. Remplacement automatique plutôt que simple proposition (décision du 06/11/2026).

### Jalon — Fenêtre de nouvelle version : ajustements — 06/11/2026 · PR [#66](https://github.com/Fugeekies/LMU-DeltaRacing/pull/66)
Suite du jalon « Version et mises à jour » : la fenêtre qui annonce une nouvelle version était petite et disparaissait trop vite pour être lue. Spécification : [`UI_DESIGN_SPEC.md`](UI_DESIGN_SPEC.md) (§ 6).
- [x] Agrandir un peu la fenêtre (marges et taille des textes).
- [x] Passer sa durée d'affichage de 3 à 5 secondes.

### Jalon — Interface multilingue — 05/11/2026 · PR [#60](https://github.com/Fugeekies/LMU-DeltaRacing/pull/60), [#61](https://github.com/Fugeekies/LMU-DeltaRacing/pull/61), [#62](https://github.com/Fugeekies/LMU-DeltaRacing/pull/62)
Avant ce jalon, l’application n’avait aucun mécanisme de traduction : tous les textes étaient écrits en dur en français. Ce jalon change une règle documentée dans `AGENTS.md` (« l'UI est en français ») : proposition écrite à valider avant de coder. Langues : les mêmes que le guide d'utilisation (anglais, français, allemand, italien, espagnol).
- [x] Rédiger la proposition : mécanisme de traduction, langues proposées, langue par défaut (français, ou langue de Windows au premier lancement), sort de la console selon le jalon « Avenir de la console Spectre » et de l'installeur Inno Setup. → [`propositions/interface-multilingue.md`](propositions/interface-multilingue.md), validée le 05/11/2026.
- [x] Extraire les textes des pages Razor et ceux des calculs partagés de `Presentation`, sans changer le rendu français (`Presentation/Resources/Texts.resx`, PR 1). La fenêtre de reconnexion, les ordinaux, les noms de mois et les formats de date ont suivi en PR 2.
- [x] Section **Langue et date** dans Options (langue et format de date), enregistrée dans `appsettings.json` (section `LMU`). Traductions anglaise, allemande, italienne et espagnole (PR 2).
- [x] Adapter à la langue les formats qui en dépendent (`1er`, `2e`, dates), sans toucher à `CultureInfo.InvariantCulture` pour la lecture des temps. Ordinaux, dates et noms de mois suivent la langue ; nombres, temps et écarts gardent le même format partout (décision du 05/11/2026).
- [x] Test vérifiant que chaque texte existe dans toutes les langues. (`TextsTests` : mêmes clés et mêmes paramètres).
- [x] Mettre à jour `AGENTS.md`, `docs/UI_DESIGN_SPEC.md` et les specs `docs/UISpec/`. Installeur en cinq langues, guides cités dans la langue de l’écran (PR 3).

### Jalon — Exploiter les événements du `Stream` — 05/11/2026 · PR [#53](https://github.com/Fugeekies/LMU-DeltaRacing/pull/53), clôture [#59](https://github.com/Fugeekies/LMU-DeltaRacing/pull/59)
La donnée la plus riche des fichiers LMU n'était pas utilisée : incidents, contacts, limites de piste, pénalités et scores (`<Stream>` de chaque phase). Décisions : [`propositions/exploiter-stream.md`](propositions/exploiter-stream.md).
- [x] **Fixtures :** noms des événements du `Stream` rétablis depuis les résultats d'origine de l'historique git, en conservant les retouches de démonstration de Driver Tom.
- [x] Documenter le format réel des événements du `Stream` dans `AGENTS.md`.
- [x] **Parser :** lire les contacts, les limites de piste et les pénalités du `Stream`, sans extrapoler les balises.
- [x] Afficher les incidents et les avertissements de limites de piste dans Détail des courses.
- Abandonné : fiche pilote avec ses statistiques cumulées et sa « propreté » de pilotage, couverte par la page Profil de conduite.
- Reporté au jalon « Analyse de course tour par tour » : comparer plusieurs sessions sur un même circuit et une même catégorie.

### Jalon — README multilingue — 05/11/2026 · PR [#58](https://github.com/Fugeekies/LMU-DeltaRacing/pull/58)
Rendre le guide utilisateur lisible hors de France. Langues retenues : anglais, français, allemand, italien, espagnol. Un wiki GitHub a été écarté : ses pages échappent aux PR et ne suivent pas les versions.
- [x] Guide découpé en une page courte par sujet, dans `docs/guide/<langue>/` (mêmes noms de fichiers dans chaque langue), avec en tête le sélecteur vers la même page dans les autres langues.
- [x] `README.md` : page d'accueil courte, une section par langue, l'anglais en premier.
- [x] Règle de synchronisation dans `AGENTS.md` (toute modification se reporte dans les cinq langues, le français sert de référence) et tests `UserGuideTests`. Les autres documents de `docs/` restent en français.

### Jalon — Tableaux : colonnes et données — 05/11/2026 · PR [#57](https://github.com/Fugeekies/LMU-DeltaRacing/pull/57)
Améliorer les tableaux du dashboard. Specs mises à jour : [`UISpec/MyRaces.template.md`](UISpec/MyRaces.template.md), [`UISpec/RaceDetails.template.md`](UISpec/RaceDetails.template.md), [`UISpec/DriversRanking.template.md`](UISpec/DriversRanking.template.md).
- [x] **Colonnes ajustables :** largeur des colonnes réglable dans tous les tableaux du dashboard (demandé d’abord pour Mes courses, Classement des coureurs et Détail des courses). Bord droit des en-têtes à faire glisser, double-clic pour revenir aux largeurs automatiques, largeurs non mémorisées.
- [x] **Détail des courses, doublon de position :** la première colonne (POS / CLASSE) empilait la position générale et la position en classe (`01` et `LMGT3 P1`), qui se répétaient quand elles étaient égales. Séparées en deux colonnes, POS et CLASSE.
- [x] **Classement des coureurs, plus de données :** liste arrêtée dans `DriversRanking.template.md` (cote de l'adversaire, écart moyen en classe, contacts entre les deux voitures, dernière course affrontée), puis affichée.

### Jalon — Cote pilote — 03/11/2026 · PR [#55](https://github.com/Fugeekies/LMU-DeltaRacing/pull/55)
Spécification : [`UISpec/DriverRating.template.md`](UISpec/DriverRating.template.md). Décisions et simulation sur les fixtures : [`propositions/cote-pilote.md`](propositions/cote-pilote.md). Dashboard uniquement.
- [x] **Moteur :** cote Elo multijoueur de chaque pilote croisé (`DriverRatings`), course par course et classe par classe, 1 500 au départ, K = 64 pendant 5 courses de calibration puis 32 ; place attendue et force du plateau de chaque course.
- [x] **Statut `None` :** une classe sans aucun pilote arrivé n'est pas notée (19 courses sur 107 dans les fixtures, quand le pilote quitte au départ) ; les autres gardent leurs positions officielles.
- [x] Nouvel onglet **Cote pilote** : cote, rang, palier (Platine, Or, Argent, Bronze), prochains objectifs (pilote à dépasser, palier suivant), évolution en SVG avec les seuils des paliers, exploits et revers, historique, classement des pilotes.
- [x] **Détail des courses :** force du plateau, cote avant → après du pilote et cote d'avant-course de chaque pilote. **Accueil :** cote dans la tuile Pilote.
- [x] Benchmarks des calculs (catégorie `Views`) : 7 ms sur les fixtures, 187 ms à ×50.

### Jalon — Profil de conduite — 02/11/2026 · PR [#53](https://github.com/Fugeekies/LMU-DeltaRacing/pull/53)
Spécification : [`UISpec/DrivingProfile.template.md`](UISpec/DrivingProfile.template.md). Dashboard uniquement.
- [x] **Coût des incidents :** temps perdu mesuré contre le tour médian de chaque course, contacts au premier tour, places perdues, dégâts, abandons après contact, zone à risque par circuit.
- [x] **Déroulé de la course** dans Détail des courses : position tour par tour avec les incidents et les arrêts au stand.
- [x] Nouvel onglet **Profil de conduite**, centré sur le pilote sélectionné : bilan pour 100 tours comparé aux autres pilotes de ses sessions, par session, catégorie et circuit, 10 dernières courses, pilotes les plus touchés, obstacles et motifs de pénalité.
- [x] **Contacts :** les signalements d'un même accrochage comptent une fois, pour chaque pilote impliqué (regroupement à 5 s, mesuré sur les fixtures).
- [x] **Tours invalides :** LMU n'en signale pas en course. Ils sont rapportés aux tours de qualification, sans déduire de tours invalides en course.

### Jalon — Identité et chronologie des sessions — 02/11/2026 · PR [#52](https://github.com/Fugeekies/LMU-DeltaRacing/pull/52)
Issu de l'audit d'octobre 2026. Décisions et simulation sur les fixtures : [`propositions/identite-chronologie-sessions.md`](propositions/identite-chronologie-sessions.md).
- [x] **Heure :** `StartedAt` lit la date de la phase, à défaut `RaceResults/DateTime`, commun à une série de sessions. Les deux courses du 16/09/2026 ont toujours le même ordre et le même delta n-1.
- [x] **Identité :** `Session.Id` est calculé à partir du circuit, de la phase et de sa date, au lieu d'être aléatoire à chaque chargement.
- [x] **Événements :** identifiés par leur première session hors essais. Les liens `?event=` survivent à un redémarrage et au changement de l'option d'essais.
- [x] **Doublons :** le premier fichier importé est gardé, le second est signalé comme ignoré (`SkippedDuplicate`), dans le dashboard et dans la console.
- [x] `AGENTS.md` (faits vérifiés) et empreinte de référence du parser mis à jour : avec l'ancienne date, l'empreinte d'origine ressort à l'identique, donc seule la date change.

### Jalon — Version et mises à jour — 30/10/2026 · PR [#48](https://github.com/Fugeekies/LMU-DeltaRacing/pull/48)
Spécifications : [`UI_DESIGN_SPEC.md`](UI_DESIGN_SPEC.md) (§ 6) et [`UISpec/Settings.template.md`](UISpec/Settings.template.md). Dashboard uniquement : la console attend la décision du jalon « Avenir de la console Spectre ».
- [x] **Numéro de version :** inscrit dans l'application par la release (`-p:Version`, absent jusqu'ici), affiché sur l'écran de chargement et dans Options, car l'écran de chargement est très bref en Release.
- [x] **Vérification au démarrage :** une requête vers l'API GitHub, sans résultat ni nom de pilote, sans ralentir le démarrage. Option de désactivation dans Options (`LMU:CheckForUpdates`, activée par défaut).
- [x] **Fenêtre de nouvelle version :** 3 secondes, une fois par lancement, après l'écran de chargement.
- [x] **Options :** bouton de téléchargement direct de l'installeur de la dernière release ; lien vers la page des versions si la vérification échoue.
- [x] Remplacer dans le README la promesse « ne se connecte à aucun service en ligne » par « aucune donnée n'est collectée ».
- [x] `Test-PublishedApp.ps1 -ExpectedVersion` : la release échoue si la version affichée n'est pas celle du tag.

### Jalon — Garde-fous de CI — 29/10/2026 · PR [#45](https://github.com/Fugeekies/LMU-DeltaRacing/pull/45)
Issu de l'audit d'octobre 2026. Avant ce jalon, aucun test ne tournait sur les PR et la release publiait sans lancer les tests unitaires ; les garde-fous ajoutés par l'audit (avertissements traités comme erreurs, empreinte SHA-256 du parser) ne protègent le projet que s'ils sont exécutés à chaque modification.
- [x] Workflow GitHub Actions `ci.yml` sur chaque PR et sur `main` après fusion : `dotnet build LMU-DeltaRacing.slnx` en Release (avertissements de `src/` traités comme erreurs) puis `dotnet test` du projet de tests, sans les benchmarks. Les résultats de tests sont joints au run en cas d'échec.
- [x] **`release.yml` :** lancer les tests unitaires avant `dotnet publish`, pour qu'une release ne parte jamais avec des tests rouges.
- [x] Rendre le workflow `CI` obligatoire avant fusion (protection de la branche `main`, vérification « build-and-test ») : à configurer dans les réglages GitHub une fois le workflow présent sur `main`.
- [x] Garder le `#if DEBUG Task.Delay(15)` par fichier de `DashboardDataService` : il est voulu, pour vérifier à l'œil l'écran de chargement en Debug (sans effet en Release) ; un commentaire l'explique désormais dans le code.

### Jalon — Mise en avant du classement dans « Mes courses » — 27/10/2026 · PR [#42](https://github.com/Fugeekies/LMU-DeltaRacing/pull/42)
Spécification : [`UISpec/MyRaces.template.md`](UISpec/MyRaces.template.md).
- [x] Colonne **CLASSEMENT** : position en classe de la course (`ClassPosition`) au format `1er`, `2e`…, `DNF`, `DQ` ou `NC` selon le statut d'arrivée, `—` sans course.
- [x] Ligne encadrée et position colorée : or pour la 1re place, argent pour la 2e, bronze pour la 3e (bordure sur toute la ligne plutôt qu'un fond coloré).
- [x] Bleu pour les 4e et 5e places, et le même bleu plus léger pour les 6e à 10e, pour différencier les lignes (couleurs ajoutées à `UI_DESIGN_SPEC.md`).
- [x] Cas particuliers : un abandon, une disqualification, une course non classée ou une ligne sans course gardent la ligne actuelle, sans bordure.
- [x] Console : colonne CLASS., la mise en avant porte sur la cellule de la position (Spectre.Console ne colore pas une ligne entière).

### Jalon — UI : Écran de chargement au démarrage — 27/10/2026 · PR [#41](https://github.com/Fugeekies/LMU-DeltaRacing/pull/41)
Spécification : [`UI_DESIGN_SPEC.md`](UI_DESIGN_SPEC.md) (§ 6). Ne plus voir le dashboard se remplir au fil de l'eau (compteurs à 0) pendant l'import initial.
- [x] **État partagé :** indicateur `DashboardState.IsStartupImportRunning`, levé uniquement pendant l'import au démarrage et baissé une fois toutes les données en place (succès, erreur ou arrêt).
- [x] **Écran plein page :** logo de l'installeur (le `favicon.ico` existant, identique à `assets/icons`, sans nouvelle copie) et roue qui roule sur une route en guise de barre de progression (« N / M fichiers traités », pourcentage), masquant toute l'application, header compris.
- [x] Un rechargement depuis Options et les imports du watcher n'affichent pas l'écran.
- [x] Tester la visibilité, le pourcentage et le libellé (`StartupLoadingPresentation`) ainsi que le cycle de vie de l'indicateur dans `DashboardDataService`.

### Jalon — Refonte de la console Spectre — 27/10/2026 · PR [#38](https://github.com/Fugeekies/LMU-DeltaRacing/pull/38)
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

### Jalon — UI : Menu "Progression" (`Progression.razor`) — 26/10/2026 · PR [#37](https://github.com/Fugeekies/LMU-DeltaRacing/pull/37)
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

### Jalon — UI : Menu "Accueil" (`Home.razor`) — 26/10/2026 · PR [#36](https://github.com/Fugeekies/LMU-DeltaRacing/pull/36)
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

### Jalon — Fusion de pilotes — 25/10/2026 · PR [#32](https://github.com/Fugeekies/LMU-DeltaRacing/pull/32)
- [x] Bloc « Fusion de pilotes » dans Options : déclarer qu'un ancien nom et un nom conservé désignent le même pilote (changement de pseudo), sans modifier les fichiers de résultats.
- [x] Fusion appliquée en mémoire à toutes les vues, enregistrée dans `LMU:DriverMerges`, annulable.
- [x] Avertissement quand les deux pilotes ont participé à une même session (probablement deux personnes différentes), avant la fusion et dans la liste des fusions.

### Jalon — Détail des courses par événement — 25/10/2026 · PR [#31](https://github.com/Fugeekies/LMU-DeltaRacing/pull/31)
- [x] Regrouper les sessions en événements (essais → qualification → course sur un même circuit, à moins de 2 heures d'écart), séances d'essais fusionnées en un seul classement.
- [x] Sélecteur d'événement et sous-onglets **Course**, **Qualification**, **Essais** sous le sélecteur (Course par défaut).
- [x] Option « Charger les sessions d'essais » dans Options (`LMU:LoadPracticeSessions`, décochée par défaut, rechargement immédiat) ; la console ignore toujours les essais. Ils n'apparaissent que dans l'onglet Essais.

### Jalon — Écarts UI à traiter — 25/10/2026 · PR [#29](https://github.com/Fugeekies/LMU-DeltaRacing/pull/29)
Écarts relevés lors de la resynchronisation des specs `docs/UISpec/` avec le code. Les règles métier ont été arbitrées le 25/10/2026. Mettre à jour la spec `docs/UISpec/` concernée en même temps que chaque correction.

#### Prérequis : données
- [x] **Vie privée des fixtures :** noms anonymisés avec les mêmes alias dans `Name`, `Swap`, l'attribut `Driver` des événements et les textes du `Stream` ; tous les événements de course sont conservés, seul le contenu du chat est retiré. Fixtures régénérées en place (PR #28). Les résultats LMU d'origine n'étant pas disponibles, les alias utilisés dans les événements des fixtures actuelles ne correspondent pas à ceux des `<Driver>`.
- [x] **Parser :** lire `ClassPosition`, `Laps` (tours du pilote) et `FinishStatus`.
- [x] **Tours invalides :** vérification faite sur les fixtures. LMU efface déjà le temps (`--.----`) des tours visés par « Invalid Lap Cut Track » (35/35 cas vérifiés ; l'attribut `Lap` de l'événement est en base 0). Cet événement n'apparaît qu'en essais et en qualification. Règle retenue : un tour valide est un tour chronométré ; les secteurs d'un tour sans temps sont exclus des meilleurs secteurs.

#### Mes courses (`MyRaces.razor`)
- [x] **Une ligne = une paire qualif + course.** Chaque course est associée à la dernière qualification du même circuit (lieu + tracé + événement) qui la précède, sans autre course entre les deux. Une qualif sans course, ou une course sans qualif, donne une ligne partielle. Fin de la fusion par jour : sur les fixtures, les 87 courses du pilote principal donnaient 37 lignes, elles en donnent maintenant 89 (87 courses + 2 qualifs isolées).
- [x] Catégorie et voiture d'une ligne : celles de la course, à défaut celles de la qualif.
- [x] **Catégories :** Hypercar, LMP2, LMP3, LMGTE, LMGT3, chacune avec son badge et son filtre. Une classe inconnue reste affichée sous son nom brut au lieu d'être exclue.
- [x] **Delta n-1 :** inchangé (même lieu + même tracé + même catégorie, quelle que soit la voiture). Corriger `UI_DESIGN_SPEC.md`, qui exige à tort une voiture identique.
- [x] Colonne TOURS : tours effectués par le pilote (`Laps`) et non `MostLapsCompleted`.
- [x] Afficher toutes les dates en heure locale.

#### Classement des coureurs (`DriversRanking.razor`)
- [x] **Courses uniquement :** seules les sessions Race comptent dans le H2H.
- [x] **Par classe :** en session multiclasse, seuls les adversaires de la même classe que le pilote comptent, comparés sur `ClassPosition`.
- [x] Ajouter la barre visuelle victoires/défaites (vert/rouge, proportionnelle) dans la colonne % de victoire.
- [x] Positions égales : sans objet, deux pilotes d'une même session n'ont jamais la même position.

#### Détail des courses (`RaceDetails.razor`)
- [x] **Multiclasse :** meilleurs secteurs et meilleur tour calculés par classe (règle WEC). Afficher la position en classe.
- [x] **Meilleurs secteurs individuels :** chaque colonne secteur affiche le meilleur secteur du pilote, tous tours valides confondus. Une colonne « TOUR IDÉAL » affiche leur somme.
- [x] **Couleurs des secteurs :** violet = meilleur secteur de la classe, jaune sinon. Le vert n'est plus utilisé pour les secteurs ; il reste utilisé pour les deltas.
- [x] **Ajouter la colonne DIFF 1ER** en conservant les deltas actuels :
  - en course : écart de temps total (`FinishTime`) au premier de la classe, ou « +N tours » si le pilote a au moins un tour de retard ;
  - en qualif : écart au meilleur tour de la classe.
- [x] Afficher les temps au tour au format `m:ss.fff`, comme dans « Mes courses ».
- [x] Virtualiser les grandes grilles (> 40 voitures), comme demandé par `UI_DESIGN_SPEC.md`. Jusqu'à 40 voitures, les lignes sont rendues normalement (et pré-rendues). Le rendu virtualisé reste à vérifier dans un navigateur, par exemple sur la course du 16/09/2026 au Mans (54 voitures).

#### Options (`Settings.razor`)
- [x] **Bouton unique « Appliquer » :** il valide le dossier, enregistre `ResultsDirectory`, puis recharge les résultats et le watcher. Il remplace « Tester la connexion & recharger » et « Enregistrer ».
- [x] La case « démarrage automatique de la surveillance » est enregistrée dès qu'on la coche, comme le pilote principal.
- [x] Afficher en heure locale les dates du tableau des sessions récentes (aujourd'hui en UTC).
- [x] Renvoyer vers la page Options plutôt que vers `appsettings.json` dans l'encadré d'erreur de l'état des imports.

### Jalon — UI : Menu "Détail des Courses" (`RaceDetails.razor`) — 19/10/2026 · PR [#24](https://github.com/Fugeekies/LMU-DeltaRacing/pull/24)
- [x] Intégrer le sélecteur de session (dropdown) en haut de page.
- [x] Afficher le tableau de classement complet de la course sélectionnée avec les temps par secteur.
- [x] Appliquer la coloration WEC pour les secteurs (🟣 Best absolu, 🟢 Best perso, 🟡 Standard) et calculer les écarts au meilleur secteur et au meilleur tour de la session.

### Jalon — UI : Menu "Classement des Coureurs" (`DriversRanking.razor`) — 19/10/2026 · PR [#24](https://github.com/Fugeekies/LMU-DeltaRacing/pull/24)
- [x] Implémenter le service d'agrégation d'historique Head-to-Head (H2H).
- [x] Afficher le tableau avec les colonnes : `Nom`, `Courses affrontées`, `Victoires`, `Défaites`, `% Victoire`.
- [x] Ajouter la barre de recherche par nom et le tri dynamique (par défaut : volume d'affrontements décroissant).

### Jalon — Déterminer l'utilisateur principal — 17/10/2026 · PR [#23](https://github.com/Fugeekies/LMU-DeltaRacing/pull/23)
- [x] Détecter le pilote le plus fréquent dans les fichiers de résultats et enregistrer son nom dans `LMU:UserName`.
- [x] Au premier démarrage, attendre la fin du chargement des données avant de détecter le pilote si aucun nom n'est configuré.
- [x] Afficher le nom du pilote sélectionné dans la barre de navigation.
- [x] Basculer les vues de résultats sur le pilote sélectionné.
- [x] Ignorer les fichiers qui ne contiennent pas le pilote sélectionné.
- [x] Permettre de sélectionner un pilote dans Options, trié par pourcentage d'apparition global, et enregistrer ce choix dans `appsettings.json`.

### Jalon — UI : Menu "Mes Courses" (`MyRaces.razor`) — 17/10/2026 · PR [#18](https://github.com/Fugeekies/LMU-DeltaRacing/pull/18)
- [x] Structurer la vue avec groupement par circuit.
- [x] Ajouter la colonne `Catégorie` (Hypercar, LMP2, LMGT3) et les filtres associés (Circuit et Catégorie).
- [x] Implémenter le calcul dynamique du delta $n-1$ (session précédente du même circuit et de la même catégorie).
- [x] Appliquer la coloration conditionnelle des deltas (🟢 gain de temps, 🔴 perte de temps).

### Jalon — UI : Menu "Options & Paramètres" (`Settings.razor`) — 17/10/2026 · PR [#17](https://github.com/Fugeekies/LMU-DeltaRacing/pull/17)
- [x] Implémenter la vue de configuration du chemin des résultats XML (`UserData/Log/Results`).
- [x] Ajouter le bouton de parcours d'explorer et l'indicateur visuel de validité du dossier.
- [x] Permettre de tester et recharger la surveillance `FileSystemWatcher` depuis l'interface.
- [x] Déplacer le dashboard avec les imports réussi/échec ainsi que les session récente en dessous des options.
- [x] Indicateur de progression des imports dans Options.

### Jalon — UI : Shell & Layout LMU — 17/10/2026 · PR [#16](https://github.com/Fugeekies/LMU-DeltaRacing/pull/16)
- [x] Mettre en place le fond dégradé officiel LMU (`#0d1b2a` vers `#3a0d18`).
- [x] Créer la barre de navigation supérieure horizontale (`sticky top-0`) avec typographie condensée (`Oswald`).
- [x] Intégrer l'indicateur d'état du `FileSystemWatcher` dans le Header.
- [x] Appliquer le style des cartes sombres semi-transparentes avec effet de flou arrière-plan (`backdrop-filter: blur`).

### Jalon — Packaging, Déploiement & Installeur Autonome — 17/10/2026 · PR [#13](https://github.com/Fugeekies/LMU-DeltaRacing/pull/13)
- [x] Configurer le profil de publication .NET en `Self-Contained` et `Single-File` dans le projet UI.
- [x] Activer le `Trimming` pour réduire les dépendances distribuées.
- [x] Configurer un script **Inno Setup** pour générer l'installeur `LMU-DeltaRacing-Setup.exe`.
- [x] Détecter les dossiers de résultats LMU usuels à l'installation et sélectionner les fixtures si aucun résultat LMU n'est présent.
- [x] Anonymiser les noms des pilotes dans les fixtures livrées en conservant un alias cohérent entre les fichiers.
- [x] Ajouter un test automatisé vérifiant qu'aucune DLL externe n'est requise au lancement de l'exécutable publié.
- [x] Déclencher le build et la publication de l'installeur `.exe` depuis le workflow GitHub Actions lors de la création d'un tag de version.

### Jalon — Première interface Blazor — 16/10/2026 · PR [#11](https://github.com/Fugeekies/LMU-DeltaRacing/pull/11)
- [x] Mettre en place l'hébergement Blazor local, ouvert dans le navigateur par défaut et limité à `127.0.0.1`.
- [x] Afficher la liste des sessions importées.
- [x] Gérer les états de chargement, d'erreur et les listes vides.

### Jalon — Import dynamique des résultats — 16/10/2026 · PR [#10](https://github.com/Fugeekies/LMU-DeltaRacing/pull/10)
- [x] Surveiller l'arrivée de nouveaux fichiers XML (`FileSystemWatcher`) avec gestion du verrouillage de fichier.
- [x] Éviter les imports en double et gérer le registre des fichiers traités en mémoire pendant l'exécution.
- [x] Signaler les fichiers invalides sans interrompre le flux d'importation.
- [x] Exécuter un test de charge sur la surveillance de dossier avec injection simultanée de 50 fichiers XML.
- [x] Générer le compte rendu du test de charge dans `docs/benchmarks/dynamic-import-load-report.md`.

### Jalon — Statistiques, Graphiques — 16/10/2026 · PR [#9](https://github.com/Fugeekies/LMU-DeltaRacing/pull/9)
- [x] Ajouter une vue statistique groupée par circuit.
- [x] Ajouter une vue statistique groupée par circuit et par voiture (classe + équipe).
- [x] Ajouter le delta du meilleur tour valide par rapport à la session précédente du même circuit, ordonnée par date.
- [x] Afficher des graphiques Spectre.Console des meilleurs tours valides les plus récents.
- [x] Permettre de choisir dans le menu de lancement les vues statistiques à afficher et exclure les sessions Practice des statistiques.

### Jalon — Quality Gates & Benchmarks — 15/10/2026 · PR [#5](https://github.com/Fugeekies/LMU-DeltaRacing/pull/5)
- [x] Configurer un projet MSTest dédié aux benchmarks pour valider le parsing en masse de fichiers XML.
- [x] Mesurer des lots réalistes de 10/100/500 fichiers générés localement (fichiers temporaires non commités).
- [x] Mesurer le parsing en mémoire des sessions Practice, Qualifying et Race : 10 courses par jour pendant 10 ans, soit 10 × 3 × 365 × 10 = 109 500 sessions.
- [x] Générer facilement un rapport de performance Markdown, consultable dans la sortie détaillée du test et dans le dépôt.
- [x] Permettre de réduire la charge à 5 ans ou 2 ans si les ressources mémoire sont limitées (`-p:BenchmarkYears=5` ou `-p:BenchmarkYears=2`).

### Jalon — Gestion de la configuration — 15/10/2026 · PR [#4](https://github.com/Fugeekies/LMU-DeltaRacing/pull/4)
- [x] Configurer la gestion dynamique des paramètres applicatifs (`IOptionsMonitor<T>` / `appsettings.json`)
- [x] Permettre la configuration du chemin local du dossier de résultats LMU -> assets anonymisés par défaut
- [x] Configurer le chemin local du log -> fichier à côté de l'exécutable par défaut
- [x] Configurer le niveau minimal du fichier de log JSON via la configuration -> `Debug` par défaut (aucun log affiché dans la console)
- [x] Valider les chemins et le niveau de log au démarrage de l'application
- [x] Injecter les options dans les services concernés via le conteneur de dépendances
- [x] Modifier et sauvegarder les valeurs depuis le menu Spectre dans `appsettings.json`, à côté de l'exécutable

### Jalon — Monitoring des perfs et console Spectre — 14/10/2026 · PR [#1](https://github.com/Fugeekies/LMU-DeltaRacing/pull/1)
- [x] Mettre en place OpenTelemetry pour instrumenter l'application.
- [x] Configurer l'export des logs structurés en JSON Lines (un objet JSON par ligne).
- [x] Écrire les statistiques agrégées de mémoire et de durée/débit d'import dans un fichier dédié par exécution.
- [x] Ajouter une console de debug (Spectre.Console) pour suivre l'activité de l'application.
- [x] Afficher la progression du chargement des imports dans la console.

### Jalon — Socle métier et lecture XML — 14/10/2026 · commits initiaux, avant les PR
- [x] Définir les entités `Session`, `Driver`, `Lap` et `Sector`.
- [x] Ajouter les contrats du parser XML et du dépôt de sessions.
- [x] Implémenter le parser XML et le dépôt en mémoire.
- [x] Ajouter des tests ciblés à partir d'un fichier XML réel (MSTest).
