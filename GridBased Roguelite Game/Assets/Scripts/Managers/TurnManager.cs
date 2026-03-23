// TurnManager.cs - Simplified version using your existing GridManager concept
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
    
    public event Action<TurnType> OnTurnChanged;
    public TurnType CurrentTurn { get; private set; } = TurnType.Player;
    
    private List<EnemyAI> _enemies = new List<EnemyAI>();
    private int _enemiesCompletedMoving;
    private bool _isProcessingEnemyTurn;

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
            Debug.Log($"Registered enemy: {enemy.name}. Total: {_enemies.Count}");
        }
    }

    public void UnregisterEnemy(EnemyAI enemy)
    {
        _enemies.Remove(enemy);
        Debug.Log($"Unregistered enemy: {enemy?.name}. Total: {_enemies.Count}");
    }

    public void EndPlayerTurn()
    {
        if (CurrentTurn != TurnType.Player) return;
        
        Debug.Log("Player turn ended, starting enemy turns");
        CurrentTurn = TurnType.Enemy;
        OnTurnChanged?.Invoke(CurrentTurn);
        
        StartEnemyTurns();
    }
    
    private void StartEnemyTurns()
    {
        // Remove any dead enemies
        _enemies.RemoveAll(e => e == null || !e.Unit.IsAlive());
        
        if (_enemies.Count == 0)
        {
            Debug.Log("No enemies, ending enemy turn");
            EndEnemyTurn();
            return;
        }
        
        _isProcessingEnemyTurn = true;
        _enemiesCompletedMoving = 0;
        
        // Tell all enemies to move at the same time
        foreach (var enemy in _enemies)
        {
            if (enemy != null && enemy.isActiveAndEnabled)
            {
                enemy.MoveOnTurn(OnEnemyMoveComplete);
            }
            else
            {
                _enemiesCompletedMoving++;
            }
        }
        
        // Check if all enemies are already done (e.g., all dead or disabled)
        if (_enemiesCompletedMoving >= _enemies.Count)
        {
            _isProcessingEnemyTurn = false;
            EndEnemyTurn();
        }
    }
    
    private void OnEnemyMoveComplete()
    {
        _enemiesCompletedMoving++;
        Debug.Log($"Enemy move complete. Progress: {_enemiesCompletedMoving}/{_enemies.Count}");
        
        if (_enemiesCompletedMoving >= _enemies.Count && _isProcessingEnemyTurn)
        {
            _isProcessingEnemyTurn = false;
            EndEnemyTurn();
        }
    }
    
    private void EndEnemyTurn()
    {
        Debug.Log("Enemy turn ended, starting player turn");
        CurrentTurn = TurnType.Player;
        OnTurnChanged?.Invoke(CurrentTurn);
    }
}