using UnityEngine;

namespace Aim.Services
{
    public sealed class CampaignProgressStore
    {
        const string Key = "Aim.Campaign.Index";

        public int GetIndex() => Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));

        public void SetIndex(int index)
        {
            PlayerPrefs.SetInt(Key, Mathf.Max(0, index));
            PlayerPrefs.Save();
        }
    }
}
