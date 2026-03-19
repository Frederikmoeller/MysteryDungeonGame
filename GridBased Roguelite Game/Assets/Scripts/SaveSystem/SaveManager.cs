using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private string _filePath;
    [SerializeField] private string _saveFileName = "savegame";
    private string _savePath;
    
    // Cache for saveable fields to improve performance
    private Dictionary<Type, List<FieldInfo>> _saveableFields = new();
    private Dictionary<Type, List<PropertyInfo>> _saveableProperties = new();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        _savePath = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(_savePath))
        {
            Directory.CreateDirectory(_savePath);
        }

        CacheSaveableMembers();
    }

    void CacheSaveableMembers()
    {
        // This will find all types that might have saveable members
        // You can expand this as needed
        var types = new[]
        {
            typeof(HealthSystem),
            typeof(ManaSystem),
            typeof(ExperienceSystem),
            typeof(Unit),
            typeof(CombatController)
        };

        foreach (var type in types)
        {
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<SaveableAttribute>() != null).ToList();
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<SaveableAttribute>() != null && p.CanRead)
                .ToList();

            if (fields.Any())
            {
                _saveableFields[type] = fields;
            }

            if (properties.Any())
            {
                _saveableProperties[type] = properties;
            }
        }
    }

    public Dictionary<string, object> CaptureSaveableData(UnityEngine.Object obj)
    {
        var data = new Dictionary<string, object>();
        var type = obj.GetType();
        
        // Save fields marked with SaveableAttribute
        if (_saveableFields.TryGetValue(type, out var fields))
        {
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<SaveableAttribute>();
                string key = attr.Key ?? field.Name;
                data[key] = field.GetValue(obj);
            }
        }

        if (_saveableProperties.TryGetValue(type, out var properties))
        {
            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<SaveableAttribute>();
                string key = attr.Key ?? prop.Name;
                data[key] = prop.GetValue(obj);
            }
        }

        return data;
    }

    public void ApplySaveableData(UnityEngine.Object obj, Dictionary<string, object> data)
    {
        var type = obj.GetType();
        
        // Apply fields
        if (_saveableFields.TryGetValue(type, out var fields))
        {
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<SaveableAttribute>();
                string key = attr.Key ?? field.Name;

                if (data.TryGetValue(key, out object value))
                {
                    try
                    {
                        // Handle type conversion
                        if (value != null)
                        {
                            object convertedValue = Convert.ChangeType(value, field.FieldType);
                            field.SetValue(obj, convertedValue);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to set field {field.Name}: {e.Message}");
                    }
                }
            }
        }
        
        // Apply properties (only if they have setters)
        if (_saveableProperties.TryGetValue(type, out var properties))
        {
            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;
                
                var attr = prop.GetCustomAttribute<SaveableAttribute>();
                string key = attr.Key ?? prop.Name;
                
                if (data.TryGetValue(key, out object value))
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(value, prop.PropertyType);
                        prop.SetValue(obj, convertedValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to set property {prop.Name}: {e.Message}");
                    }
                }
            }
        }
    }

    public void SaveGame(int saveSlot = 0, object additionalData = null)
    {
        Debug.Log("=== SAVING GAME ===");
        
        var saveContainer = new SaveContainer
        {
            saveTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
            saveSlot = saveSlot
        };
        
        // Save global game data
        saveContainer.GameData["gameVersion"] = Application.version;
        saveContainer.GameData["playTime"] = Time.time;
        
        // Save all units
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (var unit in allUnits)
        {
            var unitData = new Dictionary<string, object>();
            
            // Save unit's own data
            var unitOwnData = CaptureSaveableData(unit);
            foreach (var kvp in unitOwnData)
                unitData["unit_" + kvp.Key] = kvp.Value;
            
            // Save health system data
            if (unit.Health != null)
            {
                var healthData = CaptureSaveableData(unit.Health);
                foreach (var kvp in healthData)
                    unitData["health_" + kvp.Key] = kvp.Value;
            }
            
            // Save mana system data
            if (unit.Mana != null)
            {
                var manaData = CaptureSaveableData(unit.Mana);
                foreach (var kvp in manaData)
                    unitData["mana_" + kvp.Key] = kvp.Value;
            }
            
            // Save experience system data
            if (unit.Experience != null)
            {
                var expData = CaptureSaveableData(unit.Experience);
                foreach (var kvp in expData)
                    unitData["exp_" + kvp.Key] = kvp.Value;
            }
            
            // Generate a unique ID for the unit (use name + type + position hash)
            string unitId = $"{unit.UnitName}_{unit.Type}_{unit.transform.position.GetHashCode()}";
            saveContainer.unitsData[unitId] = unitData;
        }
        
        // Add any additional custom data
        if (additionalData != null)
        {
            saveContainer.GameData["custom"] = additionalData;
        }
        
        // Serialize and save
        string json = JsonUtility.ToJson(saveContainer, true);
        string filePath = Path.Combine(_savePath, $"{_saveFileName}_{saveSlot}.json");
        File.WriteAllText(filePath, json);
        
        Debug.Log($"Game saved to: {filePath}");
    }

    // Load game state
    public SaveContainer LoadGame(int saveSlot = 0)
    {
        string filePath = Path.Combine(_savePath, $"{_saveFileName}_{saveSlot}.json");
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Save file not found: {filePath}");
            return null;
        }
        
        string json = File.ReadAllText(filePath);
        var saveContainer = JsonUtility.FromJson<SaveContainer>(json);
        
        Debug.Log($"Game loaded from: {filePath}");
        return saveContainer;
    }

    // Apply loaded data to current game state
    public void ApplyLoadedData(SaveContainer saveData)
    {
        // Apply global data
        if (saveData.GameData.ContainsKey("playTime"))
        {
            // Restore play time or other global stats
        }

        // Apply settings
        if (saveData.gameSettings.ContainsKey("volume"))
        {
            AudioListener.volume = Convert.ToSingle(saveData.gameSettings["volume"]);
        }
        if (saveData.gameSettings.ContainsKey("quality"))
        {
            QualitySettings.SetQualityLevel(Convert.ToInt32(saveData.gameSettings["quality"]));
        }
        
        // Apply unit data
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        // This is simplified - in reality you'd need to match units by ID
        foreach (var unit in allUnits)
        {
            // Find matching unit data (you'd need a better matching system)
            var unitData = saveData.unitsData.Values.FirstOrDefault();
            if (unitData == null) continue;
            
            // Apply data to each system
            ApplySaveableData(unit.Health, FilterDataByPrefix(unitData, "health_"));
            ApplySaveableData(unit.Mana, FilterDataByPrefix(unitData, "mana_"));
            ApplySaveableData(unit.Experience, FilterDataByPrefix(unitData, "exp_"));
            
            // Apply unit's own data
            ApplySaveableData(unit, FilterDataByPrefix(unitData, "unit_"));
        }
    }
    
    private Dictionary<string, object> FilterDataByPrefix(Dictionary<string, object> data, string prefix)
    {
        return data
            .Where(kvp => kvp.Key.StartsWith(prefix))
            .ToDictionary(kvp => kvp.Key.Substring(prefix.Length), kvp => kvp.Value);
    }
}
