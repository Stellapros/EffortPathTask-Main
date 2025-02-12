using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemDebugger : MonoBehaviour
{
    void Start()
    {
        // Find ALL EventSystems, including inactive and DontDestroyOnLoad ones
        EventSystem[] systems = Resources.FindObjectsOfTypeAll<EventSystem>();
        
        // Print detailed info about each EventSystem found
        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem es = systems[i];
            Debug.Log($"EventSystem {i + 1}:");
            Debug.Log($"- Name: {es.gameObject.name}");
            Debug.Log($"- Path: {GetGameObjectPath(es.gameObject)}");
            Debug.Log($"- Active: {es.gameObject.activeInHierarchy}");
            Debug.Log($"- Scene: {es.gameObject.scene.name}");
            Debug.Log("-------------------");
        }
    }

    // Helper method to get full path of GameObject in hierarchy
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}