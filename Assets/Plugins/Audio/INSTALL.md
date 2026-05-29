# FMOD + Steam Audio setup (DZ_3C)

Unity **2022.3.17f1** — complete these steps once on each machine.

## 1. FMOD Studio + FMOD for Unity

1. Create a free account at [fmod.com](https://www.fmod.com/) and install **FMOD Studio 2.02+**.
2. Download **FMOD for Unity** (`.unitypackage`) matching your FMOD Studio version from [fmod.com/download](https://www.fmod.com/download).
3. In Unity: **Assets → Import Package → Custom Package** → import all FMOD files.
4. Open the FMOD project at repository path [`FMOD/DZ_3C.fspro`](../../../FMOD/DZ_3C.fspro) (create/open in FMOD Studio).
5. Copy Steam Audio FMOD plugin files from `steamaudio_fmod.zip` (Steam Audio release) into `FMOD/Plugins/` (see [Steam Audio FMOD docs](https://valvesoftware.github.io/steam-audio/doc/fmod/getting-started.html)).
6. Unity menu **FMOD → Edit Settings**:
   - **Studio Project Path**: `FMOD/DZ_3C.fspro`
   - **Build path**: `Assets/StreamingAssets/FMOD/Desktop` (or enable Live Update during development)
   - **Dynamic Plugins**: add `phonon_fmod`
7. Run **GameBuild → Audio → FMOD Migration Window** to generate `FMODSoundEvent` assets from existing prefabs.
8. Author events in FMOD Studio (paths must match generated CSV / `event:/...` strings), add **Valve → Steam Audio Spatializer** on 3D SFX events, then **Build** banks.

## 2. Steam Audio for Unity

1. Download [Steam Audio](https://github.com/ValveSoftware/steam-audio/releases) (`steamaudio_unity.zip`).
2. Import the Unity integration package from the zip.
3. Import `SteamAudioFMODStudio.unitypackage` from the same zip (`unity/` folder).
4. Unity menu **Steam Audio → Settings** → **Audio Engine** = **FMOD Studio**.
5. Run **GameBuild → Audio → Add Steam Audio Manager To Scene** on gameplay scenes.
6. Run **GameBuild → Audio → Tag Static Geometry for Steam Audio** on level meshes (or tag manually).

## 3. Validate

**GameBuild → Audio → Validate FMOD + Steam Audio Setup**

## 4. Re-wire scene references

After changing `*Audio` scripts to `FMODSoundEvent`:

- Assign events on Player, enemies, `MusicStateAudioController`, Timeline scene objects.
- Timeline: `TimelineLoadScene` / `ENDsequence` → `timelineBgm`, `timelineDialogue`, `timelineEndBgm`.

## Optional: download helper (Windows)

From repo root, if GitHub is reachable:

```powershell
.\Tools\DownloadAudioDependencies.ps1
```
