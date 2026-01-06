using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Diagnostics;

namespace CS2_HitMark;

public class Globals
{
    public class PlayerDataClass
    {
        public CCSPlayerController Player { get; }
        public string Sound_HeadShot { get; set; }
        public string Sound_BodyShot { get; set; }
        public int DamageAnimToken { get; set; }
        public bool HitMarkEnabled { get; set; }
        public bool SoundEnabled { get; set; }
        public bool SettingsLoaded { get; set; }
        public bool SettingsLoading { get; set; }
        public bool SettingsDirty { get; set; }

        public PlayerDataClass(CCSPlayerController player, string soundHeadShot, string soundBodyShot)
        {
            Player = player;
            Sound_HeadShot = soundHeadShot;
            Sound_BodyShot = soundBodyShot;
            DamageAnimToken = 0;
            HitMarkEnabled = true;
            SoundEnabled = true;
            SettingsLoaded = false;
            SettingsLoading = false;
            SettingsDirty = false;
        }
    }
    public Dictionary<CCSPlayerController, PlayerDataClass> Player_Data = new Dictionary<CCSPlayerController, PlayerDataClass>();
}
