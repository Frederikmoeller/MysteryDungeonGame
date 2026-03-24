using System;

public interface IEnemy
{
    bool IsAlive();
    bool WantsToAttack();         // Called each enemy phase to decide action
    void Attack(Action onComplete);
    void Move(Action onComplete);
    int Priority { get; }         // Higher priority attacks first (e.g., distance, speed)
}
