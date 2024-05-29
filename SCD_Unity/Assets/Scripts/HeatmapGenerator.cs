using System.Collections.Generic;
using UnityEngine;
using Tobii.GameIntegration.Net;

public class HeatmapGenerator : MonoBehaviour
{
    public ComputeShader gaussianIntensityShader;
    public ComputeShader maxIntensityShader;
    public ComputeShader heatmapColorizationShader;
    public Shader blendShader;
    public enum HeatmapVisualizationMods { Color, HeightMap, None };
    public HeatmapVisualizationMods visualizationMode = HeatmapVisualizationMods.Color;
    public float sigma = 40.0f;
    public float overlayAlpha = 0.5f;
    public float decayRate = 0.99f; // weight 감소 비율
    public float pointLifetime = 2.0f; // weight가 유지되는 시간 (초)
    
    [HideInInspector] public int textureWidth;
    [HideInInspector] public int textureHeight;

    private RenderTexture heatmapTexture;
    private RenderTexture heightHeatmapTexture;
    private Material blendMaterial;
    private List<HeatPoint> heatPoints = new List<HeatPoint>();
    private float elapsedTime = 0.0f;


    void Start()
    {
        textureWidth = Screen.width / 8;
        textureHeight = Screen.height / 8;

        heatmapTexture = CreateRenderTexture();
        heightHeatmapTexture = CreateRenderTexture();

        blendMaterial = new Material(blendShader);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        Debug.Log(TobiiGameIntegrationApi.IsTrackerConnected());

        // 마우스 위치를 기록
        Vector2 normalizedMousePos = GetMousePositionNormalized();
        heatPoints.Add(new HeatPoint(normalizedMousePos, elapsedTime));

        // 오래된 포인트 제거
        heatPoints.RemoveAll(point => elapsedTime - point.timestamp > pointLifetime);

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (visualizationMode == HeatmapVisualizationMods.None)
                visualizationMode = HeatmapVisualizationMods.Color;
            else
                visualizationMode += 1;
        }

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
        Graphics.SetRenderTarget(heatmapTexture);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(null);

        foreach (var point in heatPoints)
        {
            float weight = Mathf.Pow(decayRate, elapsedTime - point.timestamp);
            DispatchGaussianIntensityShader(point.position, weight);
        }

        maxIntensityShader.SetInt("tw", textureWidth);
        maxIntensityShader.SetInt("th", textureHeight);
        maxIntensityShader.SetTexture(0, "lum_tex", heatmapTexture);
        maxIntensityShader.SetTexture(0, "Result", heightHeatmapTexture);
        maxIntensityShader.Dispatch(0, textureWidth / 16 + 1, textureHeight / 16 + 1, 1);

        float maxval = GetMaxIntensityValue();

        heatmapColorizationShader.SetFloat("maxval", maxval);
        heatmapColorizationShader.SetInt("width", textureWidth);
        heatmapColorizationShader.SetInt("height", textureHeight);
        heatmapColorizationShader.SetTexture(0, "height_tex", heightHeatmapTexture);
        heatmapColorizationShader.SetTexture(0, "Result", heatmapTexture);
        heatmapColorizationShader.Dispatch(0, textureWidth / 16 + 1, textureHeight / 16 + 1, 1);
    }

    void DispatchGaussianIntensityShader(Vector2 position, float weight)
    {
        gaussianIntensityShader.SetInt("img_w", textureWidth);
        gaussianIntensityShader.SetInt("img_h", textureHeight);
        gaussianIntensityShader.SetFloat("sigma", sigma);
        gaussianIntensityShader.SetFloat("weight", weight);
        gaussianIntensityShader.SetVector("center", position);
        gaussianIntensityShader.SetTexture(0, "Result", heatmapTexture);
        gaussianIntensityShader.Dispatch(0, textureWidth / 16 + 1, textureHeight / 16 + 1, 1);
    }

    float GetMaxIntensityValue()
    {
        RenderTexture.active = heightHeatmapTexture;
        Texture2D tempTexture = new Texture2D(heightHeatmapTexture.width, heightHeatmapTexture.height, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, heightHeatmapTexture.width, heightHeatmapTexture.height), 0, 0);
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
        if (visualizationMode == HeatmapVisualizationMods.Color)
        {
            blendMaterial.SetTexture("_MainTex", src);
            blendMaterial.SetTexture("_OverlayTex", heatmapTexture);
            blendMaterial.SetFloat("_OverlayAlpha", overlayAlpha);
            Graphics.Blit(src, dest, blendMaterial);
        }
        else if (visualizationMode == HeatmapVisualizationMods.HeightMap)
        {
            Graphics.Blit(heightHeatmapTexture, dest);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }

    public float GetCenterHeatmapValue()
    {
        // Read the center pixel value from the heatmap texture
        RenderTexture.active = heightHeatmapTexture;
        Texture2D tempTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null;

        Color centerPixel = tempTexture.GetPixel(textureWidth / 2, textureHeight / 2);
        return centerPixel.r; // Assuming the heatmap value is stored in the red channel
    }

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
