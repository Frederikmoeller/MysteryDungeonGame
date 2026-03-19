using UnityEngine;

public class UiManager : MonoBehaviour
{
    
    [SerializeField] private GameObject _titleScreenParent;
    [SerializeField] private GameObject _dungeonUiParent;
    [SerializeField] private GameObject _cityUiParent;
    [SerializeField] private GameObject _pauseScreenParent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        
        DontDestroyOnLoad(gameObject);
    }
}
