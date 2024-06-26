using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public UDPReceiver UDPReceiver;
    public HeatmapGenerator heatmapGenerator;

    private float pitch;
    private float yaw;
    private float roll;
    private bool clutch = false;
    public bool usePitch = true;

    public float rotationGain = 1f;
    public float maxRotationGain = 6f;
    private float minRotationGain = 1f;
    
    public float gainFactor;
    private float intensity;
    private float pre_intensity;
    
    [SerializeField] private Quaternion targetRotation;
    [SerializeField] private Vector3 targetRotationVec;
    [SerializeField] private float forward = 0f;

    private void Start()
    {
        minRotationGain = rotationGain;
        maxRotationGain -= minRotationGain;
    }

    // Update is called once per frame
    void Update()
    {
        if (UDPReceiver == null) return;

        if (usePitch) pitch = UDPReceiver.pitch;
        yaw = UDPReceiver.yaw;
        roll = UDPReceiver.roll;
        clutch = UDPReceiver.clutch;

        if (clutch)
        {
            forward = transform.localRotation.eulerAngles.y;
            rotationGain = 0;
        }
        else
        {
            bool isGazeLeft = heatmapGenerator.gazePoint.X < 0.0f;
            bool isYawLeft = yaw > 0.0f;

            intensity = heatmapGenerator.GetHeatmapValueClamped(heatmapGenerator.textureWidth/2, heatmapGenerator.textureHeight/2);
            if (isGazeLeft != isYawLeft)
            {
                // gain down
                intensity = (1.0f - intensity) * maxRotationGain;
            }
            else
            {
                // gain up before red
                if (intensity < 0.65)
                    intensity *= maxRotationGain;
                else
                    intensity = 0.0f;
            }
            pre_intensity = intensity;
            rotationGain = minRotationGain + intensity;
        }

        targetRotationVec = new Vector3(pitch, yaw * rotationGain + forward, 0);

        targetRotation = Quaternion.Euler(targetRotationVec);
        targetRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(targetRotationVec), Time.deltaTime * 12f);
        transform.localRotation = targetRotation;
    }
}
