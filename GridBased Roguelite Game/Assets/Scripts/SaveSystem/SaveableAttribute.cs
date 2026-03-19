using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SaveableAttribute : Attribute
{
    public string Key { get; set; }

    public SaveableAttribute(string key = null)
    {
        Key = key;
    }
}
