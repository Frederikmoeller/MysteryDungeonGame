using System.Collections.Generic;
using UnityEngine;

public static class GridPathFinder
{
    // <summary>
    // Finds a path from start to target using BFS, considering walkable tiles.
    // Target tile itself is considered unwalkable (so path ends adjacent).
    // Returns a list of positions from start (excluding start) to the closest tile adjacent to target.
    // Returns null if no path exists.
    // </summary>

    public static List<Vector2Int> FindPath(GridManager grid, Vector2Int start, Vector2Int target,
        int maxIterations = 500)
    {
        if (grid == null) return null;
        if (!grid.Data.IsInBounds(start.x, start.y) || !grid.Data.IsInBounds(target.x, target.y)) return null;
        
        // BFS setup
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start;

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, new Vector2Int(1, 1),
            new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        int iterations = 0;
        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector2Int current = queue.Dequeue();
            
            // If we reached a tile adjacent to target, reconstruct path
            if (Vector2Int.Distance(current, target) <= 1.1f)
            {
                return ReconstructPath(cameFrom, start, current);
            }

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                
                // Check bounds
                if (!grid.Data.IsInBounds(neighbor.x, neighbor.y))
                    continue;
                
                if (visited.Contains(neighbor))
                    continue;
                
                // Check walkability – but allow neighbor if it's the target? 
                // We treat target as unwalkable (occupied by player), so we won't step on it.
                // Also consider that other enemies block (IsWalkable checks occupant == null)
                if (!grid.IsWalkable(neighbor.x, neighbor.y))
                    continue;

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        return null;
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start,
        Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }
}
