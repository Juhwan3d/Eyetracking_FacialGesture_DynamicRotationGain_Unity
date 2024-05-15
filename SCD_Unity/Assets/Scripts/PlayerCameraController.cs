using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public UDPReceiver UDPReceiver;
    public float rotationSpeedFactor = 10f;
    public float rotationGain = 3f;
    public bool usePitch = true;
    public bool LerpRotate = true;

    private float pitch;
    private float yaw;
    private float roll;
    private bool clutch = false;

    [SerializeField] private Quaternion targetRotation;
    [SerializeField] private Vector3 targetRotationVec;
    [SerializeField] private float forward = 0f;
    [SerializeField] private Quaternion smoothedRotation;

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

        targetRotationVec = new Vector3(pitch, yaw * rotationGain + forward, 0);
        targetRotation = Quaternion.Euler(targetRotationVec);

        if (LerpRotate)
        {
            smoothedRotation = Quaternion.Lerp(smoothedRotation, targetRotation, Time.deltaTime * rotationSpeedFactor);
            transform.localRotation = smoothedRotation;
        }
        else
        {
            transform.localRotation = targetRotation;
        }
/*        else
        {
            smoothedRotation = Quaternion.RotateTowards(smoothedRotation, targetRotation, Time.deltaTime * rotationSpeedFactor);
            transform.localRotation = smoothedRotation;
        }*/
    }

    // �ð������� smoothedRotation�� ǥ���ϴ� �Լ�
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 2f);

        // smoothedRotation �ð������� ǥ��
        DrawQuaternion(transform.position, smoothedRotation.normalized, 1f);
    }

    // Quaternion�� �ð������� ǥ���ϴ� �Լ�
    void DrawQuaternion(Vector3 position, Quaternion rotation, float size)
    {
        // Quaternion�� ��ķ� ��ȯ
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

        // Quaternion�� �ð������� ǥ��
        Gizmos.matrix = matrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size, size, size));
    }
}
