# IkLab — Unity 二维 IK 关节链演示

在 Unity 中创建一条**关节数可选（2–8）**的二维关节链：**根节点固定在原点**，用户用鼠标**拖拽末端执行器**，程序用 **CCD / FABRIK** 两种 IK 算法反解各关节角度，并在 HUD 中实时显示每个关节的转角。

- 纯 C# 数学核心（`Assets/Scripts/Core`）不依赖 `UnityEngine`，可用 .NET `csc` 离线编译与断言测试；
- Unity 层使用 IMGUI HUD + 正交平面视图，无需任何美术资源；
- Editor 菜单一键生成并保存演示场景。

## 快速开始

1. 用 Unity Hub 以 **Unity 2022.3 LTS** 打开本工程（`F:\DoubaoWork\IkLab`）；
2. 菜单 **Tools → IkLab → 一键搭建演示场景**（会创建正交相机、平行光、`IKLab` 对象并保存 `Assets/Scenes/IkLab.unity`）；
3. 点击 **Play**，左键拖拽绿色末端小球（或红色目标小球）即可看到整条链跟随运动、各关节角度实时变化；
4. 在左上角面板里切换 **关节数（2–8）** 与 **IK 算法（CCD / FABRIK）**。

> 也可以不用菜单：新建空场景，创建正交相机（Size≈7，位置 `(0,0,-10)`），在任意 GameObject 上挂 `IkController` 即可。

## 操作说明

| 操作 | 功能 |
| --- | --- |
| 左键按住末端/目标小球 | 拖拽末端执行器（目标点），链实时反解 |
| 面板「关节数」 | 2–8 段，切换后链重置为沿 +X 伸直并重新求解 |
| 面板「IK 算法」 | CCD（坐标下降）/ FABRIK（前后向迭代） |
| `R` | 重置姿态（目标回到 `(总长, 0)`） |
| `H` | 显示/隐藏 HUD |
| 滚轮 | 相机正交缩放（1.5–16） |
| 中键拖拽 | 相机平移 |
| `F` | 重置相机视角 |

颜色约定：**蓝线** = 骨骼，**白球** = 中间关节，**金球** = 固定根节点，**绿球** = 末端执行器，**红球** = 目标点，灰圆 = 可达范围（半径 = 总长）。

## 算法说明

### 链的表示

链由 `n` 段**等长**骨组成，端点为 `P0…Pn`：`P0` 为固定根，`Pn` 为末端，`|Pi+1 − Pi| = L`，总长 `nL`。

### CCD（Cyclic Coordinate Descent）

从**末端关节向根部**逐关节处理：对关节 `j`，计算向量 `(Pn − Pj)` 到 `(target − Pj)` 的带符号夹角 `θ`，把 `Pj` 之后的所有端点绕 `Pj` 旋转 `θ`；整条链扫描一遍为一次迭代，末端误差小于容差或达到迭代上限即停止。

### FABRIK（Forward And Backward Reaching IK）

每次迭代两遍：

1. **后向**：把末端放到目标点，从末端向根部逐段回推，`Pi = Pi+1 + normalize(Pi − Pi+1) · L`；
2. **前向**：把根放回固定位置，从根部向末端逐段推出，`Pi = Pi−1 + normalize(Pi − Pi−1) · L`。

若目标距离根超过总长（不可达），两种算法都会把链**沿目标方向拉直**，此时末端停在可达圆上，界面提示「超出可达范围」。

### 关节角度定义

- **关节 0（根关节）**：第一段骨的绝对方向角（相对 +X 轴，逆时针为正）；
- **关节 i（i ≥ 1）**：第 i 段相对第 i−1 段的转角，归一化到 `(-180°, 180°]`；
- HUD 同时给出每段的**绝对段角**，便于对照。

默认参数：迭代上限 64，容差 0.002。

## 工程结构

```
IkLab/
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Core/
│  │  │  ├─ Vec2.cs              # 二维向量（加减、点积、叉积、旋转、带符号夹角）
│  │  │  └─ IkChain2D.cs         # 关节链 + CCD / FABRIK + 角度输出（纯 C#）
│  │  ├─ IkController.cs         # 主组件：建链、求解、拖拽、绘制、HUD
│  │  ├─ PlanarCameraController.cs # 正交相机：缩放/平移/重置
│  │  └─ ObjectFinder.cs         # 跨版本查找辅助
│  ├─ Editor/
│  │  └─ IkLabSetup.cs           # Tools → IkLab → 一键搭建演示场景
│  └─ Scenes/                    # 由菜单生成 IkLab.unity
├─ Tests/
│  ├─ IkTest.cs                  # 55 项离线断言
│  └─ IkTest.exe                 # 由测试命令生成
├─ ProjectSettings/ProjectVersion.txt
├─ .gitignore
└─ README.md
```

## 离线测试

数学核心不依赖引擎，可直接用 .NET Framework 编译器运行：

```powershell
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo `
  /out:F:\DoubaoWork\IkLab\Tests\IkTest.exe `
  F:\DoubaoWork\IkLab\Tests\IkTest.cs `
  F:\DoubaoWork\IkLab\Assets\Scripts\Core\Vec2.cs `
  F:\DoubaoWork\IkLab\Assets\Scripts\Core\IkChain2D.cs
& F:\DoubaoWork\IkLab\Tests\IkTest.exe
```

当前结果：**RESULT: ALL PASS (PASS 55, FAIL 0)**。

断言覆盖：构链几何、CCD/FABRIK 可达收敛、骨长保持、根固定、超出可达时拉直、单关节圆上/圆内行为、两算法末端一致（20 例随机目标）、直链与手摆姿态的角度、角度归一化、按段角重建末端、迭代上限与重复求解稳定性、100 例随机目标批量收敛、极端目标与目标=根等退化输入（无 NaN）。

## 已知限制

- 演示为**二维平面链**（XY 平面），不含 3D 扭转自由度；
- 骨长固定且**无关节角度限位**（不模拟机械臂限位/自碰撞）；
- 目标超出可达范围时链拉直指向目标，末端不会贴合目标（HUD 会提示）；
- 仅使用内置渲染管线默认 Shader（`Unlit/Color` → URP Unlit → Standard 逐级回退），未做管线适配工程配置。
