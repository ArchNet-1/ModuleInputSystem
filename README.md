<p align="center">
  <a href="https://archnet-plateform.fr"><img src="https://archnet-plateform.fr/ArchNetLogoWithBck.png" alt="ArchNet" width="240"></a>
</p>

# ArchNet · Input Module

Game controls on Unity's Input System, ready in one click, with the Back key handled everywhere and a screen where players change their keys.

- Unity **6000.0** or newer · Needs `com.unity.inputsystem` 1.11+, `com.unity.ugui` and [ArchNet Core](https://github.com/ArchNet-1/ArchNetCore)
- Part of [ArchNet](https://archnet-plateform.fr), free and open-source Unity packages

> The project must use the Input System (*Project Settings > Player > Active Input Handling*: Input System Package or Both). Unity asks to switch, and restarts, the first time the package is installed.

## Inputs (one click)

*ArchNet > Hub > Services > Inputs > Add to game* creates, in `Assets/ArchNet/Inputs/`:

| File | What it is |
|---|---|
| `GameControls.inputactions` | **Gameplay**: Move (WASD, arrows, left stick), Look (mouse, right stick), Jump (Space / A), Interact (E / X), Sprint (Shift / left stick press). **UI**: Back (Escape, Start, B). Keyboard & Mouse and Gamepad schemes. Edit it with **Open tool**. |
| `Scripts/GameInput.cs` | Your class: one property per action. |
| `Prefabs/Inputs.prefab` | Under the ArchNet Bootstrap. |

```csharp
var input = Services.Get<GameInput>();
Vector2 move = input.Move.ReadValue<Vector2>();
if (input.Jump.WasPressedThisFrame()) Jump();
```

**The Back key** (Escape, gamepad Start or B, Android back) closes the last ArchNet screen opened (popup = cancel, settings = saved, credits, controls, pause menu = resume). When none is open it raises `BackPressed`: the Hub's Gameplay scene pauses on it. Click **Update** on the Gameplay and Main menu cards after adding Inputs.

## Controls (one click)

*Services > Controls > Add to game* adds `Assets/ArchNet/Controls/Prefabs/Controls.prefab` under the Bootstrap: every action of the Gameplay map with its keyboard / mouse key and gamepad button; click one, press the new key (Escape cancels). Changes are saved to `persistentDataPath/input-bindings.json` and come back on the next launch; **Reset keys** goes back to the asset. The Main menu and the pause menu get a Controls button (Update their cards; a scene is created again only if you delete it).

```csharp
Services.Get<ControlsScreen>().Open();
```

## License

MIT, see [LICENSE.md](LICENSE.md).
