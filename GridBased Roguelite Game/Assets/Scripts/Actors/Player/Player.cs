using UnityEngine;

[RequireComponent(typeof(Unit))]
public class Player : MonoBehaviour
{
    [SerializeField] private Unit _baseUnit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        _baseUnit = GetComponent<Unit>();
    }
}
