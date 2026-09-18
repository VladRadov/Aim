using System;
using System.Collections.Generic;

namespace Aim.Services
{
    [Serializable]
    public sealed class GameLevelRecordSave
    {
        public string Id;
        public float Accuracy = -1f;
        public float Time = float.MaxValue;
    }

    public sealed class GameSaveData
    {
        public float MusicVolume = 0.7f;
        public float SfxVolume = 0.85f;
        public int Coins;
        public int CampaignIndex;
        public string OwnedWeapons = string.Empty;
        public string EquippedWeapon = string.Empty;
        public List<GameLevelRecordSave> Records { get; } = new();

        public GameLevelRecordSave GetOrCreateRecord(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (var i = 0; i < Records.Count; i++)
            {
                if (Records[i] != null && Records[i].Id == id)
                    return Records[i];
            }

            var created = new GameLevelRecordSave { Id = id };
            Records.Add(created);
            return created;
        }
    }

    public interface IGameSaveService
    {
        GameSaveData Data { get; }
        event Action Loaded;
        void Flush();
    }

    public sealed class MemoryGameSaveService : IGameSaveService
    {
        public GameSaveData Data { get; } = new();
        public event Action Loaded;

        public void Flush()
        {
        }
    }

    public static class GameSaveService
    {
        public static IGameSaveService Current { get; set; } = new MemoryGameSaveService();
    }
}
