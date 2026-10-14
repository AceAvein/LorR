using UnityEngine;

public static class LevelVoices
{
    static AudioClip easy;
    static AudioClip moderate;
    static AudioClip hard;
    static AudioClip three;
    static AudioClip two;
    static AudioClip one;
    static AudioSource speaker;
    static int lastTier = -1;

    public static void Reset()
    {
        lastTier = -1;
    }

    public static void PlayTier(int tier)
    {
        AudioClip clip = ClipFor(tier);
        if (clip == null || tier == lastTier)
            return;

        lastTier = tier;
        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();

        AudioSource source = Speaker();
        source.PlayOneShot(clip, 1f);
    }

    public static void PlayCount(int number)
    {
        AudioClip clip = CountClip(number);
        if (clip == null)
            return;

        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();

        Speaker().PlayOneShot(clip, 1f);
    }

    static AudioClip CountClip(int number)
    {
        if (number == 3)
        {
            if (three == null)
                three = Resources.Load<AudioClip>("Voices/3");
            return three;
        }

        if (number == 2)
        {
            if (two == null)
                two = Resources.Load<AudioClip>("Voices/2");
            return two;
        }

        if (one == null)
            one = Resources.Load<AudioClip>("Voices/1");
        return one;
    }

    static AudioClip ClipFor(int tier)
    {
        if (tier <= 0)
        {
            if (easy == null)
                easy = Resources.Load<AudioClip>("Voices/Easy");
            return easy;
        }

        if (tier == 1)
        {
            if (moderate == null)
                moderate = Resources.Load<AudioClip>("Voices/Moderate");
            return moderate;
        }

        if (hard == null)
            hard = Resources.Load<AudioClip>("Voices/Hard");
        return hard;
    }

    static AudioSource Speaker()
    {
        if (speaker == null)
        {
            GameObject host = new GameObject("LevelVoice");
            Object.DontDestroyOnLoad(host);
            speaker = host.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            speaker.volume = 1f;
        }

        return speaker;
    }
}
