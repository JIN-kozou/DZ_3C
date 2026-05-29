# FMOD + Steam Audio build checklist

Run after importing packages (see `Assets/Plugins/Audio/INSTALL.md`).

## Editor

- [ ] **GameBuild → Audio → Validate FMOD + Steam Audio Setup** — both OK
- [ ] **GameBuild → Audio → FMOD Migration Window** — generate `FMODSoundEvent` assets + CSV
- [ ] FMOD Studio — build banks to `Assets/StreamingAssets/FMOD/Desktop`
- [ ] Assign `FMODSoundEvent` on Player, enemies, music controller, Timeline scenes
- [ ] **GameBuild → Audio → Tag Static Geometry for Steam Audio** on `Level test`

## Play mode

- [ ] Footsteps / gunfire spatialized, muffled behind walls
- [ ] Exploration ↔ combat music crossfade
- [ ] Timeline intro/outro BGM fade
- [ ] AI still reacts to `AINoiseAudioBridge` (unchanged)

## Player build

- [ ] Development Build — no FMOD bank load errors in log
- [ ] `StreamingAssets/FMOD/Desktop/*.bank` present in build output
- [ ] Windows: `phonon.dll` / FMOD plugins included (verify if using Steam Audio)
