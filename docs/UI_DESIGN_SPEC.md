# Directives Graphiques et Maquette UI - LMU-DeltaRacing

Ce document sert de référence visuelle et fonctionnelle pour la conception des composants de l'interface utilisateur Blazor de **LMU-DeltaRacing**, inspirée par l'identité officielle du championnat WEC et de *Le Mans Ultimate*.

---

## 🎨 Charte Graphique & Code Couleurs WEC

L'interface doit adopter une esthétique sombre (*Dark Racing Theme*), rappelant un mur des stands ou un logiciel d'ingénierie de course professionnel.

### 1. Palette de Couleurs Principales
- **Fond Principal (Background) :** `#0e1116` (Gris très sombre / Noir mat)
- **Fond des Cartes / Panneaux (Cards/Surfaces) :** `#161b22` ou `#1f242c`
- **Bordures & Séparateurs :** `#30363d` (Gris anthracite discret)
- **Texte Principal :** `#f0f6fc` (Blanc cassé / très lisible)
- **Texte Secondaire :** `#8b949e` (Gris clair pour les libellés)
- **Accent Principal (Brand) :** `#3b82f6` (Bleu électrique LMU)

### 2. Le Code Couleur Magique du Chronométrage (Secteurs & Deltas)
Ces codes couleurs doivent impérativement être respectés pour l'affichage des temps au tour et des secteurs (*Timing Screen*) :
- 🟣 **Violet (`#b066ff`)** : Meilleur secteur absolu de la session (*Session Best*).
- 🟢 **Vert (`#22c55e`)** : Meilleur secteur personnel (*Personal Best*).
- 🟡 **Jaune (`#eab308`)** : Secteur standard / correct.
- 🔴 **Rouge (`#ef4444`)** : Secteur en retard / perte de temps.

### 3. Badges de Catégories WEC
- **Hypercar :** Fond rouge foncé / texte blanc
- **LMP2 :** Fond bleu vif / texte blanc
- **LMGT3 :** Fond orange / texte noir ou blanc

---

## 📐 Maquette du Dashboard Principal ("Le Mur des Stands")

Ci-dessous la structure visuelle attendue pour la vue d'accueil / tableau de bord général :

```text
┌────────────────────────────────────────────────────────────────────────────────────────────────┐
│ 🏎️ LMU-DeltaRacing                      🟢 FileSystemWatcher : Actif (12 sessions en mémoire)  │
├────────────────────────────────────────────────────────────────────────────────────────────────┤
│ [ 📊 Dashboard ]    [ ⏱️ Timing Screen ]    [ 📈 Analyse & Delta ]    [ ⚙️ Configuration ]    │
└────────────────────────────────────────────────────────────────────────────────────────────────┘

 ⚡ BIENVENUE DANS TON ESPACE INGÉNIEUR DE PISTE

 ┌──────────────────────────────┐  ┌──────────────────────────────┐  ┌──────────────────────────────┐
 │ 🕒 DERNIÈRE SESSION          │  │ ⏱️ TEMPS TOTAL PILOTÉ        │  │ 🛡️ INDICE DE PROPRETÉ        │
 │ 6H Spa-Francorchamps         │  │ 42h 18m                      │  │ 94.2%                        │
 │ Hypercar • Ferrari 499P      │  │ Équivalent 7 relais complets │  │ Moyenne des pénalités : Basse│
 └──────────────────────────────┘  └──────────────────────────────┘  └──────────────────────────────┘

 📋 HISTORIQUE RÉCENT DES COURSES IMPORTÉES
──────────────────────────────────────────────────────────────────────────────────────────────────
 Date / Heure       Circuit            Catégorie        Voiture              Pilotes   Meilleur Tour
──────────────────────────────────────────────────────────────────────────────────────────────────
 16/10/2026 18:00   Spa-Francorchamps  [ HYPERCAR ]     Ferrari 499P         24        2:02.451  👁️
 15/10/2026 21:15   Le Mans (Circuit)  [   LMP2   ]     Oreca 07 Gibson      18        3:26.102  👁️
 14/10/2026 19:30   Monza              [  LMGT3   ]     BMW M4 GT3           30        1:47.890  👁️
 12/10/2026 14:00   Sebring            [ HYPERCAR ]     Toyota GR010         22        1:45.120  👁️
──────────────────────────────────────────────────────────────────────────────────────────────────
```

---

## 🛠️ Instructions d'implémentation pour l'UI Blazor

1. **Isolation des styles :** Utiliser des composants Razor modulaires avec des classes CSS dédiées ou Tailwind CSS si configuré.
2. **Accessibilité & Contraste :** Le texte blanc sur fond sombre doit conserver un ratio de contraste élevé pour éviter la fatigue visuelle lors des sessions de simulation nocturnes.
3. **Réactivité :** Les tableaux de timing et de sessions doivent supporter le défilement fluide (*scroll*) en cas de grilles de départ importantes (plus de 40 voitures).
