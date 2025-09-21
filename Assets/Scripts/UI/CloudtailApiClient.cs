using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable] public class RecommendRequest { public string content; }

[Serializable] public class RecommendResponse {
    public int planet_index;
    public string planet_key;
    public string display_name;
    public string emotion;
    public float confidence;
    public string reason;
    public Essence essence;

    [Serializable] public class Essence {
        public string internal_;   // maps backend field "internal"
        public string element;
        public string[] tags;
        public float raw_value;
    }
}

[Serializable] public class PlanetStatus {
    public string state_tag;
    public string dominant_emotion;
    public string[] emotion_history;
    public string[] color_palette;
    public string visual_theme;
    public string last_updated;
}

[DisallowMultipleComponent]
[AddComponentMenu("Cloudtail/Cloudtail Api Client")]
public class CloudtailApiClient : MonoBehaviour
{
    [Header("Backend")]
    [Tooltip("Backend base URL (local debug: http://127.0.0.1:8020)")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8020";

    [Header("UI References")]
    [Tooltip("Input field for memory text")]
    public TMP_InputField input;
    [Tooltip("Log output text")]
    public TextMeshProUGUI logText;

    [Header("Optional Planet Switch")]
    [Tooltip("Planet object for planet_key=ambered")]
    public GameObject ambered;
    [Tooltip("Planet object for planet_key=rippled")]
    public GameObject rippled;
    [Tooltip("Planet object for planet_key=spiral")]
    public GameObject spiral;
    [Tooltip("Planet object for planet_key=woven")]
    public GameObject woven;

    /// <summary>
    /// Entry point called by UI Button.
    /// </summary>
    public void Send()
    {
        var text = input != null ? input.text : string.Empty;
        StopAllCoroutines();
        StartCoroutine(RunFlow(text));
    }

    private IEnumerator RunFlow(string text)
    {
        SetLog("Posting memory...");
        yield return PostMemory(text,
            ok => SetLog("Memory OK"),
            err => SetLog("Memory ERR: " + err));

        SetLog("Recommending...");
        RecommendResponse rec = null;
        yield return Recommend(text,
            ok => {
                rec = ok;
                SetLog($"Recommend: {ok.display_name} ({ok.reason})");
                if (!string.IsNullOrEmpty(ok.planet_key)) SetPlanetActive(ok.planet_key);
            },
            err => SetLog("Recommend ERR: " + err));

        SetLog("Planet status...");
        yield return GetPlanetStatus(
            ok => {
                SetLog($"Status: {ok.dominant_emotion} / theme {ok.visual_theme}");
                if (ok.color_palette != null && ok.color_palette.Length > 0)
                {
                    if (ColorUtility.TryParseHtmlString(ok.color_palette[0], out var c))
                    {
                        var cam = Camera.main;
                        if (cam != null) cam.backgroundColor = c;
                    }
                }
            },
            err => SetLog("Status ERR: " + err));
    }

    // ---------------- API Calls ----------------

    public IEnumerator Recommend(string content, Action<RecommendResponse> onOk, Action<string> onErr)
    {
        var body = new RecommendRequest { content = content ?? string.Empty };
        var json = JsonUtility.ToJson(body);

        using var www = new UnityWebRequest($"{baseUrl}/api/recommend", "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onErr?.Invoke($"{www.error} | {www.downloadHandler.text}");
            yield break;
        }

        // Patch reserved field name "internal" to "internal_"
        var raw = www.downloadHandler.text;
        var patched = raw.Replace("\"internal\":", "\"internal_\":");

        RecommendResponse resp = null;
        try
        {
            resp = JsonUtility.FromJson<RecommendResponse>(patched);
        }
        catch (Exception ex)
        {
            onErr?.Invoke("JSON Parse Error (recommend): " + ex.Message + " | raw=" + raw);
            yield break;
        }

        onOk?.Invoke(resp);
    }

    public IEnumerator GetPlanetStatus(Action<PlanetStatus> onOk, Action<string> onErr)
    {
        using var www = UnityWebRequest.Get($"{baseUrl}/planet/status");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onErr?.Invoke($"{www.error} | {www.downloadHandler.text}");
            yield break;
        }

        PlanetStatus resp = null;
        try
        {
            resp = JsonUtility.FromJson<PlanetStatus>(www.downloadHandler.text);
        }
        catch (Exception ex)
        {
            onErr?.Invoke("JSON Parse Error (status): " + ex.Message);
            yield break;
        }

        onOk?.Invoke(resp);
    }

    public IEnumerator PostMemory(string content, Action<bool> onOk, Action<string> onErr)
    {
        var json = JsonUtility.ToJson(new RecommendRequest { content = content ?? string.Empty });

        using var www = new UnityWebRequest($"{baseUrl}/api/memories/", "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onErr?.Invoke($"{www.error} | {www.downloadHandler.text}");
            yield break;
        }

        onOk?.Invoke(true);
    }

    // ---------------- Helpers ----------------

    private void SetPlanetActive(string key)
    {
        if (ambered != null) ambered.SetActive(key == "ambered");
        if (rippled != null) rippled.SetActive(key == "rippled");
        if (spiral != null) spiral.SetActive(key == "spiral");
        if (woven != null)  woven.SetActive(key == "woven");
    }

    private void SetLog(string msg)
    {
        if (logText != null) logText.SetText(msg);
        else Debug.Log(msg);
    }
}
