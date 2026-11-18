# P5R 对话选项标注 Mod —— 制作与扩展手册

> Mod ID：`p5r.dialogue.star`  
> 位置：`D:\downlode\P5RMod\ReLoad2\Mods\p5r.dialogue.star\`  
> 版本：v0.2.2 ｜ 记录日期：2026-11-18  
> 适用：Persona 5 Royal 简体中文版（Steam）

---

## 一、这个 Mod 做了什么

在游戏的对话与选项文本里追加四种符号，每类含义**互不重叠**：

| 符号 | 含义 | 数量 |
| -- | --- | --- |
| ♡ | **恋人选项**——10 位可攻略女性角色的专属告白项，决定结局是恋人还是朋友 | 10 处 |
| ◆ | **友人选项**——同一选项框里的另一项，走好友路线 | 10 处 |
| ↑ | **Coop 好感提升最大**的选项（BMD 的 f8 标记 1038 + BF 选项分支内加点 1） | 1039 处 |
| ★ | **正确答案 / 加分选项**：五维答题（147）+ 课堂追加思考（39）+ 考试思考（25）+ 便利店条形码（12）+ 电视猜谜（12）+ 汉堡挑战（1） | 236 处 |

效果示例（东乡一二三 Rank 9）：

```
请你和我交往♡      ← 恋人路线，之后剧情按恋人处理
我们永远是朋友◆    ← 友人路线
```

普通 Coop 对话：

```
我很期待↑          ← 好感 +15（最优）
真有干劲            ← 好感 +10
……你说的是什么时候   ← 好感 +5
```

> **为什么把 ♡ 让出来**：v0.1.0 里 ♡ 表示「好感最优」，玩家在不可攻略角色身上看到 ♡，
> 会形成「♡ = 加分项」的固定认知；轮到可攻略角色时，真正的告白项反而不显眼。
> v0.2.0 起 ♡ 专用于恋爱路线，好感最优改用 ↑。

**不改存档，不改存档结构，不动 exe。** 只替换游戏 CPK 数据包中的脚本文件，与存档完全无关。

---

## 二、核心原理

### 2.1 关键结论：必须在 CPK 层替换，`FEmulator` 目录走不通

P5R 的文件替换有三条路，本项目**只有第三条能生效**：

| 方案                                                       | 结果      | 原因                                               |
| -------------------------------------------------------- | ------- | ------------------------------------------------ |
| `FEmulator\BF\*.msg` 源码                                  | ❌ 无效    | 只放 `.msg` 不会触发任何接管，日志里 `Building BF File` 一次都没出现 |
| `FEmulator\BF\*.msg` + `P5REssentials\CPK` 空 dummy `.BF` | ⚠️ 理论可行 | 空 dummy 会让模拟器失去原始基础文件，不推荐                        |
| **`P5REssentials\CPK\<CPK名>\<完整路径>\<完整二进制文件>`**          | ✅ 有效    | 见下                                               |

**生效机制**（`CriFs.V2.Hook` 源码）：

- 该 Mod 会探测 Mod 包内的 `CRIFsV2Hook/CPK`（新版常量）与 `P5REssentials/CPK`（legacy 路径）
- 命中的真实文件会以 **`priority = Int32.MaxValue`** 直接 bind 进 CPK binder
- 效果 = 完全覆盖 CPK 包内的同名文件，**不需要编译、不需要重打包 CPK**

> 这也是为什么日志里的 `Bind Complete! 1 files` 一开始具有误导性——那个 1 是别的音频 mod 的文件，不是我们的对话脚本。

### 2.2 中文版的特殊性（重要坑）

简体中文补丁把整套 `FIELD/NPC/*.BF`（161 个）放在 **`SC.CPK`** 里，`BASE.CPK` 里根本没有：

```
base_list.txt（BASE.CPK，44945 项） → 无 FNPC002_002_01.BF
sc_list.txt （SC.CPK，  8597 项） → 有 FIELD/NPC/FNPC002_002_01.BF
```

所以部署路径里必须带 CPK 名 `SC.CPK`：

```
P5REssentials\CPK\SC.CPK\FIELD\NPC\FNPC002_002_01.BF
                 ^^^^^^ 必须带上
```

放到 `P5REssentials\CPK\BASE.CPK\...` 会失效。

### 2.3 标注依据：游戏数据本身就标了答案

**这是整个项目最有价值的发现：不需要攻略数据，游戏自己的选项 token 里就带着「最佳选项」标记。**

| 场景           | 标记 token             | 参数 | 数量              |
| ------------ | -------------------- | -- | --------------- |
| Coop 好感最佳选项  | `<f 8 \| 2,1,2217>`  | 固定 | 1040 处 / 342 文件（2 处同时是恋人选项，标 ♡） |
| 课堂答题正确答案     | `<f 7 \| 0,0,25,1>`  | 固定 | 147 处 / 6 文件    |
| 主线关键选择（不加数值） | `<f 7 \| 0,1,971,1>` | 固定 | 60 处（**不标注**）   |
| 恋人选项（恋爱路线）  | `<f 7 \| 0,1,N,1>` | N=729~737/745 | 10 处 / 10 文件 |

验证方式：拿坂本龙司（战车，`CORP007` / `E707_*`）的 6 组选项逐条对照 biligame wiki：

| Rank | 游戏内标了     | wiki 分值      | 一致 |
| ---- | --------- | ------------ | -- |
| 1→2  | 我很期待      | 15（最高）       | ✅  |
| 2→3  | 别吵架 / 冷静点 | 5 / 10（各自最高） | ✅  |
| 3→4  | 你很在意吗？    | 5（最高）        | ✅  |
| 3→4  | 龙司还没有完蛋   | 15（最高）       | ✅  |
| 8→9  | 松一口气了吗？   | 15（最高）       | ✅  |
| 9→10 | 我也是       | 10（最高）       | ✅  |

并列最优时（如「是啊／是吗？」都 15 分），游戏里**两个都带标记**，与攻略完全一致。

**这个标记位是「最优选项」，不是「分数最高」——所以同分并列会全部命中，这正是我们想要的。**

---

### 2.4 恋爱路线标志位（v0.2.0 新增）

10 位可攻略女性角色在 Rank 9 的告白场景里，会有一个「恋人 / 友人」的二选一。
**恋人选项的 token 里直接带了「设置恋爱 flag」的指令**，这就是可编程识别的依据：

```
<f 7 | 0,1,729,1>      ← 把 729 号剧情 flag 置 1 = 确立与某个角色的恋人关系
```

于是不需要攻略站，也不靠选项文本匹配：

| 判定 | 条件 | 动作 |
| --- | --- | --- |
| 恋人选项 | 选项含 `<f 7 \| 0,1,N,1>`，且 N ∈ 恋爱标志位集合 | 追加 ♡ |
| 友人选项 | 同一 `SelectionDialog` 里的**另一项**（这两处都是二选一） | 追加 ◆ |
| 好感最优 | 选项含 `<f 8 \| 2,1,2217>`，且不是上面两类 | 追加 ↑ |

**标志位 → 角色对照**（全部实测自 `E700\*.BMD`）：

| 标志位 | 角色 | 塔罗 | 文件 | 选项框 | 恋人 / 友人 |
| --- | --- | --- | --- | --- | --- |
| 729 | 新岛真 | 女教皇 | `E702_092` | `SEL_016_5_0` | 我喜欢你♡ / 不是这个意思◆ |
| 730 | 奥村春 | 女帝 | `E703_090` | `SEL_042_0_0` | 我也很喜欢你♡ / 在我眼中你是个好朋友◆ |
| 731 | 高卷杏 | 恋人 | `E706_090` | `SEL_036_0_0` | 还有我在♡ / 还有大家在◆ |
| 732 | 佐仓双叶 | 隐者 | `E709_090` | `SEL_015_0_0` | 因为我喜欢双叶♡ / 因为你是重要的同伴◆ |
| 733 | 千早 | 命运 | `E710_090` | `SEL_035_0_0` | 想见到千早♡ / 想让你占卜◆ |
| 734 | 武见妙 | 死神 | `E713_090` | `SEL_026_0_0` | 别岔开话题♡ / 请『诊断』◆ |
| 735 | 川上贞代 | 节制 | `E714_091` | `SEL_026_0_0` | 是男人和女人♡ / 是主人和女仆◆ |
| 736 | 大宅一子 | 恶魔 | `E715_090` | `SEL_044_0_0` | 我喜欢一子♡ / 开玩笑的◆ |
| 737 | 东乡一二三 | 星 | `E717_091` | `SEL_021_5_0` | 请你和我交往♡ / 我们永远是朋友◆ |
| 745 | 芳泽霞 | 信念 | `E722_092` | `SEL_033_0_0` | 我也喜欢堇♡ / 让我们当朋友吧◆ |

几个要点：

- **729~737 是连续的 9 个位**，正好对应 9 位老角色；霞是 Royal 新增，单独给了 745。
- 同一批角色还用了 738/739/740/741/743 等**别的 flag**（如武见的 739 出现 3 次、大宅的 741 出现 3 次），
  那些是剧情进度位，**不要**误当成恋爱位。
- 新岛真的「我喜欢你」和武见的「别岔开话题」**同时带 f8**（本来就是好感最优）。按「恋人 > 好感」优先级，
  这两处标 ♡ 而不是 ↑，否则玩家会以为只是个加分项。
- 判定粒度是 `SelectionDialog`：先认恋人，再把**同一框里的另一项**标成 ◆。这两处的选项框都是 `opts=2`；
  若将来遇到 3 选项，工具会打印 warn 并只标恋人，不会乱标友人。

### 2.5 课堂「追加思考」与打工条形码（v0.2.1 新增）

v0.2.0 只标了**考试**的思考题（`E737_002.BF` 25 个），漏了**平时上课**的追加思考。
这类选择框同样在 BF 里、同样没有数据标记，但有一条更硬的判定通道：**反编译出控制流**。

```
var6 = SEL(56);
if (var6 == 2)          ← 这个 == k 就是正确选项，不需要猜
{
    MSG(60, 0);         ← 正确分支的反应台词
}
else { MSG(57, 0); }    ← 错误分支
```

`BmdTool.exe bfdecomp` 把 BF 反编译成可读脚本后，每个 `SEL` 后面紧跟的
`if (varX == k)` 就是**正确选项索引**，k 是编译器直接写进指令的常量，不存在歧义。

| 文件 | 选择框 | 对应课次 |
| --- | --- | --- |
| `E731_002.BF` | 29 个 `THINK_*_SEL_*` | 4/25 确信犯、4/30 才能、5/23 synesthesia、9/28 phantom、1/24 juice、7/11 长期记忆… |
| `E731_012.BF` | 7 个 | 6/20 智能手机、7/7 银河·素面、1/11 八百万 |
| `E738_002.BF` | 3 个 | 11/12 合成语音 |
| `E737_002.BF` | 25 个 | 考试思考（v0.2.0 已标） |

**一个重要发现**：`E731_002.BF` 里有 29 个 THINK 选择框，但整个文件只有 **20 条 `SEL` 指令**。
剩下 9 个（`THINK_1_SEL_00/02` 与 `THINK_0124_*` 两组，共 3 组 9 框）
**在脚本里没有任何指令引用**——它们是同内容的另一套编号，属于遗留副本。
仍按反应台词一并标注（不生效也无害），并在规则文件里用注释标明。

规则文件：`_work\class_think_E731_002.txt` / `_E731_012.txt` / `_E738_002.txt`。

#### 打工：777 便利店「找条形码」

`EVENT_DATA\SCRIPT\E700\E762_001.BF`。商品是**随机抽**的（`SUB_BAKODO_SENTAKU` 用 `RND` 选编号），
但每件商品对应的正确选项写死在 `SUB_BAKODO_naiyou()` 里：

```
if (EVT_GET_LOCAL_COUNT(1) == 1)  { sVar1 = 86; ... sVar5 = 0; }   ← 产品1，正确选项 0
if (EVT_GET_LOCAL_COUNT(1) == 2)  { sVar1 = 91; ... sVar5 = 1; }   ← 产品2，正确选项 1
```

`sVar1` = 选择框索引，`sVar5` = 正确选项。12 件商品全部解出，与 `MND_KADAI_01..12`
的提示文案（「条形码印在公司标志的附近」…）逐条吻合。

| 产品 | 选择框 | 正确项 |
| --- | --- | --- |
| 1 牛鲜奶 | `SEL_Bakodo_01` | 0 找公司商标附近 |
| 2 超大杯香草冰 | `SEL_Bakodo_02` | 1 角色插图的旁边 |
| 3 斑马拉面 | `SEL_Bakodo_03` | 2 找斑马小常识的旁边 |
| 4 黑色甜甜圈 | `SEL_Bakodo_04` | 0 黑糖的说明附近 |
| 5 江户捏饭团 | `SEL_Bakodo_05` | 1 看看背面 |
| 6 | `SEL_Bakodo_06` | 2 找尾巴附近 |
| 7 | `SEL_Bakodo_07` | 1 找内侧的标签 |
| 8 | `SEL_Bakodo_08` | 2 找背面公司信息旁边 |
| 9 | `SEL_Bakodo_09` | 0 找把手的背面 |
| 10 | `SEL_Bakodo_10` | 0 找内侧的公司商标旁边 |
| 11 | `SEL_Bakodo_11` | 2 找短评专栏的旁边 |
| 12 | `SEL_Bakodo_12` | 1 找找封面的右边 |

规则文件：`_work\job_bakodo.txt`。

#### 对话交涉（hold up）为什么仍然标不了

结论：**没有可静态使用的标记**，不是没找，是数据里确实没有。四条证据：

1. **性格在文件维度，不在选项维度**：`BATTLE\TALK\` 下 11 个中文文件对应 11 种敌人类型
   （`01JIGAKU`/`02YOUNGMEN`/…/`11HEEHAW`），性格是"用哪个文件"决定的。
2. **真正的性格是运行时读的**：`init_parameter()` 里
   `sVar7 = AI_GET_ID_TALK_PERSON(敌方ID)`、`sVar8 = AI_GET_ID_TALK_TYPE(敌方ID)`，
   反应台词按 `(选项, sVar7)` 组合查表（`chatFQ_get_reaction_FQ_0X()`），
   同一个选项在不同性格下走向完全不同。
3. **选项本身只有一个说话人前缀**：反编译成 `.msg` 后三个选项都是
   `[f 0 5 65278][f 2 1]正文[e]`，前缀逐字节相同，**没有任何类型/对错字段**。
4. **唯一的按选项分支只管五维门槛**：`check_talkparamFQ/SQ` 里的
   `GET_PC_PARAM_LV(1, sVar18) < var54` 是"该选项要求某五维到几级"，
   而同一问题的三个选项给出的 `(sVar18, var54)` 完全相同（如都是 知识 Lv1）。

另外 `BTL_TALK_ICON_PLAY(sVar26)` 的图标是**恶魔台词的情绪标签**（0~12 共 13 种），
不是给玩家选项的标签，不能拿来当"正确答案"用。

### 2.6 选项分支内直接加点（v0.2.2 新增）

前面所有标注都建立在「BMD 的 f7 / f8 标记」上，但有两处例外：答案不在 BMD 里，
而在 BF 的**选项分支体内**直接调用加点指令。全量扫描 5181 个 BF，只有这 2 处符合条件。

两条指令不是一回事：

| 指令                 | COMM  | 作用        | 参数形态                                        |
| ------------------ | ----- | --------- | ------------------------------------------- |
| `CMM_ADD_POINT`    | 12305 | 加**五维**   | `PUSHIS 5` / `PUSHIS 2` → 值 5、类型 2          |
| `CMM_ADD_POINT_ID` | 12306 | 加**角色好感** | `PUSHLIX 21` / `PUSHIS 5` / `PUSHLIX 0`      |

反汇编片段（`E695_001.BF` 的「挑战吧」分支）：

```
  21: PUSHIS 5
  22: PUSHIS 2
  23: COMM 12305  ; CMM_ADD_POINT     ← 只有 opt0 这条分支有
  24: GOTO 2
```

`CORP017.BF` 同型，但用带 ID 的版本：

```
 107: PUSHLIX 21
 108: PUSHIS 5
 109: PUSHLIX 0
 110: COMM 12306  ; CMM_ADD_POINT_ID  ← 好感加点
```

| 文件                                  | 选项框                    | 正确项      | 标注 | 理由                |
| ----------------------------------- | ---------------------- | -------- | -- | ----------------- |
| `EVENT_DATA\SCRIPT\E600\E695_001.BF` | `SEL_002_0_0`          | 「挑战吧」opt0 | ★  | 加五维，与课堂答题同类       |
| `FIELD\NPC\CORP017.BF`              | `SEL_C17_YES_08_01`    | 「尽全力上吧」opt0 | ↑  | 加角色好感，与 f8 那批同类   |

**注意 `CORP017.BF` 里前 6 个选项框**（「搭话」「陪她度过」「在教堂下将棋？」「拜托她进行指导」…）
**都没有加点**，不能整文件刷符号。`bfsel1` 会把它们的 opt0 也打印成「=> 最佳选项 opt0」，
那是找不到标记时的兜底输出，不是答案——这也是当初没有机械标注的原因。

规则文件：`_work\opt_e695.txt`、`_work\opt_corp017.txt`。
标注后校验：两个文件的指令数完全不变（619 / 2734 条），`bfcmp` 差异 0，只有消息段多出 1 个字符。

## 三、当前部署清单

```
Mods\p5r.dialogue.star\
├─ ModConfig.json
└─ P5REssentials\CPK\SC.CPK\
   ├─ EVENT_DATA\MESSAGE\E700\ E800\  → 348 个 .BMD（↑1038 + ♡10 + ◆10 + ★147）
   ├─ EVENT_DATA\SCRIPT\E700\E737_002.BF → 考试「思考」环节（★25）
   ├─ EVENT_DATA\SCRIPT\E700\E731_002.BF → 课堂追加思考（★29，其中 9 框脚本未引用）
   ├─ EVENT_DATA\SCRIPT\E700\E731_012.BF → 课堂追加思考（★7）
   ├─ EVENT_DATA\SCRIPT\E700\E738_002.BF → 课堂追加思考（★3）
   ├─ EVENT_DATA\SCRIPT\E700\E762_001.BF → 777 便利店找条形码（★12）
   ├─ FIELD\TV\TV_PROGRAM.BF          → 电视猜谜（★12）
   ├─ EVENT_DATA\SCRIPT\E600\E695_001.BF → 大爆炸汉堡挑战（★1）
   └─ FIELD\NPC\CORP017.BF            → 一二三 Coop「尽全力上吧」（↑1）
```

共 **358 个文件 / 2.51 MiB**（348 BMD + 8 BF + ModConfig + README；小文件多，磁盘占用约 6.5 MB）。

| 文件                                     | 内容                         | 标注             |
| -------------------------------------- | -------------------------- | -------------- |
| `EVENT_DATA\MESSAGE\E700\E7xx_###.BMD` | 各角色 Rank 对话、SNS、电话回复       | ↑ / ♡ / ◆ / ★（按 token） |
| `EVENT_DATA\MESSAGE\E700\E700_300.BMD` | 电话/SNS/聊天回复库（1330 dialogs） | ↑              |
| `EVENT_DATA\SCRIPT\E700\E737_002.BF`   | 考试思考题（25 个 `THINK_x_SEL`）  | ★              |
| `EVENT_DATA\SCRIPT\E700\E731_002.BF`   | 课堂追加思考（29 个，20 个有效）        | ★              |
| `EVENT_DATA\SCRIPT\E700\E731_012.BF`   | 课堂追加思考（7 个）                 | ★              |
| `EVENT_DATA\SCRIPT\E700\E738_002.BF`   | 课堂追加思考（3 个）                 | ★              |
| `EVENT_DATA\SCRIPT\E700\E762_001.BF`   | 777 便利店找条形码（12 题）            | ★              |
| `FIELD\TV\TV_PROGRAM.BF`               | 电视猜谜（863 dialogs，12 题）     | ★              |
| `EVENT_DATA\SCRIPT\E600\E695_001.BF`   | 大爆炸汉堡挑战（3 选项 1 框）          | ★              |
| `FIELD\NPC\CORP017.BF`                 | 一二三 Coop 事件（7 选项框，1 框有加点）  | ↑              |

### ModConfig.json 关键字段

```json
{
  "ModId": "p5r.dialogue.star",
  "ModDll": "",
  "ModDependencies": [
    "reloaded.universal.fileemulationframework",
    "reloaded.universal.fileemulationframework.bf",
    "crifs.v2.hook",
    "p5rpc.modloader"
  ],
  "SupportedAppId": ["p5r.exe"]
}
```

`ModDll` **必须为空**——这是纯数据 Mod，没有代码。

### 启用方式

除了在 Reloaded-II 界面勾选，还要确认 `Apps\p5r.exe\AppConfig.json` 的 `EnabledMods` 里有它：

```json
"EnabledMods": ["p5rpc.modloader", "crifs.v2.hook", "p5r.dialogue.star"]
```

---

## 四、完整制作流程（从零复现）

### 步骤 0：环境前置

1. **确认 `dinput8.dll` 冲突已解决**  
   `D:\STEAM\steamapps\common\P5R\dinput8.dll` 如果被别的工具（如 P5r Tool）占用，会让 Reloaded-II 注入失败、游戏卡在 `Starting BMD.File.Emulator`。  
   处理：改名为 `dinput8.dll.disabled`。
2. **确认依赖 Mod 已装齐**  
   `reloaded.universal.fileemulationframework`、`.bf`、`.bmd`、`crifs.v2.hook`、`p5rpc.modloader`
3. **确认工具能跑**  
   需要 .NET 9 SDK（`dotnet --version`）。

### 步骤 1：解包 CPK

```
CpkCli.exe <CPK路径> PREFIX:FIELD/        # 按前缀批量提取
CpkCli.exe <CPK路径> FIELD/NPC/X.BF      # 单文件提取
CpkCli.exe <CPK路径>                      # 只列 TOC 清单
```

产物：

- `_work\extract\` —— 2019 个 `.BMD`（30 MB）
- `_work\bf\` —— 5182 个 `.BF`（113 MB）

> *CriPakTools 原生版在 P5R 的 CPK 上会崩（条目 FileSize 为 null），必须用修过的 `CpkCli`。*  
> *修的 bug：① 条目名匹配区分大小写 ② 缺 Shift-JIS 编码注册（`CodePagesEncodingProvider`）。*

### 步骤 2-A：BMD 标注（自动，覆盖大部分场景）

```
BmdTool.exe bmdromance <源目录> <输出目录> ♡ ◆ ↑ ★ [恋爱标志位,逗号分隔]
```

参数依次是：恋人符号、友人符号、好感最优符号、五维答案符号（省略第 5 个参数就用默认的 729~737+745）。

逻辑（`BmdRomance.cs` → `Tag`）：

```
遍历每个 SelectionDialog：
  1. 先找恋人选项：含 f7 且参数 0,1,N,1、N 属于恋爱标志位
       → 追加 ♡；若该框是二选一，另一项追加 ◆
  2. 其余选项：
       含 FunctionToken 8             → 追加 ↑
       含 f7 且参数 0,0,25,1           → 追加 ★
```

符号插在**最后一个 `StringToken` 的末尾空白之前**，即紧贴正文、不破坏原有的尾部空格：

```
原文：请你和我交往<f 7 | 0,1,737,1>　　（末尾两个全角空格是原文件就有的）
结果：请你和我交往♡<f 7 | 0,1,737,1>　　→ 显示为「请你和我交往♡」
```

> v0.1.0 的旧命令 `bmddual` 仍保留（只标 ♡ ★），新版本请用 `bmdromance`。

### 步骤 2-B：BF 标注（需要人工判定答案时）

BF 是流程脚本，内嵌 MessageScript。选项在 `SelectionDialog` 里，但**没有答案标记**，需要外部规则文件：

```
BmdTool.exe bfselmark <源BF> <输出BF> <规则文件>
```

|                  |      |                  |
| ---------------- | ---- | ---------------- |
| 规则文件格式（每行 \`对话框名 | 选项索引 | 符号`，`#\` 开头为注释）： |

```
# 电视猜谜
SEL_QUIZ_0519|1|★
SEL_QUIZ_0526|0|★
# 考试思考题
THINK_1_SEL_00|2|★
```

答案从哪来（两条独立来源互相验证）：

- **电视猜谜**：① wiki 红字（HTML 里 `style="color:#F00"`）② 游戏脚本自带的判定句 `正确的判决是……Ｂ！`（全角字母 = 选项索引）。12 题两源 100% 吻合。
- **考试思考题**：每个选项分支后面跟着一句**反应台词**。正确 = 确认句；错误 = 含「不对吧／不是啦／真的吗／再重新想想／从头整理」。25 条与 wiki 考试答案表逐条吻合。

### 步骤 3：部署

```bash
DEST="D:/downlode/P5RMod/ReLoad2/Mods/p5r.dialogue.star/P5REssentials/CPK/SC.CPK"
cp -r <输出目录>/EVENT_DATA "$DEST/"
```

**保持相对路径完全一致**，否则 bind 时匹配不上。

### 步骤 4：验证（必做）

```
BmdTool.exe bmdverify <原始目录> <标注目录> ♡★
```

逐 dialog / 逐行 / 逐 token 比对：把标注符号从末尾去掉后，必须与原文件 **100% 一致**。

当前 348 个 BMD 的验证结果：`0 diff / 0 error`。

这样能保证「只加了一个字符，没有破坏任何文本、控制符、说话人、标签」。

---

## 五、工具链命令参考

位置：`C:\Users\主人的猫\WorkBuddy\2026-11-17-15-43-10\tools\`  
可执行：`BmdTool\bin\Release\net9.0\BmdTool.exe`、`CpkCli\bin\Release\net9.0\CpkCli.exe`

### BMD 相关

| 命令          | 用法                              | 说明                               |
| ----------- | ------------------------------- | -------------------------------- |
| `bmdtok`    | `<文件\|目录> <out>`                | 导出选项 token 详情（含 `<f 8>` 标记）★诊断核心 |
| `bmdtag`    | `<src> <dst> <符号> [文件名过滤]`      | 只给 f8 选项加符号                      |
| `bmdromance` | `<srcRoot> <dstRoot> <mLove> <mFriend> <mBest> <mAns> [flags]` | 四类一次标完 ★主用（v0.2.0） |
| `bmddual`   | `<srcRoot> <dstRoot> <m8> <m7>` | 旧版：f8→m8，f7[0,0,25,1]→m7        |
| `bmdverify` | `<srcRoot> <dstRoot> <marks>`   | 校验标注完整性 ★必用                      |

### BF 相关

| 命令                      | 用法                                  | 说明                         |
| ----------------------- | ----------------------------------- | -------------------------- |
| `bfdecomp`              | `<src> <out.flow>`                  | **反编译成可读脚本**，选项正确分支一目了然 ★v0.2.1 |
| `bfdis`                 | `<src> <out>`                       | 反汇编，`COMM` 自动注释函数名         |
| `bfmsgdump`             | `<src> <out>`                       | 导出内嵌消息脚本（可读 `.msg` 格式）     |
| `bfsel1`                | `<src> <out>`                       | 单文件选项分析（含选项文本）             |
| `bfselmark`             | `<src> <dst> <ruleFile>`            | 按规则给指定选项加符号                |
| `bfstar`                | `<src> <dst> <前缀> <消息符号> <选项符号...>` | 按 dialog 名前缀批量加符号          |
| `bfinfo`                | `<bf>`                              | dump BF 内部结构（段表 / 大小 / 地址） |
| `bfrt`                  | `<src> <dst>`                       | 往返测试（读→写）                  |
| `bffind`                | `<root> <out> <关键词...>`             | 全量按文本搜索 BF                 |
| `bfmerge` / `bfcmp`     |                                     | 消息合并 / 差异比对                |
| `bfcommstat`            | `<root> <out>`                      | 全量 COMM 调用直方图              |
| `bfselstat` / `bfparam` | `<root> <out>`                      | 统计含选项 / 含五维加点的脚本           |

### CPK 相关

| 命令        | 用法                                         |
| --------- | ------------------------------------------ |
| 列清单       | `CpkCli.exe <CPK>`                         |
| 单文件提取     | `CpkCli.exe <CPK> <内部路径>`                  |
| 前缀批量提取    | `CpkCli.exe <CPK> PREFIX:FIELD/NPC/`       |
| 替换（❌ 不可用） | 重打包会在 `Program.cs:128` 因 FileSize 为 null 崩 |

---

## 六、关键技术坑（踩过的）

### 1. `FlowScriptBinary.ToFile()` 不更新文件头 —— 会写出坏文件

P5R 的 BF 是**大端**结构：

```
0x00  header 32B     0x04 = FileSize
0x10  section 数量
0x20  section 表，每项 16B：type / elemSize / count / addr
      type: 0=ProcedureLabel 1=JumpLabel 2=Text 3=MessageScript 4=String
```

`ToFile()` 写完**不更新 header 和 section 表**，结果文件被截断（TV_PROGRAM 204593 → 202303），再读报  
`Stream is too small for the data described`。

**解决**（`BfEdit.SaveFixed`）：

```csharp
result = orig[0 : msgAddr]                                    // 保留原头部与前置段
       + rt[msgAddr : msgAddr + newMsgLen]                    // 新消息段
       + orig[msgAddr + msgLen :]                             // 保留尾段
WriteBE(result, 0x04, result.Length);                         // FileSize
WriteBE(result, msgOff + 8, newMsgLen);                       // msg 段 count
后续各段 addr += delta;                                        // 修正后续段偏移
```

BMD 没有这个问题（`MessageScript.ToFile` 正常）。

### 2. BMD 重新编译后体积会略小

例：`E700_300.BMD` 244766 → 242673。属格式差异（对齐/填充），内容无损，`bmdverify` 已验证。

### 3. `TokenText` 的 API 限制

`TokenText` 没有 `FindLastIndex` / 索引器，必须先转换：

```csharp
var list = toks.Tokens as IList<Token>;
```

### 4. `Operand` 是装箱类型

反汇编里读 `COMM` 索引不能直接 `(int)operand`，要走字符串解析。

### 5. `E700_300.BMD` 容易被漏

它是电话/SNS/聊天回复库（1330 dialogs / 190 处标记）。第一轮只标了 `E707_*`，导致用户反馈「电话选项《好友当然要帮到底》没加 ●」——不是数据缺失，是过滤范围太窄。**批量时必须全量扫描，不要按文件名过滤。**

---

## 七、字符支持表（实测）

P5R 中文字库（`P5R_CHS.tsv`，7476 字）与字体字形是两件事，下表是**真机实测**结果。

### ✅ 已验证可显示（19 个）

```
★ ☆ ◆ ◇ ▲ △ ● ○ ◎ ■ □ ♡ → ← ↑ ↓ ※ ♂ ♀ §
```

一次测试方式：把这些符号全部塞进同一行对话 + 选项里，进游戏用眼睛确认。

### ❌ 不支持

```
▼ ▽ ♪ ♥ ¶ † ‡ ⬆(U+2B06) 半角 A-Z
```



> 注意 `♥`（黑心）不支持，但 `♡`（白心 U+2661）支持——本项目用的就是后者。  
> 半角字母不可用，全角 `Ａ-Ｚ` 可用（游戏内答案判定句就用全角）。

### 当前配色约定

| 符号 | 含义 | 为什么用这个 |
| -- | --- | --- |
| ♡ | 恋人选项 | 爱心天然对应恋爱，且只出现在 10 处，不会误读 |
| ◆ | 友人选项 | 菱形中性，与 ♡ 成对出现，一眼看出是另一条分支 |
| ↑ | 好感最优选项 | 向上箭头＝数值往上涨，与「爱心」区分开 |
| ★ | 五维正确答案 | 星＝答对，沿用旧版 |

四个符号都在真机验证过（见上表），`⭐`、`❤`、`⬆`(U+2B06) 这类 emoji/变体字符不支持。

---

## 八、覆盖情况

### 已自动覆盖

| 场景 | 机制 | 数量 |
| --- | --- | --- |
| 恋人选项（10 位女性角色） | f7[0,1,729~737/745,1] | 10 |
| 友人选项（同一选项框的另一项） | 与恋人配对 | 10 |
| Coop Rank 对话 | f8 标记 | 1038 |
| 电话 / SNS / 聊天回复 | f8 标记 | （含在上面） |
| 约会 / 日常选择 | f8 标记 | （含在上面） |
| 课堂问答 | f7[0,0,25,1] | 147 |
| 考试选择题 | f7[0,0,25,1] | （含在上面） |
| 考试思考题 | BF 反应台词判定 | 25 |
| 课堂追加思考 | BF 控制流 `if (varX == k)` 直接读正确索引 | 39 |
| 777 便利店找条形码 | `SUB_BAKODO_naiyou()` 的 `sVar5` | 12 |
| 电视猜谜 | BF 判定句 + wiki 红字 | 12 |
| 大爆炸汉堡挑战 | BF 选项分支内 `CMM_ADD_POINT` | 1 |
| 一二三 Coop「尽全力上吧」 | BF 选项分支内 `CMM_ADD_POINT_ID` | 1 |

### 无法覆盖（游戏数据不含答案，非能力问题）

| 场景            | 原因                                                                                                     |
| ------------- | ------------------------------------------------------------------------------------------------------ |
| 对话交涉（hold up） | 对错取决于运行时 `AI_GET_ID_TALK_PERSON()` 返回的敌方性格，同一选项在不同性格下走向相反；选项 token 里只有说话人前缀，无任何类型/对错字段（详见 2.5 末） |
| 打工（牛丼店 / 花店）  | 顾客要求**每次随机**，同一选项框这次对下次错，静态标注无意义（777 便利店的条形码位置是写死的，已标）                    |
| 电视问答以外的五维场景   | 游戏没有答案位（扫了 5181 个 BF 确认）                                                                               |

### 相关但未标注的标记

|        |                                                                                              |
| ------ | -------------------------------------------------------------------------------------------- |
| \`<f 7 | 0,1,971,1>`（60 处，`E696_001`/`E731_001\`）= 主线关键选择（如「发布预告信吧」），不增加数值，故未标注。若后续想标「剧情分支」，可以复用这套通道。 |

---

## 九、如何扩展（重点）

### 场景 A：游戏更新 / 重装后重做

```bash
# 1. 重新解包
CpkCli.exe "<游戏>\CPK\SC.CPK" PREFIX:EVENT_DATA/MESSAGE/E7  # 按需
CpkCli.exe "<游戏>\CPK\SC.CPK" PREFIX:EVENT_DATA/SCRIPT/E7

# 2. 重新标注
BmdTool.exe bmdromance <extract> <outBmd> ♡ ◆ ↑ ★
BmdTool.exe bfselmark <E737_002.BF> <outBF> exam_think.txt
BmdTool.exe bfselmark <TV_PROGRAM.BF> <outTV> tv_answers.txt

# 3. 校验（把四个符号都列入，校验器会跳过末尾空白后去掉符号再比对）
BmdTool.exe bmdverify <extract> <outBmd> ♡◆↑★

# 4. 部署（保持相对路径）
cp -r <outBmd>/EVENT_DATA "$DEST/"
```

### 场景 B：换用其他符号

只要改命令行参数即可，不用改代码：

```
bmdromance <src> <dst> ♡ ◆ ↑ ★      # 当前组合
bmdromance <src> <dst> ♥ ◆ ▲ ○      # 想换一套也行（注意 ♥ 不支持，见第七章）
```

已实测可用的是第七章那 19 个符号。**注意**：`bmdverify` 的第 3 个参数要把用到的符号全写进去，
否则那几个符号不会被剥掉，校验会报 diff。

### 场景 C：新增一类「有标记但没覆盖」的场景

1. 用 `bmdtok` 全量导出，统计所有 `FunctionToken` 的索引与参数组合：

```bash
BmdTool.exe bmdtok <extract> all.txt
grep -o "<f [0-9]* | [^>]*>" all.txt | sort | uniq -c | sort -rn
```

1. 找到目标标记后，在 `BmdTag.cs` 的 `TagDual` 里加一条分支，或在 `HasFunc(tt, idx, args)` 里加新组合。
2. 抽样 5-6 条人工核对，确认语义后再全量。
3. `bmdverify` 校验，部署。

### 场景 D：新增一类「没有标记」的场景

参考考试思考题的做法：**找选项分支后面的反应台词**。

```bash
BmdTool.exe bfmsgdump <目标.BF> dump.txt    # 看选项与后续台词
BmdTool.exe bfdis <目标.BF> dis.txt         # 看分支跳转结构
```


如果每个选项后面跟着不同的反馈文案（「对／错」语义明显），就能用规则文件标注；  
如果分支只是 `CALL` 子过程、看不到反馈，那静态分析就到头了，只能人工整理。

### 场景 E：把 Mod 分发给别人

只需要打包整个 `p5r.dialogue.star` 目录（358 文件 / 6.5 MB）成 zip。  
对方需要先装好 4 个依赖 Mod，然后在 Reloaded-II 里启用。

---

## 十、回滚 / 卸载

**最安全：整目录改名或删除**

```
Mods\p5r.dialogue.star  →  改名为  p5r.dialogue.star.disabled
```

或在 Reloaded-II 里取消勾选（同时从 `AppConfig.json` 的 `EnabledMods` 移除）。

**只回滚部分场景**：删掉 `P5REssentials\CPK\SC.CPK\` 下对应子目录即可，例如

```
rm -rf .../SC.CPK/FIELD/TV        # 只撤销电视猜谜
rm -rf .../SC.CPK/EVENT_DATA/SCRIPT  # 只撤销考试思考题
```

改的都是 CPK 数据包，**存档（16 个）完全不受影响**。

---

## 十一、关键路径速查

| 项目          | 路径                                                                                        |
| ----------- | ----------------------------------------------------------------------------------------- |
| 游戏          | `D:\STEAM\steamapps\common\P5R\`                                                          |
| 中文补丁包       | `D:\STEAM\steamapps\common\P5R\CPK\SC.CPK`（797 MB）                                        |
| Reloaded-II | `D:\downlode\P5RMod\ReLoad2\`                                                             |
| 本 Mod       | `...\ReLoad2\Mods\p5r.dialogue.star\`                                                     |
| 启用列表        | `...\ReLoad2\Apps\p5r.exe\AppConfig.json`                                                 |
| 日志          | `%APPDATA%\Reloaded-Mod-Loader-II\Logs\`                                                  |
| 解包 BMD      | `D:\downlode\P5RMod\_work\extract\`（2019 个）                                               |
| 解包 BF       | `D:\downlode\P5RMod\_work\bf\`（5182 个）                                                    |
| 工具源码        | `C:\Users\主人的猫\WorkBuddy\2026-11-17-15-43-10\tools\`                                      |
| 函数表         | `...\Mods\reloaded.universal.fileemulationframework.bmd\Libraries\Persona5Royal\Modules\` |
| 字符表         | `...\Mods\reloaded.universal.fileemulationframework.bmd\Charsets\P5R_CHS.tsv`             |

### 角色编号规律

角色编号 = **塔罗牌序号**，不是人物 ID：

| 角色    | 塔罗     | 编号                   |
| ----- | ------ | -------------------- |
| 坂本龙司  | 战车 7   | `CORP007` / `E707_*` |
| 新岛真   | 女教皇 2  | `CORP002` / `E702_*` |
| 东乡一二三 | 星 17   | `CORP017` / `E717_*` |
| 芳泽霞   | 信念（特殊） | 单独编号                 |

Rank 对话文件编号：`E7xx_02x`=Rank1→2，`03x`=Rank2→3，… `10x`=Rank9→10，`2xx`=第三学期。

### BF 里选项的指令形态

```
PUSHIS <对话框索引>
COMM 36            ; SEL（COMM 36 = 选项）
PUSHREG
POPLIX v
PUSHIS 0
PUSHLIX v
EQ
IF lbl_xxx         ; 分支跳转
```

常用 COMM 索引（P5R 函数表）：

| 索引      | 函数                        |
| ------- | ------------------------- |
| 5       | MSG                       |
| 34 / 35 | MSG_WND_DSP / MSG_WND_CLS |
| 36      | **SEL**（选项）               |
| 12305   | CMM_ADD_POINT（加好感）        |
| 12306   | CMM_ADD_POINT_ID          |
| 12466   | CMM_ADD_POINT_ID_ST       |

> 全量扫描结论：5181 个 BF 中 1873 个含选项，只有 12 个同时含加点调用，能可靠解析出「某选项分支内直接加点」的仅 2 个。**P5R 的 Coop 好感绝大多数是对话结束统一结算，不在分支里**——这也是为什么必须靠 BMD 的 f8 标记，而不能靠 BF 逻辑分析。
那 2 处例外已在 v0.2.2 标注：`E695_001` 的「挑战吧」标 ★、`CORP017` 的「尽全力上吧」标 ↑（见 2.6）。

---

## 十二、开发历程要点（供复盘）

1. 先解决 Reloaded-II 启动不了 → `dinput8.dll` 被 P5r Tool 占用，改名停用。
2. 技能改名 Mod 生效 → 证明「替换文本」这条路可行。
3. 尝试 `FEmulator\BF\*.msg` → 两次失败，日志无 `Building BF File`。
4. 查源码发现：只放 `.msg` 不触发接管；且二进制 `.bf`/`.bmd` 会被静默跳过。
5. 查 CPK 清单发现中文版 BF 只在 `SC.CPK`。
6. 找到 `P5REssentials\CPK` 通道 → 放完整二进制 BF → **成功**，★ ● ■ 正常显示。
7. 研究「哪个选项正确」→ 发现 f8 标记 → 与 wiki 对照 6 组全中。
8. 发现课堂用另一套 f7 标记 → ★ 覆盖课堂与考试。
9. 全量 348 个 BMD 标注 + 校验，部署。
10. 补电视猜谜（BF + 规则文件）、考试思考题（反应台词判定）。
11. 确认交涉 / 打工无法自动标注（数据不含答案）。
12. 补 2 处选项分支内加点（`E695_001` ★ / `CORP017` ↑），并校准符号统计口径（★ 235→236、↑ 1038→1039）。

**核心经验：先实证再动手。** 每次失败都靠日志与源码定位（`Bind Complete` 的真实含义、`ToFile` 不更新头部、BF 是大端），而不是靠猜。
