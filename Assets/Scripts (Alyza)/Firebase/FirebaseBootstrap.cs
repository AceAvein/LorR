using System;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Initializes Firebase App + Anonymous Auth (device identity for clinic headsets).
/// </summary>
public class FirebaseBootstrap : MonoBehaviour
{
    public static FirebaseBootstrap Instance { get; private set; }
    public static bool Ready { get; private set; }
    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseUser User { get; private set; }
    public static string DeviceId => User != null ? User.UserId : null;

    public static event Action OnReady;

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

    void OnDestroy()
    {
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
