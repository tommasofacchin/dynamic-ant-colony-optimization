using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System;

public class Ant : MonoBehaviour
{

    private enum AntState { Foraging, Returning }
    [SerializeField] private AntState state = AntState.Foraging;
    [SerializeField] private Nest nest;
    [SerializeField] private float radarRadious;

    private List<Vector3Int> tabuList = new List<Vector3Int>();
    private Vector3Int currentCell;
    private Vector3Int lastDepositedCell;
    private Vector3Int lastDirection = Vector3Int.zero;
    private Vector3 targetPos;
    private GridManager gridManager;
    private Tilemap tilemap;
    private bool isMoving = false;

    private List<Vector3Int> path = new List<Vector3Int>();
    private int returnSteps = 0;


    public void Init(GridManager gridManager)
    {
        
        this.gridManager = gridManager;
        this.tilemap = gridManager.tilemap;
        this.nest = gridManager.nestObject.GetComponent<Nest>();

        currentCell = tilemap.WorldToCell(transform.position);

        tabuList.Clear();
        tabuList.Add(currentCell);
    }


    void Update()
    {

        if (gridManager == null || tilemap == null)
            return;

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, gridManager.moveSpeed * Time.deltaTime);
            currentCell = tilemap.WorldToCell(transform.position);


            foreach (var food in gridManager.foodList)
            {
                if (food == null || food.foodAmount <= 0) continue;
                Vector3Int foodCell = gridManager.tilemap.WorldToCell(food.transform.position);

                if ((foodCell - currentCell).sqrMagnitude <= radarRadious * radarRadious)
                {
                    targetPos = tilemap.CellToWorld(foodCell) + new Vector3(0.5f, 0.5f, 0);
                    lastDirection = foodCell - currentCell;

                    if (foodCell == currentCell)
                    {
                        PickUpFood(food);
                        isMoving = false;
                        targetPos = transform.position;
                    }

                    break;
                }
            }

