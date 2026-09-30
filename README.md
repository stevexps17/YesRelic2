# YesRelic2

《杀戮尖塔 2》单人模组：持有 **Yes!** 遗物时，战斗后的卡牌奖励可以自由多选。

**当前版本：0.1.1 单人测试版。适配 v0.111.0；尚未完成实机交互与存读档验证。**

## 下载与安装

[下载 0.1.1 单人测试版](releases/YesRelic2-0.1.1-singleplayer.zip)

退出游戏，将压缩包中的 `YesRelic2` 文件夹解压至游戏的 `mods` 目录，在游戏内启用模组。按 **F9** 打开设置，然后新开一局单人游戏。无需 BaseLib 或 RitsuLib。

## 功能

- 战斗卡牌奖励可逐张拿取，包括全部拿走；拿过牌后点击“完成选择”放弃剩余牌。
- 可选开局额外获得遗物，保留角色原有遗物，默认开启。
- 可选加入所有角色共用的普通遗物池，默认关闭。
- 默认无限次，也可设置 1–999 次。
- 有限模式下，成功拿到同组奖励的第二张牌才扣一次；只拿一张或完全跳过不扣。
- 同一场战斗的多组奖励共享一次额度；最后一次扣到零后，该场其他奖励仍可多选。
- 次数及本场扣次状态随遗物存档。
- 仅影响单人战斗来源卡牌奖励，不修改事件、商店、升级、删牌和变牌。

设置中的次数用于之后获得的遗物，不会重置已有遗物的剩余次数。开局发放仅对新局生效。


## 源码与编译

`source/` 包含源码和 `build.ps1`。目前脚本使用 .NET SDK 6.0.410 中的 C# 编译器，引用游戏自带的 .NET 9 程序集：

```powershell
./source/build.ps1 -GamePath 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
```

需要自行安装游戏；仓库不包含游戏程序集或反编译代码。

## 致谢

玩法灵感来自 rael_kid 的一代 [YesRelic](https://steamcommunity.com/sharedfiles/filedetails/?id=2805381186)。本项目是独立实现，不使用原模组代码或美术，不代表原作者或 Mega Crit。

一代公开说明确认多选、额外初始遗物、普通遗物池、次数限制和多组奖励共享额度的设计。“第二张成功入库才扣次”是本移植明确采用的规则，未声称与一代内部实现完全一致。

## Language / 语言

The mod automatically follows the game language at startup. English and other non-Chinese locales use English; Chinese locales use Chinese text. The F9 settings panel refreshes when the game language changes, without resetting settings or remaining uses. The mod-list name is bilingual.

0.1.1：修复设置界面仅在创建时决定语言的问题，启动与切换游戏语言后自动同步。遗物描述与完成按钮跟随游戏语言。本次更新已编译，未启动游戏测试。

