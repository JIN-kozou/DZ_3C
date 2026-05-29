# DZ_3C FMOD Studio project

1. Open **FMOD Studio** → **Open** → select `DZ_3C.fspro` (create from template if missing).
2. Add **Steam Audio** plugins under `Plugins/` (from `steamaudio_fmod.zip`).
3. Event naming (examples):
   - `event:/Music/Exploration`, `event:/Music/Combat`
   - `event:/Player/Footstep`, `event:/Player/breath`
   - `event:/Enemy/lazer`, `event:/Timeline/Intro_BGM`
4. Build banks to Unity: `Assets/StreamingAssets/FMOD/Desktop`

Use **GameBuild → Audio → FMOD Migration Window** in Unity to sync ScriptableObject paths with prefab clips.
