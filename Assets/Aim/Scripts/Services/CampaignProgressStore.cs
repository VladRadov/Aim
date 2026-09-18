using UnityEngine;

namespace Aim.Services
{
    public sealed class CampaignProgressStore
    {
        public int GetIndex() => Mathf.Max(0, GameSaveService.Current.Data.CampaignIndex);

        public void SetIndex(int index)
        {
            GameSaveService.Current.Data.CampaignIndex = Mathf.Max(0, index);
            GameSaveService.Current.Flush();
        }
    }
}