            if ((transform.position - targetPos).sqrMagnitude <= 0.05f * 0.05f)
            {
                isMoving = false;
                transform.position = targetPos;
                currentCell = tilemap.WorldToCell(transform.position);
                OnArrivedAtCell();
            }
            return;
        }


        Vector3Int nextCell;
        if (state == AntState.Foraging)
        {
            nextCell = ChooseNextCell();
            if (nextCell == currentCell) return;
        }
        else nextCell = AcoReturnToNest();



        lastDirection = nextCell - currentCell;
        currentCell = nextCell;



        targetPos = tilemap.CellToWorld(currentCell) + new Vector3(0.5f, 0.5f, 0);
        isMoving = true;


        if (!path.Contains(currentCell))
            path.Add(currentCell);

        if (!tabuList.Contains(currentCell))
            tabuList.Add(currentCell);

        if (tabuList.Count > gridManager.tabuLength)
            tabuList.RemoveAt(0);

    }

    List<Vector3Int> GetCellsBetween(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> cells = new List<Vector3Int>();
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;
        int err = dx - dy;

        int x = start.x;
        int y = start.y;

        while (true)
        {
            cells.Add(new Vector3Int(x, y, 0));
            if (x == end.x && y == end.y) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }

        return cells;

    }


    Vector3Int ChooseNextCell(int forwardRange = 10, int sideRange = 5)
    {
        if (lastDirection == Vector3Int.zero)
        {
            var neighbors = GetWalkableNeighbors8(currentCell);
            if (neighbors.Count == 0) return currentCell;
            Vector3Int chosen = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
            lastDirection = chosen - currentCell;
            return chosen;
        }

        Vector3Int forward = new Vector3Int(
            lastDirection.x != 0 ? lastDirection.x / Mathf.Abs(lastDirection.x) : 0,
            lastDirection.y != 0 ? lastDirection.y / Mathf.Abs(lastDirection.y) : 0,
            0
        );

        Vector3Int rightOffset = new Vector3Int(forward.y, -forward.x, 0);
        Vector3Int leftOffset = new Vector3Int(-forward.y, forward.x, 0);

        List<Vector3Int> candidates = new List<Vector3Int>();
        List<Vector3Int> foodCandidates = new List<Vector3Int>();



        for (int f = 1; f <= forwardRange; f++)
        {
            bool blockedForward = false;
            for (int w = -sideRange; w <= sideRange; w++)
            {
                Vector3Int offset = forward * f + rightOffset * w;
                Vector3Int cell = currentCell + offset;

                if (!gridManager.IsWalkable(cell))
                {
                    if (w == 0) blockedForward = true;
                    continue;
                }

                if (blockedForward && w != 0) continue;


                bool hasFood = false;
                foreach (var food in gridManager.foodList)
                {
                    if (food == null || food.foodAmount <= 0) continue;
                    Vector3Int foodCell = gridManager.tilemap.WorldToCell(food.transform.position);
                    if (foodCell == cell)
                    {
                        hasFood = true;
                        break;
                    }
                }

                if (hasFood) foodCandidates.Add(cell);
                else candidates.Add(cell);
            }

            if (blockedForward) break;
        }



        if (foodCandidates.Count > 0)
        {
            foodCandidates.Sort((a, b) => Vector3Int.Distance(a, currentCell).CompareTo(Vector3Int.Distance(b, currentCell)));
            Vector3Int toFoodCell = foodCandidates[0];
            lastDirection = toFoodCell - currentCell;
            return toFoodCell; 
        }

        if (candidates.Count == 0)
            candidates = GetWalkableNeighbors8(currentCell);


        List<float> weights = new List<float>();
        float totalWeight = 0f;

        List<Vector3Int> validCandidates = new List<Vector3Int>();

        foreach (var c in candidates)
        {
            List<Vector3Int> line = GetCellsBetween(currentCell, c);
            bool blocked = false;
            foreach (var cell in line)
            {
                if (!gridManager.IsWalkable(cell))
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked)
                validCandidates.Add(c);
        }

        candidates = validCandidates;

        foreach (var c in candidates)
        {
            float tau = gridManager.GetPheromone(c);
            float eta = 1f;      
            float minDist = float.MaxValue;

            foreach (var food in gridManager.foodList)
            {
                if (food == null || food.foodAmount <= 0) continue;
                Vector3Int foodCell = gridManager.tilemap.WorldToCell(food.transform.position);
                float dist = Mathf.Abs((foodCell - c).x) + Mathf.Abs((foodCell - c).y); // Manhattan
                if (dist < minDist) minDist = dist;
            }

            if (minDist < float.MaxValue)
                eta = 1f / (minDist + 1f);
            else
                eta = 1f;

            eta = 1f / Mathf.Pow(minDist + 1f, 2);

            float weight = Mathf.Pow(tau + 0.01f, gridManager.alpha) * Mathf.Pow(eta, gridManager.beta);

            weights.Add(weight);
            totalWeight += weight;
        }


        if (candidates.Count == 0)
        {
            lastDirection = Vector3Int.zero;
            return currentCell;
        }


        float r = UnityEngine.Random.value * totalWeight;
        float cum = 0f;
        Vector3Int chosenCell = candidates[0];
        for (int i = 0; i < candidates.Count; i++)
        {
            cum += weights[i];
            if (r <= cum)
            {
                chosenCell = candidates[i];
                break;
            }
        }

        lastDirection = chosenCell - currentCell;
        return chosenCell;
    }




    Vector3Int AcoReturnToNest(int forwardRange = 10, int sideRange = 5)
    {
        Vector3Int nestCell = gridManager.GetNestCell();

        if (currentCell == nestCell)
        {
            ReachNest();
            return nestCell;
        }

        if (lastDirection == Vector3Int.zero)
            lastDirection = nestCell - currentCell;

        Vector3Int forward = new Vector3Int(
            lastDirection.x != 0 ? lastDirection.x / Mathf.Abs(lastDirection.x) : 0,
            lastDirection.y != 0 ? lastDirection.y / Mathf.Abs(lastDirection.y) : 0,
            0
        );

        Vector3Int rightOffset = new Vector3Int(forward.y, -forward.x, 0);
        Vector3Int leftOffset = new Vector3Int(-forward.y, forward.x, 0);

        List<Vector3Int> candidates = new List<Vector3Int>();



        for (int f = 1; f <= forwardRange; f++)
        {
            bool blockedForward = false;
            for (int w = -sideRange; w <= sideRange; w++)
            {
                Vector3Int offset = forward * f + rightOffset * w;
                Vector3Int cell = currentCell + offset;

                if (!gridManager.IsWalkable(cell))
                {
                    if (w == 0) blockedForward = true;
                    gridManager.DepositPheromone(cell, -gridManager.pheromoneDepositAmount);
                    continue;
                }

                if (blockedForward && w != 0) continue;

                candidates.Add(cell);
            }

            if (blockedForward) break;
        }



        List<Vector3Int> validCandidates = new List<Vector3Int>();

        foreach (var c in candidates)
        {
            List<Vector3Int> line = GetCellsBetween(currentCell, c);
            bool blocked = false;
            foreach (var cell in line)
            {
                if (!gridManager.IsWalkable(cell))
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked)
                validCandidates.Add(c);
        }

        candidates = validCandidates;


        if (candidates.Count == 0)
            candidates = GetWalkableNeighbors8(currentCell);

        List<Vector3Int> allowedCandidates = new List<Vector3Int>();
        foreach (var c in candidates)
        {
            if (!tabuList.Contains(c))
                allowedCandidates.Add(c);
        }

        if (allowedCandidates.Count == 0)
            allowedCandidates = candidates;

        List<float> weights = new List<float>();
        float totalWeight = 0f;

        foreach (var c in allowedCandidates)
        {
            float tau = gridManager.GetPheromone(c);
            float dist = Vector3Int.Distance(c, nestCell);
            float eta = 1f / (dist + 1f); 
            float weight = Mathf.Pow(tau + 0.01f, gridManager.alpha) * Mathf.Pow(eta, gridManager.beta);

            Vector3Int dir = c - currentCell;
            if (dir == -lastDirection)
                weight *= 0.1f;

            weights.Add(weight);
            totalWeight += weight;
        }

        if (weights.Count == 0 || totalWeight <= 0f)
        {
            lastDirection = Vector3Int.zero;
            return currentCell;
        }


        float r = UnityEngine.Random.value * totalWeight;
        float cum = 0f;
        Vector3Int chosen = allowedCandidates[0];
        for (int i = 0; i < allowedCandidates.Count; i++)
        {
            cum += weights[i];
            if (r <= cum)
            {
                chosen = allowedCandidates[i];
                break;
            }
        }

        if (returnSteps <= 500)
        {
            List<Vector3Int> pathCells = GetCellsBetween(currentCell, chosen);
            foreach (var cell in pathCells)
            {
                gridManager.DepositPheromone(cell, gridManager.pheromoneDepositAmount);

                if (!path.Contains(cell))
                    path.Add(cell);
            }
        }

        returnSteps++;
        lastDirection = chosen - currentCell;

        return chosen;
    }



    private void OnArrivedAtCell()
    {
        foreach (var food in gridManager.foodList)
        {
            if (food == null || food.foodAmount <= 0) continue;
            Vector3Int foodCell = gridManager.tilemap.WorldToCell(food.transform.position);
            if (foodCell == currentCell)
            {
                PickUpFood(food);
                break;
            }
        }
    }

    List<Vector3Int> GetWalkableNeighbors8(Vector3Int cell)
    {
        Vector3Int[] dirs = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
            new Vector3Int(1,1,0), new Vector3Int(1,-1,0), new Vector3Int(-1,1,0), new Vector3Int(-1,-1,0)
        };

        List<Vector3Int> neighbors = new List<Vector3Int>();
        foreach (var dir in dirs)
        {
            Vector3Int n = cell + dir;
            if (gridManager.IsWalkable(n))
                neighbors.Add(n);
        }
        return neighbors;
    }

    public void PickUpFood(FoodItem food)
    {
        if (state == AntState.Foraging && food.foodAmount > 0)
        {
            food.RemoveFood(1);
            state = AntState.Returning;
            tabuList.Clear();
            gridManager.DepositPheromone(currentCell, gridManager.pheromoneDepositAmount);

            lastDirection = -lastDirection;

            if (food.foodAmount <= 0)
                Destroy(food.gameObject);
        }
    }

    private void ReachNest()
    {
        nest.DepositFood(1);
        state = AntState.Foraging;
        tabuList.Clear();
        gridManager.CheckElitism(path);
        path.Clear();
        lastDirection = -lastDirection;
        returnSteps = 0;
    }

    public bool IsForaging()
    {
        return state == AntState.Foraging;
    }


}
