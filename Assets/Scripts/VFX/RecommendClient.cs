using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// DTO for /api/recommend response.
/// Missing fields are tolerated by default initializers.
/// </summary>
[System.Serializable]
public class RecommendResp
{
    public int planet_index = 3;   // default fallback range: 0..3
    public string emotion = "";
    public float confidence = 0f;
    public string reason = "";
}

/// <summary>
/// Minimal client for Cloudtail backend /api/recommend.
/// - Prevents empty submissions.
/// - 8s timeout and single quick retry with small backoff.
/// - Safe JSON parsing (JsonUtility) with defaults on failure.
/// - Falls back to a safe planet index to avoid dead-ends.
/// - Optional tip popup integration via SendMessage("Show", string).
/// </summary>
public class RecommendClient : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("Backend base URL, no trailing slash. Example: http://127.0.0.1:8001")]
    public string baseUrl = "http://127.0.0.1:8001";

    [Header("Behavior")]
    [Tooltip("Seconds before request timeout.")]
    public int timeoutSeconds = 8;

    [Tooltip("Total retry attempts (including first try). 1 = no retry, 2 = one retry.")]
    [Range(1, 3)]
    public int totalAttempts = 2;

    [Tooltip("Backoff seconds before retry.")]
    public float retryBackoffSeconds = 0.4f;

    [Tooltip("Fallback planet index used on failure.")]
    [Min(0)]
    public int fallbackPlanetIndex = 3;

    [Header("UX (Optional)")]
    [Tooltip("Name of a GameObject that receives SendMessage(\"Show\", string) for tips.")]
    public string tipReceiverObjectName = "";

    /// <summary>
    /// Public entry for UI Button binding.
    /// TMP_InputField.text should be passed as the argument.
    /// </summary>
    /// <param name="content">User-entered text content.</param>
    public void Send(string content)
    {
        // Guard: reject whitespace-only input
        if (string.IsNullOrWhiteSpace(content))
        {
            Tip("Enter text first.");
            return;
        }

        // Ensure baseUrl is valid-ish
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Tip("Backend URL not set.");
            Fallback();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(PostRecommendWithRetry(content));
    }

    IEnumerator PostRecommendWithRetry(string content)
    {
        // Trim potential trailing slash for consistent concatenation
        var url = baseUrl.TrimEnd('/') + "/api/recommend";

        // Prepare JSON body (escape double-quotes)
        var safeContent = (content ?? string.Empty).Replace("\"", "\\\"");
        var raw = "{\"content\":\"" + safeContent + "\"}";
        var body = Encoding.UTF8.GetBytes(raw);

        // Attempt loop
        int attempts = Mathf.Max(1, totalAttempts);
        int round = 0;
        bool success = false;

        while (round < attempts && !success)
        {
            round++;

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = Mathf.Max(1, timeoutSeconds);

                yield return req.SendWebRequest();

                bool httpOk =
                    req.result == UnityWebRequest.Result.Success &&
                    req.responseCode >= 200 && req.responseCode < 300;

                if (httpOk)
                {
                    var text = req.downloadHandler != null ? req.downloadHandler.text : "{}";
                    var resp = SafeParse(text);

                    // Clamp index to expected range [0..3]
                    int idx = Mathf.Clamp(resp.planet_index, 0, 3);

                    // Invoke PlanetSwitcher if available
                    var ps = FindObjectOfType<PlanetSwitcher>();
                    if (ps != null)
                    {
                        ps.OpenWithSuggestionIndex(idx);
                    }

                    Debug.Log($"[recommend] idx={idx} emotion={resp.emotion} conf={resp.confidence} reason={resp.reason}");
                    success = true;
                }
                else
                {
                    Debug.LogWarning($"[recommend] failed attempt {round}/{attempts} " +
                                     $"status={req.responseCode} result={req.result} err={req.error}");

                    if (round < attempts)
                    {
                        yield return new WaitForSeconds(Mathf.Max(0f, retryBackoffSeconds));
                    }
                }
            }
        }

        if (!success)
        {
            // Final fallback path
            Fallback();
            Tip("Network issue. Used a safe suggestion.");
        }
    }

    /// <summary>
    /// Safe JSON parsing with default object on failure.
    /// </summary>
    private static RecommendResp SafeParse(string json)
    {
        try
        {
            var obj = JsonUtility.FromJson<RecommendResp>(string.IsNullOrEmpty(json) ? "{}" : json);
            return obj ?? new RecommendResp();
        }
        catch
        {
            return new RecommendResp();
        }
    }

    /// <summary>
    /// Fallback switch to prevent UX dead-ends.
    /// </summary>
    private void Fallback()
    {
        int idx = Mathf.Clamp(fallbackPlanetIndex, 0, 3);
        var ps = FindObjectOfType<PlanetSwitcher>();
        if (ps != null)
        {
            ps.OpenWithSuggestionIndex(idx);
        }
        Debug.Log($"[recommend] fallback -> idx={idx}");
    }

    /// <summary>
    /// Optional lightweight tip popup dispatch.
    /// Expects a component that implements public void Show(string).
    /// </summary>
    private void Tip(string msg)
    {
        if (string.IsNullOrEmpty(tipReceiverObjectName)) return;
        var go = GameObject.Find(tipReceiverObjectName);
        if (go == null) return;
        go.SendMessage("Show", msg, SendMessageOptions.DontRequireReceiver);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Normalize baseUrl to avoid accidental trailing slash duplication
        if (!string.IsNullOrEmpty(baseUrl))
        {
            baseUrl = baseUrl.Trim();
        }
        timeoutSeconds = Mathf.Max(1, timeoutSeconds);
        totalAttempts = Mathf.Clamp(totalAttempts, 1, 3);
        retryBackoffSeconds = Mathf.Max(0f, retryBackoffSeconds);
    }
#endif
}
