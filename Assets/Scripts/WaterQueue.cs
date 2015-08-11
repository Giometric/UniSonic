using UnityEngine;
using System.Collections;

public class WaterQueue : MonoBehaviour
{
    private Renderer ren;

    void Awake()
    {
        ren = GetComponent<Renderer>();
        ren.sortingLayerName = "Water";
    }
}