using UnityEngine;

namespace Aim.Services
{
    public static class ProceduralSfx
    {
        const int SampleRate = 22050;

        public static AudioClip Shoot() => CreateGunshot();

        public static AudioClip Hit() =>
            Create("sfx_hit", 0.14f, (t, d) =>
            {
                var env = FastDecay(t, d, 8f);
                var ping = Mathf.Sin(t * 1320f * Mathf.PI * 2f) * 0.55f;
                var sparkle = Mathf.Sin(t * 1980f * Mathf.PI * 2f) * 0.25f;
                return (ping + sparkle) * env;
            });

        public static AudioClip Miss() =>
            Create("sfx_miss", 0.12f, (t, d) =>
            {
                var env = FastDecay(t, d, 7f);
                var thud = Mathf.Sin(t * 90f * Mathf.PI * 2f) * 0.5f;
                var dust = (RandomValue(t * 503f) * 2f - 1f) * 0.2f;
                return (thud + dust) * env;
            });

        public static AudioClip Win() =>
            Create("sfx_win", 0.7f, (t, d) =>
            {
                var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
                var idx = Mathf.Clamp(Mathf.FloorToInt(t / d * notes.Length), 0, notes.Length - 1);
                var localT = t - idx * (d / notes.Length);
                var env = FastDecay(localT, d / notes.Length, 5f);
                return Mathf.Sin(t * notes[idx] * Mathf.PI * 2f) * 0.45f * env;
            });

        public static AudioClip Lose() =>
            Create("sfx_lose", 0.65f, (t, d) =>
            {
                var start = 392f;
                var end = 130f;
                var freq = Mathf.Lerp(start, end, t / d);
                var env = FastDecay(t, d, 2.2f);
                return Mathf.Sin(t * freq * Mathf.PI * 2f) * 0.5f * env;
            });

        public static AudioClip Click() =>
            Create("sfx_click", 0.05f, (t, d) =>
            {
                var env = FastDecay(t, d, 28f);
                return Mathf.Sin(t * 1400f * Mathf.PI * 2f) * 0.35f * env;
            });

        public static AudioClip WeaponChange() =>
            Create("sfx_weapon", 0.16f, (t, d) =>
            {
                var env = FastDecay(t, d, 9f);
                var metal = Mathf.Sin(t * 720f * Mathf.PI * 2f) * 0.3f;
                var clack = Mathf.Sin(t * 210f * Mathf.PI * 2f) * 0.4f;
                var noise = (RandomValue(t * 1301f) * 2f - 1f) * 0.2f;
                return (metal + clack + noise) * env;
            });

        public static AudioClip Spawn() =>
            Create("sfx_spawn", 0.18f, (t, d) =>
            {
                var env = FastDecay(t, d, 6f);
                var whoosh = Mathf.Sin(t * Mathf.Lerp(420f, 880f, t / d) * Mathf.PI * 2f) * 0.35f;
                var pop = Mathf.Sin(t * 240f * Mathf.PI * 2f) * 0.2f;
                return (whoosh + pop) * env;
            });

        public static AudioClip HealthDrain() =>
            Create("sfx_health_drain", 0.16f, (t, d) =>
            {
                var buzz = Mathf.Sin(t * 640f * Mathf.PI * 2f) * 0.22f;
                var saw = (t * 180f % 1f * 2f - 1f) * 0.12f;
                var hiss = (RandomValue(t * 2700f) * 2f - 1f) * 0.08f;
                var pulse = 0.65f + 0.35f * Mathf.Sin(t * 28f * Mathf.PI * 2f);
                return (buzz + saw + hiss) * pulse;
            });

        public static AudioClip Coin() =>
            Create("sfx_coin", 0.22f, (t, d) =>
            {
                var env = FastDecay(t, d, 6.5f);
                var ding = Mathf.Sin(t * 1318.5f * Mathf.PI * 2f) * 0.5f;
                var over = Mathf.Sin(t * 1760f * Mathf.PI * 2f) * 0.28f;
                return (ding + over) * env;
            });

        public static AudioClip Music() =>
            Create("music_loop", 6.4f, (t, d) =>
            {
                var beat = t * (60f / 92f);
                var pulse = 0.55f + 0.45f * Mathf.Sin(beat * Mathf.PI * 2f);
                var bass = Mathf.Sin(t * 110f * Mathf.PI * 2f) * 0.22f;
                var padA = Mathf.Sin(t * 220f * Mathf.PI * 2f) * 0.12f;
                var padB = Mathf.Sin(t * 329.63f * Mathf.PI * 2f) * 0.08f;
                var hat = (RandomValue(t * 4000f) * 2f - 1f) * 0.03f * pulse;
                return (bass + padA + padB + hat) * 0.9f;
            });

        static AudioClip CreateGunshot()
        {
            const int rate = 44100;
            const float duration = 0.32f;
            var length = Mathf.Max(1, Mathf.RoundToInt(duration * rate));
            var data = new float[length];

            var lp = 0f;
            var hp = 0f;
            var echo18 = Mathf.RoundToInt(0.018f * rate);
            var echo42 = Mathf.RoundToInt(0.042f * rate);

            for (var i = 0; i < length; i++)
            {
                var t = i / (float)rate;
                var white = RandomValue(i * 12.9898f + 78.233f) * 2f - 1f;

                // One-pole filters on noise: low blast + high crack.
                lp += 0.12f * (white - lp);
                hp = white - lp;

                var crackEnv = Mathf.Exp(-t * 220f);
                var blastEnv = Mathf.Exp(-t * 38f);
                var thumpEnv = Mathf.Exp(-t * 22f);
                var tailEnv = Mathf.Exp(-t * 9f);

                var crack = hp * crackEnv * 0.95f;
                var blastNoise = lp * blastEnv * 0.7f;
                var thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(165f, 62f, Mathf.Clamp01(t * 14f)) * t) * thumpEnv * 0.55f;
                var shell = Mathf.Sin(2f * Mathf.PI * 2100f * t) * Mathf.Exp(-t * 70f) * 0.08f;
                var tail = lp * tailEnv * 0.18f;

                var sample = crack + blastNoise + thump + shell + tail;
                if (i >= echo18)
                    sample += data[i - echo18] * 0.22f;
                if (i >= echo42)
                    sample += data[i - echo42] * 0.1f;

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            SoftClip(data);
            var clip = AudioClip.Create("sfx_shoot", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void SoftClip(float[] data)
        {
            var peak = 0.0001f;
            for (var i = 0; i < data.Length; i++)
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));

            var gain = 0.92f / peak;
            for (var i = 0; i < data.Length; i++)
            {
                var x = data[i] * gain;
                data[i] = (float)System.Math.Tanh(x * 1.35);
            }
        }

        static AudioClip Create(string name, float duration, System.Func<float, float, float> sample)
        {
            var length = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[length];
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sample(t, duration), -1f, 1f);
            }

            var clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float FastDecay(float t, float duration, float sharpness)
        {
            if (duration <= 0.0001f)
                return 0f;
            var n = Mathf.Clamp01(t / duration);
            return Mathf.Exp(-n * sharpness) * (1f - n);
        }

        static float RandomValue(float seed)
        {
            var x = Mathf.Sin(seed) * 43758.5453f;
            return x - Mathf.Floor(x);
        }
    }
}
