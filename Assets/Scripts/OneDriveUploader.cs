using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class OneDriveUploader : MonoBehaviour
{
    private const string CLIENT_ID = "m.li.14@bham.ac.uk";
    private const string REDIRECT_URI = "https://play.unity.com/en/games/52050d3f-c1cf-4f90-b79a-84ca9d751616/the-motivation-expedition";
    private const string ONEDRIVE_SCOPE = "files.readwrite";
    private const string AUTH_ENDPOINT = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const string TOKEN_ENDPOINT = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

    private string accessToken;
    private bool isAuthenticated = false;

public IEnumerator UploadFileToOneDrive(string csvContent, string fileName)
{
    if (string.IsNullOrEmpty(csvContent))
    {
        Debug.LogError("CSV content is empty or null!");
        yield break;
    }

    // Ensure authentication is complete
    if (!isAuthenticated)
    {
        Debug.Log("Starting OAuth flow...");
        yield return StartOAuthFlow();
    }

    // Upload the file
    string uploadUrl = "https://graph.microsoft.com/v1.0/me/drive/root:/GameLogs/" + fileName + ":/content";
// string uploadUrl = "https://effortpatch-0b3abd136749.herokuapp.com/upload";

    UnityWebRequest request = new UnityWebRequest(uploadUrl, "PUT");
    byte[] bodyRaw = Encoding.UTF8.GetBytes(csvContent);
    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Authorization", "Bearer " + accessToken);
    request.SetRequestHeader("Content-Type", "text/csv");

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        Debug.Log("File uploaded successfully to OneDrive");
    }
    else
    {
        Debug.LogError($"Error uploading to OneDrive: {request.error}");
    }
}

    private IEnumerator StartOAuthFlow()
    {
        string authUrl = $"{AUTH_ENDPOINT}?client_id={CLIENT_ID}&response_type=token&redirect_uri={REDIRECT_URI}&scope={ONEDRIVE_SCOPE}";
        Application.OpenURL(authUrl);

        // Wait for the access token to be set via JavaScript bridge
        yield return new WaitUntil(() => !string.IsNullOrEmpty(accessToken));
        isAuthenticated = true;
        Debug.Log("OAuth flow completed successfully");
    }

    // Called from JavaScript when auth token is received
    public void SetAccessToken(string token)
    {
        accessToken = token;
        Debug.Log("Access token set successfully");
    }
}