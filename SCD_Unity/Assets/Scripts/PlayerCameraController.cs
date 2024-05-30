using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public UDPReceiver UDPReceiver;
    public float rotationGain = 3f;
    public float maxRotationGain = 6f;
    public bool usePitch = true;
    public HeatmapGenerator heatmapGenerator;
    public float centerHeatmapValue;

    private float pitch;
    private float yaw;
    private float roll;
    private bool clutch = false;
    private float minRotationGain;

    [SerializeField] private Quaternion targetRotation;
    [SerializeField] private Vector3 targetRotationVec;
    [SerializeField] private float forward = 0f;
    [SerializeField] private Quaternion smoothedRotation;

    private void Start()
    {
        minRotationGain = rotationGain;
    }

    // Update is called once per frame
    void Update()
    {
        if (UDPReceiver == null) return;
        clutch = UDPReceiver.clutch;

        rotationGain = minRotationGain;
        centerHeatmapValue = heatmapGenerator.GetHeatmapValue(heatmapGenerator.textureWidth/2, heatmapGenerator.textureHeight/2);
        float gainFactor = Mathf.Clamp(centerHeatmapValue / heatmapGenerator.maxval * (maxRotationGain-rotationGain), 0, maxRotationGain-rotationGain);
        Debug.Log("before clamp: " + (centerHeatmapValue/heatmapGenerator.maxval));
        Debug.Log("gainFactor: " + gainFactor);
        rotationGain = (maxRotationGain - gainFactor);

        Debug.Log("Center Heatmap Value: " + centerHeatmapValue);

        if (clutch)
        {
            forward = transform.localRotation.eulerAngles.y;
            rotationGain = 0;
        }

        pitch = UDPReceiver.pitch;
        yaw = UDPReceiver.yaw;
        roll = UDPReceiver.roll;

        if (!usePitch)
            pitch = 0;

        targetRotationVec = new Vector3(pitch, yaw * rotationGain + forward, 0);
        targetRotation = Quaternion.Euler(targetRotationVec);

        transform.localRotation = targetRotation;
    }
}
