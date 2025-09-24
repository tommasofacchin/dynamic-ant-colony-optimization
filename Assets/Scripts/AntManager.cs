using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AntManager : MonoBehaviour
{
    public Ant antPrefab;
    public int antCount;
    public GridManager gridManager;
    public Tilemap tilemap;
    public Transform spawnPoint;

    void Start()
    {
        //gridManager.RefreshGrid(); 
        SpawnAnts();
    }

    void SpawnAnts()
    {
        for (int i = 0; i < antCount; i++)
        {
            Ant newAnt = Instantiate(antPrefab, spawnPoint);
            newAnt.Init(gridManager);
        }
    }
}
