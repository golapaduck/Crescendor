using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DataManager
{
    public string jsonDataFromServer;
    public Define.UserReplayRecord userReplayRecord;

    public void Init()
    {
        jsonDataFromServer = "init data";
    }

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject prefab = Load<GameObject>($"Prefabs/{path}");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : {path}");
            return null;
        }

        GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
        int index = go.name.IndexOf("(Clone)");
        if (index > 0)
            go.name = go.name.Substring(0, index);

        return go;
    }

    public void Destroy(GameObject go)
    {
        if (go == null)
            return;

        UnityEngine.Object.Destroy(go);
    }

    public void GetRankListFromServer(string songFileName)
    {
        UnityWebRequest www = UnityWebRequest.Get($"http://15.164.2.49:3000/ranking/{songFileName}");

        www.SendWebRequest();  // 응답이 올때까지 대기한다.
        while (!www.isDone) { }

        if (www.error == null)  // 에러가 나지 않으면 동작.
        {
            Debug.Log("Get Data Success");
            jsonDataFromServer = $"{{\"records\":{www.downloadHandler.text}}}";
        }
        else
        {
            Debug.LogError("Error to Get Data");
        }
    }

    public float GetBestRankFromServer(string userId, string songFileName)
    {
        UnityWebRequest www = UnityWebRequest.Get($"http://15.164.2.49:3000/record/getscore/{userId}/{songFileName}");

        www.SendWebRequest();  // 응답이 올때까지 대기한다.
        while (!www.isDone) { }

        if (www.error == null)  // 에러가 나지 않으면 동작.
        {
            Debug.Log("Get Data Success");
            
            if (www.downloadHandler.text.Length == 2)
            {
                return -1;
            }
            else
            {
                Debug.Log(www.downloadHandler.text.Substring(10, www.downloadHandler.text.Length - 12));
                return float.Parse(www.downloadHandler.text.Substring(10, www.downloadHandler.text.Length - 12));
            }
        }
        else
        {
            Debug.LogError("Error to Get Data");
            return -2;
        }
    }

    public void SetBestRankFromServer(string userId, string songFileName, float score, string replayData)
    {
        UnityWebRequest www = new UnityWebRequest($"http://15.164.2.49:3000/record/setscore/{userId}/{songFileName}", "PUT");
        string jsonData = $"{{\"score\":{score}, \"midi\":\"{replayData}\"}}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        www.SendWebRequest();  // 응답이 올때까지 대기한다.
        while (!www.isDone) { }

        if (www.error == null)  // 에러가 나지 않으면 동작.
        {
            Debug.Log("Set Data Success");
        }
        else
        {
            Debug.LogError("Error to Set Data");
        }
    }

    public void AddBestRankFromServer(string userId, string songFileName, float score, string replayData)
    {
        UnityWebRequest www = new UnityWebRequest($"http://15.164.2.49:3000/record/setscore/{userId}/{songFileName}", "POST");
        string jsonData = $"{{\"score\":{score}, \"midi\":\"{replayData}\"}}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        www.SendWebRequest();  // 응답이 올때까지 대기한다.
        while (!www.isDone) { }

        if (www.error == null)  // 에러가 나지 않으면 동작.
        {
            Debug.Log("Add Data Success");
        }
        else
        {
            Debug.LogError(www.error);
        }
    }
}
