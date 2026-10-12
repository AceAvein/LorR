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
    static AudioSource starSource;

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
