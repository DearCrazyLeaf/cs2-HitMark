using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_HitMarkApi;

namespace CS2_HitMark;

public class HitMarkApi : IHitMarkApi
{
    public void StartHitMark(CCSPlayerController attacker, bool headShot, int? damage, Vector? impactPos)
    {
        Helper.StartHitMark(attacker, headShot, damage, impactPos);
    }
}