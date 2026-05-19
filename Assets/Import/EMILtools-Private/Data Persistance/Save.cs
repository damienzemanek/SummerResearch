using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DesignPatterns.CreationalPatterns;
using EMILtools.Extensions;
using Extensions;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using static Save;

[DefaultExecutionOrder(-500)]
public class Save : DesignPatterns.CreationalPatterns.Singleton<Save>
{
    public SaveData currentData;


    [Serializable]
    public class SaveData
    {
        


        public SaveData()
        {

        }
    }


    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        LoadGame();
    }

    public void ResetData()
    {
        string path = Path.Combine(Application.persistentDataPath, "savefile.json");
        if (File.Exists(path)) File.Delete(path);

        currentData = null;
        SaveGame();
    }

    public void SaveGame()
    {
        if (currentData == null) currentData = new SaveData();
        string json = JsonUtility.ToJson(currentData);
        string path = Path.Combine(Application.persistentDataPath, "savefile.json");
        File.WriteAllText(path, json);

        this.Log($"Saved data at [  {path} ]  ");
    }

    public void LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, "savefile.json");

        if (!File.Exists(path))
        {
            SaveData newData = new SaveData();
            currentData = newData;
            SaveGame();
            return;
        }

        var json = File.ReadAllText(path);
        var readData = JsonUtility.FromJson<SaveData>(json);
        currentData = readData;

        this.Log($"Loaded data at [  {path} ]  ");
    }

    public void OnApplicationQuit()
    {
        SaveGame();
    }
}
