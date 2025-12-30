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

namespace CS2_HitMark;


public class HitMarkPlugin : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "CS2 HitMark";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "DearCrazyLeaf";
    public override string ModuleDescription => "Particle hitmark with damage digits and configurable sound toggles.";
	public static HitMarkPlugin Instance { get; private set; } = null!;
    public Globals g_Main = new();
    private readonly Dictionary<uint, int> _particleOwnerByIndex = new();
    private readonly Dictionary<int, int> _activeParticleCountBySlot = new();
    private readonly Dictionary<int, ImpactInfo> _lastImpactBySlot = new();
    private const int MinParticleLifetimeMs = 50;
    private const float ImpactTimeoutSec = 0.2f;
    private readonly IStringLocalizer<HitMarkPlugin> _localizer;
    
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
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerHurt>(OnEventPlayerHurt);
        RegisterEventHandler<EventBulletImpact>(OnEventBulletImpact);
        RegisterEventHandler<EventRoundStart>(OnEventRoundStart);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        RegisterListener<Listeners.OnEntityDeleted>(OnEntityDeleted);
        AddCommand("css_hitmark", "Toggle hitmark particles on/off for yourself.", OnToggleHitMarkCommand);
        AddCommand("css_hitsound", "Toggle hitmark sounds on/off for yourself.", OnToggleSoundCommand);


        if (Config.MuteDefaultHeadshotBodyshot)
        {
            HookUserMessage(208, um =>
            {
                var soundevent = um.ReadUInt("soundevent_hash");
                uint HeadShotHit_ClientSide = 2831007164;
                uint Player_Got_Damage_ClientSide = 708038349;

                bool HH = g_Main.Player_Data.Any(playerdata => !string.IsNullOrEmpty(playerdata.Value.Sound_HeadShot) && soundevent == HeadShotHit_ClientSide) ? true : false;
                bool BH = g_Main.Player_Data.Any(playerdata => !string.IsNullOrEmpty(playerdata.Value.Sound_BodyShot) && soundevent == Player_Got_Damage_ClientSide) ? true : false;
                if (HH || BH)
                {
                    return HookResult.Stop;
                }
                return HookResult.Continue;

            }, HookMode.Pre);
        }
    }

    public HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        Helper.ClearVariables();

        foreach(var players in Helper.GetPlayersController())
        {
            if(players == null || !players.IsValid)continue;

            Helper.InitializePlayerData(players);
        }
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

        return HookResult.Continue;
    }

    private void OnMapEnd()
    {
        Helper.ClearVariables();
        _particleOwnerByIndex.Clear();
        _activeParticleCountBySlot.Clear();
        _lastImpactBySlot.Clear();
    }

    public override void Unload(bool hotReload)
    {
        Helper.ClearVariables();
        _particleOwnerByIndex.Clear();
        _activeParticleCountBySlot.Clear();
        _lastImpactBySlot.Clear();
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
        info.ReplyToCommand(_localizer[
            playerData.HitMarkEnabled
                ? "hitmark.command.hitmark_on"
                : "hitmark.command.hitmark_off"
        ]);
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
        info.ReplyToCommand(_localizer[
            playerData.SoundEnabled
                ? "hitmark.command.sound_on"
                : "hitmark.command.sound_off"
        ]);
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
