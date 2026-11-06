# 勤务之星 / Service Stars 1.2.1

Source repository: https://github.com/Shane-Xue/spt-service-star

License: [MIT](LICENSE), copyright 2026 Shane Xue.

## Profile binding and safe removal

Service Stars does **not modify SPT's main user profile files** and does not require the mod to remain installed for a profile to load. The Forge's **Disable Profile Binding Notice** option can be enabled for this mod.

- Server progress is stored separately in `user/profileData/ServiceStars/<profile-id>.json`, relative to the server directory.
- Client counters awaiting synchronization and the display cache are stored in `BepInEx/config/ServiceStars/`.
- The mod reads the authenticated SPT profile to identify the player, but does not write its own fields, items, quests, or progression into the main profile.
- To uninstall, close the game and server, then remove `BepInEx/plugins/ServiceStars/` and the server's `user/mods/ServiceStars/` folder. No main-profile repair or migration is required.
- Separate progress/cache files remain unless you delete them manually. Keep both locations to retain progress for reinstallation; they are player data and are not included in release packages.

## Download / 下载

[ServiceStars 1.2.1 — ready-to-install ZIP](releases/ServiceStars-1.2.1-SPT-4.1.6.zip)

Includes the compiled client and server 1.2.1, default configuration, installation instructions, and MIT license. This is the minimum usable baseline; it does not include the newer career-page preview.

**Mod GUID (both client and server): `com.shane-xue.service-stars`.** Enter this exact value in The Forge's Mod GUID field. The publication build replaces the earlier local-development GUID; replace both DLLs together. Existing ServiceStars statistics and pending counters use unchanged storage locations and remain available. To retain old F12 preferences, copy `BepInEx/config/local.service-stars.cfg` to `BepInEx/config/com.shane-xue.service-stars.cfg` with the game closed. Otherwise the new configuration uses defaults.

Publication source tag: `v1.2.1-forge.1`. The original `v1.2.1` tag remains the pre-publication baseline. To rebuild the publication ZIP, run `build.ps1`, `test.ps1`, then `python tools/package-publication.py`; this packaging helper does not deploy anything.

**最小好用版本 / 回退基线：1.2.1。** 详见 [BASELINE.md](BASELINE.md)。后续新功能只提供安装包，由用户手动安装。

适用本机 SPT 4.1.6 / EFT 0.16.9.5.40743。客户端 BepInEx 插件与独立服务端模组配套使用。

## 功能

- 安装后开始统计玩家 PMC 的枪械、手雷、刀具、独立和下挂发射器击杀，每个武器模板单独累计。默认每 100 杀获得一颗星，无上限。当前版本暂不改变伤害、后坐力或其他属性；未来可能探索可通过配置启用或关闭的属性加成，尚未实现。升级保留已有枪械进度，无需迁移存档。
- 同型号不同实例、不同改装共享进度；不同模板（包括自定义枪）分别统计。
- 已鉴定枪械、手雷、刀具与下挂发射器的检视窗口属性区域显示星数、累计击杀、下一颗星进度。
- 升星时屏幕中央偏上显示做旧黄铜星章、枪械名称、“勤务之星”与星数，淡入并轻微收拢，停留 3.2 秒后淡出。配原创轻量金属提示音；使用透明图片，无需外部素材或 AssetBundle。
- 计入游戏归属于本地 PMC 的子弹、钝击、近战、爆炸、破片及保留武器来源的流血、燃烧/毒伤等战斗击杀。不计 Scav 玩家、自己、同组队友、摔落和饥渴等环境死亡。
- 手雷爆炸与破片按实际手雷型号累计；一颗雷击杀多人分别计数，同一受害者不因爆炸/破片重复触发而多算。刀具按型号累计。榴弹按游戏伤害记录中的原始发射武器累计；记录保留下挂发射器时计入该配件，而非当前手持的主枪。
- 客户端按游戏物品类型/近战组件识别，服务端按 WEAPON、THROW_WEAP、KNIFE、LAUNCHER 的模板继承关系验证，因此常规模组新增的对应武器可自动参与。无法提供原始武器或绕过标准击杀统计流程的特殊模组尚不保证支持；不使用当前手持武器或弹药模板猜测击杀来源。
- 死亡不清零，无需撤离。使用伤害记录中的枪械，避免换枪后错记到手持武器。
- 从安装后开始记录：EFT 没有向本模组提供可完整、可靠导入的各武器历史击杀记录，无法准确重建旧战绩，因此不回填历史记录。

## 安装与使用

发行包解压到游戏根目录，目录对应：

```text
BepInEx/plugins/ServiceStars/ServiceStars.Client.dll
SPT_Runtime/user/mods/ServiceStars/ServiceStars.Server.dll
SPT_Runtime/user/mods/ServiceStars/config.json
```

重启服务端和游戏即可。升级至 1.2.0 需要同时更新客户端和服务端，旧版服务端可能不会接收新增类别的进度。主菜单等待首次同步完成后检视一把武器。缺少服务端模组时显示“待同步”，新击杀仍保留在本机等待补传。

远程服务端：将发行包中 `SPT_Runtime/user/mods/ServiceStars` 文件夹安装到实际服务器的 `user/mods/`；每位玩家安装客户端。客户端使用 SPT 自带请求通道、当前服务器地址和会话身份，不填写服务器地址或角色 ID。无需由 Fika 房主转发。已基于 SPT 4.1.6 / EFT 0.16.9.5.40743 构建并测试。根据玩家的 Fika 合作实测，各人可正常查看自己的勤务之星；目前不支持查看队友的战绩。这不代表已经覆盖所有 Fika 版本与模组组合。

