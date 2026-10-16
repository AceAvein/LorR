using UnityEngine;

public class LevelMusic : MonoBehaviour
{
    static LevelMusic active;

    AudioSource source;
    AudioClip easy;
    AudioClip moderate;
    AudioClip hard;
    int tier = -1;
    bool pausedByMenu;

    public static void Play(int tier)
    {
        if (active == null)
        {
            GameObject host = new GameObject("LevelMusic");
            active = host.AddComponent<LevelMusic>();
        }

        active.Switch(tier);
    }

    void Awake()
    {
        if (active != null && active != this)
        {
            Destroy(gameObject);
            return;
        }

        active = this;
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.22f;
        easy = Resources.Load<AudioClip>("Music/Easy");
        moderate = Resources.Load<AudioClip>("Music/Moderate");
        hard = Resources.Load<AudioClip>("Music/Hard");
    }

    void OnDestroy()
    {
        if (active == this)
            active = null;
    }

    void Update()
    {
        if (source == null || source.clip == null)
            return;

        if (GamePauseMenu.IsFrozen)
        {
            if (source.isPlaying)
            {
                source.Pause();
                pausedByMenu = true;
            }
            return;
        }

        if (pausedByMenu)
        {
            source.UnPause();
            pausedByMenu = false;
        }
    }

    void Switch(int nextTier)
    {
        if (nextTier < 0)
            nextTier = 0;
        if (nextTier > 2)
            nextTier = 2;
        if (nextTier == tier)
            return;

        tier = nextTier;
        AudioClip clip = ClipFor(tier);
        if (clip == null || source == null)
            return;

        source.clip = clip;
        if (GamePauseMenu.IsFrozen)
        {
            pausedByMenu = true;
            return;
        }

        source.Play();
        pausedByMenu = false;
    }

    AudioClip ClipFor(int index)
    {
        if (index <= 0)
            return easy;
        if (index == 1)
            return moderate;
        return hard;
    }
}
