using System;
using Aim.Services;
using UnityEngine;
using YG;

namespace Aim.Platform
{
    /// <summary>
    /// Maps game progress onto YG2 Storage (YG2.saves / SaveProgress).
    /// Lives outside Aim.asmdef so it can reference PluginYG2.
    /// </summary>
    public sealed class Yg2GameSaveService : IGameSaveService
    {
        const string MusicKey = "Aim.MusicVolume";
        const string SfxKey = "Aim.SfxVolume";
        const string CoinsKey = "Aim.Coins";
        const string CampaignKey = "Aim.Campaign.Index";
        const string OwnedKey = "Aim.Shop.OwnedWeapons";
        const string EquippedKey = "Aim.Shop.EquippedWeapon";

        bool _bound;

        public GameSaveData Data { get; } = new();
        public event Action Loaded;

        public void BindSdk()
        {
#if Storage_yg
            if (!_bound)
            {
                YG2.onGetSDKData += OnSdkData;
                _bound = true;
            }

            OnSdkData();
#else
            Debug.LogWarning("Yg2GameSaveService: Storage module define is missing (Storage_yg).");
#endif
        }

        public void Flush()
        {
#if Storage_yg
            CopyToYg();
            if (!YG2.isSDKEnabled)
                return;

            YG2.SaveProgress();
#endif
        }

        void OnSdkData()
        {
#if Storage_yg
            var migrated = TryMigratePlayerPrefs();
            CopyFromYg();
            Loaded?.Invoke();
            if (migrated)
                Flush();
#endif
        }

        void CopyFromYg()
        {
#if Storage_yg
            var saves = YG2.saves;
            Data.MusicVolume = Mathf.Clamp01(saves.musicVolume);
            Data.SfxVolume = Mathf.Clamp01(saves.sfxVolume);
            Data.Coins = Mathf.Max(0, saves.coins);
            Data.CampaignIndex = Mathf.Max(0, saves.campaignIndex);
            Data.OwnedWeapons = saves.ownedWeapons ?? string.Empty;
            Data.EquippedWeapon = saves.equippedWeapon ?? string.Empty;
            Data.Records.Clear();
            if (saves.levelRecords == null)
                return;

            for (var i = 0; i < saves.levelRecords.Count; i++)
            {
                var record = saves.levelRecords[i];
                if (record == null || string.IsNullOrEmpty(record.id))
                    continue;

                Data.Records.Add(new GameLevelRecordSave
                {
                    Id = record.id,
                    Accuracy = record.accuracy,
                    Time = record.time
                });
            }
#endif
        }

        void CopyToYg()
        {
#if Storage_yg
            var saves = YG2.saves;
            saves.musicVolume = Mathf.Clamp01(Data.MusicVolume);
            saves.sfxVolume = Mathf.Clamp01(Data.SfxVolume);
            saves.coins = Mathf.Max(0, Data.Coins);
            saves.campaignIndex = Mathf.Max(0, Data.CampaignIndex);
            saves.ownedWeapons = Data.OwnedWeapons ?? string.Empty;
            saves.equippedWeapon = Data.EquippedWeapon ?? string.Empty;
            saves.levelRecords ??= new System.Collections.Generic.List<AimLevelRecordSave>();
            saves.levelRecords.Clear();
            for (var i = 0; i < Data.Records.Count; i++)
            {
                var record = Data.Records[i];
                if (record == null || string.IsNullOrEmpty(record.Id))
                    continue;

                saves.levelRecords.Add(new AimLevelRecordSave
                {
                    id = record.Id,
                    accuracy = record.Accuracy,
                    time = record.Time
                });
            }
#endif
        }

        bool TryMigratePlayerPrefs()
        {
#if Storage_yg
            if (YG2.saves.idSave > 0)
                return false;

            var hasLegacy =
                PlayerPrefs.HasKey(CoinsKey) ||
                PlayerPrefs.HasKey(MusicKey) ||
                PlayerPrefs.HasKey(SfxKey) ||
                PlayerPrefs.HasKey(CampaignKey) ||
                PlayerPrefs.HasKey(OwnedKey) ||
                PlayerPrefs.HasKey(EquippedKey);

            if (!hasLegacy)
                return false;

            YG2.saves.musicVolume = PlayerPrefs.GetFloat(MusicKey, YG2.saves.musicVolume);
            YG2.saves.sfxVolume = PlayerPrefs.GetFloat(SfxKey, YG2.saves.sfxVolume);
            YG2.saves.coins = PlayerPrefs.GetInt(CoinsKey, YG2.saves.coins);
            YG2.saves.campaignIndex = PlayerPrefs.GetInt(CampaignKey, YG2.saves.campaignIndex);
            YG2.saves.ownedWeapons = PlayerPrefs.GetString(OwnedKey, YG2.saves.ownedWeapons);
            YG2.saves.equippedWeapon = PlayerPrefs.GetString(EquippedKey, YG2.saves.equippedWeapon);
            return true;
#else
            return false;
#endif
        }
    }

    public static class Yg2GameSaveBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            GameSaveService.Current = new Yg2GameSaveService();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bind()
        {
            if (GameSaveService.Current is Yg2GameSaveService service)
                service.BindSdk();
        }
    }
}

namespace YG
{
    public static partial class YG2
    {
        [StartYG]
        static void BindAimGameSaves()
        {
            if (Aim.Services.GameSaveService.Current is Aim.Platform.Yg2GameSaveService service)
                service.BindSdk();
        }
    }
}
