using TMPro;
using UnityEngine;

public class UpdateShadowDetectorUI : MonoBehaviour
{
    private StealthShadowDetector stealthShadowDetector;
    private TextMeshProUGUI textMeshProUGUI;
    void Start()
    {
        stealthShadowDetector = PlayerController.Instance.GetStealthShadowDetector();
        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        textMeshProUGUI.text = stealthShadowDetector.darknessLevel.ToString("F2");
    }
}
