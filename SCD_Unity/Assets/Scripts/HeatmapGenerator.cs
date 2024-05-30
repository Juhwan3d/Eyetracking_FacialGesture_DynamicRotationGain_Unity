using System.Collections.Generic;
using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Reflection;
using UnityEditor;

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
    public float decayRate = 0.99f;
    public float pointLifetime = 1.0f;

    public string trackerUrl = "tobii-prp://IS5FF-100203157272";
    
    [HideInInspector] public int textureWidth;
    [HideInInspector] public int textureHeight;
    [HideInInspector] public float maxval;

    private RenderTexture heatmapTexture;
    private RenderTexture heightHeatmapTexture;
    private Material blendMaterial;
    private List<HeatPoint> heatPoints = new();
    private float elapsedTime = 0.0f;

    public Vector2 gazePosVec;

    private TrackerInfo trackerInfo;
    private GazePoint gazePoint;
    private TobiiRectangle tobiiRectangle;

    void Start()
    {
        textureWidth = Screen.width / 8;
        textureHeight = Screen.height / 8;
        Debug.Log(string.Format("Screen Width: {0}, Height: {1}", Screen.width, Screen.height));

        heatmapTexture = CreateRenderTexture();
        heightHeatmapTexture = CreateRenderTexture();

        blendMaterial = new Material(blendShader);

        // Set Eye tracker
        bool isTobiiInit = TobiiGameIntegrationApi.IsApiInitialized();
        
        TobiiGameIntegrationApi.SetApplicationName("SCD_Unity");
        TobiiGameIntegrationApi.TrackTracker(trackerUrl);
        TobiiGameIntegrationApi.PrelinkAll();
        
        //trackerInfo = TobiiGameIntegrationApi.GetTrackerInfo(trackerUrl);
        //Rect gameView = GetGameViewRect();
        //tobiiRectangle.Right = (int)gameView.xMax;
        //tobiiRectangle.Bottom = (int)gameView.yMax;
        //tobiiRectangle.Left = (int)gameView.xMin;
        //tobiiRectangle.Top = (int)gameView.yMin;
        //TobiiGameIntegrationApi.TrackRectangle(tobiiRectangle);
        //Debug.Log(string.Format("TobiiRectangle Right: {0}, Bottom: {1}, Left: {2}, Top: {3}",
        //    tobiiRectangle.Right,
        //    tobiiRectangle.Bottom,
        //    tobiiRectangle.Left,
        //    tobiiRectangle.Top));

        Debug.Log("Init API: " + isTobiiInit);
        Debug.Log("Tracker Connected: " + TobiiGameIntegrationApi.IsTrackerConnected());
        Debug.Log("Tracker Enabled: " + TobiiGameIntegrationApi.IsTrackerEnabled());
    }

    public static Rect GetGameViewRect()
    {
        // Get the game view window using reflection
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        EditorWindow gameView = EditorWindow.GetWindow(T);

        // Get the position and size of the game view
        Rect gameViewRect = gameView.position;

        // Get the size of the viewport within the game view window
        System.Type sizeType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        MethodInfo getSizeOfMainGameView = sizeType.GetMethod("GetSizeOfMainGameView", BindingFlags.NonPublic | BindingFlags.Static);
        Vector2 gameViewSize = (Vector2)getSizeOfMainGameView.Invoke(null, null);

        gameViewRect.size = gameViewSize;
        return gameViewRect;
    }

    void Update()
    {
        TobiiGameIntegrationApi.Update();
        TobiiGameIntegrationApi.UpdateTrackerInfos();

        Vector2 gazePos = GetGazePisition();
        elapsedTime += Time.deltaTime;
        heatPoints.Add(new HeatPoint(gazePos, elapsedTime));
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

    Vector2 GetGazePisition()
    {
        //Vector2 mousePosition = Input.mousePosition;
        //Vector2 normalizedPosition = new(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        //return normalizedPosition;

        TobiiGameIntegrationApi.TryGetLatestGazePoint(out gazePoint);
        gazePosVec = new(gazePoint.X + 1, gazePoint.Y + 1);
        gazePosVec /= 2.0f;
        return gazePosVec;
    }

    RenderTexture CreateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGBFloat);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
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

        maxval = GetMaxIntensityValue();
        Debug.Log("maxval: " + maxval);
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

    public float GetHeatmapValue(int x, int y)
    {
        RenderTexture.active = heightHeatmapTexture;
        Texture2D tempTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null;

        Color centerPixel = tempTexture.GetPixel(x, y);
        return centerPixel.r;
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
