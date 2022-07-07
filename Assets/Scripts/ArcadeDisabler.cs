using Mirror;
using UltimateArcade;
using UnityEngine;

public class ArcadeDisabler : MonoBehaviour
{
    private void Awake()
    {
        if (!Application.isEditor) return;
        GetComponent<AutoConnect>().enabled = false;
        gameObject.AddComponent<NetworkManagerHUD>();
    }
}