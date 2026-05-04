using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    public string sceneName;
}

[System.Serializable]
public class PermanentData
{
    public int totalRunsCompleted = 0;
    public bool char_vanta_unlocked = false;
    public bool char_lena_unlocked = false;
}

public class SaveLoadManager : MonoBehaviour
{
    string path => Application.persistentDataPath + "/save.json";

    public PermanentData permanentData = new PermanentData();

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

    public void SavePermanent()
    {
        string json = JsonUtility.ToJson(permanentData);
        PlayerPrefs.SetString("PermanentData", json);
        PlayerPrefs.Save();
    }

    public void LoadPermanent()
    {
        if (PlayerPrefs.HasKey("PermanentData"))
        {
            string json = PlayerPrefs.GetString("PermanentData");
            permanentData = JsonUtility.FromJson<PermanentData>(json);
        }
    }
}