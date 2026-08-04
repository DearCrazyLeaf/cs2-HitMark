using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_HitMarkApi;

public interface IHitMarkApi
{
    public static PluginCapability<IHitMarkApi> Capability { get; } = new("hitmark");

    public void StartHitMark(CCSPlayerController attacker, bool headShot, int? damage, Vector? impactPos);
}