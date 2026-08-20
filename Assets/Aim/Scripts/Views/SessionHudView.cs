using Aim.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Aim.Views
{
    public sealed class SessionHudView : MonoBehaviour
    {
        [SerializeField] Text hitsText;
        [SerializeField] Text ammoText;
        [SerializeField] Text stateText;
        [SerializeField] Image hitsIcon;
        [SerializeField] Image ammoIcon;
        [SerializeField] CanvasGroup hitsGroup;
        [SerializeField] CanvasGroup ammoGroup;

        public void SetHits(int hits, int required)
        {
            if (hitsText != null)
                hitsText.text = $"{hits} / {required}";

            if (hitsGroup != null)
                hitsGroup.alpha = 1f;

            if (hitsIcon != null)
            {
                var complete = required > 0 && hits >= required;
                hitsIcon.color = complete
                    ? new Color(0.45f, 1f, 0.55f, 1f)
                    : Color.white;
            }

            if (hitsText != null)
            {
                hitsText.color = required > 0 && hits >= required
                    ? new Color(0.55f, 1f, 0.65f, 1f)
                    : Color.white;
            }
        }

        public void SetAmmo(int remaining, int capacity)
        {
            if (ammoText != null)
                ammoText.text = $"{remaining} / {capacity}";

            var visible = capacity > 0;
            if (ammoGroup != null)
                ammoGroup.alpha = visible ? 1f : 0f;

            if (ammoIcon != null)
            {
                var lowAmmo = capacity > 0 && remaining <= Mathf.CeilToInt(capacity * 0.25f);
                ammoIcon.color = lowAmmo
                    ? new Color(1f, 0.45f, 0.35f, 1f)
                    : Color.white;
                ammoIcon.enabled = visible;
            }

            if (ammoText != null)
            {
                ammoText.enabled = visible;
                ammoText.color = capacity > 0 && remaining <= 0
                    ? new Color(1f, 0.35f, 0.35f, 1f)
                    : Color.white;
            }
        }

        public void SetState(LevelState state)
        {
            if (stateText == null)
                return;

            stateText.text = state switch
            {
                LevelState.Playing => string.Empty,
                LevelState.Won => "WIN",
                LevelState.Lost => "OUT OF AMMO",
                _ => string.Empty
            };
        }
    }
}
