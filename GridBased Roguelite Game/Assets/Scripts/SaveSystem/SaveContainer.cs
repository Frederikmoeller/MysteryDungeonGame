using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveContainer
{
    public string saveVersion = "1.0";
    public string saveTime;
    public int saveSlot;

    public Dictionary<string, object> GameData = new();

    public Dictionary<string, Dictionary<string, object>> unitsData = new();

    public Dictionary<string, object> achievements = new();
    public Dictionary<string, object> gameSettings = new();
    public Dictionary<string, object> worldState = new();

    public T GetData<T>(string key, T defaultValue = default)
    {
        if (GameData.ContainsKey(key) && GameData[key] is T value)
        {
            return value;
        }

        return defaultValue;
    }

    public void SetData(string key, object value)
    {
        GameData[key] = value;
    }
}
