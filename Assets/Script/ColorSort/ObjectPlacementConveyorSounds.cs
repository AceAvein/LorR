using UnityEngine;

/// <summary>
/// Procedural factory conveyor audio (motor loop + button click).
/// </summary>
public static class ObjectPlacementConveyorSounds
{
    static AudioClip motorLoop;
    static AudioClip buttonClick;

    public static AudioClip MotorLoop()
    {
        if (motorLoop == null)
            motorLoop = BuildMotorLoop();
        return motorLoop;
    }

    public static AudioClip ButtonClick()
    {
        if (buttonClick == null)
            buttonClick = BuildButtonClick();
        return buttonClick;
    }

    static AudioClip BuildMotorLoop()
    {
        const int rate = 44100;
        const float duration = 1.0f;
        int length = (int)(rate * duration);
        float[] samples = new float[length];

        // Build raw motor, then seamless-loop blend (no silent gap at seam).
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float hum = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.35f
                        + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.22f
                        + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.12f
                        + Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.08f;
            float roller = Mathf.Sin(2f * Mathf.PI * 12f * t);
            roller = roller * roller * Mathf.Sin(2f * Mathf.PI * 380f * t) * 0.12f;
            float grit = (Mathf.PerlinNoise(t * 55f, 1.7f) - 0.5f) * 0.14f;
            samples[i] = hum + roller + grit;
        }

        int fade = rate / 10;
        float[] blended = new float[length];
        for (int i = 0; i < length; i++)
        {
            if (i < fade)
            {
                float w = i / (float)fade;
                blended[i] = samples[i] * w + samples[length - fade + i] * (1f - w);
            }
            else
            {
                blended[i] = samples[i];
            }
        }

        for (int i = 0; i < length; i++)
            blended[i] = Mathf.Clamp(blended[i] * 1.35f, -1f, 1f);

        AudioClip clip = AudioClip.Create("OP_ConveyorMotor", length, 1, rate, false);
        clip.SetData(blended, 0);
        return clip;
    }

    static AudioClip BuildButtonClick()
    {
        const int rate = 44100;
        const float duration = 0.14f;
        int length = (int)(rate * duration);
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 22f);
            samples[i] = Mathf.Clamp(
                Mathf.Sin(2f * Mathf.PI * 210f * t) * 0.45f * env
                + Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.18f * env
                + (Random.value - 0.5f) * 0.05f * env,
                -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("OP_ButtonClick", length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
