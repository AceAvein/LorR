using UnityEngine;

/// <summary>
/// Drives Dr. Alex Rivera Mixamo animator parameters.
/// Assign DrRivera.controller on the same GameObject's Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DrRiveraAnimator : MonoBehaviour
{
    public static class Params
    {
        public const string IsWalking = "IsWalking";
        public const string IsTalking = "IsTalking";
        public const string IsObserving = "IsObserving";
        public const string IsSad = "IsSad";
        public const string Point = "Point";
        public const string Clap = "Clap";
        public const string Wave = "Wave";
        public const string Agree = "Agree";
        public const string Greet = "Greet";
    }

    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetWalking(bool on) => SetExclusiveHold(Params.IsWalking, on);
    public void SetTalking(bool on) => SetExclusiveHold(Params.IsTalking, on);
    public void SetObserving(bool on) => SetExclusiveHold(Params.IsObserving, on);
    public void SetSad(bool on) => SetExclusiveHold(Params.IsSad, on);

    public void Point() => Fire(Params.Point);
    public void Clap() => Fire(Params.Clap);
    public void Wave() => Fire(Params.Wave);
    public void Agree() => Fire(Params.Agree);
    public void Greet() => Fire(Params.Greet);

    public void ReturnToIdle()
    {
        if (animator == null)
            return;

        animator.SetBool(Params.IsWalking, false);
        animator.SetBool(Params.IsTalking, false);
        animator.SetBool(Params.IsObserving, false);
        animator.SetBool(Params.IsSad, false);
    }

    /// <summary>Welcome beat: greet one-shot then hold talking.</summary>
    public void PlayWelcome()
    {
        ReturnToIdle();
        Greet();
        SetTalking(true);
    }

    /// <summary>Unguided observer pose.</summary>
    public void PlayObserve()
    {
        ReturnToIdle();
        SetObserving(true);
    }

    /// <summary>Commend / GOOD JOB.</summary>
    public void PlayCommend()
    {
        ReturnToIdle();
        Clap();
    }

    /// <summary>Soft fail / unmet metrics.</summary>
    public void PlaySoftFail()
    {
        ReturnToIdle();
        SetSad(true);
    }

    void SetExclusiveHold(string holdParam, bool on)
    {
        if (animator == null)
            return;

        if (on)
        {
            animator.SetBool(Params.IsWalking, holdParam == Params.IsWalking);
            animator.SetBool(Params.IsTalking, holdParam == Params.IsTalking);
            animator.SetBool(Params.IsObserving, holdParam == Params.IsObserving);
            animator.SetBool(Params.IsSad, holdParam == Params.IsSad);
        }
        else
        {
            animator.SetBool(holdParam, false);
        }
    }

    void Fire(string trigger)
    {
        if (animator == null)
            return;

        animator.SetTrigger(trigger);
    }
}
