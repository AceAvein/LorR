using UnityEngine;

public static class ReactionLightSounds
{
    static AudioClip oopsClip;

    public static void PlayOops()
    {
        if (oopsClip == null)
            oopsClip = BuildOops();

        Speaker().PlayOneShot(oopsClip, 0.42f);
    }

    static AudioSource Speaker()
    {
        GameObject speaker = GameObject.Find("ReactionLightEffectSound");
        if (speaker == null)
        {
            speaker = new GameObject("ReactionLightEffectSound");
            Object.DontDestroyOnLoad(speaker);
            AudioSource created = speaker.AddComponent<AudioSource>();
            created.playOnAwake = false;
            created.spatialBlend = 0f;
            return created;
        }

        return speaker.GetComponent<AudioSource>();
    }

    static AudioClip BuildOops()
    {
        const int rate = 44100;
        const float duration = 0.32f;
        float[] notes = { 392f, 329.63f };
        int length = (int)(rate * duration);
        float[] samples = new float[length];

        for (int n = 0; n < notes.Length; n++)
        {
            float start = n * 0.12f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float local = t - start;
                if (local < 0f)
                    continue;

                float envelope = Mathf.Exp(-local * 7f);
                samples[i] += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * 0.22f * envelope;
            }
        }

        AudioClip clip = AudioClip.Create("ReactionOops", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
