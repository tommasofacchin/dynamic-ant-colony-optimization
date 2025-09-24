using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Nest : MonoBehaviour
{
    [SerializeField] private int totalFood = 0;
    [SerializeField] private TextMeshProUGUI text;

    public void DepositFood(int amount)
    {
        totalFood += amount;
        text.text = totalFood.ToString();
    }
}
