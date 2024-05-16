using UnityEngine;

public class HeatmapGenerator : MonoBehaviour
{
    public ComputeShader gaussianIntensityShader;
    public ComputeShader maxIntensityShader;
    public ComputeShader heatmapColorizationShader;
    public Shader blendShader;
    public int textureWidth = 512;
    public int textureHeight = 512;
    public float sigma = 10.0f;
    public float overlayAlpha = 0.5f;

    private RenderTexture gaussianTexture;
    private RenderTexture maxIntensityTexture;
    private RenderTexture heatmapTexture;
    private Material blendMaterial;
    private Vector2 center;

    void Start()
    {
        gaussianTexture = CreateRenderTexture();
        maxIntensityTexture = CreateRenderTexture();
        heatmapTexture = CreateRenderTexture();

        blendMaterial = new Material(blendShader);
    }

    void Update()
    {
        center = GetMousePositionNormalized();
        GenerateHeatmap();
    }

    RenderTexture CreateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGBFloat);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    Vector2 GetMousePositionNormalized()
    {
        Vector2 mousePosition = Input.mousePosition;
        Vector2 normalizedPosition = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        Debug.Log("mousePosition:" + normalizedPosition);
        return normalizedPosition;
    }

    void GenerateHeatmap()
    {
        // Dispatch Gaussian Intensity Shader
        gaussianIntensityShader.SetInt("img_w", textureWidth);
        gaussianIntensityShader.SetInt("img_h", textureHeight);
        gaussianIntensityShader.SetFloat("sigma", sigma);
        gaussianIntensityShader.SetVector("center", center);
        gaussianIntensityShader.SetTexture(0, "Result", gaussianTexture);
        gaussianIntensityShader.Dispatch(0, textureWidth / 16, textureHeight / 16, 1);

        // Dispatch Maximum Intensity Shader
        maxIntensityShader.SetInt("tw", textureWidth);
        maxIntensityShader.SetInt("th", textureHeight);
        maxIntensityShader.SetTexture(0, "lum_tex", gaussianTexture);
        maxIntensityShader.SetTexture(0, "Result", maxIntensityTexture);
        maxIntensityShader.Dispatch(0, textureWidth / 16, textureHeight / 16, 1);

        // Read max intensity value from maxIntensityTexture
        float maxval = GetMaxIntensityValue();
        Debug.Log("Max Intensity Value: " + maxval);

        // Dispatch Heatmap Colorization Shader
        heatmapColorizationShader.SetFloat("maxval", maxval);
        heatmapColorizationShader.SetTexture(0, "height_tex", maxIntensityTexture);
        heatmapColorizationShader.SetTexture(0, "Result", heatmapTexture);
        heatmapColorizationShader.Dispatch(0, textureWidth / 16, textureHeight / 16, 1);
    }

    float GetMaxIntensityValue()
    {
        RenderTexture.active = maxIntensityTexture;
        Texture2D tempTexture = new Texture2D(maxIntensityTexture.width, maxIntensityTexture.height, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, maxIntensityTexture.width, maxIntensityTexture.height), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null;

        float maxval = 0.0f;
        Color[] pixels = tempTexture.GetPixels();

        foreach (Color pixel in pixels)
        {
            if (pixel.r > maxval)
            {
                maxval = pixel.r;
            }
        }

        Destroy(tempTexture);
        return maxval;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // Set the textures and alpha to the blend material
        blendMaterial.SetTexture("_MainTex", src);
        blendMaterial.SetTexture("_OverlayTex", heatmapTexture);
        blendMaterial.SetFloat("_OverlayAlpha", overlayAlpha);

        // Use Graphics.Blit to blend the original scene with the heatmap
        Graphics.Blit(src, dest, blendMaterial);
    }

    public float GetCenterHeatmapValue()
    {
        // Read the center pixel value from the heatmap texture
        RenderTexture.active = heatmapTexture;
        Texture2D tempTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null;

        Color centerPixel = tempTexture.GetPixel(textureWidth / 2, textureHeight / 2);
        return centerPixel.r; // Assuming the heatmap value is stored in the red channel
    }
}
