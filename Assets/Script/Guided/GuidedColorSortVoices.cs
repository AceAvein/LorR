using UnityEngine;

public class GuidedColorSortVoices : MonoBehaviour
{
    public AudioClip intro;
    public AudioClip step1;
    public AudioClip step2;
    public AudioClip step3;
    public AudioClip step4;
    public AudioClip step5;
    public AudioClip step6;
    public AudioClip step7;
    public AudioClip step8;
    public AudioClip step9;
    public AudioClip step10;
    public AudioClip practice;
    public AudioClip openGrab;
    public AudioClip useLeft;
    public AudioClip useRight;
    public AudioClip yellowBoth;
    public AudioClip wrongBox;
    public AudioClip overBox;
    public AudioClip other;
    public AudioClip done;

    AudioSource speaker;

    public static GuidedColorSortVoices Find()
    {
        return FindFirstObjectByType<GuidedColorSortVoices>(FindObjectsInactive.Include);
    }

    void Awake()
    {
        EnsureSpeaker();
    }

    public void PlayIntro()
    {
        Play(intro);
    }

    public void PlayStep(int step)
    {
        if (step <= 0)
            return;

        if (step >= 11)
        {
            Play(practice);
            return;
        }

        if (step == 1)
            Play(step1);
        else if (step == 2)
            Play(step2);
        else if (step == 3)
            Play(step3);
        else if (step == 4)
            Play(step4);
        else if (step == 5)
            Play(step5);
        else if (step == 6)
            Play(step6);
        else if (step == 7)
            Play(step7);
        else if (step == 8)
            Play(step8);
        else if (step == 9)
            Play(step9);
        else if (step == 10)
            Play(step10);
    }

    public bool IsPlaying()
    {
        return speaker != null && speaker.isPlaying;
    }

    public void PlayOpen()
    {
        if (IsPlaying())
            return;

        Play(openGrab);
    }

    public void PlayUseHand(bool left)
    {
        if (IsPlaying())
            return;

        Play(left ? useLeft : useRight);
    }

    public void PlayYellowBoth()
    {
        if (IsPlaying())
            return;

        Play(yellowBoth);
    }

    public void PlayWrongBox()
    {
        Play(wrongBox);
    }

    public void PlayOverBox()
    {
        if (IsPlaying())
            return;

        Play(overBox);
    }

    public void PlayOther()
    {
        if (IsPlaying())
            return;

        Play(other);
    }

    public void PlayDone()
    {
        Play(done);
    }

    void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSpeaker();
        speaker.Stop();
        speaker.clip = clip;
        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();
        speaker.Play();
    }

    void EnsureSpeaker()
    {
        if (speaker != null)
            return;

        speaker = GetComponent<AudioSource>();
        if (speaker == null)
            speaker = gameObject.AddComponent<AudioSource>();

        speaker.playOnAwake = false;
        speaker.spatialBlend = 0f;
        speaker.loop = false;
        speaker.volume = 1f;
    }
}
