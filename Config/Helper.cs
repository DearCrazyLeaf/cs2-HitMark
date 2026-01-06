using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Translations;
using CS2_HitMark.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CS2_HitMark;

public class Helper
{
    public static void AdvancedPlayerPrintToChat(CCSPlayerController player, string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return;

        for (int i = 0; i < args.Length; i++)
        {
            message = message.Replace($"{{{i}}}", args[i].ToString());
        }
        if (Regex.IsMatch(message, "{nextline}", RegexOptions.IgnoreCase))
        {
            string[] parts = Regex.Split(message, "{nextline}", RegexOptions.IgnoreCase);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                trimmedPart = trimmedPart.ReplaceColorTags();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    player.PrintToChat(" " + trimmedPart);
                }
            }
        }
        else
        {
            message = message.ReplaceColorTags();
            player.PrintToChat(message);
        }
    }

    public static void AdvancedPlayerPrintToConsole(CCSPlayerController player, string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return;

        for (int i = 0; i < args.Length; i++)
        {
            message = message.Replace($"{{{i}}}", args[i].ToString());
        }
        if (Regex.IsMatch(message, "{nextline}", RegexOptions.IgnoreCase))
        {
            string[] parts = Regex.Split(message, "{nextline}", RegexOptions.IgnoreCase);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                trimmedPart = trimmedPart.ReplaceColorTags();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    player.PrintToConsole(" " + trimmedPart);
                }
            }
        }
        else
        {
            message = message.ReplaceColorTags();
            player.PrintToConsole(message);
        }
    }

    public static void AdvancedServerPrintToChatAll(string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message)) return;

        for (int i = 0; i < args.Length; i++)
        {
            message = message.Replace($"{{{i}}}", args[i].ToString());
        }
        if (Regex.IsMatch(message, "{nextline}", RegexOptions.IgnoreCase))
        {
            string[] parts = Regex.Split(message, "{nextline}", RegexOptions.IgnoreCase);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                trimmedPart = trimmedPart.ReplaceColorTags();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    Server.PrintToChatAll(" " + trimmedPart);
                }
            }
        }
        else
        {
            message = message.ReplaceColorTags();
            Server.PrintToChatAll(message);
        }
    }

    public static List<CCSPlayerController> GetPlayersController(bool IncludeBots = false, bool IncludeSPEC = true, bool IncludeCT = true, bool IncludeT = true)
    {
        var playerList = Utilities
            .FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller")
            .Where(p => p != null && p.IsValid &&
                        (IncludeBots || (!p.IsBot && !p.IsHLTV)) &&
                        p.Connected == PlayerConnectedState.PlayerConnected &&
                        ((IncludeCT && p.TeamNum == (byte)CsTeam.CounterTerrorist) ||
                        (IncludeT && p.TeamNum == (byte)CsTeam.Terrorist) ||
                        (IncludeSPEC && p.TeamNum == (byte)CsTeam.Spectator)))
            .ToList();

        return playerList;
    }

    public static int GetPlayersCount(bool IncludeBots = false, bool IncludeSPEC = true, bool IncludeCT = true, bool IncludeT = true)
    {
        return Utilities.GetPlayers().Count(p =>
            p != null &&
            p.IsValid &&
            p.Connected == PlayerConnectedState.PlayerConnected &&
            (IncludeBots || (!p.IsBot && !p.IsHLTV)) &&
            ((IncludeCT && p.TeamNum == (byte)CsTeam.CounterTerrorist) ||
            (IncludeT && p.TeamNum == (byte)CsTeam.Terrorist) ||
            (IncludeSPEC && p.TeamNum == (byte)CsTeam.Spectator))
        );
    }

    public static void ClearVariables()
    {
        var g_Main = HitMarkPlugin.Instance.g_Main;
        g_Main.Player_Data.Clear();
    }

    public static void DebugMessage(string message, bool prefix = true)
    {
        if (!HitMarkPlugin.Instance.Config.Debug) return;

        Console.ForegroundColor = ConsoleColor.Magenta;
        string Prefix = $"[HitMark]: ";
        Console.WriteLine(prefix ? Prefix + message : message);

        Console.ResetColor();
    }

    public static CCSGameRules? GetGameRules()
    {
        try
        {
            var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            return gameRulesEntities.First().GameRules;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsWarmup()
    {
        return GetGameRules()?.WarmupPeriod ?? false;
    }

    public static void InitializePlayerData(CCSPlayerController player)
    {
        var g_Main = HitMarkPlugin.Instance.g_Main;
        var config = HitMarkPlugin.Instance.Config;

        if (player == null || !player.IsValid) return;

        string? headSound = ResolveSoundPath(config.HeadshotSounds);
        string? bodySound = ResolveSoundPath(config.BodyshotSounds);

        try
        {
            g_Main.Player_Data[player] = new Globals.PlayerDataClass(
                player,
                headSound ?? string.Empty,
                bodySound ?? string.Empty
            );

            HitMarkPlugin.Instance.QueueLoadSettings(player);
        }
        catch (Exception ex)
        {
            DebugMessage($"Failed to initialize player data for {player.PlayerName}: {ex.Message}");
        }
    }

    public static void StartHitMark(CCSPlayerController attacker, CCSPlayerController victim, bool headShot, int damage, Vector? impactPos)
    {
        var g_Main = HitMarkPlugin.Instance.g_Main;
        var config = HitMarkPlugin.Instance.Config;

        if (attacker == null || !attacker.IsValid) return;
        if (victim == null || !victim.IsValid) return;

        if (!g_Main.Player_Data.ContainsKey(attacker))
        {
            InitializePlayerData(attacker);
        }

        if (!g_Main.Player_Data.TryGetValue(attacker, out var playerData))
        {
            return;
        }

        try
        {
            if (playerData.HitMarkEnabled && config.HitMarkEnabled)
            {
                TrySpawnHitParticle(attacker, victim, headShot, config, impactPos);
            }
            if (playerData.HitMarkEnabled && config.DamageDigitsEnabled)
            {
                TrySpawnDamageParticles(attacker, victim, damage, headShot, config, impactPos);
            }

            if (!playerData.SoundEnabled)
            {
                DebugMessage($"HitSound skipped: SoundEnabled=false for slot {attacker.Slot}.");
            }
            else if (headShot)
            {
                if (!string.IsNullOrWhiteSpace(playerData.Sound_HeadShot))
                {
                    DebugMessage($"HitSound headshot: play {playerData.Sound_HeadShot} (slot {attacker.Slot}).");
                    attacker.ExecuteClientCommand($"play {playerData.Sound_HeadShot}");
                }
                else
                {
                    DebugMessage($"HitSound headshot missing path (slot {attacker.Slot}).");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(playerData.Sound_BodyShot))
                {
                    DebugMessage($"HitSound bodyshot: play {playerData.Sound_BodyShot} (slot {attacker.Slot}).");
                    attacker.ExecuteClientCommand($"play {playerData.Sound_BodyShot}");
                }
                else
                {
                    DebugMessage($"HitSound bodyshot missing path (slot {attacker.Slot}).");
                }
            }
        }
        catch (Exception ex)
        {
            DebugMessage($"Failed to show hitmark: {ex.Message}");
        }
    }

    private static void TrySpawnHitParticle(CCSPlayerController attacker, CCSPlayerController victim, bool headShot, Config config, Vector? impactPos)
    {
        string path = headShot ? config.HitMarkHeadshotParticle : config.HitMarkBodyshotParticle;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        float lifetime = headShot ? config.HitMarkHeadshotDuration : config.HitMarkBodyshotDuration;
        if (impactPos != null)
        {
            SpawnParticleAtPosition(attacker, impactPos!, path, lifetime, config.HitMarkInput, attacker.PlayerPawn?.Value?.EyeAngles);
        }
        else
        {
            SpawnCrosshairParticle(attacker, path, config.HitMarkDistance, lifetime, config.HitMarkInput);
        }
    }

    private static void TrySpawnDamageParticles(CCSPlayerController attacker, CCSPlayerController victim, int damage, bool headShot, Config config, Vector? impactPos)
    {
        var digits = config.DamageDigitParticles;
        if (digits == null || digits.Count < 10)
        {
            DebugMessage("Damage digits list missing or incomplete (need 10 entries).");
            return;
        }

        string damageText = Math.Max(0, damage).ToString(CultureInfo.InvariantCulture);
        if (damageText.Length == 0)
        {
            DebugMessage("Damage text empty, skipping digits.");
            return;
        }

        float lifetime = headShot ? config.DamageHeadshotDuration : config.DamageBodyshotDuration;
        DebugMessage($"Spawning damage digits '{damageText}' (headshot={headShot}).");
        SpawnDamageDigitParticles(attacker, victim, damageText, digits, config, lifetime, impactPos);
    }

    public static bool SpawnCrosshairParticle(CCSPlayerController player, string effectName, float distance, float lifetime, string? acceptInput)
    {
        return SpawnParticleAtCrosshair(player, effectName, distance, lifetime, acceptInput, null);
    }

    private static void SpawnDamageDigitParticles(CCSPlayerController attacker, CCSPlayerController victim, string damageText, List<string> digits, Config config, float lifetime, Vector? impactPos)
    {
        int count = damageText.Length;
        if (count <= 0)
        {
            return;
        }

        var attackerPawn = attacker.PlayerPawn?.Value;
        if (attackerPawn == null || !attackerPawn.IsValid)
        {
            return;
        }

        var basePos = impactPos ?? GetCrosshairPosition(attackerPawn, config.HitMarkDistance);
        if (basePos == null)
        {
            return;
        }

        Vector right = RightFromYaw(attackerPawn.EyeAngles);

        float spacing = MathF.Max(0f, config.DamageSpacing);
        float totalWidth = (count - 1) * spacing;
        Vector startPos = Subtract(basePos, Multiply(right, totalWidth * 0.5f));

        for (int i = 0; i < count; i++)
        {
            int digitIndex = damageText[i] - '0';
            if (digitIndex < 0 || digitIndex > 9)
            {
                DebugMessage($"Invalid digit '{damageText[i]}' at index {i}.");
                continue;
            }

            string path = digits[digitIndex];
            if (string.IsNullOrWhiteSpace(path))
            {
                DebugMessage($"Digit particle path missing for {digitIndex}.");
                continue;
            }

            Vector offset = Multiply(right, (i * spacing) + config.DamageOffsetX);
            Vector digitPos = Add(startPos, offset);
            digitPos = new Vector(digitPos.X, digitPos.Y, digitPos.Z + config.DamageHeight + config.DamageOffsetY);
            SpawnParticleAtPosition(attacker, digitPos, path, lifetime, config.DamageInput, attackerPawn.EyeAngles);
        }
    }

    private static bool SpawnParticleAtCrosshair(CCSPlayerController player, string effectName, float distance, float lifetime, string? acceptInput, Vector? offset)
    {
        if (player == null || !player.IsValid)
        {
            return false;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(effectName))
        {
            return false;
        }

        var plugin = HitMarkPlugin.Instance;
        if (plugin == null)
        {
            return false;
        }

        if (!plugin.CanSpawnParticle(player.Slot, HitMarkPlugin.Instance.Config.MaxActiveParticlesPerPlayer))
        {
            DebugMessage($"Particle cap reached for slot {player.Slot}.", false);
            return false;
        }

        Server.NextFrame(() =>
        {
            if (player == null || !player.IsValid)
            {
                return;
            }

            var framePawn = player.PlayerPawn.Value;
            if (framePawn == null || !framePawn.IsValid)
            {
                return;
            }

            var spawnPos = GetCrosshairPosition(framePawn, distance);
            if (spawnPos == null)
            {
                return;
            }

            if (offset != null)
            {
                var right = RightFromYaw(framePawn.EyeAngles);
                spawnPos = new Vector(
                    spawnPos.X + (right.X * offset.X),
                    spawnPos.Y + (right.Y * offset.X),
                    spawnPos.Z + offset.Y
                );
            }

            SpawnParticleAtPosition(player, spawnPos, effectName, lifetime, acceptInput, framePawn.EyeAngles);
        });

        return true;
    }

    private static void SpawnParticleAtPosition(CCSPlayerController player, Vector position, string effectName, float lifetime, string? acceptInput, QAngle? angles)
    {
        var plugin = HitMarkPlugin.Instance;
        if (plugin == null)
        {
            return;
        }

        if (!plugin.CanSpawnParticle(player.Slot, HitMarkPlugin.Instance.Config.MaxActiveParticlesPerPlayer))
        {
            DebugMessage($"Particle cap reached for slot {player.Slot}.", false);
            return;
        }

        var particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (particle == null || !particle.IsValid)
        {
            return;
        }

        particle.EffectName = effectName;
        particle.DispatchSpawn();
        particle.Teleport(position, angles, new Vector());

        if (!string.IsNullOrWhiteSpace(acceptInput) &&
            !acceptInput.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            particle.AcceptInput(acceptInput);
        }

        float safeLifetime = MathF.Max(0.05f, lifetime);
        particle.AddEntityIOEvent("Kill", null, null, "", safeLifetime);

        plugin.RegisterParticleOwner(particle.Index, player.Slot);
    }

    private static Vector? GetCrosshairPosition(CCSPlayerPawn pawn, float distance)
    {
        var origin = pawn.AbsOrigin;
        if (origin == null)
        {
            return null;
        }

        var viewOffset = pawn.ViewOffset;
        var eyePos = new Vector(
            origin.X + viewOffset.X,
            origin.Y + viewOffset.Y,
            origin.Z + viewOffset.Z
        );

        var forward = ForwardFromAngles(pawn.EyeAngles);
        float spawnDistance = MathF.Max(1f, distance);
        return new Vector(
            eyePos.X + forward.X * spawnDistance,
            eyePos.Y + forward.Y * spawnDistance,
            eyePos.Z + forward.Z * spawnDistance
        );
    }

    private static Vector ForwardFromAngles(QAngle angles)
    {
        float pitchRad = angles.X * (MathF.PI / 180f);
        float yawRad = angles.Y * (MathF.PI / 180f);

        float cp = MathF.Cos(pitchRad);
        float sp = MathF.Sin(pitchRad);
        float cy = MathF.Cos(yawRad);
        float sy = MathF.Sin(yawRad);

        return new Vector(cp * cy, cp * sy, -sp);
    }

    private static Vector RightFromYaw(QAngle angles)
    {
        float yawRad = angles.Y * (MathF.PI / 180f);
        return new Vector(-MathF.Sin(yawRad), MathF.Cos(yawRad), 0f);
    }

    private static Vector Add(Vector a, Vector b)
    {
        return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    private static Vector Subtract(Vector a, Vector b)
    {
        return new Vector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    private static Vector Multiply(Vector v, float scalar)
    {
        return new Vector(v.X * scalar, v.Y * scalar, v.Z * scalar);
    }

    private static Vector Cross(Vector a, Vector b)
    {
        return new Vector(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    private static Vector Normalize(Vector v)
    {
        float length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        if (length <= 0.0001f)
        {
            return new Vector(0f, 0f, 0f);
        }

        return new Vector(v.X / length, v.Y / length, v.Z / length);
    }
    private static string? ResolveSoundPath(List<string>? entries)
    {
        if (entries == null)
        {
            return null;
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string trimmed = entry.Trim().Trim('"');
            if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        return null;
    }

}
