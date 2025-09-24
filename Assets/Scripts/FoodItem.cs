using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodItem : MonoBehaviour
{
    public int foodAmount;
    [SerializeField] private float baseScale;    
    [SerializeField] private float maxScale;     
    [SerializeField] private int maxFood;

    public System.Action<FoodItem> OnDestroyed;


    private void OnEnable()
    {
        FindObjectOfType<GridManager>().RegisterFood(this);
        UpdateScale(100);
    }

    private void OnDestroy()
    {
        var gm = FindObjectOfType<GridManager>();

        if (gm != null)
            gm.UnregisterFood(this);

        OnDestroyed?.Invoke(this);
    }

    public void AddFood(int amount)
    {
        foodAmount += amount;
        UpdateScale(100);
    }

    public void RemoveFood(int amount)
    {
        foodAmount -= amount;
        if (foodAmount < 0) foodAmount = 0;
        UpdateScale(5);
    }

    private void UpdateScale(float probability)
    {
        float t = Mathf.Log(foodAmount + 1) / Mathf.Log(maxFood + 1);
        float finalScale = Mathf.Lerp(baseScale, maxScale, t);

        if(Random.Range(1, 100) <= probability)
            transform.localScale = new Vector3(finalScale, finalScale, 1f);

    }

}
