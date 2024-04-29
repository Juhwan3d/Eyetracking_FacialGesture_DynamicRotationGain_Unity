using System.Collections;
using System.Collections.Generic;
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

    [SerializeField]
    private Quaternion targetRotation;

    // Update is called once per frame
    void Update()
    {
        if (UDPReceiver == null) return;

        pitch = UDPReceiver.pitch;
        yaw = UDPReceiver.yaw;
        roll = UDPReceiver.roll;

        if (!usePitch)
            pitch = 0;
        Vector3 targetRotationVec = new(pitch, yaw * rotationGain, 0);
        
        targetRotation = Quaternion.Euler(targetRotationVec);

        if (smoothRotate)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationSpeedFactor * Time.deltaTime);
        else
            transform.localRotation = targetRotation;
    }
}
