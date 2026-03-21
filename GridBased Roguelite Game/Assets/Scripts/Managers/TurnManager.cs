using System;
using UnityEngine;
using System.Collections.Generic;

public enum TurnType
{
    Player,
    Enemy
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    public Action OnTurnChanged;
    public TurnType CurrentTurn = TurnType.Player;
    
    private List<EnemyAI> _enemies = new List<EnemyAI>();
    private int _currentEnemyIndex;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (!_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyAI enemy)
    {
        _enemies.Remove(enemy);
    }

    public void EndTurn()
    {
        if (CurrentTurn == TurnType.Player)
        {
            // Switch to enemy turn
            CurrentTurn = TurnType.Enemy;
            OnTurnChanged?.Invoke();
            StartEnemyTurns();
        }
        else if (CurrentTurn == TurnType.Enemy)
        {
            // Move to next enemy or back to player
            _currentEnemyIndex++;
            
            if (_currentEnemyIndex >= _enemies.Count)
            {
                // All enemies done, back to player
                CurrentTurn = TurnType.Player;
                OnTurnChanged?.Invoke();
            }
            else
            {
                // Next enemy's turn
                OnTurnChanged?.Invoke();
                _enemies[_currentEnemyIndex].TakeTurn();
            }
        }
    }
    
    private void StartEnemyTurns()
    {
        _currentEnemyIndex = 0;
        
        if (_enemies.Count > 0)
        {
            _enemies[0].TakeTurn();
        }
        else
        {
            // No enemies, go back to player
            CurrentTurn = TurnType.Player;
            OnTurnChanged?.Invoke();
        }
    }
}