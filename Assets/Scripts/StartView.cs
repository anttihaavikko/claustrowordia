using System;
using AnttiStarterKit.Animations;
using UnityEngine;

public class StartView : MonoBehaviour
{
    [SerializeField] private SpeechBubble bubble;
    
    private void Start()
    {
        Invoke(nameof(Greet), 1.5f);
    }

    private void Greet()
    {
        if (!PlayerPrefs.HasKey("PlayerName")) return;
        var plr = PlayerPrefs.GetString("PlayerName", "");
        bubble.Show($"Hello ({plr})!");
    }

    public void ToNameOrLang()
    {
        var scene = PlayerPrefs.HasKey("PlayerName") ? "Language" : "Name";
        SceneChanger.Instance.ChangeScene(scene);
    }
}