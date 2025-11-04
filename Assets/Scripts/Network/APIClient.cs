using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

/// <summary>
/// کلاینت API برای ارتباط با سرور
/// ✅ FIXED: Proper Content-Type header and JSON handling
/// ✅ FIXED: 429 Rate Limit Handler
/// </summary>
public class APIClient : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float requestTimeout = 30f;
    [SerializeField] private int maxRetries = 3;
    
    private string serverURL;
    
    public void SetServerURL(string url)
    {
        serverURL = url;
        Debug.Log($"🌐 Server URL set to: {serverURL}");
        TestConnection();
    }
    
    private void TestConnection()
    {
        Debug.Log($"🔍 Testing connection to: {serverURL}");
        StartCoroutine(TestConnectionCoroutine());
    }
    
    private IEnumerator TestConnectionCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        string testUrl = serverURL + "/api/health";
        Debug.Log($"🧪 Test URL: {testUrl}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(testUrl))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Connection test successful!");
                Debug.Log($"📥 Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ Connection test failed!");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
            }
        }
    }
    
    // ===== Request Methods =====
    
    public void Get(string endpoint, Action<bool, string> callback, string token = null)
    {
        StartCoroutine(SendRequestCoroutine(UnityWebRequest.Get(serverURL + endpoint), callback, token));
    }
    
    public void Post(string endpoint, object data, Action<bool, string> callback, string token = null)
    {
        // ✅ Convert object to JSON once
        string jsonPayload = data != null ? JsonUtility.ToJson(data) : "{}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        
        // ✅ Debug logging
        Debug.Log($"📤 POST to: {serverURL}{endpoint}");
        Debug.Log($"📦 JSON: {jsonPayload}");
        
        UnityWebRequest request = new UnityWebRequest(serverURL + endpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        // ✅ CRITICAL: Set Content-Type header HERE
        request.SetRequestHeader("Content-Type", "application/json");
        
        StartCoroutine(SendRequestCoroutine(request, callback, token));
    }
    
    public void Put(string endpoint, string json, Action<bool, string> callback, string token = null)
    {
        StartCoroutine(SendRequestCoroutine(
            UnityWebRequest.Put(serverURL + endpoint, json), 
            callback, 
            token)
        );
    }
    
    public void Delete(string endpoint, Action<bool, string> callback, string token = null)
    {
        StartCoroutine(SendRequestCoroutine(
            UnityWebRequest.Delete(serverURL + endpoint), 
            callback, 
            token)
        );
    }
    
    // ===== Core Request Handler =====
    
    private IEnumerator SendRequestCoroutine(UnityWebRequest request, Action<bool, string> callback, string token)
    {
        // Add authorization if token provided
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
        }
        
        request.timeout = (int)requestTimeout;
        
        // Send request
        yield return request.SendWebRequest();

        // ⭐ HANDLE 429 RATE LIMIT
        if (request.responseCode == 429)
        {
            Debug.LogWarning("⚠️ Rate limited (429)! Request rejected.");
            callback?.Invoke(false, "Rate limited. Please wait before trying again.");
            yield break;
        }

        // Process response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"📥 Response ({request.responseCode}): {request.downloadHandler.text}");
            callback?.Invoke(true, request.downloadHandler.text);
        }
        else if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorText = request.downloadHandler?.text ?? "Unknown error";
            Debug.Log($"📥 Response ({request.responseCode}): {errorText}");
            callback?.Invoke(false, errorText);
        }
        else
        {
            Debug.LogError($"❌ Connection Error: {request.error}");
            callback?.Invoke(false, request.error);
        }
    }
    
    // ===== File Upload =====
    
    public void UploadFile(string endpoint, byte[] fileData, string fileName, Action<bool, string> callback, string token = null)
    {
        StartCoroutine(UploadFileCoroutine(endpoint, fileData, fileName, callback, token));
    }
    
    private IEnumerator UploadFileCoroutine(string endpoint, byte[] fileData, string fileName, Action<bool, string> callback, string token = null)
    {
        string url = serverURL + endpoint;
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName);
        
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }
            
            request.timeout = (int)(requestTimeout * 2);
            
            Debug.Log($"📤 Uploading file: {fileName}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"✅ File uploaded successfully");
                callback?.Invoke(true, response);
            }
            else
            {
                Debug.LogError($"❌ File upload failed: {request.error}");
                callback?.Invoke(false, request.error);
            }
        }
    }
    
    // ===== Download Texture =====
    
    public void DownloadTexture(string url, Action<bool, Texture2D> callback)
    {
        StartCoroutine(DownloadTextureCoroutine(url, callback));
    }
    
    private IEnumerator DownloadTextureCoroutine(string url, Action<bool, Texture2D> callback)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = (int)requestTimeout;
            
            Debug.Log($"📥 Downloading texture: {url}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                Debug.Log($"✅ Texture downloaded");
                callback?.Invoke(true, texture);
            }
            else
            {
                Debug.LogError($"❌ Texture download failed: {request.error}");
                callback?.Invoke(false, null);
            }
        }
    }
    
    // ===== Server Status =====
    
    public void CheckServerStatus(Action<bool> callback)
    {
        Get("/api/health", (success, response) =>
        {
            callback?.Invoke(success);
        });
    }
    
    public void GetServerInfo(Action<ServerInfo> callback)
    {
        Get("/api/info", (success, response) =>
        {
            if (success)
            {
                try
                {
                    ServerInfo info = JsonUtility.FromJson<ServerInfo>(response);
                    callback?.Invoke(info);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Failed to parse server info: {ex.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                callback?.Invoke(null);
            }
        });
    }
}

