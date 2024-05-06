using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public UDPReceiver UDPReceiver;
    public float rotationSpeedFactor = 5f;
    public float rotationGain = 3f;
    public bool usePitch = true;

    private float pitch;
    private float yaw;
    private float roll;
    private bool clutch = false;

    [SerializeField] private Quaternion targetRotation;
    [SerializeField] private float forward = 0f;
    [SerializeField] private Quaternion smoothedRotation;

    // Update is called once per frame
    void Update()
    {
        if (UDPReceiver == null) return;
        clutch = UDPReceiver.clutch;
        // clutch가 true이면 현재 yaw 로테이션을 forwardRotation으로 지정
        if (clutch)
        {
            forward = transform.localRotation.eulerAngles.y;
            return; // clutch가 true일 때는 회전을 진행하지 않음
        }

        pitch = UDPReceiver.pitch;
        yaw = UDPReceiver.yaw;
        roll = UDPReceiver.roll;

        if (!usePitch)
            pitch = 0;

        // 이동 평균된 회전값 사용
        Vector3 targetRotationVec = new Vector3(pitch, yaw * rotationGain + forward, 0);
        targetRotation = Quaternion.Euler(targetRotationVec);

        // 부드러운 회전을 위해 이전 회전값과 현재 회전값 사이를 보간
        smoothedRotation = Quaternion.Lerp(smoothedRotation, targetRotation, Time.deltaTime * rotationSpeedFactor);
        transform.localRotation = smoothedRotation;
    }
}
