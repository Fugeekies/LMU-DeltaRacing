## 4. ⚙️ Menu : Options (Settings.razor)

### Description
Panneau de configuration permettant de modifier l'emplacement des fichiers XML du jeu et d'ajuster les options de surveillance.

### Spécifications fonctionnelles
* Gestion du dossier : Champ de saisie + Bouton "Parcourir..." (sélecteur de dossier).
* Vérification : Indicateur d'état dynamique (🟢 Dossier valide avec nombre de fichiers XML / 🔴 Dossier introuvable).
* Boutons d'action : "Tester la connexion & recharger" et "Enregistrer".

### Wireframe ASCII
```text
┌───────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [LMU DELTARACING]   MES COURSES   CLASSEMENT DES COUREURS   DÉTAIL DES COURSES   OPTIONS       🟢 ONLINE   │
├───────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                           │
│  ⚙️ CONFIGURATION & PARAMÈTRES                                                                            │
│                                                                                                           │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 📁 EMPLACEMENT DES FICHIERS DU JEU                                                                  │  │
│  │                                                                                                     │  │
│  │ Dossier des résultats XML (UserData/Log/Results) :                                                  │  │
│  │ [ C:\Program Files (x86)\Steam\steamapps\common\Le Mans Ultimate\UserData\Log\Results   ] [PARCOURIR]│  │
│  │                                                                                                     │  │
│  │ Status : 🟢 Dossier valide (365 fichiers détectés)                                                  │  │
│  │ [ 🔄 TESTER LA CONNEXION & RECHARGER ]                                                              │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                                           │
│  ┌─────────────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ 🔔 SURVEILLANCE & DÉMARRAGE                                                                         │  │
│  │                                                                                                     │  │
│  │ ☑️ Lancer la surveillance des fichiers XML automatiquement au démarrage de l'application           │  │
│  └─────────────────────────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                                           │
│                                                                                     [ 💾 ENREGISTRER ]    │
└───────────────────────────────────────────────────────────────────────────────────────────────────────────┘
