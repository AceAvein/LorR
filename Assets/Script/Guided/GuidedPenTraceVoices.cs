using UnityEngine;

public class GuidedPenTraceVoices : MonoBehaviour
{
    public AudioClip intro;
    public AudioClip step1;
    public AudioClip step2;
    public AudioClip step3;
    public AudioClip step4;
    public AudioClip step5;
    public AudioClip wrongHand;
    public AudioClip retry;
    public AudioClip done;

    AudioSource speaker;

    public static GuidedPenTraceVoices Find()
    {
        return FindFirstObjectByType<GuidedPenTraceVoices>(FindObjectsInactive.Include);
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
    }

    public bool IsPlaying()
    {
        return speaker != null && speaker.isPlaying;
    }

    public void PlayWrongHand()
    {
        if (IsPlaying())
            return;

        Play(wrongHand);
    }

    public void PlayRetry()
    {
        Play(retry);
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
