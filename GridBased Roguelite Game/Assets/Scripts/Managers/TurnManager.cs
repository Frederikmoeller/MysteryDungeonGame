// TurnManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnType
{
    Player,
    Enemy
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public event Action<TurnType> OnTurnChanged;
    public event Action OnPlayerTurnStarted;
    public event Action OnEnemyPhaseStarted;
    public event Action OnEnemyPhaseFinished;

    public TurnType CurrentTurn { get; private set; } = TurnType.Player;
    public bool IsProcessing { get; private set; } = false;

    private List<IEnemy> _enemies = new List<IEnemy>();
    private bool _playerIsMoving = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterEnemy(IEnemy enemy)
    {
        if (!_enemies.Contains(enemy))
            _enemies.Add(enemy);
    }

    public void UnregisterEnemy(IEnemy enemy)
    {
        _enemies.Remove(enemy);
    }

    public void ClearEnemies()
    {
        _enemies.Clear();
    }

    // Called when the player starts moving (grid movement)
    public void OnPlayerMovementStarted()
    {
        if (CurrentTurn != TurnType.Player) return;
        if (IsProcessing) return;

        _playerIsMoving = true;
        StartCoroutine(ProcessEnemyPhaseWithPlayerMovement());
    }

    // Called when the player finishes moving
    public void OnPlayerMovementComplete()
    {
        _playerIsMoving = false;
    }

    // Called after a non‑movement player action (attack, item, etc.)
    public void EndPlayerTurnAfterAction()
    {
        if (CurrentTurn != TurnType.Player) return;
        if (IsProcessing) return;

        Debug.Log("Player action ended, starting enemy phase");
        CurrentTurn = TurnType.Enemy;
        OnTurnChanged?.Invoke(CurrentTurn);
        StartCoroutine(ProcessEnemyPhase());
    }

    // Enemy phase for when player movement is already happening in parallel
    private IEnumerator ProcessEnemyPhaseWithPlayerMovement()
    {
        IsProcessing = true;
        OnEnemyPhaseStarted?.Invoke();

        // Remove dead enemies
        _enemies.RemoveAll(e => e == null || !e.IsAlive());

        if (_enemies.Count == 0)
        {
            // No enemies, just wait for player movement to finish
            yield return new WaitUntil(() => !_playerIsMoving);
            EndEnemyPhase();
            yield break;
        }

        // Separate attackers and movers
        List<IEnemy> attackers = new List<IEnemy>();
        List<IEnemy> movers = new List<IEnemy>();

        foreach (var enemy in _enemies)
        {
            if (enemy.WantsToAttack())
                attackers.Add(enemy);
            else
                movers.Add(enemy);
        }

        // Sort attackers by priority (higher first)
        attackers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // Process attacks sequentially
        if (attackers.Count > 0)
        {
            foreach (var attacker in attackers)
            {
                bool attackDone = false;
                attacker.Attack(() => attackDone = true);
                yield return new WaitUntil(() => attackDone);
            }
        }

        // Process movements simultaneously
        int remainingMoves = movers.Count;
        if (remainingMoves > 0)
        {
            foreach (var mover in movers)
            {
                mover.Move(() => { remainingMoves--; });
            }
        }

        // Wait for both enemy movements AND player movement to complete
        yield return new WaitUntil(() => remainingMoves == 0 && !_playerIsMoving);

        EndEnemyPhase();
    }

    // Standard enemy phase for when player is not moving (attacks, items)
    private IEnumerator ProcessEnemyPhase()
    {
        IsProcessing = true;
        OnEnemyPhaseStarted?.Invoke();

        // Remove dead enemies
        _enemies.RemoveAll(e => e == null || !e.IsAlive());

        if (_enemies.Count == 0)
        {
            EndEnemyPhase();
            yield break;
        }

        // Separate attackers and movers
        List<IEnemy> attackers = new List<IEnemy>();
        List<IEnemy> movers = new List<IEnemy>();

        foreach (var enemy in _enemies)
        {
            if (enemy.WantsToAttack())
                attackers.Add(enemy);
            else
                movers.Add(enemy);
        }

        // Sort attackers by priority
        attackers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // Process attacks sequentially
        if (attackers.Count > 0)
        {
            foreach (var attacker in attackers)
            {
                bool attackDone = false;
                attacker.Attack(() => attackDone = true);
                yield return new WaitUntil(() => attackDone);
            }
        }

        // Process movements simultaneously
        int remainingMoves = movers.Count;
        if (remainingMoves > 0)
        {
            foreach (var mover in movers)
            {
                mover.Move(() => { remainingMoves--; });
            }
            yield return new WaitUntil(() => remainingMoves == 0);
        }

        EndEnemyPhase();
    }

    private void EndEnemyPhase()
    {
        IsProcessing = false;
        CurrentTurn = TurnType.Player;
        OnTurnChanged?.Invoke(CurrentTurn);
        OnEnemyPhaseFinished?.Invoke();
        Debug.Log("Enemy phase finished, player turn starts");
    }
}