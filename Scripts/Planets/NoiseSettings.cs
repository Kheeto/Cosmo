using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class NoiseSettings {

    [Range(1, 8)] 
    public int Layers = 1;
    public float strength = 1f;
    public float baseRoughness = 1f;
    public float roughness = 2f;
    public float persistence = .5f;
    public float minValue;
    public Vector3 centre;
}
