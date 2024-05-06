using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public UDPReceiver UDPReceiver;
    public TextMeshProUGUI clutchText;

    private void Start()
    {
        clutchText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (UDPReceiver == null || clutchText == null) return;

        clutchText.gameObject.SetActive(UDPReceiver.clutch);
    }

}
