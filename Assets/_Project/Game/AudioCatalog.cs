using System.Collections.Generic;
using UnityEngine;

namespace BTH.Game
{
    /// <summary>
    /// Hợp đồng dịch vụ âm thanh BGM & SFX.
    /// </summary>
    public interface IAudioService
    {
        void PlayBgm(string bgmId);
        void PlaySfx(string sfxId);
        void SetVolumes(float master, float bgm, float sfx);
    }

    public static class AudioCatalog
    {
        public const string StoryTheme = "forest_theme";
        public const string DuelTheme = "boss_theme";
        public const float DuelPitch = 0.92f;

        public static readonly string[] BgmTracks = { StoryTheme, DuelTheme };

        public static readonly string[] SfxTracks =
        {
            "attack1", "attack2", "attack3",
            "boss_attack", "boss_roar",
            "player_death", "player_hurt",
            "enemy_death", "enemy_hurt",
            "transform_dark", "transform_light",
            "checkpoint", "dash",
            "ui_confirm", "ui_select"
        };

        public static IReadOnlyDictionary<string, string> GetAudioDescriptions() => new Dictionary<string, string>
        {
            ["forest_theme"] = "BGM Oakhaven & đối thoại tâm lý: Giai điệu cello và piano trầm buồn",
            ["boss_theme"] = "BGM Quyết đấu Boss & Duel Phase 2: Nhịp trống trận dồn dập và thánh ca bi tráng",
            ["attack1"] = "SFX Đoản kiếm Kẻ lang thang chém xé gió (Nhát 1)",
            ["attack2"] = "SFX Kỹ năng Trảm Tốc / Bùng Nổ Hoại Tử (Nhát 2)",
            ["attack3"] = "SFX Đòn phản công kết liễu khi Thắng Clash (Nhát 3)",
            ["boss_attack"] = "SFX Đòn vung vũ khí nặng của Boss",
            ["boss_roar"] = "SFX Tiếng gầm bộc phát nộ khí khi Boss xuống dưới 70% HP (Chuyển Phase 2)",
            ["player_death"] = "SFX Tiếng vỡ tan của đồng hồ thời gian khi kích hoạt vòng lặp tử vong",
            ["enemy_death"] = "SFX Tiếng quái vật / Boss gục ngã",
            ["player_hurt"] = "SFX Kẻ Lang Thang trúng đòn và còn sống",
            ["enemy_hurt"] = "SFX Quái vật hoặc Boss trúng đòn và còn sống",
            ["checkpoint"] = "SFX Xác nhận khi mở rương",
            ["transform_dark"] = "SFX Hiệu ứng ma thuật hắc ám & Tua ngược thời gian (Rewind)",
            ["transform_light"] = "SFX Hiệu ứng Thánh thuật & Hộ Kiếm (Guard / Blade Guard)",
            ["dash"] = "SFX Tiếng bước chân lướt qua ô cờ",
            ["ui_confirm"] = "SFX Xác nhận lệnh / Qua lượt / Tiếp tục hội thoại",
            ["ui_select"] = "SFX Chọn ô cờ / Chuyển chế độ lệnh trên HUD"
        };
    }

    /// <summary>
    /// Mô hình cài đặt trò chơi (GDD §3.3 - Game Options: Audio, Video/Animation Speed, Assist Options).
    /// </summary>
    public sealed class GameOptionsModel
    {
        public static readonly (int Width, int Height)[] SupportedResolutions =
        {
            (1280, 720),
            (1600, 900),
            (1920, 1080)
        };

        public float MasterVolume { get; set; } = 1.0f;
        public float BgmVolume { get; set; } = 0.40f;
        public float SfxVolume { get; set; } = 0.75f;
        public float AnimationSpeed { get; set; } = 1.0f; // x1, x1.5, x2
        public int ResolutionIndex { get; set; } = 0; // 0: 1280x720, 1: 1600x900, 2: 1920x1080
        public bool Fullscreen { get; set; } = false;
        public bool ShowGridOverlay { get; set; } = true;
        public bool ShowDangerZoneWarning { get; set; } = true;
        public bool ShowDevStageBar { get; set; } = false;

        public void LoadFromPrefs()
        {
            MasterVolume = PlayerPrefs.GetFloat("BTH_MasterVol", 1.0f);
            BgmVolume = PlayerPrefs.GetFloat("BTH_BgmVol", 0.40f);
            SfxVolume = PlayerPrefs.GetFloat("BTH_SfxVol", 0.75f);
            AnimationSpeed = PlayerPrefs.GetFloat("BTH_AnimSpeed", 1.0f);
            ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt("BTH_ResIndex", 0), 0, SupportedResolutions.Length - 1);
            Fullscreen = PlayerPrefs.GetInt("BTH_Fullscreen", 0) == 1;
            ShowGridOverlay = PlayerPrefs.GetInt("BTH_ShowGrid", 1) == 1;
            ShowDangerZoneWarning = PlayerPrefs.GetInt("BTH_ShowDanger", 1) == 1;
            ShowDevStageBar = PlayerPrefs.GetInt("BTH_DevStageBar", 0) == 1;
        }

        public void SaveToPrefs()
        {
            PlayerPrefs.SetFloat("BTH_MasterVol", MasterVolume);
            PlayerPrefs.SetFloat("BTH_BgmVol", BgmVolume);
            PlayerPrefs.SetFloat("BTH_SfxVol", SfxVolume);
            PlayerPrefs.SetFloat("BTH_AnimSpeed", AnimationSpeed);
            PlayerPrefs.SetInt("BTH_ResIndex", ResolutionIndex);
            PlayerPrefs.SetInt("BTH_Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("BTH_ShowGrid", ShowGridOverlay ? 1 : 0);
            PlayerPrefs.SetInt("BTH_ShowDanger", ShowDangerZoneWarning ? 1 : 0);
            PlayerPrefs.SetInt("BTH_DevStageBar", ShowDevStageBar ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyDisplaySettings()
        {
#if !UNITY_EDITOR
            int idx = Mathf.Clamp(ResolutionIndex, 0, SupportedResolutions.Length - 1);
            var (w, h) = SupportedResolutions[idx];
            Screen.SetResolution(w, h, Fullscreen);
#endif
        }
    }
}
