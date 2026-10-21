using UnityEngine;

public static class PenSounds
{
    public enum Tool
    {
        Pencil,
        Ballpen,
        Marker
    }

    static AudioClip pencil;
    static AudioClip ballpen;
    static AudioClip marker;
    static AudioClip starChime;
    static AudioClip fairChime;
    static AudioClip goodChime;
    static AudioClip greatChime;
    static AudioSource effectSource;

    public static AudioClip Clip(Tool tool)
    {
        switch (tool)
        {
            case Tool.Ballpen:
                if (ballpen == null)
                    ballpen = Resources.Load<AudioClip>("Pen/BallpenWrite");
                return ballpen;
            case Tool.Marker:
                if (marker == null)
                    marker = Resources.Load<AudioClip>("Pen/MarkerWrite");
                return marker;
            default:
                if (pencil == null)
                    pencil = Resources.Load<AudioClip>("Pen/PencilWrite");
                return pencil;
        }
    }

    public static void PlayStar()
    {
        if (starChime == null)
            starChime = BuildStarChime();

        Speaker().PlayOneShot(starChime, 0.85f);
    }

    public static void PlayRating(float accuracy)
    {
        AudioClip clip;
        if (accuracy < 60f)
        {
            if (fairChime == null)
                fairChime = BuildNotes("FairChime", 0.4f, 0.16f, 349.23f, 261.63f);
            clip = fairChime;
        }
        else if (accuracy < 85f)
        {
            if (goodChime == null)
                goodChime = BuildNotes("GoodChime", 0.72f, 0.13f, 523.25f, 659.25f, 783.99f);
            clip = goodChime;
        }
        else
        {
            if (greatChime == null)
                greatChime = BuildNotes("GreatChime", 0.9f, 0.11f, 523.25f, 659.25f, 783.99f, 1046.5f);
            clip = greatChime;
        }

        Speaker().PlayOneShot(clip, 0.9f);
    }

    static AudioSource Speaker()
    {
        if (effectSource == null)
        {
            GameObject speaker = new GameObject("PenEffectSound");
            Object.DontDestroyOnLoad(speaker);
            effectSource = speaker.AddComponent<AudioSource>();
            effectSource.playOnAwake = false;
            effectSource.spatialBlend = 0f;
        }

        return effectSource;
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
