using UnityEngine;

public class GuidedBallDropVoices : MonoBehaviour
{
    public AudioClip intro;
    public AudioClip step1;
    public AudioClip step2;
    public AudioClip step3;
    public AudioClip step4;
    public AudioClip step5;
    public AudioClip wrongLeft;
    public AudioClip wrongRight;
    public AudioClip miss;
    public AudioClip done;

    AudioSource speaker;

    public static GuidedBallDropVoices Find()
    {
        return FindFirstObjectByType<GuidedBallDropVoices>(FindObjectsInactive.Include);
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

    public void PlayWrong(bool forLeftHand)
    {
        Play(forLeftHand ? wrongLeft : wrongRight);
    }

    public void PlayMiss()
    {
        Play(miss);
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
