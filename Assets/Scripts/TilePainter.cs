using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TilePainter : MonoBehaviour
{

    [SerializeField] private GameObject foodPrefab;

    public Tilemap tilemap;
    public TileBase paintTile1;
    public TileBase paintTile2;
    public int brushRadius = 3;
    public GridManager gridManager;

    private Camera _camera;
    [SerializeField] private int currentTile;
    private Vector3Int? lastCell = null;
    private bool isPainting = false;

    private List<Vector3Int> circleBuffer = new List<Vector3Int>();

    private Dictionary<Vector3Int, FoodItem> foodDict = new Dictionary<Vector3Int, FoodItem>();
    [SerializeField] private int foodQty;

    void Start()
    {
        _camera = Camera.main;
        PrecomputeCircleOffsets(brushRadius);
        InitFood();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isPainting = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }

        if (!Input.GetMouseButton(0))
        {
            lastCell = null;
            isPainting = false;
            return;
        }

        //UI click
        if (!isPainting)
        {
            lastCell = null;
            return;
        }

        Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int currentCell = tilemap.WorldToCell(mouseWorldPos);

        if (lastCell.HasValue)
            PaintLine(lastCell.Value, currentCell);
        else
            PaintCircle(currentCell);

        lastCell = currentCell;
    }


    void InitFood()
    {
        FoodItem[] allFood = FindObjectsOfType<FoodItem>();
        foreach (var food in allFood)
        {
            Vector3Int cell = tilemap.WorldToCell(food.transform.position);
            if (!foodDict.ContainsKey(cell))
            {
                foodDict[cell] = food;
                gridManager.RegisterFood(food); 
            }
        }
    }

    void PrecomputeCircleOffsets(int radius)
    {
        circleBuffer.Clear();
        int rSquared = radius * radius;
        for (int x = -radius; x <= radius; x++)
        {
            int x2 = x * x;
            for (int y = -radius; y <= radius; y++)
            {
                if (x2 + y * y <= rSquared)
                    circleBuffer.Add(new Vector3Int(x, y, 0));
            }
        }
    }

    void PaintCircle(Vector3Int center)
    {
        switch (currentTile)
        {
            case 1:
                foreach (var offset in circleBuffer)
                {
                    Vector3Int cellPos = center + offset;
                    gridManager.SetWalkable(cellPos, true, paintTile1);
                }
                break;

            case 2:
                foreach (var offset in circleBuffer)
                {
                    Vector3Int cellPos = center + offset;
                    gridManager.SetWalkable(cellPos, false, paintTile2);
                }
                break;

            case 3:
                HandleFood(center);
                break;
        }
    }

    void PaintLine(Vector3Int start, Vector3Int end)
    {
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            PaintCircle(new Vector3Int(x0, y0, 0));

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    public void SetCurrentTile(int tileIndex)
    {
        currentTile = tileIndex;
    }

    void HandleFood(Vector3Int cellPos)
    {
        if (tilemap.GetTile(cellPos) != paintTile1) return;

        if (foodDict.TryGetValue(cellPos, out FoodItem food) && food != null)
        {
            food.AddFood(foodQty);
        }
        else
        {
            Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);
            GameObject foodObj = Instantiate(foodPrefab, spawnPos, Quaternion.identity);
            food = foodObj.GetComponent<FoodItem>();

            foodDict[cellPos] = food;

            food.OnDestroyed += OnFoodDestroyed;

            gridManager.RegisterFood(food);
        }
    }

    private void OnFoodDestroyed(FoodItem food)
    {
        Vector3Int cell = tilemap.WorldToCell(food.transform.position);
        if (foodDict.ContainsKey(cell))
        {
            foodDict.Remove(cell);
        }
    }





}
