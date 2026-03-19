using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectPool", menuName = "Effects/Effect Pool")]
public class EffectPool : ScriptableObject
{
    public List<EffectData> PossibleEffects;

    public EffectData GetRandomEffect()
    {
        if (PossibleEffects == null || PossibleEffects.Count == 0) return null;

        int index = Random.Range(0, PossibleEffects.Count);
        return PossibleEffects[index];
    }
}
