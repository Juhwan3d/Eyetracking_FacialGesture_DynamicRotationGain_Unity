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
        // clutch�� true�̸� ���� yaw �����̼��� forwardRotation���� ����
        if (clutch)
        {
            forward = transform.localRotation.eulerAngles.y;
            return; // clutch�� true�� ���� ȸ���� �������� ����
        }

        pitch = UDPReceiver.pitch;
        yaw = UDPReceiver.yaw;
        roll = UDPReceiver.roll;

        if (!usePitch)
            pitch = 0;

        // ���� �����̼ǿ� ���� ������ ȸ������ ���
        Vector3 targetRotationVec = new Vector3(pitch, yaw * rotationGain + forward, 0);
        Quaternion targetRotation = Quaternion.Euler(targetRotationVec);

        // �ε巴�� ȸ���ϰų� �ٷ� ȸ������ ���ο� ���� ȸ�� ����
        if (smoothRotate)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationSpeedFactor * Time.deltaTime);
        else
            transform.localRotation = targetRotation;
    }
}
