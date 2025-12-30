using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace CS2_HitMark.Models
{
    public class Config : IBasePluginConfig
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("mute_default_headshot_bodyshot")]
        public bool MuteDefaultHeadshotBodyshot { get; set; } = true;

        [JsonPropertyName("hitmark_enabled")]
        public bool HitMarkEnabled { get; set; } = true;

        [JsonPropertyName("hitmark_headshot_particle")]
        public string HitMarkHeadshotParticle { get; set; } = "particles/exg/exg_hitmarker_3d_head.vpcf";

        [JsonPropertyName("hitmark_bodyshot_particle")]
        public string HitMarkBodyshotParticle { get; set; } = "particles/exg/exg_hitmarker2.vpcf";

        [JsonPropertyName("hitmark_headshot_duration")]
        public float HitMarkHeadshotDuration { get; set; } = 0.2f;

        [JsonPropertyName("hitmark_bodyshot_duration")]
        public float HitMarkBodyshotDuration { get; set; } = 0.2f;

        [JsonPropertyName("hitmark_distance")]
        public float HitMarkDistance { get; set; } = 60f;

        [JsonPropertyName("hitmark_input")]
        public string HitMarkInput { get; set; } = "Start";

        [JsonPropertyName("damage_digits_enabled")]
        public bool DamageDigitsEnabled { get; set; } = true;

        [JsonPropertyName("damage_digit_particles")]
        public List<string> DamageDigitParticles { get; set; } =
        [
            "particles/exg/hitmarker/0.vpcf",
            "particles/exg/hitmarker/01.vpcf",
            "particles/exg/hitmarker/02.vpcf",
            "particles/exg/hitmarker/03.vpcf",
            "particles/exg/hitmarker/04.vpcf",
            "particles/exg/hitmarker/05.vpcf",
            "particles/exg/hitmarker/06.vpcf",
            "particles/exg/hitmarker/07.vpcf",
            "particles/exg/hitmarker/08.vpcf",
            "particles/exg/hitmarker/09.vpcf"
        ];

        [JsonPropertyName("damage_headshot_duration")]
        public float DamageHeadshotDuration { get; set; } = 0.4f;

        [JsonPropertyName("damage_bodyshot_duration")]
        public float DamageBodyshotDuration { get; set; } = 0.4f;

        [JsonPropertyName("damage_height")]
        public float DamageHeight { get; set; } = 5f;

        [JsonPropertyName("damage_spacing")]
        public float DamageSpacing { get; set; } = 13f;

        [JsonPropertyName("damage_offset_x")]
        public float DamageOffsetX { get; set; } = 0f;

        [JsonPropertyName("damage_offset_y")]
        public float DamageOffsetY { get; set; } = 0f;

        [JsonPropertyName("damage_input")]
        public string DamageInput { get; set; } = "Start";

        [JsonPropertyName("max_active_particles_per_player")]
        public int MaxActiveParticlesPerPlayer { get; set; } = 30;

        [JsonPropertyName("headshot_sounds")]
        public List<string> HeadshotSounds { get; set; } =
        [
            "sounds/goldkingz/hitmark/headshot.vsnd"
        ];

        [JsonPropertyName("bodyshot_sounds")]
        public List<string> BodyshotSounds { get; set; } =
        [
            "sounds/goldkingz/hitmark/bodyhit.vsnd"
        ];

        [JsonPropertyName("debug")]
        public bool Debug { get; set; } = false;

        public void Validate()
        {
            if (HitMarkHeadshotDuration <= 0f || HitMarkHeadshotDuration > 10f) HitMarkHeadshotDuration = 0.2f;
            if (HitMarkBodyshotDuration <= 0f || HitMarkBodyshotDuration > 10f) HitMarkBodyshotDuration = 0.2f;
            if (HitMarkDistance < 1f || HitMarkDistance > 200f) HitMarkDistance = 60f;
            if (DamageHeadshotDuration <= 0f || DamageHeadshotDuration > 10f) DamageHeadshotDuration = 0.4f;
            if (DamageBodyshotDuration <= 0f || DamageBodyshotDuration > 10f) DamageBodyshotDuration = 0.4f;
            if (DamageHeight < 1f || DamageHeight > 200f) DamageHeight = 5f;
            if (DamageSpacing < 0f || DamageSpacing > 50f) DamageSpacing = 13f;
            if (DamageOffsetX < -50f || DamageOffsetX > 50f) DamageOffsetX = 0f;
            if (DamageOffsetY < -50f || DamageOffsetY > 50f) DamageOffsetY = 0f;
            if (MaxActiveParticlesPerPlayer < 0 || MaxActiveParticlesPerPlayer > 200) MaxActiveParticlesPerPlayer = 30;

            HitMarkHeadshotParticle ??= string.Empty;
            HitMarkBodyshotParticle ??= string.Empty;
            HitMarkInput ??= "Start";
            DamageInput ??= "Start";
            DamageDigitParticles ??= new List<string>();
            while (DamageDigitParticles.Count < 10)
            {
                DamageDigitParticles.Add(string.Empty);
            }
            if (DamageDigitParticles.Count > 10)
            {
                DamageDigitParticles = DamageDigitParticles.GetRange(0, 10);
            }
        }
    }
}
