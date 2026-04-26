
# 《角色控制器 v2.1 说明及使用指南》

### 【介绍】基于 Animancer 的第三人称控制器&#xA;

本控制器是基于 Animancer 和有限状态机制作的第三人称控制器，包含丰富的动画状态，如 idle 状态、八方向起步、walk（forward 或者倾斜）、run（forward 或者倾斜）、蹲移动、索敌移动、跳跃、下落、着陆接 idle、着陆接移动、高处下落着陆、翻越、攀爬、横梁移动等。同时，还具备玩家脚部 IK 功能，该功能基于 unity 自带的动画 IK 系统。


（**注意**：仅供学习使用，请勿商用、进行任何形式倒卖）


### 【推荐版本】&#xA;

Unity 2022.3 系列


### 【使用指南】角色控制器 v2.1&#xA;

#### 一、前期准备：资源下载与导入&#xA;



1.  **核心控制器下载**

    在发行版中找到第一个版本 `v2.1`，下载 `UnityPackage` 格式的安装包。


2.  **导入核心控制器**

    打开你的 Unity 工程项目，通过「Assets → Import Package → Custom Package」导入刚下载的 `UnityPackage`。


3.  **依赖插件安装（必选）**

    需依次导入以下插件，否则可能出现编译错误：


*   **Animancer**：请前往 [Unity](https://assetstore.unity.com/packages/tools/animation/animancer-pro-v8-293522)[ Asse](https://assetstore.unity.com/packages/tools/animation/animancer-pro-v8-293522)[t Sto](https://assetstore.unity.com/packages/tools/animation/animancer-pro-v8-293522)[re](https://assetstore.unity.com/packages/tools/animation/animancer-pro-v8-293522) 购买并下载最新正版（支持开发者正版授权）。


*   **Cinemachine**：在 Unity 编辑器中打开「Window → Package Manager」，搜索并安装。


*   **InputSystem**：同上，通过 Package Manager 搜索并安装。


#### 二、控制器配置步骤（安装教程）&#xA;

控制器核心组件说明：




*   `Player（脚本）`：控制角色运动逻辑


*   `RaycastFootIK（脚本）`：处理脚部 IK 效果


*   `CameraController（脚本）`：管理第三人称相机逻辑


##### 1. 角色模型配置（替换 / 使用预设）&#xA;

###### 方案一：直接使用预设（推荐新手）&#xA;

在路径 `AnimancerController/Resources/Prefab` 中找到预设的玩家预制件，拖入场景即可直接运行。


###### 方案二：自定义模型替换&#xA;



*   将 `Player` 脚本挂载到你的玩家模型上（挂载后会自动生成 `CharacterController`、`Animator`、`AnimancerComponent`）。


*   确保模型的 FBX 动画类型为 `Humanoid`，并创建对应的 `Avatar`，赋值给模型的 `Animator` 组件。


*   **关键配置**：



    *   将 `Animator` 组件引用赋值给 `AnimancerComponent` 中的 `Animator` 字段。


    *   调整 `CharacterController` 的碰撞体大小以匹配模型。


    *   在 `Player` 脚本中设置「地面层」（需提前创建一个表示地面的 Layer，赋值给场景中的地面物体，再在此处引用）。


    *   在 `Player` 脚本中赋值 `PlayerSO` 配置文件（预设文件已包含，直接引用即可）。


*   （可选）若需要脚部 IK 功能，在模型上额外挂载 `RaycastFootIK` 脚本。


##### 2. 第三人称相机配置&#xA;



*   **方案一：使用预设**

    在路径 `AnimancerController/Resources/Prefab/Camera` 中找到预设的 `CameraController` 预制件，拖入场景后，在组件中设置 `LookAt` 和 `Follow` 为角色对象即可。


*   **方案二：手动创建**

    通过 Cinemachine 创建 `VirtualCamera`，同样配置 `LookAt`（角色头部 / 骨骼）和 `Follow`（角色根节点）。


#### 三、操作说明&#xA;



| 按键 / 操作&#xA; | 功能描述&#xA;       |
| ------------ | --------------- |
| WASD&#xA;    | 角色移动&#xA;       |
| Shift&#xA;   | 切换跑步模式&#xA;     |
| C&#xA;       | 下蹲&#xA;         |
| 空格&#xA;      | 跳跃 / 翻越障碍&#xA;  |
| Tab&#xA;     | 索敌功能&#xA;       |
| 鼠标移动&#xA;    | 控制镜头方向&#xA;     |
| 鼠标滚轮&#xA;    | 调整相机与角色的距离&#xA; |

#### 四、框架 & 插件使用&#xA;



1.  [Anima](https://assetstore.unity.com/packages/tools/animation/animancer-pro-v8-293522)[ncer](https://assetstore.unity.com/packages/tools/animation/animancer-pro-v8-293522)：动画状态机管理插件，负责角色动画的流畅切换与融合。


2.  **Cinemachine**：相机系统插件，用于实现平滑的第三人称视角跟随与切换。


3.  **InputSystem**：输入管理插件，统一处理键盘、鼠标等输入逻辑。




***

按上述步骤配置后，即可正常使用角色控制器的全部功能。若有问题可提交 Issue 反馈，欢迎贡献代码优化！


> （注：文档部分内容可能由 AI 生成）
>