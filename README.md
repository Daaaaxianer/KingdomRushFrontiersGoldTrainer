# KingdomRushFrontiersGoldTrainer

[English](README.en.md) | 简体中文

适用于 Steam《王国保卫战：前线》的 Windows x64 单文件金币倍率修改器。

## 使用

1. 在 [Releases](https://github.com/Daaaaxianer/KingdomRushFrontiersGoldTrainer/releases) 下载 KingdomRushFrontiersGoldTrainer.exe。
2. 从 Steam 启动《王国保卫战：前线》并进入关卡。
3. 打开修改器，点击 **连接游戏**，用滑块或加减按钮选择 **1–10 倍**金币收入。

**恢复 1 倍**保留连接。**断开连接**或正常关闭窗口会恢复后续正常收入，已获得的金币会保留。异常退出后，心跳超时约 2.5 秒恢复 1 倍。

倍率作用于击杀金币和提前开波奖励。支持连接正在运行的原版游戏及当前关卡。界面沿用 [KingdomRushGoldTrainer](https://github.com/Daaaaxianer/KingdomRushGoldTrainer)。

## 兼容性

Windows x64、.NET Framework 4.8。兼容性按游戏及 lua51.dll 的 SHA256 校验，具体值见 compatibility.json。游戏更新后可能需要重新适配。让游戏与修改器使用相同用户权限运行。

连接时在游戏进程中加载内嵌 DLL，并安装 Lua 金币奖励处理。首次连接会把本地 kingdom_rush_frontiers 存档备份到 %LOCALAPPDATA%/KRFLocalGoldTrainer/存档备份。连接模块释放到系统临时目录。

已通过编译、界面控件、缩放、后台连接及模拟会话测试。实际关卡连接与金币奖励效果尚未完成实机验证。

## 构建

需要 Python 3.9+、Windows .NET Framework x64 csc.exe 和 MinGW-w64 x64 GCC。

```powershell
python scripts/build.py --test --gcc "路径/gcc.exe"
```

产物位于 dist/KingdomRushFrontiersGoldTrainer.exe，校验值位于 dist/SHA256SUMS.txt。

## 许可

采用 MIT License，基于 Daaaaxianer/KingdomRushGoldTrainer 适配。仓库包含原创修改器源码。Kingdom Rush 及相关商标属于其权利人，本项目为非官方工具。
