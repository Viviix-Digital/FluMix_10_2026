# Hero VFX Audit Report

Date: 2026-10-23 (cleanup applied)  
Scope: Hero shot + win VFX on `FluMix` scene.

## Current live path

```
BattleController.FireBeam → FxPlayer.PlayBeam → FluMixShotVfx.PlayOverUi
  → LineShotA / LineShotG / LineShotT Configure

BossSequence → FxPlayer.PlayWinShield → FluMixShotVfx.PlayWinOverUi (EnergyShield)
```

## FluMixShotVfx fields (post-cleanup)

| Field | Asset |
|-------|-------|
| `lineShotAPrefab` | `Line EnergyBeams/LineShotA.prefab` |
| `lineShotGPrefab` | `Line EnergyBeams/LineShotG.prefab` |
| `lineShotTPrefab` | `Line EnergyBeams/LineShotT Configure.prefab` |
| `sceneShield` / `shieldPrefab` | EnergyShield |
| `shotPlaneZ` | `111.73` (replaces Canvas EnergyBeams Z) |

## Kept under `Assets/FluMix/VFX/`

- `Line EnergyBeams/LineShotA.prefab`
- `Line EnergyBeams/LineShotG.prefab`
- `Line EnergyBeams/LineShotT Configure.prefab`
- `EnergyShield/` (win particles)

## Removed in cleanup

**Code:** dead/idle fields + fallback `MakeShot` path; `Bind` / `PlayWinRoutine` / `SilenceOldBeams` / `EnergyBeamFx.cs`

**Scene / prefab:** `Canvas/EnergyBeams`, `FluMix/EnergyBeams`, EnergyBeams nested in `Assets/Prefabs/Canvas.prefab`

**Assets deleted:** Laser beam 1–10, demo interactions, Slash_orange_once, `EnergyBeams/` folder, `EnergyBeamFx.cs`

Hovl Studio vendor pack under `Assets/Hovl Studio/` left intact.

## Verify after cleanup (Play Mode)

| Check | Result |
|-------|--------|
| PlayOverUi A/G/T | PASS |
| LineShotA/G/T instances | PASS |
| PlayWinOverUi (21 particles) + HideWin | PASS |
| EnergyBeams remaining in hierarchy | 0 |

## Note

`Assets/Scenes/TestParticle.unity` previously referenced the deleted `EnergyBeams.prefab` — that test scene may show missing prefab refs and is outside the FluMix hero path.
