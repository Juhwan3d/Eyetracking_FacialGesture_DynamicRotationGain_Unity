using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public UDPReceiver UDPReceiver;
    public TextMeshProUGUI clutchText;
    public TextMeshProUGUI rotateLabel;

    private float pitch, yaw, roll;
    private bool clutch;

    private void Start()
    {
        clutchText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (UDPReceiver == null) return;

        pitch = UDPReceiver.pitch;
        yaw = UDPReceiver.yaw;
        roll = UDPReceiver.roll;
        clutch = UDPReceiver.clutch;

        clutchText.gameObject.SetActive(clutch);
        rotateLabel.text = string.Format("Pitch: {0:N2}, Yaw: {1:N2}, Roll: {2:N2}, Clutch: {3}",pitch,yaw,roll,clutch);
    }

}
