using System.Collections.Generic;
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
    public float decayRate = 0.99f; // weight ���� ����
    public float pointLifetime = 2.0f; // weight�� �����Ǵ� �ð� (��)

    private RenderTexture heatmapTexture;
    private RenderTexture finalHeatmapTexture;
    private Material blendMaterial;
    private List<HeatPoint> heatPoints = new List<HeatPoint>();
    private float elapsedTime = 0.0f;

    void Start()
    {
        heatmapTexture = CreateRenderTexture();
        finalHeatmapTexture = CreateRenderTexture();

        blendMaterial = new Material(blendShader);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // ���콺 ��ġ�� ���
        Vector2 normalizedMousePos = GetMousePositionNormalized();
        heatPoints.Add(new HeatPoint(normalizedMousePos, elapsedTime));

        // ������ ����Ʈ ����
        heatPoints.RemoveAll(point => elapsedTime - point.timestamp > pointLifetime);

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
        return normalizedPosition;
    }

    void GenerateHeatmap()
    {
        // ��Ʈ�� �ؽ�ó�� �ʱ�ȭ
        RenderTexture.active = heatmapTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;

        foreach (var point in heatPoints)
        {
            float weight = Mathf.Pow(decayRate, elapsedTime - point.timestamp);
            DispatchGaussianIntensityShader(point.position, weight);
        }

        // Max Intensity Shader ����ġ
        maxIntensityShader.SetInt("tw", textureWidth);
        maxIntensityShader.SetInt("th", textureHeight);
        maxIntensityShader.SetTexture(0, "lum_tex", heatmapTexture);
        maxIntensityShader.SetTexture(0, "Result", finalHeatmapTexture);
        maxIntensityShader.Dispatch(0, textureWidth / 16, textureHeight / 16, 1);

        // Max Intensity ���� �о����
        float maxval = GetMaxIntensityValue();

        // Heatmap Colorization Shader ����ġ
        heatmapColorizationShader.SetFloat("maxval", maxval);
        heatmapColorizationShader.SetInt("width", textureWidth);
        heatmapColorizationShader.SetInt("height", textureHeight);
        heatmapColorizationShader.SetTexture(0, "height_tex", finalHeatmapTexture);
        heatmapColorizationShader.SetTexture(0, "Result", heatmapTexture);
        heatmapColorizationShader.Dispatch(0, textureWidth / 16, textureHeight / 16, 1);
    }

    void DispatchGaussianIntensityShader(Vector2 position, float weight)
    {
        gaussianIntensityShader.SetInt("img_w", textureWidth);
        gaussianIntensityShader.SetInt("img_h", textureHeight);
        gaussianIntensityShader.SetFloat("sigma", sigma);
        gaussianIntensityShader.SetFloat("weight", weight);
        gaussianIntensityShader.SetVector("center", position);
        gaussianIntensityShader.SetTexture(0, "Result", heatmapTexture);
        gaussianIntensityShader.Dispatch(0, textureWidth / 16, textureHeight / 16, 1);
    }

    float GetMaxIntensityValue()
    {
        RenderTexture.active = finalHeatmapTexture;
        Texture2D tempTexture = new Texture2D(finalHeatmapTexture.width, finalHeatmapTexture.height, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, finalHeatmapTexture.width, finalHeatmapTexture.height), 0, 0);
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

    // HeatPoint ����ü ����
    struct HeatPoint
    {
        public Vector2 position;
        public float timestamp;

        public HeatPoint(Vector2 position, float timestamp)
        {
            this.position = position;
            this.timestamp = timestamp;
        }
    }
}
