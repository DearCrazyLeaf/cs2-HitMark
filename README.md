<p align="center">
  <a href="https://github.com/roflmuffin/CounterStrikeSharp">
    <img src="https://docs.cssharp.dev/images/cssharp.svg" width="60" height="60" style="vertical-align: middle; margin-right: 10px;" />
  </a>
</p>

<h3 align="center">
  <span style="vertical-align: middle; font-weight: 600;">
    <code style="vertical-align: middle;">CounterStrikeSharp</code>
  </span>
</h3>

---

# cs2-HitMark

[![中文版介绍](https://img.shields.io/badge/跳转到中文版-中文介绍-red)](#中文版介绍)
[![Release](https://img.shields.io/github/v/release/DearCrazyLeaf/cs2-HitMark?include_prereleases&color=blueviolet)](https://github.com/DearCrazyLeaf/cs2-HitMark/releases/latest)
[![License](https://img.shields.io/badge/License-GPL%203.0-orange)](https://www.gnu.org/licenses/gpl-3.0.txt)
[![Issues](https://img.shields.io/github/issues/DearCrazyLeaf/cs2-HitMark?color=darkgreen)](https://github.com/DearCrazyLeaf/cs2-HitMark/issues)
[![Pull Requests](https://img.shields.io/github/issues-pr/DearCrazyLeaf/cs2-HitMark?color=blue)](https://github.com/DearCrazyLeaf/cs2-HitMark/pulls)
[![Downloads](https://img.shields.io/github/downloads/DearCrazyLeaf/cs2-HitMark/total?color=brightgreen)](https://github.com/DearCrazyLeaf/cs2-HitMark/releases)
[![GitHub Stars](https://img.shields.io/github/stars/DearCrazyLeaf/cs2-HitMark?color=yellow)](https://github.com/DearCrazyLeaf/cs2-HitMark/stargazers)

**A lightweight CS2 hitmark plugin that renders particle hitmarks and particle damage digits at crosshair with per-player toggles.**

## Credits

- Based on refactoring and modifications of [cs2-HealthBar-HitMark-GoldKingZ](https://github.com/oqyh/cs2-HealthBar-HitMark-GoldKingZ)
- Particle usage approach inspired by [cs2-store](https://github.com/schwarper/cs2-store)

## Features

- **Particle HitMark**: Headshot/bodyshot particles at crosshair
- **Particle Damage Digits**: Compose damage numbers using digit particles (0-9)
- **Per-Player Toggles**: Players can toggle hitmarks and sounds separately
- **Configurable Durations**: Separate timings for headshot and bodyshot effects
- **Performance Guard**: Optional max active particles per player

![123](https://github.com/user-attachments/assets/0467a3b9-b39c-4ec5-8a74-a3f93c309cfe)

## Requirements

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [MetaMod:Source (CS2 branch)](https://www.metamodsource.net/downloads.php?branch=dev)

## Installation

1. Download the latest release
2. Extract to `game/csgo/addons/counterstrikesharp/plugins`
3. Start the server once to generate config
4. Edit config and restart the server

> [!IMPORTANT]
> Particle resources are not bundled with this plugin. You must upload the particle assets to your own Workshop addon and precache them yourself.

## Configuration

Config path:
`addons/counterstrikesharp/configs/plugins/cs2-HitMark/cs2-HitMark.json`

```json
{
  "version": 1,                      // Don't change this
  "mute_default_headshot_bodyshot": true, // Mute default hit sounds if custom sounds exist
  "hitmark_enabled": true,           // Enable hitmark particles
  "hitmark_headshot_particle": "particles/exg/exg_hitmarker_3d_head.vpcf",
  "hitmark_bodyshot_particle": "particles/exg/exg_hitmarker2.vpcf",
  "hitmark_headshot_duration": 0.3,  // Headshot particle lifetime (sec)
  "hitmark_bodyshot_duration": 0.25, // Bodyshot particle lifetime (sec)
  "hitmark_distance": 60,            // Distance in front of view (world units)
  "hitmark_input": "Start",          // Optional input, use empty or NONE to skip
  "damage_digits_enabled": true,     // Enable particle damage digits
  "damage_digit_particles": [
    "particles/exg/hitmarker/0.vpcf",
    "particles/exg/hitmarker/1.vpcf",
    "particles/exg/hitmarker/2.vpcf",
    "particles/exg/hitmarker/3.vpcf",
    "particles/exg/hitmarker/4.vpcf",
    "particles/exg/hitmarker/5.vpcf",
    "particles/exg/hitmarker/6.vpcf",
    "particles/exg/hitmarker/7.vpcf",
    "particles/exg/hitmarker/8.vpcf",
    "particles/exg/hitmarker/9.vpcf"
  ],
  "damage_headshot_duration": 0.3,   // Headshot digits lifetime (sec)
  "damage_bodyshot_duration": 0.25,  // Bodyshot digits lifetime (sec)
  "damage_height": 5,               // Height above victim (world units)
  "damage_spacing": 6,               // Digit spacing (world units)
  "damage_offset_x": 0,              // Horizontal offset (world units)
  "damage_offset_y": 0,              // Vertical offset (world units)
  "damage_input": "Start",           // Optional input, use empty or NONE to skip
  "max_active_particles_per_player": 30, // 0 = unlimited
  "headshot_sounds": [
    "sounds/goldkingz/hitmark/headshot.vsnd"
  ],
  "bodyshot_sounds": [
    "sounds/goldkingz/hitmark/bodyhit.vsnd"
  ],
  "debug": false,
  "mysql": {
    "enabled": false,
    "host": "127.0.0.1",
    "port": 3306,
    "database": "",
    "username": "",
    "password": "",
    "table": "cs2_hitmark_settings"
  }
}
```

> [!NOTE]
> Particle paths must be precached by another plugin or system
> This plugin does not precache resources

## MySQL Persistence

Enable the `mysql` section to persist per-player hitmark/sound toggles across sessions.

## Commands

- `!hitmark` - Toggle hitmark particles for yourself
- `!hitsound` - Toggle hitmark sounds for yourself

## Contributing

Issues and pull requests are welcome

## License

<a href="https://www.gnu.org/licenses/gpl-3.0.txt" target="_blank" style="margin-left: 10px; text-decoration: none;">
    <img src="https://img.shields.io/badge/License-GPL%203.0-orange?style=for-the-badge&logo=gnu" alt="GPL v3 License">
</a>

---

# 中文版介绍

[![English](https://img.shields.io/badge/Back%20to%20English-English-red)](#cs2-hitmark)
[![Release](https://img.shields.io/github/v/release/DearCrazyLeaf/cs2-HitMark?include_prereleases&color=blueviolet)](https://github.com/DearCrazyLeaf/cs2-HitMark/releases/latest)
[![License](https://img.shields.io/badge/License-GPL%203.0-orange)](https://www.gnu.org/licenses/gpl-3.0.txt)
[![Issues](https://img.shields.io/github/issues/DearCrazyLeaf/cs2-HitMark?color=darkgreen)](https://github.com/DearCrazyLeaf/cs2-HitMark/issues)
[![Pull Requests](https://img.shields.io/github/issues-pr/DearCrazyLeaf/cs2-HitMark?color=blue)](https://github.com/DearCrazyLeaf/cs2-HitMark/pulls)
[![Downloads](https://img.shields.io/github/downloads/DearCrazyLeaf/cs2-HitMark/total?color=brightgreen)](https://github.com/DearCrazyLeaf/cs2-HitMark/releases)
[![GitHub Stars](https://img.shields.io/github/stars/DearCrazyLeaf/cs2-HitMark?color=yellow)](https://github.com/DearCrazyLeaf/cs2-HitMark/stargazers)

**一个轻量级 CS2 HitMark 插件：使用粒子在准星位置显示击中标记与伤害数字，支持玩家本地开关。**

## 致谢

- 基于 [cs2-HealthBar-HitMark-GoldKingZ](https://github.com/oqyh/cs2-HealthBar-HitMark-GoldKingZ) 的重构与修改
- 粒子应用方法参考 [cs2-store](https://github.com/schwarper/cs2-store)

## 功能

- **粒子 HitMark**：爆头/身体命中不同粒子特效
- **数字粒子拼接**：使用 0-9 粒子组合显示伤害
- **玩家开关**：独立开关 HitMark 与声音
- **独立时间**：爆头与身体命中时长分别配置
- **性能保护**：可限制玩家活跃粒子数量

![123](https://github.com/user-attachments/assets/0467a3b9-b39c-4ec5-8a74-a3f93c309cfe)

## 需求

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [MetaMod:Source (CS2 branch)](https://www.metamodsource.net/downloads.php?branch=dev)

## 安装

1. 下载最新版本
2. 解压到 `game/csgo/addons/counterstrikesharp/plugins`
3. 启动服务器生成配置文件
4. 修改配置后重启服务器

> [!IMPORTANT]
> 本插件不包含任何粒子资源，请自行将粒子资源上传至创意工坊并完成预载。

## 配置

配置路径：
`addons/counterstrikesharp/configs/plugins/cs2-HitMark/cs2-HitMark.json`

```json
{
  "version": 1,                      // 请勿修改
  "mute_default_headshot_bodyshot": true, // 有自定义音效时静音默认命中音效
  "hitmark_enabled": true,           // 启用 HitMark 粒子
  "hitmark_headshot_particle": "particles/exg/exg_hitmarker_3d_head.vpcf",
  "hitmark_bodyshot_particle": "particles/exg/exg_hitmarker2.vpcf",
  "hitmark_headshot_duration": 0.3,  // 爆头时长(秒)
  "hitmark_bodyshot_duration": 0.25, // 身体时长(秒)
  "hitmark_distance": 60,            // 准星前方距离(世界单位)
  "hitmark_input": "Start",          // 可选输入，留空或 NONE 跳过
  "damage_digits_enabled": true,     // 启用数字粒子
  "damage_digit_particles": [
    "particles/exg/hitmarker/0.vpcf",
    "particles/exg/hitmarker/1.vpcf",
    "particles/exg/hitmarker/2.vpcf",
    "particles/exg/hitmarker/3.vpcf",
    "particles/exg/hitmarker/4.vpcf",
    "particles/exg/hitmarker/5.vpcf",
    "particles/exg/hitmarker/6.vpcf",
    "particles/exg/hitmarker/7.vpcf",
    "particles/exg/hitmarker/8.vpcf",
    "particles/exg/hitmarker/9.vpcf"
  ],
  "damage_headshot_duration": 0.3,   // 爆头数字时长(秒)
  "damage_bodyshot_duration": 0.25,  // 身体数字时长(秒)
  "damage_height": 5,               // 受击者上方高度(世界单位)
  "damage_spacing": 6,               // 数字间距(世界单位)
  "damage_offset_x": 0,              // 横向偏移
  "damage_offset_y": 0,              // 纵向偏移
  "damage_input": "Start",           // 可选输入，留空或 NONE 跳过
  "max_active_particles_per_player": 30, // 0 = 不限制
  "headshot_sounds": [
    "sounds/goldkingz/hitmark/headshot.vsnd"
  ],
  "bodyshot_sounds": [
    "sounds/goldkingz/hitmark/bodyhit.vsnd"
  ],
  "debug": false,
  "mysql": {
    "enabled": false,
    "host": "127.0.0.1",
    "port": 3306,
    "database": "",
    "username": "",
    "password": "",
    "table": "cs2_hitmark_settings"
  }
}
```

> [!NOTE]
> 粒子资源需要由其他插件或系统进行预载，本插件不负责预载

## MySQL 数据库配置

将 `mysql` 设置为 `true` 来使用数据库持久化储存玩家设置

## 命令

- `!hitmark` - 开关 HitMark 特效
- `!hitsound` - 开关 HitMark 声音

## 贡献

欢迎提交 Issue 或 Pull Request

## 许可证

<a href="https://www.gnu.org/licenses/gpl-3.0.txt" target="_blank" style="margin-left: 10px; text-decoration: none;">
    <img src="https://img.shields.io/badge/License-GPL%203.0-orange?style=for-the-badge&logo=gnu" alt="GPL v3 License">
</a>
