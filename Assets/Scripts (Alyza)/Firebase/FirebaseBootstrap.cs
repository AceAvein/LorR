using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Initializes Firebase App + Anonymous Auth (device identity for clinic headsets).
/// Also pumps a main-thread action queue for Firestore callbacks.
/// </summary>
public class FirebaseBootstrap : MonoBehaviour
{
    public static FirebaseBootstrap Instance { get; private set; }
    public static bool Ready { get; private set; }
    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseUser User { get; private set; }
    public static string DeviceId => User != null ? User.UserId : null;

    public static event Action OnReady;

    readonly Queue<Action> mainThreadActions = new Queue<Action>();
    readonly object mainThreadLock = new object();
    Coroutine pollRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    void Update()
    {
        while (true)
        {
            Action action = null;
            lock (mainThreadLock)
            {
                if (mainThreadActions.Count == 0)
                    break;
                action = mainThreadActions.Dequeue();
            }

            try { action?.Invoke(); }
            catch (Exception ex)
            {
                Debug.LogWarning("[Firebase] Main-thread action failed: " + ex.Message);
            }
        }
    }

    void OnDestroy()
    {
        if (pollRoutine != null)
        {
            StopCoroutine(pollRoutine);
            pollRoutine = null;
        }

        if (Instance == this)
            Instance = null;
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        var go = new GameObject("FirebaseBootstrap");
        go.AddComponent<FirebaseBootstrap>();
    }

    public static void RunOnMainThread(Action action)
    {
        if (action == null)
            return;

        if (Instance == null)
        {
            action();
            return;
        }

        lock (Instance.mainThreadLock)
            Instance.mainThreadActions.Enqueue(action);
    }

    public static void StartProfilePoll()
    {
        if (Instance == null)
            return;

        if (Instance.pollRoutine != null)
            Instance.StopCoroutine(Instance.pollRoutine);

        Instance.pollRoutine = Instance.StartCoroutine(Instance.PollProfilesLoop());
    }

    public static void StopProfilePoll()
    {
        if (Instance == null || Instance.pollRoutine == null)
            return;

        Instance.StopCoroutine(Instance.pollRoutine);
        Instance.pollRoutine = null;
    }

    IEnumerator PollProfilesLoop()
    {
        var wait = new WaitForSecondsRealtime(1.5f);
        while (Ready)
        {
            yield return wait;
            FirebaseProfileSync.PullOnceFromServer();
        }
    }

    void Initialize()
    {
        Ready = false;
        try
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[Firebase] Dependency check failed: " + task.Exception);
                    return;
                }

                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogWarning("[Firebase] Dependencies not available: " + task.Result);
                    return;
                }

                Auth = FirebaseAuth.DefaultInstance;
                SignInAnonymous();
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Firebase] Init exception: " + ex.Message);
        }
    }

    void SignInAnonymous()
    {
        if (Auth == null)
            return;

        if (Auth.CurrentUser != null)
        {
            User = Auth.CurrentUser;
            MarkReady();
            return;
        }

        Auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("[Firebase] Anonymous sign-in failed: " + task.Exception);
                return;
            }

            User = task.Result != null ? task.Result.User : Auth.CurrentUser;
            if (User == null)
            {
                Debug.LogWarning("[Firebase] Anonymous sign-in returned no user.");
                return;
            }

            MarkReady();
        });
    }

    void MarkReady()
    {
        Ready = true;
        Debug.Log("[Firebase] Ready. deviceId=" + DeviceId);
        OnReady?.Invoke();
    }
}
