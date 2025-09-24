using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> tilemaps;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject UIPanel;
    [SerializeField] private GameObject UIButtons;
    [SerializeField] private GameObject UISliders;
    [SerializeField] private GameObject FoodObject;

    private int currentIndex;


    private void Start()
    {
        Time.timeScale = 0f;
    }

    public void SelectMap(int index)
    {

        foreach (var map in tilemaps)
            map.SetActive(false);

        tilemaps[index].SetActive(true);

        gridManager.tilemap = tilemaps[index].GetComponent<Tilemap>();
        gridManager.RefreshGrid();

        currentIndex = index;
        UIPanel.SetActive(false);
        UIButtons.SetActive(true);
        UISliders.SetActive(true);
        FoodObject.SetActive(true);
        Time.timeScale = 1f;
    }

    public void SetCurrentTile(int tileIndex)
    {
        tilemaps[currentIndex].GetComponent<TilePainter>().SetCurrentTile(tileIndex);
    }


}
