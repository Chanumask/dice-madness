# Dice Madness

Unity 6 prototype for a dice-based roguelite flow.

The project currently includes:
- a physics-based 3-dice prototype
- weighted face logic for dice outcomes
- a scene-based menu UI
- editor helpers for rebuilding dice visuals and menu UI

## Requirements

- Unity `6000.4.1f1`

## Project Structure

- `Assets/Scenes/Main.unity`
  Main working scene for the prototype.
- `Assets/Prefabs/Dice.prefab`
  Main dice prefab.
- `Assets/Scripts/Core/`
  Round flow, UI flow, and scene-level logic.
- `Assets/Scripts/Dice/`
  Dice rolling, face reading, and evaluation behavior.
- `Assets/Editor/`
  Editor tools for rebuilding prototype assets and UI.

## Getting Started

1. Open the project in Unity `6000.4.1f1`.
2. Open [Main.unity](/Users/joelfriedrich/Documents/Repositories/dice-madness/Assets/Scenes/Main.unity).
3. Press Play.
4. The project should start in the main menu.
5. Click `Enter Round / Play` to enter the dice prototype.
6. Press `Space` during a round to roll the dice.

## Current Flow

- `Main Menu`
  Entry point for the prototype.
- `Shop`
  Placeholder progression categories.
- `Challenges`
  Placeholder challenge/achievement view.
- `Settings`
  Placeholder settings view.
- `Round`
  Dice tray gameplay with in-round utility actions.

## Controls

- `Space`
  Roll dice during a round.
- UI buttons
  Navigate menus and return to the main menu.

## Useful Editor Tools

Available in Unity under `Tools > Dice Prototype`:

- `Build Prototype`
  Rebuilds the dice prototype scene/prefab setup.
- `Build Menu UI In Scene`
  Rebuilds the scene-based menu UI in `Main.unity`.
- `Refresh Dice Visuals`
  Regenerates the dice visual assets.
- `Refresh Menu TMP Text Styles`
  Reapplies standardized TMP text styles.
- `Refresh Menu Visual Styles`
  Reapplies centralized menu surface/button styling.
- `Refresh All Menu Styles`
  Reapplies both text and visual menu styles.

## Main Scripts

- [DiceManager.cs](/Users/joelfriedrich/Documents/Repositories/dice-madness/Assets/Scripts/Core/DiceManager.cs)
  Coordinates roll flow, result display, and post-roll evaluation positioning.
- [PrototypeGameFlowController.cs](/Users/joelfriedrich/Documents/Repositories/dice-madness/Assets/Scripts/Core/PrototypeGameFlowController.cs)
  Handles menu navigation and transitions between menu and round states.
- [DiceRoller.cs](/Users/joelfriedrich/Documents/Repositories/dice-madness/Assets/Scripts/Dice/DiceRoller.cs)
  Drives die motion and evaluation presentation behavior.
- [DiceFaceReader.cs](/Users/joelfriedrich/Documents/Repositories/dice-madness/Assets/Scripts/Dice/DiceFaceReader.cs)
  Stores face definitions and resolves the current top face.

## Version Control Notes

The repo uses a Unity-focused `.gitignore`, so generated folders like `Library/`, `Temp/`, `Logs/`, and IDE files should stay out of version control.

Files that should normally be committed:
- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `.meta` files

## Notes

- `Main.unity` is the active prototype scene.
- `SampleScene.unity` is still present because it is referenced by current Unity project settings.
- Some shop/challenge/settings content is still placeholder content intended for later expansion.
