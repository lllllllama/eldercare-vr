# PICO ElderCare VR/MR

面向 PICO 设备的老年人 VR/MR 综合康养项目。现有主导航保留健康游戏、康复运动、VR旅游、场景视频入口；健康游戏进入二级选择场景，再分别进入独立的乒乓球训练、双手拉弓射箭训练和单手投掷飞镖训练场景。

![mode-vr](https://img.shields.io/badge/mode-VR-blue) ![mode-mr](https://img.shields.io/badge/mode-MR-orange) ![unity](https://img.shields.io/badge/Unity-2022.3.62f3-black) ![sdk](https://img.shields.io/badge/PICO%20SDK-3.4.0-green)

- **VR 模式**：主导航 + 健康游戏选择页 + 独立乒乓球 / 射箭 / 飞镖训练场景。
- **MR 模式**：乒乓球和射箭场景分别复用项目的 XR/MR 基础配置；两套玩法互不注入、互不依赖。

---

## 目录

- [项目信息](#项目信息)
- [核心特性](#核心特性)
- [快速开始](#快速开始)
- [Unity 菜单](#unity-菜单)
- [命令行构建与自测](#命令行构建与自测)
- [MR/XR 使用方式](#mrxr-使用方式)
- [目录结构](#目录结构)
- [关键脚本](#关键脚本)
- [实机检查清单](#实机检查清单)
- [常见问题](#常见问题)
- [开发约定](#开发约定)
- [第三方与许可](#第三方与许可)

---

## 项目信息

| 项 | 值 |
| --- | --- |
| Unity | `2022.3.62f3` |
| 目标设备 | PICO 4 Ultra / 支持视频透视的 PICO XR 设备 |
| PICO SDK | `com.unity.xr.picoxr`，来自 `PICO-Unity-Integration-SDK` 的 `release_3.4.0` |
| 主场景 | `Assets/_Project/Scenes/00_MainEntry.unity` |
| 自检场景 | `Assets/_Project/Scenes/00_DeviceTest.unity` |
| 第三方说明 | `THIRD_PARTY_NOTICES.md` |

---

## 核心特性

**综合首页**
- 运行后先进入 `VR康养服务` 世界空间首页，而不是直接开始乒乓球。
- 首页原有布局和入口保持不变，`健康游戏` 不再直接启动乒乓球。
- `健康游戏` 打开 `02_HealthGameMenu`，该二级场景提供 `乒乓球训练`、`射箭训练`、`飞镖训练` 三个入口。
- 三个玩法分别运行在 `01_PingPongDemo`、`03_ArcheryTraining`、`04_DartsTraining`，互不注入、互不依赖。
- 首页卡片使用 VR 大字号、发光 hover、线性图标和手柄/手势选择提示。

**射箭训练（双手柄协作，坐姿可玩）**
- 一手持弓、另一手在弓侧握紧 Grip 或扳机搭弦，向后拉开再松手放箭；`持弓手` 按钮可一键切换左/右利手并记忆偏好。
- 拉距、出箭速度、命中环数、辅助瞄准全部由 `ArcherySolver` 纯函数解算，可离线回归。
- 开始训练时按当前头部高度自动校准靶心高度（0.9–1.75 米限位），坐轮椅或沙发上也能平视瞄准；箭道朝向按当前头部朝向对齐，面板上还有 `重新对准` 按钮随时把箭道转回正前方。
- **适老手感**：瞄准方向做防手抖低通平滑；`辅助瞄准` 开启时拉弓显示弹道预览弧线，且落点向靶心做限角（≤4°）微调——只帮"差一点"的箭，不接管乱射的箭；默认开启，可随时关闭。
- **拟真反馈**：弓臂随拉弓实时弯曲、弓弦三段式跟手；搭弦/拉弓/放箭三段双手差异化震动；程序合成音效（搭弦咔哒、拉弓渐紧、放箭弦响、命中闷响、环数越高音越亮的钟声、金环三连音）零音频资源依赖。
- **游戏化**：5 色环靶（白/黑/蓝/红/金对应 2/4/6/8/10 环），近/中/远三档靶距；每轮 10 支箭，命中点弹出飘分文字，金环触发金色粒子庆祝；每轮结束按命中率给 1–5 星评价和鼓励语；各难度历史最佳成绩用 `PlayerPrefs` 持久化，破纪录有专属提示音。
- 箭矢采用自写弹道（重力 + 线性阻力）+ SphereCast 扫掠命中检测，附拖尾光带，插靶带随机滚转姿态，不依赖刚体碰撞，低速高速都不穿靶。


**飞镖训练（单手投掷，坐姿可玩）**
- 任一手握紧 Grip 或扳机拿镖，朝镖盘挥臂时松手投出；`投掷手` 按钮一键切换左/右利手并记忆偏好。
- 出手速度由挥臂手速映射（时间窗平均测速抗手抖），慢速松手视为"把镖放回"不投出——点面板按钮永不误投。
- 镖盘高度按头部高度自动校准（0.9–1.75 米限位），近/标准/远三档盘距（1.8/2.4/3 米）；辅助瞄准限角 8° 向盘心纠偏（投掷比拉弓更难控向，纠偏比射箭更宽）。
- 经典镖盘配色（米白/黑/绿/红 + 金色盘心对应 2/4/6/8/10 环），每轮 10 镖，星级评价、鼓励语、飘分、金心粒子、历史最佳与射箭同一套游戏化体系。
- 弹道/计分/辅助瞄准/星级全部复用 `ArcherySolver` 纯函数，可离线回归；音效复用程序合成器（新增出手破空声）。

**交互**
- 右手球拍自动跟随控制器击球，支持持球发球与自由球击打两套策略。
- 左手 Grip 抓球与释放球，松手速度自动转换为出球速度。
- 左手 Grip 拖动桌子黄色把手摆放球桌，松开可存档。

**物理**
- 桌面、球网、球拍、地面分别标注 `PingPongSurface` 类型，反弹/摩擦参数独立可调。
- 击球解算统一走 `PingPongHitSolver`，支持法向反弹、切向摩擦、自旋转移、最小闭合速度、方向约束。
- 球体自写空气动力学（阻力 + Magnus），`maxAngularVelocity` 提升到 180 rad/s，避免 Unity 默认上限截断发球旋转。
- `ContinuousDynamic` 碰撞检测 + `SphereCast` 扫掠 fallback，双重保护高速球穿桌。

**发球**
- 自动发球循环，支持 Basic / Topspin / Backspin / Sidespin / RandomMixed 五种 profile。
- 弹道求解保证过网，目标点带随机扰动，旋转方向轴从水平速度动态计算。

**MR**
- 视频透视（Video See-Through），虚拟地面/背景墙自动隐藏。
- Plane Detection 自动对齐桌面高度到真实地面。
- 桌子位置通过 `PlayerPrefs` 记忆，下次进入 MR 场景自动恢复。

**工具链**
- `PingPongDemoSceneBuilder` 一键生成 VR/MR 场景，无需手工改 `.unity`。
- `PingPongPhysicsSelfTests` 编辑器自测覆盖 Solver 关键路径，可在 batchmode 下跑回归。
- `VRTableTennis` 资产分层复用：`Original` 保原始素材，`Adapted` 保清理后的可用 prefab。

---

## 快速开始

1. 用 Unity Hub 打开本项目，编辑器版本必须是 `2022.3.62f3`。
2. 首次打开等 Package Manager 拉完依赖（PICO SDK 走 Git，需要外网）。
3. 打开主场景：

   ```text
   Assets/_Project/Scenes/00_MainEntry.unity
   ```

4. 点击 `Tools/PICO ElderCare/Build Unified MVP Scenes` 生成导航和训练场景并配置 Build Settings。
5. Build And Run 到 PICO 设备。运行后先看到 `VR康养服务` 首页；选择 `健康游戏` 后，可继续选择乒乓球、射箭或飞镖训练。

---

## Unity 菜单

所有菜单都在 `Tools/PICO ElderCare/` 下。

| 菜单项 | 作用 |
| --- | --- |
| `Build Unified MVP Scenes` | 按主导航、健康游戏选择、乒乓球、射箭、康复训练的顺序生成场景并配置 Build Settings。 |
| `Build Main Entry Scene` | 生成现有主导航；`健康游戏` 只绑定到二级健康游戏选择场景。 |
| `Build Health Game Menu Scene` | 生成纯导航的 `02_HealthGameMenu`，包含乒乓球、射箭和返回入口。 |
| `Build PingPong Demo Scene` | 只生成乒乓球训练内容，不生成射箭对象。 |
| `Build PingPong Mixed Reality Scene` | 生成乒乓球 MR 训练内容，不生成射箭对象。 |
| `Build VRTableTennis Adapted Assets` | 从 `External/VRTableTennis/Original` 的模型、音频、材质生成 `Adapted` 下的可用 prefab。 |
| `Build Archery Training Scene` | 生成独立的 `03_ArcheryTraining`，包含 XR/MR 基础、弓、箭、靶和计分板。 |
| `Build Darts Training Scene` | 生成独立的 `04_DartsTraining`，包含 XR/MR 基础、镖、镖盘和计分板。 |
| `Repair PingPong Demo Scene Objects` | 脚本升级后修复已有场景对象，避免整场景重建。 |
| `Run PingPong Physics Self Tests` | 在编辑器里跑 Solver/发球/空气动力学的物理自测。 |
| `Run Archery Self Tests` | 跑射箭拉弓/弹道/环数计分/坐姿校准及独立场景路由自测。 |
| `Run Darts Self Tests` | 跑飞镖出手速度映射/弹道/计分/换手/场景路由自测。 |

---

## 命令行构建与自测

适合 CI 或者本地冒烟验证。所有命令在项目根目录执行。

**生成全部导航与训练场景（主导航 / 健康游戏选择 / 乒乓球 / 射箭 / 康复）**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod RehabSceneBuilder.BuildUnifiedMvpScenes -logFile 'Logs\unity_unified_build.log'
```

**构建 MR 版本（仅乒乓球训练场景，不含射箭）**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongDemoSceneBuilder.BuildMixedRealityDemoScene -logFile 'Logs\unity_mr_build.log'
```

**构建 VR 版本（仅乒乓球训练场景，不含射箭；射箭场景用 `ArcheryGameSceneBuilder.BuildArcheryTrainingScene`）**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongDemoSceneBuilder.BuildDemoScene -logFile 'Logs\unity_vr_build.log'
```

**跑物理自测**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongPhysicsSelfTests.RunAll -logFile 'Logs\unity_physics_tests.log'
```

**跑射箭自测**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod ArcherySelfTests.RunAll -logFile 'Logs\unity_archery_tests.log'
```

**跑飞镖自测**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod DartsSelfTests.RunAll -logFile 'Logs\unity_darts_tests.log'
```

自测通过时日志里会出现：

```text
PingPong physics self tests passed.
Archery self tests passed.
Darts self tests passed.
```

如果 batchmode 直接退出并在日志里看到 `No valid Unity Editor license found`，说明 Unity 授权未激活，先在 Unity Hub 登录并激活许可证。

---

## MR/XR 使用方式

1. 运行 `Build PingPong Mixed Reality Scene`。
2. Build And Run 到支持视频透视的 PICO 设备。
3. 进入后真实房间作为背景，虚拟球桌、球、球拍和 UI 叠加在真实空间中。
4. 左手靠近球桌左前方黄色把手 `LeftTableDragHandle`。
5. 按住左手 Grip 并移动手柄调整球桌位置；松开 Grip 位置会保存。
6. 下次进入 MR 场景时自动加载上次保存的位置。
7. 检测到真实地面后，`RoomPlaneAligner` 会把球桌高度对齐到地面，并同步发球高度、反弹高度和控制器限位高度。

---

## 目录结构

```
Assets/
  _Project/
    Docs/               # 绑定指引 / VRTableTennis 迁移说明
    External/
      VRTableTennis/
        Original/       # 精选复制的 FBX、音频、材质、LICENSE
        Adapted/        # 清理后的 table/net/paddle/ball prefab
    Materials/PingPong/ # 项目自有 URP 材质（含 MR 可视化材质）
    Materials/Archery/  # 射箭弓/箭/靶材质（构建器自动生成）
    Prefabs/PingPong/   # fallback 球 prefab
    Scenes/             # 00_MainEntry / 02_HealthGameMenu / 01_PingPongDemo / 03_ArcheryTraining / 04_DartsTraining / MR_Rehab_Main
    Scripts/
      Archery/          # 弓/箭/靶/计分/会话管理（双手柄射箭）
      Darts/            # 镖/镖盘/投掷/计分/会话管理（单手飞镖）
      HealthGame/       # 健康游戏二级菜单的跨场景导航
      Common/Events/    # PingPongEvents 事件总线
      Common/Feedback/  # HitFeedbackManager 音效/特效
      Editor/           # 场景构建器 + 物理自测
      PingPong/
        Ball/           # PingPongBall / BallSpawner / BallLifetime
        Paddle/         # PaddleFollower / PaddleVelocityTracker
        Interaction/    # 抓球、拖桌、桌子锁定、穿桌限位、视角对齐
        MR/             # MR 管理器 + 地面对齐
        UI/             # ScoreManager
        PingPongGeometry.cs / PingPongHitSolver.cs / PingPongSurface.cs
  Resources/            # PICO Debugger / ProjectSetting / PlatformSetting
  XR/ + XRI/            # XR Management 与 XRI 设置
```

---

## 关键脚本

**玩法核心**

| 脚本 | 职责 |
| --- | --- |
| `PingPong/PingPongGeometry.cs` | 球桌、球网、球、球拍的统一尺寸与物理常量。 |
| `PingPong/PingPongSurface.cs` | 表面类型（Table/Net/Paddle/Floor）标注、法线估计、PhysicMaterial 缓存。 |
| `PingPong/PingPongHitSolver.cs` | 纯函数碰撞解算器（法向反弹、切向摩擦、自旋、方向约束）。 |
| `PingPong/Ball/PingPongBall.cs` | 球物理、空气动力学、碰撞回弹、扫掠 fallback。 |
| `PingPong/Ball/BallSpawner.cs` | 自动发球、弹道求解、profile 选择、旋转轴构造。 |
| `PingPong/Ball/BallLifetime.cs` | 球体生命周期与 miss 上报。 |
| `PingPong/Paddle/PaddleFollower.cs` | 球拍跟随控制器 Transform。 |
| `PingPong/Paddle/PaddleVelocityTracker.cs` | 差分速度、角速度、接触点速度、击球点局部坐标。 |

**射箭玩法**

| 脚本 | 职责 |
| --- | --- |
| `Archery/ArcheryGeometry.cs` | 弓、箭、靶、拉距、速度、靶距、坐姿高度限位的统一常量。 |
| `Archery/ArcherySolver.cs` | 纯函数解算：拉弓状态、出箭速度、环数计分、弹道预测、坐姿高度校准、箭道朝向。 |
| `Archery/ArcheryEvents.cs` | 拉弓/放箭/命中/脱靶/训练开始结束事件与结构体。 |
| `Archery/BowController.cs` | 左手持弓跟随、右手 Grip/扳机搭弦拉弓、松手放箭、弓弦视觉与震动反馈。 |
| `Archery/ArrowProjectile.cs` | 箭矢弹道积分（重力 + 阻力）、SphereCast 扫掠命中、插靶与脱靶上报。 |
| `Archery/ArcheryTarget.cs` | 靶面命中点换算环数并广播事件。 |
| `Archery/ArcheryGameManager.cs` | 训练会话：每轮箭数、总分、星级评价、历史最佳持久化、难度靶距、坐姿校准、辅助瞄准/利手设置。 |
| `Archery/ArcheryScorePanel.cs` | 世界空间计分板：总分/剩余箭数/上一箭/历史最佳/难度/辅助瞄准/持弓手/重新对准/返回健康游戏菜单。 |
| `Archery/ArcheryAudioManager.cs` | 程序合成音效（零资源依赖）：搭弦、拉弓渐紧、弦响、命中、环数钟声、金环与破纪录提示音。 |
| `Archery/ArcheryTrajectoryHint.cs` | 拉弓时的弹道预览弧线（辅助瞄准开启时显示）。 |
| `Archery/ArcheryScorePopup.cs` | 命中点 3D 飘分文字（面向玩家、上浮渐隐）。 |
| `Common/Input/PicoGripOrTriggerInputSource.cs` | Grip 或扳机任一按下即可拉弓的输入源（老年用户友好）。 |
| `Editor/ArcheryGameSceneBuilder.cs` | 独立射箭训练场景的一键生成与保存。 |
| `Editor/ArcherySelfTests.cs` | 拉弓/弹道/计分/校准/独立路由批处理自测。 |

**交互与 MR**

| 脚本 | 职责 |
| --- | --- |
| `PingPong/Interaction/ControllerBallGrabber.cs` | 左手 Grip 抓球、释放速度估计、挡出冷却。 |
| `PingPong/Interaction/TableDragHandle.cs` | 左手拖桌、PlayerPrefs 存档、同步发球点和桌面高度。 |
| `PingPong/Interaction/TablePassiveMotionLock.cs` | 非拖动期间锁定桌子。 |
| `PingPong/Interaction/ControllerTableCollisionLimiter.cs` | 限制手柄/球拍视觉穿入桌面。 |
| `PingPong/Interaction/PlayerTableBoundary.cs` | 头部/XR Rig 不进入桌内。 |
| `PingPong/Interaction/GrabHandPoseAnimator.cs` | 根据 Grip 值驱动手指张合动画。 |
| `PingPong/Interaction/VrInitialViewAligner.cs` | 进入 VR 时对齐相机朝向球桌。 |
| `PingPong/MR/PingPongMixedRealityManager.cs` | PICO 视频透视、透明主相机、隐藏虚拟环境。 |
| `PingPong/MR/PingPongRoomPlaneAligner.cs` | 地面检测、桌子对齐、高度相关数据同步。 |

**事件与反馈**

| 脚本 | 职责 |
| --- | --- |
| `Common/Events/PingPongEvents.cs` | 发球、击打、反弹、miss、训练开始/结束事件与结构体。 |
| `Common/Feedback/HitFeedbackManager.cs` | 按速度播放击打/反弹音效与特效。 |
| `PingPong/UI/ScoreManager.cs` | 命中、发球、miss、命中率、速度、旋转的 TMP 文本。 |

**工具链**

| 脚本 | 职责 |
| --- | --- |
| `Editor/PingPongDemoSceneBuilder.cs` | VR/MR 场景生成、prefab 装配、PICO 项目设置切换、修复工具。 |
| `Editor/PingPongPhysicsSelfTests.cs` | Solver、发球、空气动力学、旋转上限的批处理自测。 |

---

## 实机检查清单

**进入场景**
- 初始视角是否正对球桌。
- 右手球拍是否跟随控制器。
- 左手是否显示握持视觉并随 Grip 张合。

**击球与发球**
- 击球方向和力量是否符合挥拍动作。
- 左手 Grip 是否可以抓球、释放球。
- 发球是否能过网并落到桌面。
- 上旋、下旋、侧旋是否有明显轨迹差异。

**物理安全**
- 球是否不再穿透桌面或球拍。
- 球网是否没有明显空气墙。
- 左右手和球拍是否不会明显穿进桌体。

**射箭训练**
- 主导航选择 `健康游戏`，再选择 `射箭训练` 后，箭道是否朝向当前视线方向、靶心高度是否与视线齐平（坐姿也应如此）。
- 拉弦手靠近弓身握 Grip 或扳机是否能搭弦（有咔哒声与轻震），向后拉时弓弦、搭箭、弓臂弯曲是否跟手。
- 辅助瞄准开启时拉弓是否显示弹道预览弧线，关闭后弧线是否消失。
- 松手后箭是否沿瞄准方向飞出并带拖尾，命中靶面是否插靶、飘分、播放环数音；金环是否有金色粒子和三连音。
- 近/中/远切换后靶距是否变化，`再来一轮` 是否清空靶上旧箭并重置计分。
- 一轮 10 箭结束后是否显示星级与鼓励语，破纪录时历史最佳是否更新并有提示音。
- `持弓手` 切换后左右手职责是否互换（引导文案也应跟着换），`重新对准` 是否把箭道转回正前方。
- 拉弓过程中双手柄是否有渐进震动（拉弦手更强），放箭瞬间是否有明显震动；拉弓不足松手应有轻微“失败”震动且不放箭。
- 输入互斥：深拉弓（超过 1/3）时面板按钮应点不动；点面板按钮松开扳机时不应误射出一支箭。
- 飘分文字中文应正常显示（打包 Noto 字体），并且浮在靶面前方不与靶板穿插。
- MR 模式：箭不应在半空撞上看不见的房间感知网格凭空消失（RoomSensing 层已从命中掩码剔除）。

**MR 与摆放**
- 拖拽球桌后，发球点、目标点、UI、碰撞限位是否同步。
- MR 模式下真实房间是否可见，虚拟地面和背景墙是否隐藏。
- MR 地面对齐后，球桌高度和发球高度是否合理。
- 退出再进入 MR 场景，上次桌子位置是否正确恢复。

---

## 常见问题

**Unity 打不开或包导入失败**
确认使用 `2022.3.62f3`，让 Package Manager 完整拉取 PICO SDK。PICO SDK 来自 Git URL，首次导入需要网络。

**batchmode 直接退出**
日志出现 `No valid Unity Editor license found` 时，先在 Unity Hub 激活许可证，再重新运行 batchmode 命令。

**右手球拍不跟随**
选择 `PingPong/Paddle_Right`，把 XR Origin 的右手控制器 Transform 手动拖到 `PaddleFollower.controllerTransform`。

**左手无法拖拽球桌**
确认场景里存在 `LeftTableDragHandle`，并且左手控制器靠近黄色球形把手时按住 Grip。

**MR 模式仍显示虚拟地面或背景墙**
重新运行 `Tools/PICO ElderCare/Build PingPong Mixed Reality Scene`，MR 构建路径会禁用 `Floor` 和 `BackWall` 并启用透明相机。

**普通 VR 模式仍开启透视**
重新运行 `Tools/PICO ElderCare/Build PingPong Demo Scene`，VR 构建路径会移除 MR 对象、关闭 PICO MR 设置并恢复主相机背景。

**球太快或太难接**
调整 `Managers/BallSpawner`：
- `serveSpeed`：发球速度。
- `serveInterval`：发球间隔。
- `upwardArc`：弹道高度。
- `PingPongBall` 的 `paddleVelocityMultiplier / forwardBoost / upwardBoost / maxSpeed` 控制回球。

---

## 开发约定

- 不要直接导入参考项目的完整场景，避免旧 XR/Oculus/SteamVR/Photon 依赖污染当前 PICO 工程。
- 修改场景生成逻辑后，优先跑对应的 Builder 菜单，而不是手工批量改场景对象。
- 新增 Unity 资源时必须同时提交对应的 `.meta` 文件。
- 提交前建议跑一次 `git diff --check`，避免 YAML/meta 尾随空格产生 review 噪声。
- MR 相关设置会写入 `Assets/Resources/PXR_ProjectSetting.asset`，切换 VR/MR 场景前后确认当前目标模式。
- 物理/Solver 相关改动建议配套更新 `PingPongPhysicsSelfTests`，保留可回归覆盖。

---

## 第三方与许可

本项目参考并复用了以下项目的设计方向或资源：

- [`kushal-goenka/VRTableTennis`](https://github.com/kushal-goenka/VRTableTennis)
- [`tomgoddard/PingPang`](https://github.com/tomgoddard/PingPang)
- [`Pico-Developer/InteractionSample-Unity`](https://github.com/Pico-Developer/InteractionSample-Unity)

详细边界见 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。重新分发构建包或源代码前，请确认复制资源的原始授权（特别是 `BarcadeGamesAssetPack` 相关资产可能标注 `licenseType: Store`）。
