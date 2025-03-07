using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OneDriveUploader : MonoBehaviour
{
    // URL of your Google Apps Script web app
    private string uploadUrl = "https://script.google.com/macros/s/AKfycbwJ6d0zelejUI-bIK6t5h0XNhc6iZD5ShBACM6cnDOrUC8PSUT46qw-8Q2vezMqwLBp/exec";

    public IEnumerator UploadFileToOneDrive(string csvContent, string fileName)
    {
        // Try uploading directly first
        bool success = false;

        if (csvContent.Length < 100000) // If content is small enough for direct upload
        {
            yield return StartCoroutine(UploadDirectly(csvContent, fileName, (result) =>
            {
                success = result;
            }));
        }

        // If direct upload failed or content is too large, try chunked upload
        if (!success && csvContent.Length > 10000)
        {
            yield return StartCoroutine(UploadInChunks(csvContent, fileName));
        }
    }

    private IEnumerator UploadDirectly(string csvContent, string fileName, System.Action<bool> callback)
    {
        WWWForm form = new WWWForm();

        // Add base64 encoded data for direct upload
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        string base64Data = System.Convert.ToBase64String(bytes);

        form.AddField("participant_id", LogManager.Instance.participantID);
        form.AddField("json_data", CreateJsonWrapper(csvContent));

        Debug.Log($"Uploading file directly to: {uploadUrl}");

        using (UnityWebRequest www = UnityWebRequest.Post(uploadUrl, form))
        {
            // Set timeout to 60 seconds
            www.timeout = 60;

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Upload successful: " + www.downloadHandler.text);
                callback(true);
            }
            else
            {
                Debug.LogError("Upload error: " + www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
                callback(false);
            }
        }
    }

    private IEnumerator UploadInChunks(string csvContent, string fileName)
    {
        // Split content into chunks of approximately 50KB
        const int chunkSize = 50000;
        int totalChunks = Mathf.CeilToInt((float)csvContent.Length / chunkSize);

        Debug.Log($"Uploading file in {totalChunks} chunks");

        for (int i = 0; i < totalChunks; i++)
        {
            int startIndex = i * chunkSize;
            int length = Mathf.Min(chunkSize, csvContent.Length - startIndex);
            string chunk = csvContent.Substring(startIndex, length);

            WWWForm form = new WWWForm();
            form.AddField("chunk_number", i + 1);
            form.AddField("total_chunks", totalChunks);
            form.AddField("participant_id", LogManager.Instance.participantID);

            // Wrap the chunk in a JSON structure for the server to process
            form.AddField("json_data", CreateJsonWrapper(chunk, i + 1, totalChunks));

            using (UnityWebRequest www = UnityWebRequest.Post(uploadUrl, form))
            {
                www.timeout = 30;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Chunk {i + 1}/{totalChunks} uploaded successfully");
                }
                else
                {
                    Debug.LogError($"Failed to upload chunk {i + 1}: {www.error}");
                    // Wait a bit before retrying
                    yield return new WaitForSeconds(2f);
                    i--; // Retry this chunk
                }
            }

            // Wait briefly between chunks to not overload the server
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("Chunked upload complete");
    }

    private string CreateJsonWrapper(string csvContent, int? chunkNumber = null, int? totalChunks = null)
    {
        // Create a JSON wrapper for the CSV content
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{");

        // Add participant info
        jsonBuilder.Append($"\"participantID\":\"{LogManager.Instance.participantID}\",");

        // Add chunk info if provided
        if (chunkNumber.HasValue && totalChunks.HasValue)
        {
            jsonBuilder.Append($"\"chunkNumber\":{chunkNumber.Value},");
            jsonBuilder.Append($"\"totalChunks\":{totalChunks.Value},");
        }

        // Convert CSV content to a JSON array of data points
        jsonBuilder.Append("\"csvData\":\"");

        // Escape special characters in CSV
        string escapedCsv = csvContent.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        jsonBuilder.Append(escapedCsv);

        jsonBuilder.Append("\"");
        jsonBuilder.Append("}");

        return jsonBuilder.ToString();
    }
}