using UnityEngine;

[CreateAssetMenu()]
public class ShapeSettings : ScriptableObject {

    public float planetRadius = 1f;
    public NoiseLayer[] noiseLayers;

    [System.Serializable]
    public class NoiseLayer
    {
        public bool enabled = true;
        public bool useFirstLayerAsMask;
        public NoiseSettings noiseSettings;
    }
}
