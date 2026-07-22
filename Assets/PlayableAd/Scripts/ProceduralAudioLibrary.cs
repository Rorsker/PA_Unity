using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    public static class ProceduralAudioLibrary
    {
        private const int SampleRate = 22050;

        public static void FillMissing(AudioFeedbackSettings settings, List<AudioClip> owned)
        {
            Assign(ref settings.footstepsLoop, MakeFootsteps(), owned);
            Assign(ref settings.runningWindLoop, MakeLoop("PA_WindLoop", 1f, 74f, 0.1f), owned);
            Assign(ref settings.speedEnergyLoop, MakeLoop("PA_EnergyLoop", 0.8f, 175f, 0.075f), owned);
            Assign(ref settings.elixirPickup, MakeChirp("PA_ElixirPickup", 0.16f, 620f, 1180f, 0.34f), owned);
            Assign(ref settings.elixirAbsorb, MakeChirp("PA_ElixirAbsorb", 0.32f, 230f, 760f, 0.26f), owned);
            Assign(ref settings.tierUpgrade, MakeSequence("PA_TierUpgrade", new[] { 392f, 523.25f, 659.25f, 783.99f }, 0.12f, 0.34f), owned);
            Assign(ref settings.tierUpgradeMajor, MakeSequence("PA_TierUpgradeMajor", new[] { 196f, 392f, 523.25f, 783.99f }, 0.13f, 0.34f), owned);
            Assign(ref settings.tierUpgradeMax, MakeSequence("PA_TierUpgradeMax", new[] { 196f, 392f, 659.25f, 987.77f }, 0.14f, 0.38f), owned);
            Assign(ref settings.tierDrop, MakeSequence("PA_TierDrop", new[] { 440f, 349.23f, 261.63f }, 0.13f, 0.3f), owned);
            Assign(ref settings.impactPenalty, MakeImpact("PA_ImpactPenalty", 0.24f, 82f, 0.43f, 0.2f, 31), owned);
            Assign(ref settings.speedGainImpact, MakeChirp("PA_SpeedGainImpact", 0.16f, 430f, 780f, 0.18f), owned);
            Assign(ref settings.neutralImpact, MakeImpact("PA_NeutralImpact", 0.18f, 132f, 0.3f, 0.13f, 43), owned);
            Assign(ref settings.speedLossImpact, MakeImpact("PA_SpeedLossImpact", 0.25f, 76f, 0.42f, 0.18f, 59), owned);

            if (settings.soldierImpactVariants == null || settings.soldierImpactVariants.Length < 4)
            {
                AudioClip[] previous = settings.soldierImpactVariants ?? Array.Empty<AudioClip>();
                settings.soldierImpactVariants = new AudioClip[4];
                Array.Copy(previous, settings.soldierImpactVariants, Mathf.Min(previous.Length, settings.soldierImpactVariants.Length));
            }
            for (int i = 0; i < settings.soldierImpactVariants.Length; i++)
            {
                if (settings.soldierImpactVariants[i] != null) continue;
                AudioClip generated = MakeHardImpact("PA_SoldierImpact_" + (i + 1), 0.14f + i * 0.018f,
                    118f + i * 17f, 0.42f, 0.24f, 101 + i * 17);
                settings.soldierImpactVariants[i] = generated;
                owned.Add(generated);
            }

            Assign(ref settings.impactTransient, MakeHardImpact("PA_ImpactTransient", 0.095f, 148f, 0.48f, 0.28f, 193), owned);
            Assign(ref settings.armorContact, MakeMetalClang("PA_ArmorContact", 0.14f, 590f, 0.27f, 211), owned);
            Assign(ref settings.bodyWeight, MakeImpact("PA_BodyWeight", 0.19f, 58f, 0.46f, 0.09f, 227), owned);
            Assign(ref settings.armorBreak, MakeHardImpact("PA_ArmorBreak", 0.2f, 196f, 0.24f, 0.38f, 241), owned);
            Assign(ref settings.highSpeedWhoosh, MakeChirp("PA_HighSpeedWhoosh", 0.13f, 1180f, 310f, 0.12f), owned);
            Assign(ref settings.soldierFlyAway, MakeChirp("PA_FlyAway", 0.24f, 520f, 180f, 0.17f), owned);
            Assign(ref settings.energyReturn, MakeChirp("PA_EnergyReturn", 0.18f, 420f, 920f, 0.2f), owned);
            Assign(ref settings.wallLowImpact, MakeImpact("PA_WallLow", 0.34f, 54f, 0.55f, 0.18f, 307), owned);
            Assign(ref settings.wallStoneDebris, MakeImpact("PA_StoneDebris", 0.52f, 96f, 0.34f, 0.38f, 401), owned);
            Assign(ref settings.wallDust, MakeImpact("PA_WallDust", 0.42f, 48f, 0.12f, 0.24f, 503), owned);
            Assign(ref settings.wallImpactTail, MakeChirp("PA_WallTail", 0.58f, 130f, 48f, 0.26f), owned);
            Assign(ref settings.bossContact, MakeImpact("PA_BossContact", 0.42f, 46f, 0.64f, 0.22f, 601), owned);
            Assign(ref settings.bossStruggleLoop, MakeLoop("PA_BossStruggle", 0.8f, 60f, 0.17f), owned);
            Assign(ref settings.bossFinishImpact, MakeImpact("PA_BossFinish", 0.62f, 42f, 0.72f, 0.26f, 701), owned);
            Assign(ref settings.cageBreak, MakeImpact("PA_CageBreak", 0.48f, 156f, 0.32f, 0.42f, 809), owned);
        }

        private static void Assign(ref AudioClip target, AudioClip generated, List<AudioClip> owned)
        {
            if (target != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(generated);
                else UnityEngine.Object.DestroyImmediate(generated);
                return;
            }
            target = generated;
            owned.Add(generated);
        }

        private static AudioClip MakeFootsteps()
        {
            const float duration = 0.5f;
            return Create("PA_FootstepsLoop", duration, (t, index) =>
            {
                float phase = Mathf.Repeat(t * 2f, 1f);
                float envelope = Mathf.Exp(-phase * 22f);
                return Mathf.Sin(2f * Mathf.PI * 82f * t) * envelope * 0.2f;
            });
        }

        private static AudioClip MakeLoop(string name, float duration, float fundamental, float volume)
        {
            return Create(name, duration, (t, index) =>
            {
                float a = Mathf.Sin(2f * Mathf.PI * fundamental * t);
                float b = Mathf.Sin(2f * Mathf.PI * (fundamental * 2f) * t + 0.7f);
                float c = Mathf.Sin(2f * Mathf.PI * (fundamental * 3f) * t + 1.4f);
                return (a * 0.58f + b * 0.27f + c * 0.15f) * volume;
            });
        }

        private static AudioClip MakeChirp(string name, float duration, float startFrequency, float endFrequency, float volume)
        {
            return Create(name, duration, (t, index) =>
            {
                float normalized = t / duration;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, normalized);
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(normalized));
                return Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            });
        }

        private static AudioClip MakeSequence(string name, float[] frequencies, float noteSeconds, float volume)
        {
            float duration = frequencies.Length * noteSeconds + 0.08f;
            return Create(name, duration, (t, index) =>
            {
                int note = Mathf.Min(frequencies.Length - 1, Mathf.FloorToInt(t / noteSeconds));
                float noteTime = Mathf.Repeat(t, noteSeconds) / noteSeconds;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(noteTime));
                return Mathf.Sin(2f * Mathf.PI * frequencies[note] * t) * envelope * volume;
            });
        }

        private static AudioClip MakeImpact(string name, float duration, float fundamental, float toneVolume, float noiseVolume, int seed)
        {
            uint state = (uint)seed;
            return Create(name, duration, (t, index) =>
            {
                state = state * 1664525u + 1013904223u;
                float noise = ((state >> 8) / 8388607.5f - 1f) * noiseVolume;
                float envelope = Mathf.Exp(-t * (5.5f / duration));
                float tone = Mathf.Sin(2f * Mathf.PI * fundamental * t) * toneVolume;
                return (tone + noise) * envelope;
            });
        }

        private static AudioClip MakeHardImpact(string name, float duration, float fundamental, float toneVolume,
            float noiseVolume, int seed)
        {
            uint state = (uint)seed;
            return Create(name, duration, (t, index) =>
            {
                state = state * 1664525u + 1013904223u;
                float normalized = t / duration;
                float click = index < 24 ? (1f - index / 24f) * 0.72f : 0f;
                float noise = ((state >> 8) / 8388607.5f - 1f) * noiseVolume;
                float envelope = Mathf.Exp(-normalized * 8.5f);
                float low = Mathf.Sin(2f * Mathf.PI * fundamental * t) * toneVolume;
                float edge = Mathf.Sin(2f * Mathf.PI * fundamental * 3.7f * t) * 0.16f;
                return (click + low + edge + noise) * envelope;
            });
        }

        private static AudioClip MakeMetalClang(string name, float duration, float fundamental, float volume, int seed)
        {
            uint state = (uint)seed;
            return Create(name, duration, (t, index) =>
            {
                state = state * 1103515245u + 12345u;
                float envelope = Mathf.Exp(-t * (4.8f / duration));
                float partialA = Mathf.Sin(2f * Mathf.PI * fundamental * t);
                float partialB = Mathf.Sin(2f * Mathf.PI * fundamental * 1.47f * t + 0.4f) * 0.62f;
                float partialC = Mathf.Sin(2f * Mathf.PI * fundamental * 2.19f * t + 1.1f) * 0.34f;
                float grit = ((state >> 9) / 4194303.5f - 1f) * 0.08f;
                return (partialA + partialB + partialC + grit) * volume * envelope;
            });
        }

        private static AudioClip Create(string name, float duration, Func<float, int, float> sample)
        {
            int sampleCount = Mathf.Max(64, Mathf.CeilToInt(duration * SampleRate));
            float[] data = new float[sampleCount];
            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(sample(i / (float)SampleRate, i), -1f, 1f);
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
