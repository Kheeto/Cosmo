using UnityEngine;

public class ColourGenerator {

    ColourSettings settings;
    Texture2D texture;
    const int textureResolution = 50;
    INoiseFilter biomeNoiseFilter;

    public void UpdateSettings(ColourSettings settings)
    {
        this.settings = settings;
        // Update the texture if it's null or if it doesn't contain all of the biomes
        if (texture == null || texture.height != settings.biomeColourSettings.biomes.Length) {
            texture = new Texture2D(textureResolution * 2, settings.biomeColourSettings.biomes.Length, TextureFormat.RGBA32, false);
        }
        biomeNoiseFilter = NoiseFilterFactory.CreateNoiseFilter(settings.biomeColourSettings.noise);
    }

    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.planetMaterial.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
    }

    /// <summary>
    /// Returns a value between 0 and 1, where 0 is the first biome and 1 is the last biome.
    /// </summary>
    /// <param name="pointOnUnitSphere">The point on the sphere to calculate the biome percent from</param>
    public float BiomePercentFromPoint(Vector3 pointOnUnitSphere)
    {
        float heightPercent = (pointOnUnitSphere.y + 1) / 2f;
        float biomeNoise = biomeNoiseFilter.Evaluate(pointOnUnitSphere) - settings.biomeColourSettings.noiseOffset;
        heightPercent += biomeNoise *settings.biomeColourSettings.noiseStrength;

        float biomeIndex = 0f;
        int numBiomes = settings.biomeColourSettings.biomes.Length;
        float blendRange = settings.biomeColourSettings.blendAmount / 2f + .001f; // Add a small value so there is always some blend

        for (int i = 0; i < numBiomes; i++)
        {
            float distance = heightPercent - settings.biomeColourSettings.biomes[i].startHeight;
            float weight = Mathf.InverseLerp(-blendRange, blendRange, distance);
            biomeIndex *= (1 - weight);
            biomeIndex += i * weight;
        }

        // Mathf.Max prevents dividing by 0
        return biomeIndex / Mathf.Max(1, (numBiomes - 1));
    }

    /// <summary>
    /// Generates a 2D Texture with the gradient colours of the ocean in the first half and biomes in the second half
    /// </summary>
    public void UpdateColours()
    {
        Color[] colours = new Color[texture.width * texture.height];
        int colourIndex = 0;
        foreach (var biome in settings.biomeColourSettings.biomes)
        {
            for (int i = 0; i < textureResolution * 2; i++)
            {
                Color gradientColour;
                // The first half of the texture resolution is the ocean texture
                if (i < textureResolution)
                    gradientColour = settings.oceanColour.Evaluate(i / (textureResolution - 1f));
                else // The second half is the biome texture
                    gradientColour = biome.gradient.Evaluate((i-textureResolution) / (textureResolution - 1f));

                Color tintColour = biome.tint;
                colours[colourIndex] = gradientColour * (1 - biome.tintPercent) + tintColour * biome.tintPercent;
                colourIndex++;
            }
        }
        texture.SetPixels(colours);
        texture.Apply();
        settings.planetMaterial.SetTexture("_texture", texture);
    }
}
