using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public UDPReceiver UDPReceiver;
    public float rotationSpeedFactor = 5f;
    public float rotationGain = 3f;
    public bool smoothRotate = true;
    public bool usePitch = true;

    private float pitch;
    private float yaw;
    private float roll;
    private bool clutch = false;

    [SerializeField]
    private Quaternion targetRotation;
    [SerializeField]
    private float forward = 0f;

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

        // 기준 로테이션에 대한 보정된 회전값을 계산
        Vector3 targetRotationVec = new Vector3(pitch, yaw * rotationGain + forward, 0);
        Quaternion targetRotation = Quaternion.Euler(targetRotationVec);

        // 부드럽게 회전하거나 바로 회전할지 여부에 따라 회전 적용
        if (smoothRotate)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationSpeedFactor * Time.deltaTime);
        else
            transform.localRotation = targetRotation;
    }
}
