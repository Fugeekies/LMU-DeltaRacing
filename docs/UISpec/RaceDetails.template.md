## 3. ⏱️ Menu : Détail des Courses (`RaceDetails.razor`)

> Spécification alignée sur le code au 25/10/2026 (`RaceDetails.razor`, `RaceDetailsPresentation.cs`).

### Description
Classement détaillé d'une session choisie par l'utilisateur, avec les secteurs du meilleur tour de chaque pilote et leurs écarts aux meilleurs temps de la session (route `/race-details`).

### Spécifications fonctionnelles
* **Sélecteur de session :**
  * Liste les sessions importées (qualifications et courses) contenant le pilote sélectionné, de la plus récente à la plus ancienne.
  * Libellé : `dd/MM/yyyy HH:mm - <Lieu> (<Type de session>) - <Classe du pilote>` (heure locale ; la classe est omise si elle est vide).
  * Par défaut, la session la plus récente est sélectionnée. Si la session sélectionnée disparaît (rechargement), la plus récente est resélectionnée.
* **Métadonnées** à côté du sélecteur : badge du type de session, lieu, tracé s'il diffère du lieu.
* **Ordre du classement :** position croissante, pilotes sans position en fin de liste, égalité départagée par nom.
* **Calculs par pilote :**
  * **Meilleur tour** : tour chronométré le plus rapide du pilote.
  * **Secteurs S1 à S3 affichés** : secteurs de ce meilleur tour (et non le meilleur secteur de chaque type).
  * **Meilleur personnel d'un secteur** : plus petit temps de ce secteur sur tous les tours du pilote.
  * **Meilleur absolu d'un secteur** : plus petit temps de ce secteur sur tous les tours de tous les pilotes de la session, toutes classes confondues.
  * **Delta secteur** = secteur affiché − meilleur absolu de la session pour ce secteur.
  * **Delta tour** = meilleur tour du pilote − meilleur tour de la session.
* **Règles de couleurs WEC (secteurs) :**
  * Secteur égal au meilleur absolu de la session : fond violet / texte `#b066ff`.
  * Sinon, secteur égal au meilleur personnel du pilote : fond vert / texte `#22c55e`.
  * Sinon : texte jaune `#eab308`.
  * Secteur absent : `—`.
* **Couleurs des deltas :** delta ≤ 0 (meilleur temps) en vert, delta > 0 en rouge, valeur absente en gris (`—`).
* **Structure du tableau :**

  | Colonne | Contenu | Format |
  |---|---|---|
  | POS | Position de classement | `01`, `02`… ou `—` |
  | NOM DU COUREUR | Nom, suivi du tag **VOUS** pour le pilote sélectionné | texte |
  | VOITURE | `CarType`, à défaut `#<CarNumber>`, à défaut `—` | texte |
  | SECTEUR 1 / 2 / 3 | Secteur du meilleur tour, coloré selon les règles WEC | `38.120s` |
  | DELTA S1 / S2 / S3 | Écart au meilleur absolu du secteur | `+0.130s` / `0.000s` |
  | TEMPS TOUR | Meilleur tour du pilote | `122.451s` (secondes) |
  | DELTA TOUR | Écart au meilleur tour de la session | `+0.469s` |

* **Légende** sous le tableau : Meilleur absolu session (violet), Meilleur personnel (vert), Temps standard (jaune).
* **États de la page :**
  * Chargement initial sans session : spinner.
  * Erreur de chargement sans session : encadré d'erreur renvoyant vers Options.
  * Aucune session pour le pilote : « Aucune session disponible ».
  * Session sans pilote : « Cette session ne contient aucun résultat de pilote ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS     Driver 2038  🟢 SURVEILLANCE ACTIVE │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  ANALYSE DE SESSION                                                                                                          │
│  Détail des courses                                                                                                          │
│  Classement, meilleurs secteurs et écarts de la session sélectionnée.                                                        │
│                                                                                                                              │
│  SÉLECTIONNER UNE COURSE                                                                                                     │
│  [ 16/10/2026 19:00 - Circuit de Spa-Francorchamps (Race) - Hyper ▼ ]     [Race]  Circuit de Spa-Francorchamps               │
│                                                                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ POS NOM DU COUREUR     VOITURE       S1          Δ S1     S2          Δ S2     S3          Δ S3     TOUR      Δ TOUR  │  │
│  ├────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤  │
│  │ 01  Driver 0412        Ferrari 499P  🟣 38.120s  0.000s   🟢 44.210s  +0.030s  🟡 40.121s  +0.031s  122.451s  0.000s  │  │
│  │ 02  Driver 2161        Toyota GR010  🟢 38.250s  +0.130s  🟣 44.180s  0.000s   🟣 40.090s  0.000s   122.520s  +0.069s │  │
│  │ 03  Driver 2038 VOUS   Ferrari 499P  🟢 38.310s  +0.190s  🟡 44.400s  +0.220s  🟡 40.210s  +0.120s  122.920s  +0.469s │  │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                                                              │
│  🟣 Meilleur absolu session   🟢 Meilleur personnel   🟡 Temps standard                                                       │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Écarts connus
Les comportements à corriger ou à arbitrer sont suivis dans la [feuille de route](../ROADMAP.md), jalon « Écarts UI à traiter ».
