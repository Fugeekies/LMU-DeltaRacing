# Proposition — Interface multilingue

> Statut : **validée** le 05/11/2026 (décisions en fin de document). Jalon « Interface multilingue » de la [feuille de route](../ROADMAP.md).

## 1. Constat

- Aucun mécanisme de traduction : tous les textes sont écrits en dur en français.
- Volume estimé (relevé automatique, approximatif) :
  - pages et composants Razor : environ **390 lignes** contenant du texte affiché (contenu, `title`, `placeholder`, `aria-label`) ;
  - calculs partagés de `Presentation` : environ **180 chaînes**, dont les libellés des tuiles, descriptions d'incidents (`DrivingConductPresentation.Describe`), paliers de la cote et formats (`DisplayFormat`) ;
  - console : environ **100 chaînes** ;
  - installeur : une dizaine de libellés.
- Formats propres au français : ordinal `1er` / `2e` (`DisplayFormat.Ordinal`), dates `dd/MM/yyyy`, séparateur de milliers (`1 568`), `P1` en `fr-FR` dans la console.
- Le guide d'utilisation existe déjà en cinq langues (`docs/guide/`), qui citent les libellés français de l'écran suivis de leur traduction.

## 2. Langues

**Les mêmes que le guide** : anglais (`en`), français (`fr`), allemand (`de`), italien (`it`), espagnol (`es`). Le français reste la langue de référence : un texte s'écrit d'abord en français, puis se traduit dans les quatre autres.

## 3. Mécanisme de traduction

**Proposé : fichiers de ressources `.resx` et `IStringLocalizer`**, le mécanisme standard de .NET.

