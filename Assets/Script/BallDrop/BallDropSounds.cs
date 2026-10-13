using UnityEngine;

public static class BallDropSounds
{
    static AudioClip catchClip;
    static AudioClip missClip;
    static AudioSource effectSource;

    public static void PlayCatch()
    {
        if (catchClip == null)
            catchClip = BuildCatch();

        Speaker().PlayOneShot(catchClip, 0.9f);
    }

    public static void PlayMiss()
    {
        if (missClip == null)
            missClip = BuildMiss();

        Speaker().PlayOneShot(missClip, 0.8f);
    }

    static AudioSource Speaker()
    {
        if (effectSource == null)
        {
            GameObject speaker = new GameObject("BallDropEffectSound");
            Object.DontDestroyOnLoad(speaker);
            effectSource = speaker.AddComponent<AudioSource>();
            effectSource.playOnAwake = false;
            effectSource.spatialBlend = 0f;
        }

        return effectSource;
    }

    static AudioClip BuildCatch()
    {
        const int rate = 44100;
        const float duration = 0.16f;
        int length = (int)(rate * duration);
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float envelope = Mathf.Exp(-t * 18f);
            float tone = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.45f;
            tone += Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.25f;
            samples[i] = tone * envelope;
        }

        AudioClip clip = AudioClip.Create("BallCatch", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip BuildMiss()
    {
        const int rate = 44100;
        const float duration = 0.22f;
        int length = (int)(rate * duration);
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float envelope = Mathf.Exp(-t * 10f);
            float frequency = Mathf.Lerp(220f, 90f, Mathf.Clamp01(t / duration));
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.55f * envelope;
        }

        AudioClip clip = AudioClip.Create("BallMiss", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
