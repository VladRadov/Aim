using Aim.Models;
using UnityEngine;
using Zenject;

namespace Aim.Services
{
    public sealed class AudioSettingsService : MonoBehaviour
    {
        [Inject(Id = "Music", Optional = true)]
        AudioSource _musicSource;

        [Inject(Id = "Sfx", Optional = true)]
        AudioSource _sfxSource;

        public void Apply(SettingsModel settings)
        {
            ApplyMusic(settings.MusicVolume.Value);
            ApplySfx(settings.SfxVolume.Value);
        }

        public void ApplyMusic(float volume)
        {
            if (_musicSource != null)
                _musicSource.volume = Mathf.Clamp01(volume);
        }

        public void ApplySfx(float volume)
        {
            if (_sfxSource != null)
                _sfxSource.volume = Mathf.Clamp01(volume);
        }

        public void PlaySfx(AudioClip clip, float pitch = 1f)
        {
            if (_sfxSource == null || clip == null)
                return;

            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, 1f);
        }
    }
}
