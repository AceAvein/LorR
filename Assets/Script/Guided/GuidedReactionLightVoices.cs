using UnityEngine;

public class GuidedReactionLightVoices : MonoBehaviour
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
    public AudioClip useLeft;
    public AudioClip useRight;
    public AudioClip needPoke;
    public AudioClip avoidYellow;
    public AudioClip avoidRed;
    public AudioClip done;

    AudioSource speaker;

    public static GuidedReactionLightVoices Find()
    {
        return FindFirstObjectByType<GuidedReactionLightVoices>(FindObjectsInactive.Include);
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
        else if (step == 6)
            Play(step6);
        else if (step == 7)
            Play(step7);
        else if (step == 8)
            Play(step8);
    }

    public bool IsPlaying()
    {
        return speaker != null && speaker.isPlaying;
    }

    public void PlayUseHand(bool rightHand)
    {
        if (IsPlaying())
            return;

        Play(rightHand ? useRight : useLeft);
    }

    public void PlayNeedPoke()
    {
        if (IsPlaying())
            return;

        Play(needPoke);
    }

    public void PlayAvoidYellow()
    {
        Play(avoidYellow);
    }

    public void PlayAvoidRed()
    {
        Play(avoidRed);
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
