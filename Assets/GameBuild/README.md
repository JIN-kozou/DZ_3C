# GameBuild — 打包用资源目录

本目录集中存放 **Player 构建** 实际使用的场景、脚本、Resources、Prefab 等资源，按类型分子文件夹。

## 构建场景（Build Settings）

| 场景 | 路径 |
|------|------|
| 主菜单 | `Scenes/MainMenu.unity` |
| Timeline | `Scenes/Timeline.unity` |
| 关卡 | `Scenes/Level test.unity` |
| 结局 Timeline | `Scenes/TimelineEND.unity` |

测试场景在 `Scenes/_Dev/`（默认不勾选进 Build）。

## 目录结构

```
GameBuild/
├── Scenes/              # 发布与测试场景
├── Scripts/
│   ├── Core/            # 玩家、相机、音频、UI、状态机、Timeline
│   └── Gameplay/        # 反转重力、机器修复、AI、世界交互
├── Resources/           # Resources.Load + 打进 resources.assets
│   ├── Config/          # ScriptableObject 配置
│   ├── Model/           # 角色/武器/环境模型与动画
│   ├── Prefab/          # 运行时 Prefab（玩家、相机、AI）
│   ├── UI/              # UI 贴图
│   └── WorldInteraction/  # 世界交互 Prompt / Marker
├── Prefabs/             # 场景直接引用的 Prefab
├── Art/                 # 动画包、字体、模型资源
├── Audio/               # 旧 AudioSource Prefab（保留作 FMOD 迁移参考）
├── Shaders/             # 项目 Shader / ShaderGraph
├── Timeline/            # Timeline 资源
├── Settings/            # URP、Animancer 等项目设置
├── HUD/                 # 准星、命中标记
└── Config/              # 非 Resources 的配置资产
```

## Resources.Load 路径（运行时）

| 路径 | 用途 |
|------|------|
| `Config/UI/WorldInteractionConfig` | 世界交互配置 |
| `Config/Reverse/ReverseConfig` | 反转重力配置 |
| `Config/Weapon/DefaultGun` | 默认枪械配置 |
| `Config/MachineRepair/*` | 机器修复部件与规则 |
| `WorldInteraction/WorldInteractionPrompt` | 交互提示 UI |
| `WorldInteraction/WorldInteractionMarker` | 交互标记 |

## 仍在 `Assets/` 根目录的第三方（随场景引用打包）

- `Packages/com.kybernetik.animancer` — Animancer
- `Assets/GameSoftCraft/S.P.A.C.E/` — 星空背景
- `Assets/Blackhole/` — 黑洞天空
- `Assets/Snapshot Shaders Pro/` — 后处理（若使用）
- `Assets/Cuboom/CB Sci-Fi Pack/` — Timeline 场景道具
- `Assets/TextMesh Pro/` — UI 文字
- `Assets/Plugins/` — AllIn1 Sprite Shader、Excelsior 等

## 音频（FMOD + Steam Audio）

- FMOD 工程：仓库根目录 [`FMOD/DZ_3C.fspro`](../../FMOD/DZ_3C.fspro)
- Bank 输出：`Assets/StreamingAssets/FMOD/Desktop/`
- 运行时 API：[`Scripts/Core/Audio/GameAudio.cs`](Scripts/Core/Audio/GameAudio.cs)
- 事件资产：`Resources/Config/Audio/Events/`（`FMODSoundEvent`）
- 安装与验证：[`Assets/Plugins/Audio/INSTALL.md`](../Plugins/Audio/INSTALL.md)
- Unity 菜单：**GameBuild → Audio → …**
- AI 听觉仍使用 `AINoiseEmitter` / `HearingPerceptor`（与玩家听感分离）

## Appear 双材质揭示（Ghost + 不透明揭示层）

使用 `AppearShader` 的物体可挂 **`AppearRevealDualRenderer`**：

- 每个 **Appear 材质槽** 保持与 submesh 一一对应；**RevealedOpaque** 通过 `Graphics.DrawMesh` 按 submesh 额外绘制（不插入材质数组，避免多材质错位）。
- **Ghost**：透明，未揭示区域与改前一致；已揭示区域 Alpha→0。
- **RevealedOpaque**：`Shader Graphs/AppearShader_RevealedOpaque`，Alpha Test + ZWrite。

运行时由 `AppearRevealSceneBootstrap` 自动给所有 Appear 网格挂上双材质组件；也可手动：**GameBuild → Appear → Setup Dual Reveal Materials**。

揭示不透明层通过 URP `AppearRevealOpaqueRenderFeature` 绘制（须执行一次 **GameBuild → Appear → Add Opaque Render Feature To URP Renderer**），才能进入相机颜色缓冲并吃到后处理。

**Fancy Neon**：揭示实心区域需在 DepthNormals 预通道写入 `_CameraNormalsTexture`（Feature 在 `AfterRenderingPrePasses` 绘制 DepthNormals Pass）。Play 时用 Frame Debugger 确认 `AppearRevealOpaque DepthNormals` 出现在预通道之后、Fancy Neon 之前。

共享 mask 逻辑：`Assets/Shader/AppearRevealLib.hlsl`（球体 `_Position0..14` + UV `_Reveal`）。

**性能**：每个 Appear 网格 +1 次 Draw Call。名称含 `Glass` 的材质默认跳过。与 `AppearRevealRenderPriority` 二选一（双材质模式下不必再改 Queue）。

## 维护说明

- 新增 **发布场景**：放入 `Scenes/`，并在 *File → Build Settings* 中勾选。
- 新增 **Resources.Load**：资产必须放在 `Resources/` 下对应子路径。
- 大模型/动画若不需要 `Resources.Load`，应放在 `Art/` 或 `Prefabs/`，避免增大 `resources.assets`。

重新整理可运行菜单：**Tools → GameBuild → Organize Build Content**（需关闭其他 Unity 实例后使用批处理）。
