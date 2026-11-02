# Proposition — Identité et chronologie des sessions

Proposition demandée par le jalon du même nom dans [`ROADMAP.md`](../ROADMAP.md), validée le 02/11/2026 et implémentée dans la PR #52. Les chiffres viennent des 342 fixtures de `assets/Results`.

## En bref

| # | Décision | Choix retenu |
|---|---|---|
| 1 | Heure d'une session | `StartedAt` lit la date de la phase (`<Race><DateTime>`), à défaut `RaceResults/DateTime` |
| 2 | Identité d'une session | `Guid` calculé à partir du circuit, de la phase et de sa date |
| 3 | Fichier en double | Le premier importé est gardé, le second est signalé comme ignoré |
| 4 | Identité d'un événement | Identifiant de sa première session hors essais, à défaut de sa première session |

Effet attendu : les deux courses du 16/09/2026 ont toujours le même ordre, un lien `?event=` survit à un redémarrage et une copie de fichier n'est plus comptée deux fois. Aucun regroupement ne change.

## 1. Heure d'une session

**Constat sur les fixtures :**
- La date de phase est présente dans les 342 fichiers, essais compris, et jamais deux fois la même.
- `RaceResults/DateTime` est commun à une série de sessions : 102 dates sont partagées. Il précède la date de phase de 2 à 14 s en essais, de 3 à 205 s en qualification et de 3 à 969 s en course.
- Le `TimeString` de chaque phase est cohérent avec sa date.
- La date de phase précède toujours l'heure de sauvegarde du fichier (dans son nom). C'est compatible avec un début de phase, sans le prouver : LMU ne documente pas ce champ.
- À l'intérieur d'une série, trier par date de phase donne toujours essais, puis qualification, puis course (0 inversion).
- Une seule série a deux courses à égalité : `2026_09_16_15_09_20-06R1` et `2026_09_16_15_17_11-40R1`.

**Proposition :** garder le seul champ `StartedAt`, sans ajouter de seconde date, et y mettre la date de phase. Si elle manque, ce qui n'arrive dans aucune fixture, utiliser `RaceResults/DateTime` plutôt que de rejeter un fichier par ailleurs lisible.

## 2. Identité d'une session

**Proposition :** un `Guid` tiré des 16 premiers octets du SHA-256 de `TrackVenue|TrackCourse|nom de la phase|date de phase`. Le type de `Session.Id` et le dépôt ne changent pas.

| Option | Détecte une copie sous un autre nom | Coût | Verdict |
|---|---|---|---|
| Nom du fichier | Non, c'est le constat de départ | Nul | Écartée |
| Empreinte du contenu | Oui, si le fichier est strictement identique | 150 ms sur les 342 fichiers (25,8 Mo), pour un chargement d'environ 100 ms | Écartée |
| Circuit + phase + date | Oui, même si le fichier a été retouché | Négligeable : ces valeurs sont déjà lues | **Retenue** |

Sur les fixtures, cette clé est unique pour chaque fichier.

**Limite à accepter :** deux fichiers d'une même session venus de deux joueurs (dossiers fusionnés à la main) comptent comme un seul. Un fichier modifié à la main sans changer sa date est aussi ignoré si l'original est déjà chargé.

## 3. Fichier en double

**Proposition :**
- Nouveau statut `SessionImportStatus.SkippedDuplicate`, sur le modèle de `SkippedPractice`, avec un log `Information` qui ne cite que le nom du fichier.
- Le premier fichier importé reste en mémoire. `ISessionRepository` doit permettre un ajout qui échoue si l'identifiant existe, car `Save` remplace aujourd'hui silencieusement.
- Les doublons apparaissent comme ignorés, et non en échec, dans le résumé des imports du dashboard et de la console.

## 4. Identité d'un événement

**Constat supplémentaire :** l'identifiant d'un événement est celui de sa première session (`RaceEventPresentation.cs:72`). Même avec des sessions stables, il change quand on coche ou décoche « Charger les sessions d'essais », et les liens `?event=` cassent.

**Proposition :** prendre la première session hors essais, à défaut la première session. Seul cas restant : un événement réduit à des essais change d'identifiant à l'arrivée de sa qualification, donc un lien copié avant cette qualification ne fonctionne plus.

## Règles impactées

Simulation sur les fixtures de l'ancienne et de la nouvelle date, avec les règles actuelles :

| Règle | Avant | Après |
|---|---|---|
| Regroupement en événements (avec essais / sans essais) | 110 / 109 | Identique, aucun événement ne change |
| Appariement qualif/course de Driver Tom | 89 lignes | Identique, aucune paire ne change |
| Delta n-1 | Sens aléatoire entre les deux courses du 16/09/2026 | Toujours le même sens |
| Ordre des sessions récentes | Aléatoire pour ces deux courses | Stable |
| Granularité « jour » de Progression | — | Inchangée : aucune session ne change de jour local |
| Heures affichées | Début de la série | Début de la phase : jusqu'à 16 min plus tard en course, 3 min en qualification |

Le dernier point est le seul changement visible en dehors des deux courses du 16/09/2026.

Pour tenir compte d'une égalité future, les tris par `StartedAt` qui décident d'un ordre métier (`MyRacesPresentation`, `RaceEventPresentation`) gardent leur critère par nature de session, puis finissent sur `Session.Id`.

## Après validation

- **Parser :** lire la date de phase et calculer l'identifiant. L'empreinte SHA-256 de référence change, car elle inclut `StartedAt` : différence à justifier dans la PR, avec un rapport de benchmark à jour.
- **Tests sur les fixtures :** ordre et delta des deux courses du 16/09/2026 identiques à chaque lancement, lien `?event=` valide après un rechargement et après un changement de l'option d'essais, copie d'un fichier sous un autre nom signalée comme doublon.
- **Documentation :** faits vérifiés de `AGENTS.md` (le fait « partagent le même `RaceResults/DateTime` » reste vrai, mais ne donne plus l'heure d'une session), `docs/ARCHITECTURE.md`, `docs/UISpec/` pour les heures affichées, `CHANGELOG.md`.
- **Console :** affichage du nouveau statut dans le résumé des imports.
