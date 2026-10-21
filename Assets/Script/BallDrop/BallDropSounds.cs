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
        return BuildNotes("BallCatch", 0.72f, 0.13f, 523.25f, 659.25f, 783.99f);
    }

    static AudioClip BuildMiss()
    {
        return BuildNotes("BallMiss", 0.4f, 0.16f, 349.23f, 261.63f);
    }

    static AudioClip BuildNotes(string clipName, float duration, float spacing, params float[] notes)
    {
        const int rate = 44100;
        int length = (int)(rate * duration);
        float[] samples = new float[length];

        for (int n = 0; n < notes.Length; n++)
        {
            float start = n * spacing;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float local = t - start;
                if (local < 0f)
                    continue;

                float envelope = Mathf.Exp(-local * 5.5f);
                samples[i] += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * 0.3f * envelope;
            }
        }

        AudioClip clip = AudioClip.Create(clipName, length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
