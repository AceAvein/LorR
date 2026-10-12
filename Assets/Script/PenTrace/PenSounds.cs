using UnityEngine;

public static class PenSounds
{
    static AudioClip drawLoop;
    static AudioClip starChime;
    static AudioSource starSource;

    public static AudioClip DrawLoop
    {
        get
        {
            if (drawLoop == null)
                drawLoop = BuildDrawLoop();

            return drawLoop;
        }
    }

    public static void PlayStar()
    {
        if (starChime == null)
            starChime = BuildStarChime();

        if (starSource == null)
        {
            GameObject speaker = new GameObject("StarSound");
            Object.DontDestroyOnLoad(speaker);
            starSource = speaker.AddComponent<AudioSource>();
            starSource.playOnAwake = false;
            starSource.spatialBlend = 0f;
        }

        starSource.PlayOneShot(starChime, 0.85f);
    }

    static AudioClip BuildDrawLoop()
    {
        const int rate = 44100;
        int length = rate / 2;
        float[] samples = new float[length];
        float noise = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            noise = noise * 0.82f + (Random.value * 2f - 1f) * 0.18f;
            float scratch = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.15f + noise * 0.35f;
            float fade = Mathf.Min(i / 200f, (length - 1 - i) / 200f, 1f);
            samples[i] = scratch * fade;
        }

        AudioClip clip = AudioClip.Create("PenDraw", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip BuildStarChime()
    {
        const int rate = 44100;
        int length = (int)(rate * 0.32f);
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float envelope = Mathf.Exp(-t * 7f);
            float tone = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.55f;
            tone += Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.3f;
            samples[i] = tone * envelope;
        }

        AudioClip clip = AudioClip.Create("StarChime", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