- Un fichier par langue dans `Presentation` (partagé par l'UI et la console) : `Resources/Texts.resx` (français, langue neutre), `Texts.en.resx`, `Texts.de.resx`, `Texts.it.resx`, `Texts.es.resx`.
- Clés en anglais, nommées par page et rôle : `MyRaces.Title`, `RaceDetails.Column.Pos`, `Incident.ObstacleContact.Post`.
- Pages Razor : `@inject IStringLocalizer<Texts> T`, puis `@T["MyRaces.Title"]`. Textes avec valeurs : `T["Rating.PointsToNextTier", points]` (format composite, pas d'interpolation).
- `Presentation` : les calculs gardent leurs données ; seuls les textes destinés à l'écran passent par le localiseur. Les tests existants, qui vérifient du français, tournent en culture `fr`.
- Compatible avec la publication actuelle (`PublishSingleFile`, sans trimming) : les assemblies satellites sont incluses dans l'exécutable.

Alternatives écartées :
- **Fichiers JSON maison** : lisibles, mais à charger, valider et mettre en cache soi-même, sans outillage.
- **Une classe C# par langue** (`switch` sur la langue) : vérifiée à la compilation, mais lourde à maintenir à cinq langues et illisible pour un traducteur.

## 4. Choix de la langue

- **Nouvelle section « Langue et date »** dans Options, enregistrée dans `appsettings.json` :
  - **Langue** : `LMU:Language` (`en`, `fr`, `de`, `it`, `es`) ;
  - **Format de date** : `LMU:DateFormat`, « Selon la langue » (par défaut, `Language`) ou l'un de quatre formats fixes, chacun affiché avec un exemple : `05/11/2026` (jour/mois, `DayMonthYear`), `11/05/2026` (mois/jour, `MonthDayYear`), `2026-11-05` (ISO, `Iso`), `05.11.2026` (`DotDayMonthYear`). L'heure reste sur 24 h (`21:00`).
- **Langue par défaut :** celle de Windows (`CultureInfo.CurrentUICulture`) si elle fait partie des cinq, **anglais** sinon. Un utilisateur actuel sur un Windows français garde donc l'interface française.
- Le serveur n'écoute que `127.0.0.1` et sert un seul utilisateur : la langue est **globale à l'application** (pas de langue par navigateur). Changer d'option applique la langue et recharge la page.

## 5. Formats

- **Selon la langue choisie :** ordinaux (`1st`, `1er`, `1.`, `1º`, `1.º`), séparateur de milliers, pourcentages, et dates quand le format est « Selon la langue » (`05/11/2026` en anglais britannique et en français, `05.11.2026` en allemand…).
  - Anglais : culture `en-GB` (jour avant le mois) plutôt que `en-US` ; le format mois/jour reste disponible dans l'option Format de date.
- **Selon l'option Format de date :** toutes les dates affichées (`DisplayFormat.DateTime`, `DisplayFormat.Day`), dans le dashboard comme dans les libellés partagés.
- **Inchangés dans toutes les langues :**
  - la lecture des temps du XML (`CultureInfo.InvariantCulture`, règle d'`AGENTS.md`) ;
  - l'affichage des temps au tour et des écarts (`1:23.456`, `+0.130s`) : point décimal, comme sur les écrans de chronométrage ;
  - les données venant de LMU : noms des circuits, des voitures et des pilotes, classes (`LMGT3`, `Hypercar`), motifs de pénalité (en anglais dans les fichiers) ;
  - les logs (anglais, messages structurés).

## 6. Console

Le jalon « Avenir de la console Spectre » n'est pas tranché. **Proposé :** la console reste **en français** pour l'instant. Elle force la culture `fr` au démarrage, donc les textes partagés de `Presentation` y restent en français, sans option Langue. Elle suivra le dashboard si le jalon console décide d'en faire une application à part entière.

## 7. Installeur Inno Setup

- Ajouter les cinq langues à l'installeur (`[Languages]` : fichiers de messages fournis avec Inno Setup), avec choix de la langue au lancement, proposé par défaut selon Windows.
- Traduire ses propres libellés (icône sur le bureau, lancement) dans `[CustomMessages]`.
- À la première installation, la langue choisie dans l'installeur est écrite dans `LMU:Language`, et le format de date dans `LMU:DateFormat` à « Selon la langue », comme l'installeur le fait déjà pour le dossier des résultats. Aucune page de plus dans l'installeur : le format se change ensuite dans Options. Une mise à jour ne change ni la langue ni le format enregistrés.

## 8. Tests

- Chaque clé existe dans les cinq fichiers, sans valeur vide, avec les mêmes paramètres (`{0}`, `{1}`).
- Chaque clé utilisée dans le code existe dans la ressource (pas de clé orpheline ni de clé manquante).
- Ordinaux et dates dans chaque langue.
- Le rendu français reste identique : les tests actuels de `Presentation` et de la console passent sans modification de leurs attentes.

## 9. Documentation

- `AGENTS.md` : la règle « l'UI est en français » devient « l'UI est en cinq langues, le français fait référence » ; tout texte affiché passe par les ressources.
- `UI_DESIGN_SPEC.md` et les specs `UISpec/` restent en français et gardent les libellés français : ils servent de référence pour les clés.
- Guide d'utilisation : une fois l'interface traduite, chaque langue cite les libellés de **sa** langue, sans la traduction entre parenthèses.

## 10. Découpage proposé

1. **Mécanisme et extraction, français seul** : ressources, localiseur, extraction des textes des pages et de `Presentation`. Le rendu ne change pas, ce que vérifient les tests existants.
2. **Traductions et section Langue et date** : les quatre autres fichiers, la section dans Options (langue et format de date), la langue par défaut, les formats.
3. **Installeur et documentation** : langues de l'installeur (langue et format de date écrits à la première installation), guide sans traductions entre parenthèses, `AGENTS.md` et specs.

## Décisions (validées le 05/11/2026)

1. Mécanisme : `.resx` + `IStringLocalizer` (§ 3). **Validé.**
2. Langue par défaut : langue de Windows, anglais sinon (§ 4). **Validé.**
3. Dates : section **Langue et date** dans Options, avec le choix de la langue et du format de date (« Selon la langue » ou 4 formats fixes, heure sur 24 h) ; anglais en `en-GB` quand le format suit la langue (§ 4, § 5). **Décidé** à la place du seul `en-GB`.
4. Temps au tour et écarts gardés avec le point décimal dans toutes les langues (§ 5). **Validé.**
5. Console gardée en français en attendant le jalon console (§ 6). **Validé.**
6. Installeur en cinq langues, qui écrit la langue choisie et le format « Selon la langue » à la première installation (§ 7). **Validé, avec le format de date.**
7. Découpage en trois PR (§ 10). **Validé.**
