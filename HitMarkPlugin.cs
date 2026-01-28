using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Attributes;
using CS2_HitMark.Models;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using System.Threading.Tasks;

namespace CS2_HitMark;


public class HitMarkPlugin : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "CS2 HitMark";
    public override string ModuleVersion => "1.5.1";
    public override string ModuleAuthor => "DearCrazyLeaf";
    public override string ModuleDescription => "Particle hitmark with damage digits and configurable sound toggles.";
	public static HitMarkPlugin Instance { get; private set; } = null!;
    public Globals g_Main = new();
    private readonly Dictionary<uint, int> _particleOwnerByIndex = new();
    private readonly Dictionary<int, int> _activeParticleCountBySlot = new();
    private readonly Dictionary<int, ImpactInfo> _lastImpactBySlot = new();
    private readonly Dictionary<int, float> _lastHitmarkTimeBySlot = new();
    private const int MinParticleLifetimeMs = 50;
    private const float ImpactTimeoutSec = 0.2f;
    private const float HitmarkCooldownSec = 0.05f;
    private readonly IStringLocalizer<HitMarkPlugin> _localizer;
    private bool _muteDefaultHeadshot;
    private bool _muteDefaultBodyshot;
    private PlayerSettingsStore? _settingsStore;
    
    public Config Config { get; set; } = new();

    public HitMarkPlugin(IStringLocalizer<HitMarkPlugin> localizer)
    {
        _localizer = localizer;
        Instance = this;
    }

    public void OnConfigParsed(Config config)
    {
        config.Validate();
        Config = config;
        UpdateMuteFlags(config);
        InitializeSettingsStore(config);
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerHurt>(OnEventPlayerHurt);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventBulletImpact>(OnEventBulletImpact);
        RegisterEventHandler<EventRoundStart>(OnEventRoundStart);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        RegisterListener<Listeners.OnEntityDeleted>(OnEntityDeleted);
        AddCommand("css_hitmark", "Toggle hitmark particles on/off for yourself.", OnToggleHitMarkCommand);
        AddCommand("css_hitsound", "Toggle hitmark sounds on/off for yourself.", OnToggleSoundCommand);
        AddCommand("css_hitmark_particle_test", "Spawn a particle at your crosshair for testing.", OnTestParticleCommand);


        HookUserMessage(208, um =>
        {
            if (!Config.MuteDefaultHeadshotBodyshot)
            {
                return HookResult.Continue;
            }

            if (!_muteDefaultHeadshot && !_muteDefaultBodyshot)
            {
                return HookResult.Continue;
            }

            var soundevent = um.ReadUInt("soundevent_hash");
            uint HeadShotHit_ClientSide = 2831007164;
            uint Player_Got_Damage_ClientSide = 708038349;

            if ((_muteDefaultHeadshot && soundevent == HeadShotHit_ClientSide) ||
                (_muteDefaultBodyshot && soundevent == Player_Got_Damage_ClientSide))
            {
                return HookResult.Stop;
            }

            return HookResult.Continue;
        }, HookMode.Pre);
    }

    public HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        return HookResult.Continue;
    }

    public HookResult OnEventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var victim = @event.Userid;
        var dmgHealth = @event.DmgHealth;
        var dmgArmor = @event.DmgArmor;
        var health = @event.Health;
        var Hitgroup = @event.Hitgroup;

        if (victim == null || !victim.IsValid || victim.PlayerPawn == null || !victim.PlayerPawn.IsValid
        || victim.PlayerPawn.Value == null || !victim.PlayerPawn.Value.IsValid) return HookResult.Continue;

        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid) return HookResult.Continue;
        if (attacker == victim) return HookResult.Continue;

        int totalDamage = Math.Max(0, dmgHealth + dmgArmor);
        
        var config = Config;
        bool particleHitEnabled = config.HitMarkEnabled
            && (!string.IsNullOrWhiteSpace(config.HitMarkHeadshotParticle)
                || !string.IsNullOrWhiteSpace(config.HitMarkBodyshotParticle));
        bool particleDamageEnabled = config.DamageDigitsEnabled
            && config.DamageDigitParticles != null
            && config.DamageDigitParticles.Count >= 10;

        if (!particleHitEnabled && !particleDamageEnabled)
        {
            return HookResult.Continue;
        }

        if (!ShouldAllowHitmark(attacker.Slot))
        {
            return HookResult.Continue;
        }

        bool isHeadShot = Hitgroup == 1;
        Vector? impactPos = TryGetRecentImpact(attacker.Slot, out var impact) ? impact : null;
        Helper.StartHitMark(attacker, victim, isHeadShot, totalDamage, impactPos);
        
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event == null)return HookResult.Continue;
        var player = @event.Userid;

        if (player == null || !player.IsValid)return HookResult.Continue;

        if (g_Main.Player_Data.ContainsKey(player))g_Main.Player_Data.Remove(player);
        _lastImpactBySlot.Remove(player.Slot);
        _lastHitmarkTimeBySlot.Remove(player.Slot);

        return HookResult.Continue;
    }

    private void OnMapEnd()
    {
        _particleOwnerByIndex.Clear();
        _activeParticleCountBySlot.Clear();
        _lastImpactBySlot.Clear();
        _lastHitmarkTimeBySlot.Clear();
    }

    public override void Unload(bool hotReload)
    {
        _particleOwnerByIndex.Clear();
        _activeParticleCountBySlot.Clear();
        _lastImpactBySlot.Clear();
        _lastHitmarkTimeBySlot.Clear();
    }

    private void OnToggleHitMarkCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
        {
            info.ReplyToCommand("This command must be used by a player.");
            return;
        }

        if (!g_Main.Player_Data.ContainsKey(player))
        {
            Helper.InitializePlayerData(player);
        }

        if (!g_Main.Player_Data.TryGetValue(player, out var playerData))
        {
            info.ReplyToCommand("Failed to load your hitmark settings.");
            return;
        }

        playerData.HitMarkEnabled = !playerData.HitMarkEnabled;
        playerData.SettingsDirty = true;
        info.ReplyToCommand(_localizer[
            playerData.HitMarkEnabled
                ? "hitmark.command.hitmark_on"
                : "hitmark.command.hitmark_off"
        ]);
        QueueSaveSettings(player, playerData);
    }

    private void OnToggleSoundCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
        {
            info.ReplyToCommand("This command must be used by a player.");
            return;
        }

        if (!g_Main.Player_Data.ContainsKey(player))
        {
            Helper.InitializePlayerData(player);
        }

        if (!g_Main.Player_Data.TryGetValue(player, out var playerData))
        {
            info.ReplyToCommand("Failed to load your hitmark settings.");
            return;
        }

        playerData.SoundEnabled = !playerData.SoundEnabled;
        playerData.SettingsDirty = true;
        info.ReplyToCommand(_localizer[
            playerData.SoundEnabled
                ? "hitmark.command.sound_on"
                : "hitmark.command.sound_off"
        ]);
        QueueSaveSettings(player, playerData);
    }

    private void OnTestParticleCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
        {
            info.ReplyToCommand("This command must be used by a player.");
            return;
        }

        string path = string.Empty;
        if (info.ArgCount >= 2)
        {
            path = info.GetArg(1).Trim();
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            if (Config.DamageDigitParticles != null && Config.DamageDigitParticles.Count > 0)
            {
                path = Config.DamageDigitParticles[0];
            }
            else
            {
                path = Config.HitMarkHeadshotParticle;
            }
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            info.ReplyToCommand("No particle path available to test.");
            return;
        }

        bool spawned = Helper.SpawnCrosshairParticle(player, path, Config.HitMarkDistance, 1.0f, Config.HitMarkInput);
        info.ReplyToCommand(spawned
            ? $"Spawned test particle: {path}"
            : $"Failed to spawn particle: {path}");
    }

    private void OnEntityDeleted(CEntityInstance entity)
    {
        if (entity == null)
        {
            return;
        }

        ReleaseParticleOwner(entity.Index);
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (_particleOwnerByIndex.Count == 0)
        {
            return;
        }

        var snapshot = new List<KeyValuePair<uint, int>>(_particleOwnerByIndex);
        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null)
            {
                continue;
            }

            foreach (var entry in snapshot)
            {
                if (player.Slot != entry.Value)
                {
                    info.TransmitEntities.Remove((int)entry.Key);
                }
            }
        }
    }

    private HookResult OnEventBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        _lastImpactBySlot[player.Slot] = new ImpactInfo(
            new Vector(@event.X, @event.Y, @event.Z),
            (float)Server.CurrentTime
        );

        return HookResult.Continue;
    }

    private bool TryGetRecentImpact(int slot, out Vector position)
    {
        if (_lastImpactBySlot.TryGetValue(slot, out var impact))
        {
            if ((float)Server.CurrentTime - impact.Time <= ImpactTimeoutSec)
            {
                position = impact.Position;
                return true;
            }
        }

        position = new Vector(0f, 0f, 0f);
        return false;
    }

    private bool ShouldAllowHitmark(int slot)
    {
        float now = (float)Server.CurrentTime;
        if (_lastHitmarkTimeBySlot.TryGetValue(slot, out float last))
        {
            if (now - last < HitmarkCooldownSec)
            {
                return false;
            }
        }

        _lastHitmarkTimeBySlot[slot] = now;
        return true;
    }

    private void UpdateMuteFlags(Config config)
    {
        bool hasHeadSound = config.HeadshotSounds.Any(sound => !string.IsNullOrWhiteSpace(sound));
        bool hasBodySound = config.BodyshotSounds.Any(sound => !string.IsNullOrWhiteSpace(sound));
        _muteDefaultHeadshot = config.MuteDefaultHeadshotBodyshot && hasHeadSound;
        _muteDefaultBodyshot = config.MuteDefaultHeadshotBodyshot && hasBodySound;
    }

    private void InitializeSettingsStore(Config config)
    {
        _settingsStore = null;
        if (config.MySql == null || !config.MySql.Enabled)
        {
            return;
        }

        try
        {
            _settingsStore = new PlayerSettingsStore(config.MySql);
            _ = _settingsStore.InitializeAsync();
        }
        catch (Exception ex)
        {
            Helper.DebugMessage($"MySQL initialization failed: {ex.Message}");
            _settingsStore = null;
        }
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
        {
            return HookResult.Continue;
        }

        EnsurePlayerData(player);
        return HookResult.Continue;
    }

    private void EnsurePlayerData(CCSPlayerController player)
    {
        if (!g_Main.Player_Data.ContainsKey(player))
        {
            Helper.InitializePlayerData(player);
        }
        QueueLoadSettings(player);
    }

    public void QueueLoadSettings(CCSPlayerController player)
    {
        if (_settingsStore == null || player == null || !player.IsValid)
        {
            return;
        }

        if (!g_Main.Player_Data.TryGetValue(player, out var playerData))
        {
            return;
        }

        if (playerData.SettingsLoaded || playerData.SettingsLoading)
        {
            return;
        }

        playerData.SettingsLoading = true;

        ulong steamId = player.SteamID;
        if (steamId == 0)
        {
            return;
        }

        playerData.SettingsLoaded = true;
        _ = LoadPlayerSettingsAsync(player, playerData, steamId);
    }

    private async Task LoadPlayerSettingsAsync(CCSPlayerController player, Globals.PlayerDataClass playerData, ulong steamId)
    {
        var store = _settingsStore;
        if (store == null)
        {
            return;
        }

        try
        {
            var settings = await store.GetAsync(steamId);
            if (settings == null)
            {
                Server.NextFrame(() =>
                {
                    if (!g_Main.Player_Data.TryGetValue(player, out var currentData))
                    {
                        return;
                    }

                    currentData.SettingsLoaded = true;
                    currentData.SettingsLoading = false;
                });
                return;
            }

            Server.NextFrame(() =>
            {
                if (player == null || !player.IsValid)
                {
                    return;
                }

                if (!g_Main.Player_Data.TryGetValue(player, out var currentData))
                {
                    return;
                }

                if (settings != null && !currentData.SettingsDirty)
                {
                    currentData.HitMarkEnabled = settings.Value.HitMarkEnabled;
                    currentData.SoundEnabled = settings.Value.SoundEnabled;
                }

                currentData.SettingsLoaded = true;
                currentData.SettingsLoading = false;
            });
        }
        catch (Exception ex)
        {
            Helper.DebugMessage($"MySQL load failed: {ex.Message}");
            playerData.SettingsLoaded = false;
            playerData.SettingsLoading = false;
        }
    }

    private void QueueSaveSettings(CCSPlayerController player, Globals.PlayerDataClass playerData)
    {
        if (_settingsStore == null || player == null || !player.IsValid)
        {
            return;
        }

        ulong steamId = player.SteamID;
        if (steamId == 0)
        {
            return;
        }

        playerData.SettingsDirty = true;
        _ = _settingsStore.UpsertAsync(steamId, playerData.HitMarkEnabled, playerData.SoundEnabled);
    }

    private readonly struct ImpactInfo
    {
        public ImpactInfo(Vector position, float time)
        {
            Position = position;
            Time = time;
        }

        public Vector Position { get; }
        public float Time { get; }
    }

    public bool CanSpawnParticle(int slot, int maxPerPlayer)
    {
        if (maxPerPlayer <= 0)
        {
            return true;
        }

        return !_activeParticleCountBySlot.TryGetValue(slot, out int count) || count < maxPerPlayer;
    }

    public void RegisterParticleOwner(uint index, int slot)
    {
        _particleOwnerByIndex[index] = slot;
        if (_activeParticleCountBySlot.TryGetValue(slot, out int count))
        {
            _activeParticleCountBySlot[slot] = count + 1;
        }
        else
        {
            _activeParticleCountBySlot[slot] = 1;
        }
    }

    public void ReleaseParticleOwner(uint index)
    {
        if (_particleOwnerByIndex.TryGetValue(index, out int slot))
        {
            _particleOwnerByIndex.Remove(index);
            if (_activeParticleCountBySlot.TryGetValue(slot, out int count))
            {
                int next = Math.Max(0, count - 1);
                if (next == 0)
                {
                    _activeParticleCountBySlot.Remove(slot);
                }
                else
                {
                    _activeParticleCountBySlot[slot] = next;
                }
            }
        }
    }
}