修改服务器 `config.json` 的 `KillsPerStar` 可调整门槛（1–100000），重启服务端后生效，客户端下一次同步获取。已有击杀数保留，星数按新门槛重算。客户端 F12 配置项可关闭升星通知。

### 游戏语言 / locale

检视文案和中央升星通知自动读取游戏 `LocalizationManager.Culture`：`ch` 使用中文，`en` 使用英文，其他未翻译语言回退英文。切换游戏语言后自动刷新；武器名称使用游戏自己的本地化。旧版 F12 的 `Chinese` 项不再读取。文案集中在源码 `assets/locales.json`，打包时嵌入 DLL，不改变游戏语言设置与服务端数据。

### 中央通知预览与设置

游戏主菜单或战局中按 F12，找到 **Service Stars → Promotion → Preview notification**，勾选一次即播放第 3 颗星的演示，按当前游戏语言显示文案，开关自动复位。演示不增加击杀、不修改星数，通知开关关闭时也可以手动预览。

- `Hold seconds`：停留时长，默认 3.2 秒，另有 0.45 秒淡入和 0.65 秒淡出。
- `Sound volume`：提示音音量，默认 0.18，设为 0 静音。
- `Vertical position`：通知整体在屏幕中的高度，默认 0.67，可调整 0.35–0.85。
- `Display → Promotion notifications`：关闭真实升星时的中央提示。

图片由内置 imagegen 生成，是受早期军功章美术启发的原创设计，不是从《战地1》提取的贴图。资源源文件为 `mods-dev/service-stars/assets/service-star.png`，提示词与来源说明见同目录 `ARTWORK.md`，运行时嵌入客户端 DLL。

## 数据与同步

- 服务端权威数据：`SPT_Runtime/user/profileData/ServiceStars/<角色ID>.json`。
- 客户端待同步数据及显示缓存：`BepInEx/config/ServiceStars/<服务器与会话的SHA256>.json`。
- 两端使用同目录临时文件、刷新磁盘、原子替换，保留上一版 `.bak`。不会编辑 SPT 主档案、游戏核心 DLL 或数据库。
- 客户端第一次启动生成一个持久 stream ID，按枪保存该 stream 的累计击杀；服务端对每个 stream 每把枪取最大值，再跨 stream 求和。重发、乱序、响应丢失不会重复计数，不同电脑的新 stream 可以合并。
- 每次新击杀先保存客户端文件，随后异步发送；正常空闲约 15 秒同步，战斗中约 1 秒触发，失败后约 30 秒重试。网络请求不阻塞游戏主线程。
- 显示数 = 服务端累计 + 本机尚未确认的增量。请求期间新增的击杀保留到下一次发送。
- 备份或迁移进度时同时保留服务端数据与客户端缓存。不要把同一个客户端缓存复制给两台电脑同时游玩，两个客户端应各自生成 stream ID。服务器恢复旧备份后，本机保留的累计数可能重新补回备份之后的击杀。
- JSON 损坏会报错并停止对应记录/同步，避免覆盖为零。关闭游戏/服务端后可从同文件 `.bak` 恢复。磁盘持续写入失败时不能保证强退前的新增记录被保留。

## 验收（需要实际启动游戏）

1. 启动服务端、进入游戏，日志中无 `ServiceStars` 加载错误。检视已鉴定枪械，显示 `★0`、`击杀 0`，首次同步后“待同步”消失。
   在 F12 使用一次 `Preview notification`，确认中央星章透明背景、中文字体、淡入淡出和提示音正常；预览前后检视击杀数应相同。可重复预览、静音，并在不同分辨率下确认位置。
2. PMC 携带两种枪进图。用 A 击杀，切换 B，再检视 A/B；A 增加，B 不增加。换同型号的另一把 A，进度一致。
3. 分别用手雷、刀具、榴弹完成击杀，检视同型号物品确认各自增加。扔雷后换枪，手雷击杀应计入手雷、手持枪不增加；一雷多杀按受害者人数增加。Scav 游玩仍不计。
   死亡回到仓库后重启服务端和游戏，已有进度应保留。模组新增枪械/刀具/手雷各选一件实测，确认正常显示与同步。
4. 快速升星测试：关闭服务器，将门槛改为 `1`，重启服务器，等客户端同步后再击杀；应出现晋升提示。测试后恢复 `100`。这会记录真实新击杀，不需要修改档案。
5. 断开服务器连接或关闭服务端后完成一次有效击杀，应出现“待同步”；重启服务端并等待约 30 秒，进度同步且只增加一次。
6. 联机需分别检查房主和非房主只记录自己的击杀；同时确认与现有 GemMod/UIFixes 检视内容没有遮挡。

自动检查覆盖存储和真实 SPT 路由，不能代替 Unity 界面、实际战局与大量现有插件的运行验收。第一版不承诺跨游戏进程的第三方战局回档重放去重；同一存活战局内按受害者 ID 去重。

## 卸载

关闭游戏和服务端，移走 `BepInEx/plugins/ServiceStars` 与 `SPT_Runtime/user/mods/ServiceStars`。保留两处数据目录即可保留进度；移除模组不影响原档案。

## 开发

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\audit-bindings.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

仅引用本机安装的程序集与 .NET SDK，不下载依赖。`package.ps1 -Deploy` 在游戏与服务端退出时部署本模组；发行包使用明确的文件清单，不打包档案、缓存或其他模组。
