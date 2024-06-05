using Mirai;
using UnityEngine;
using UnityEngine.UI;

public class ShardsTechProcessing : MonoBehaviour
{
    [SerializeField] protected Text txtDeeplink;
    private string _deeplink;
    private void Awake()
    {
        //ShardsTech.OnTrySendNotification += ShardsTechOnTrySendNotification;
    }
    private void ShardsTechOnTrySendNotification(string deeplink)
    {
        gameObject.SetActive(true);
        _deeplink = deeplink;
        txtDeeplink.text = (ShardsTech.IsLinkedAddress
                               ? "Notification has been sent to address , check MiraiApp or if click link below and scan the QR code with MiraiApp\n"
                               : "Account isn't linked to any address, Please use MiraiApp scan the Qr code in link below\n")
                           + deeplink[..50] + "...";
        if (!ShardsTech.IsLinkedAddress)
            ClickLink();
    }
    
    public void ClickLink()
    {
        Application.OpenURL(_deeplink);
    }
}
