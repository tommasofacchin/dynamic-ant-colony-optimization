using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    [SerializeField] public Tilemap tilemap;
    [SerializeField] private TileBase walkableTile;

    [SerializeField] private Tilemap pheromoneTilemap;
    [SerializeField] private TileBase pheromoneTile;


    [SerializeField] public GameObject nestObject;


    [Header("Parameters")]

    [Range(0f, 200f)]
    [SerializeField] public float moveSpeed;

    [Range(0f, 5f)]
    [SerializeField] public float alpha; 

    [Range(0f, 5f)]
    [SerializeField] public float beta; 

    [Range(1, 200)]
    [SerializeField] public int tabuLength;

    [Range(0f, 1f)]
    [SerializeField] public float pheromoneDepositAmount;

    [Range(0f, 1f)]
    [SerializeField] public float evaporationRate;

    [Range(1, 20)]
    [SerializeField] public int ElitismBoost;


    [Header("Sliders")]
    [SerializeField] public Slider sliderAlpha;
    [SerializeField] public Slider sliderBeta;
    [SerializeField] public Slider sliderElitismBoost;
    [SerializeField] public Slider sliderEvaporationRate;
    [SerializeField] public Slider sliderPheromoneDepositAmount;
    [SerializeField] public Slider sliderSpeed;

    [SerializeField] public TextMeshProUGUI sliderAlphaText;
    [SerializeField] public TextMeshProUGUI sliderBetaText;
    [SerializeField] public TextMeshProUGUI sliderElitismBoostText;
    [SerializeField] public TextMeshProUGUI sliderEvaporationRateText;
    [SerializeField] public TextMeshProUGUI sliderPheromoneDepositAmountText;
    [SerializeField] public TextMeshProUGUI sliderSpeedText;



    public List<FoodItem> foodList = new List<FoodItem>();

    private bool[,] walkableGrid;
    private BoundsInt bounds;
    private int gridWidth, gridHeight;

    private float[,] pheromoneGrid;


    private Vector3Int nestCell;
    private List<Vector3Int> foodCells = new List<Vector3Int>();

    private HashSet<Vector3Int> dirtyCells = new HashSet<Vector3Int>();
    private Queue<Vector3Int> dirtyCellsQueue = new Queue<Vector3Int>();
    private float pheromoneUpdateInterval = 0.1f;
    private float pheromoneUpdateTimer = 0f;
    private int cellsPerFrame = 200;

    private int minLength = int.MaxValue;
    private List<Vector3Int> elitistPath = new List<Vector3Int>();


    void Awake()
    {
        bounds = tilemap.cellBounds;
        gridWidth = bounds.size.x;
        gridHeight = bounds.size.y;

        walkableGrid = new bool[gridWidth, gridHeight];
        pheromoneGrid = new float[gridWidth, gridHeight];
        RefreshGrid(); 
        InitNest();
        
    }
    private void Start()
    {
        InitSliders();
    }

    void Update()
    {
        pheromoneUpdateTimer += Time.deltaTime;
        if (pheromoneUpdateTimer >= pheromoneUpdateInterval)
        {
            pheromoneUpdateTimer = 0f;
            EvaporatePheromones();
            //UpdatePheromoneVisual();
        }
        UpdatePheromoneVisualBatch();
    }

    // Ricrea tutta la matrice leggendo la tilemap
    public void RefreshGrid()
    {
        foreach (var pos in bounds.allPositionsWithin)
        {
            walkableGrid[pos.x - bounds.xMin, pos.y - bounds.yMin] = tilemap.GetTile(pos) == walkableTile;
        }
    }

    // Aggiorna una singola cella
    public void SetWalkable(Vector3Int cell, bool isWalkable, TileBase tile)
    {
        if (!TryGetLocalIndex(cell, out int x, out int y)) return;

        walkableGrid[x, y] = isWalkable;
        tilemap.SetTile(cell, tile);

        if (!isWalkable)
        {
            pheromoneGrid[x, y] = 0f;
            pheromoneTilemap.SetTile(cell, null);
            dirtyCells.Remove(cell);

            if (elitistPath.Contains(cell))
            {
                elitistPath.Clear();
                minLength = int.MaxValue;
            }
        }
    }

    public bool IsWalkable(Vector3Int cell)
    {
        if (!TryGetLocalIndex(cell, out int x, out int y)) return false;
        return walkableGrid[x, y];
    }

    public void DepositPheromone(Vector3Int cell, float amount)
    {
        if (!TryGetLocalIndex(cell, out int x, out int y)) return;

        float amountNormalised = amount * (moveSpeed / sliderSpeed.maxValue);

        float oldValue = pheromoneGrid[x, y];
        pheromoneGrid[x, y] += amountNormalised;

        if (Mathf.Abs(oldValue - pheromoneGrid[x, y]) > 0.001f)
        {
            if (!dirtyCells.Contains(cell))
            {
                dirtyCells.Add(cell);
                dirtyCellsQueue.Enqueue(cell); 
            }
        }
    }

    public void EvaporatePheromones()
    {
        List<Vector3Int> toRemove = new List<Vector3Int>();

        foreach (var cell in dirtyCells)
        {
            if (!TryGetLocalIndex(cell, out int x, out int y)) continue;

            float evaporationRateNormalised = evaporationRate * (moveSpeed / sliderSpeed.maxValue);

            if (elitistPath.Contains(cell))
                evaporationRateNormalised = 0;

            pheromoneGrid[x, y] *= (1 - evaporationRateNormalised);

            if (pheromoneGrid[x, y] <= 0.001f)
                toRemove.Add(cell);
        }

        foreach (var c in toRemove)
        {
            dirtyCells.Remove(c);
            pheromoneGrid[c.x - bounds.xMin, c.y - bounds.yMin] = 0f;
        }
    }
    

    //moved to UpdatePheromoneVisualBatch to reduce lag.
    //public void UpdatePheromoneVisual()
    //{
    //    List<Vector3Int> toRemove = new List<Vector3Int>();

    //    foreach (var cell in dirtyCells)
    //    {
    //        Vector3Int local = new Vector3Int(cell.x - bounds.xMin, cell.y - bounds.yMin, 0);
    //        float value = pheromoneGrid[local.x, local.y];

    //        if (value > 0)
    //        {
    //            if (pheromoneTilemap.GetTile(cell) != pheromoneTile)
    //                pheromoneTilemap.SetTile(cell, pheromoneTile);

    //            float intensity = Mathf.Clamp01(value / 2);
    //            Color c = Color.Lerp(Color.clear, Color.magenta, intensity);
    //            pheromoneTilemap.SetColor(cell, c);
    //        }
    //        else
    //        {
    //            pheromoneTilemap.SetTile(cell, null);
    //            toRemove.Add(cell);
    //        }
    //    }

    //    foreach (var c in toRemove)
    //        dirtyCells.Remove(c);
    //}

    private void UpdatePheromoneVisualBatch()
    {
        int count = 0;
        int queueLength = dirtyCellsQueue.Count;

        for (int i = 0; i < queueLength && count < cellsPerFrame; i++)
        {
            Vector3Int cell = dirtyCellsQueue.Dequeue();

            if (!TryGetLocalIndex(cell, out int x, out int y)) continue;

            float value = pheromoneGrid[x, y];

            if (value > 0)
            {
                if (pheromoneTilemap.GetTile(cell) != pheromoneTile)
                    pheromoneTilemap.SetTile(cell, pheromoneTile);

                float intensity;
                if (elitistPath.Contains(cell))
                    intensity = 1f;
                else
                    intensity = Mathf.Clamp01(value / 2f);

                Color c = Color.Lerp(Color.clear, Color.magenta, intensity);
                pheromoneTilemap.SetColor(cell, c);

                dirtyCellsQueue.Enqueue(cell);
            }
            else
            {
                pheromoneTilemap.SetTile(cell, null);
                dirtyCells.Remove(cell);
            }

            count++;
        }
    }


    public float GetPheromone(Vector3Int cell)
    {
        Vector3Int local = new Vector3Int(cell.x - bounds.xMin, cell.y - bounds.yMin, 0);

        if (local.x < 0 || local.y < 0 ||
            local.x >= pheromoneGrid.GetLength(0) || local.y >= pheromoneGrid.GetLength(1))
            return 0f;

        return pheromoneGrid[local.x, local.y];
    }

    public void InitNest()
    {
        if (nestObject != null)
            nestCell = tilemap.WorldToCell(nestObject.transform.position);
    }

    public Vector3Int GetNestCell(){
        return nestCell;
    }


    public void InitFood()
    {
        foodCells.Clear();

        FoodItem[] allFoods = GameObject.FindObjectsOfType<FoodItem>();
        int foodLayer = LayerMask.NameToLayer("Food");

        foreach (FoodItem food in allFoods)
        {
            if (food.gameObject.layer == foodLayer)
            {
                Vector3Int cell = tilemap.WorldToCell(food.transform.position);
                foodCells.Add(cell);
            }
        }
    }

    public void RegisterFood(FoodItem food)
    {
        if (!foodList.Contains(food))
            foodList.Add(food);
    }

    public void UnregisterFood(FoodItem food)
    {
        foodList.Remove(food);
    }

    private bool TryGetLocalIndex(Vector3Int cell, out int x, out int y)
    {
        x = cell.x - bounds.xMin;
        y = cell.y - bounds.yMin;
        return x >= 0 && y >= 0 && x < gridWidth && y < gridHeight;
    }

    public void CheckElitism(List<Vector3Int> path)
    {
        if (path == null || path.Count == 0)
            return;

        int length = path.Count;

        if (length < minLength * 1.2f)
            BoostPheromonesElitism(path);

        if (length < minLength)
        {
            minLength = length;
            elitistPath = new List<Vector3Int>(path);
        }
    }

    private void BoostPheromonesElitism(List<Vector3Int> path)
    {
        foreach (var cell in path)
        {
            if (!TryGetLocalIndex(cell, out int x, out int y)) continue;

            float oldValue = pheromoneGrid[x, y];
            pheromoneGrid[x, y] += pheromoneDepositAmount * ElitismBoost;

            if (Mathf.Abs(oldValue - pheromoneGrid[x, y]) > 0.001f)
            {
                if (!dirtyCells.Contains(cell))
                {
                    dirtyCells.Add(cell);
                    dirtyCellsQueue.Enqueue(cell);
                }
            }
        }
    }


    private void InitSliders()
    {
        sliderAlpha.value = alpha;
        sliderBeta.value = beta;
        sliderElitismBoost.value = ElitismBoost;
        sliderEvaporationRate.value = evaporationRate;
        sliderPheromoneDepositAmount.value = pheromoneDepositAmount;
        sliderSpeed.value = moveSpeed;

        sliderAlphaText.text = alpha.ToString("F2");
        sliderBetaText.text = beta.ToString("F2");
        sliderElitismBoostText.text = ElitismBoost.ToString("F0");
        sliderEvaporationRateText.text = evaporationRate.ToString("F2");
        sliderPheromoneDepositAmountText.text = pheromoneDepositAmount.ToString("F2");
        sliderSpeedText.text = moveSpeed.ToString("F0");



        sliderAlpha.onValueChanged.AddListener(value =>
        {
            alpha = value;                       
            sliderAlphaText.text = value.ToString("F2");
        });

        sliderBeta.onValueChanged.AddListener(value =>
        {
            beta = value;
            sliderBetaText.text = value.ToString("F2");
        });

        sliderElitismBoost.onValueChanged.AddListener(value => 
        {
            ElitismBoost = (int)value;
            sliderElitismBoostText.text = value.ToString("F0");
        });

        sliderEvaporationRate.onValueChanged.AddListener(value =>
        {
            evaporationRate = value;
            sliderEvaporationRateText.text = value.ToString("F2");
        });

        sliderPheromoneDepositAmount.onValueChanged.AddListener(value =>
        {
            pheromoneDepositAmount = value;
            sliderPheromoneDepositAmountText.text = value.ToString("F2");
        });

        sliderSpeed.onValueChanged.AddListener(value =>
        {
            moveSpeed = value;
            sliderSpeedText.text = value.ToString("F0");
        });

    }
}
