using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    public string sceneName;
}

public class SaveLoadManager : MonoBehaviour
{
    string path => Application.persistentDataPath + "/save.json";

    public void SaveGame(string sceneName)
    {
        SaveData data = new SaveData();
        data.sceneName = sceneName;

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);

        Debug.Log("저장 완료");
    }

    public void LoadGame()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            SceneManager.LoadScene(data.sceneName);
        }
        else
        {
            Debug.Log("세이브 없음");
        }
    }
}
