using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PrivacyManager : MonoBehaviour
{
    [SerializeField] private GameObject _canvasGO;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _declineButton;
    
    void Start()
    {
        _acceptButton.onClick.AddListener(OnAccept);
        _declineButton.onClick.AddListener(OnDecline);
    }

    private void OnAccept()
    {
        Destroy(_canvasGO);
    }

    private void OnDecline()
    {
        Application.Quit();
    }
}
