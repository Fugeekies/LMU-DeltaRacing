## 3. ⏱️ Menu : Détail des Courses (`RaceDetails.razor`)

> Spécification alignée sur le code au 25/10/2026 (`RaceDetails.razor`, `RaceDetailsPresentation.cs`).

### Description
Classement détaillé d'une session choisie par l'utilisateur (route `/race-details`). Pour chaque pilote, la page affiche ses meilleurs secteurs, son tour idéal, son meilleur tour et son écart au leader. Toutes les comparaisons se font **au sein de la classe du pilote**, comme dans le chronométrage WEC.

### Spécifications fonctionnelles
* **Sélecteur de session :**
  * Liste les sessions importées (qualifications et courses) contenant le pilote sélectionné, de la plus récente à la plus ancienne.
  * Libellé : `dd/MM/yyyy HH:mm - <Lieu> (<Type de session>) - <Catégorie du pilote>` (heure locale ; la catégorie est omise si la classe est vide).
  * Par défaut, la session la plus récente est sélectionnée. Si la session sélectionnée disparaît (rechargement), la plus récente est resélectionnée.
* **Métadonnées** à côté du sélecteur : badge du type de session, lieu, tracé s'il diffère du lieu.
* **Ordre du classement :** position générale croissante, pilotes sans position en fin de liste, égalité départagée par nom.
* **Tours pris en compte :** uniquement les tours chronométrés. LMU efface le temps des tours invalidés pour limites de piste et des tours incomplets ; leurs secteurs sont donc exclus.
* **Calculs par pilote :**
  * **Classe** : `CarClass` du pilote. Les références « de la classe » ci-dessous ne portent que sur les pilotes de cette classe.
  * **Secteurs S1 à S3** : meilleur temps personnel de chaque secteur, tous tours chronométrés confondus (pas nécessairement sur le même tour).
  * **Tour idéal** : somme des trois meilleurs secteurs personnels, si les trois sont connus.
  * **Meilleur tour** : tour chronométré le plus rapide du pilote.
  * **Delta secteur** = meilleur secteur du pilote − meilleur secteur de la classe.
  * **Delta tour** = meilleur tour du pilote − meilleur tour de la classe.
  * **Leader de la classe** : pilote de plus petite position en classe (`ClassPosition`, à défaut position générale).
* **DIFF 1ER (écart au leader de la classe) :**
  * Leader de la classe : `LEADER`.
  * En course, dans cet ordre :
    1. pilote `DNF` ou `DQ` (`FinishStatus`) : le statut est affiché ;
    2. pilote avec moins de tours que le leader : `+1 tour` / `+N tours` ;
    3. sinon : temps total du pilote − temps total du leader (`FinishTime`), par exemple `+12.500s` ;
    4. donnée manquante : `—`.
  * En qualification : meilleur tour du pilote − meilleur tour de la classe.
* **Règles de couleurs WEC (secteurs) :**
  * Meilleur secteur de la classe : fond violet / texte `#b066ff`.
  * Sinon : texte jaune `#eab308`.
  * Secteur absent : `—`.
* **Couleurs des deltas :** delta ≤ 0 en vert, delta > 0 en rouge, valeur absente en gris (`—`).
* **Structure du tableau :**

  | Colonne | Contenu | Format |
  |---|---|---|
  | POS | Position générale | `01`, `02`… ou `—` |
  | CLASSE | Badge de catégorie (voir « Mes courses ») + position en classe | `LMGT3 P2` |
  | NOM DU COUREUR | Nom, suivi du tag **VOUS** pour le pilote sélectionné | texte |
  | VOITURE | `CarType`, à défaut `#<CarNumber>`, à défaut `—` | texte |
  | SECTEUR 1 / 2 / 3 | Meilleur secteur personnel, coloré selon les règles WEC | `38.120s` |
  | DELTA S1 / S2 / S3 | Écart au meilleur secteur de la classe | `+0.130s` / `0.000s` |
  | TOUR IDÉAL | Somme des meilleurs secteurs | `m:ss.fff` |
  | MEILLEUR TOUR | Meilleur tour du pilote | `m:ss.fff` |
  | DELTA TOUR | Écart au meilleur tour de la classe | `+0.469s` |
  | DIFF 1ER | Écart au leader de la classe | `LEADER`, `+12.500s`, `+1 tour`, `DNF` |

* **Grandes grilles :** au-delà de 40 pilotes, les lignes sont virtualisées (seules les lignes visibles sont rendues). Jusqu'à 40 pilotes, toutes les lignes sont rendues, y compris dans le pré-rendu serveur.
* **Légende** sous le tableau : Meilleur secteur de la classe (violet), Temps standard (jaune), et rappel que les secteurs sont des meilleurs personnels comparés par classe.
* **États de la page :**
  * Chargement initial sans session : spinner.
  * Erreur de chargement sans session : encadré d'erreur avec un lien vers Options.
  * Aucune session pour le pilote : « Aucune session disponible ».
  * Session sans pilote : « Cette session ne contient aucun résultat de pilote ».
  * La page se met à jour automatiquement à chaque nouvel import.

### Wireframe ASCII
```text
┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU] DELTARACING   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS                       Driver 2038  🟢 SURVEILLANCE ACTIVE │
├────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  ANALYSE DE SESSION                                                                                                                                │
│  Détail des courses                                                                                                                                │
│  Classement, meilleurs secteurs et écarts de la session sélectionnée, comparés au sein de chaque classe.                                          │
│                                                                                                                                                    │
│  SÉLECTIONNER UNE COURSE                                                                                                                           │
│  [ 02/10/2026 21:00 - Circuit de Spa-Francorchamps (Race) - LMGT3 ▼ ]     [Race]  Circuit de Spa-Francorchamps                                     │
│                                                                                                                                                    │
│  POS CLASSE     NOM DU COUREUR    VOITURE         S1          Δ S1     S2          Δ S2     S3          Δ S3     IDÉAL     MEILLEUR  Δ TOUR   DIFF 1ER │
│  01  LMGT3 P1   Driver 0285       BMW M4 LMGT3    🟣 41.859s  0.000s   🟡 74.351s  +0.370s  🟣 44.395s  0.000s   2:40.605  2:41.796  0.000s   LEADER   │
│  02  LMGT3 P2   Driver 1379       Porsche 911     🟡 42.567s  +0.708s  🟡 75.294s  +1.313s  🟡 45.957s  +1.561s  2:43.817  2:44.243  +2.448s  +23.480s │
│  04  LMGT3 P4   Driver 2038 VOUS  Porsche 911     🟡 42.478s  +0.619s  🟡 74.602s  +0.621s  🟡 45.835s  +1.439s  2:42.914  2:44.849  +3.054s  +49.908s │
│  07  LMGT3 P7   Driver 0282       Mercedes-AMG    🟡 43.085s  +1.225s  🟡 76.873s  +2.893s  🟡 45.953s  +1.558s  2:45.910  2:46.476  +4.680s  +1 tour  │
│  11  LMGT3 P11  Driver 0307       McLaren 720S    🟡 42.411s  +0.551s  🟡 75.358s  +1.378s  🟡 45.068s  +0.672s  2:42.836  2:43.196  +1.400s  DNF      │
│  12  LMGT3 P12  Driver 2218       Porsche 911     🟡 42.756s  +0.896s  🟣 73.981s  0.000s   🟡 45.013s  +0.617s  2:41.749  2:42.012  +0.217s  DNF      │
│                                                                                                                                                    │
│  🟣 Meilleur secteur de la classe   🟡 Temps standard   Secteurs : meilleur temps personnel sur les tours chronométrés · Écarts calculés par classe │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```
