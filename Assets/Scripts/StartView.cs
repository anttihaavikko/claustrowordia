using System;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Managers;
using UnityEngine;
using UnityEngine.UI;

public class StartView : MonoBehaviour
{
    [SerializeField] private SpeechBubble bubble;
    [SerializeField] private ButtonStyle quitButton;
    [SerializeField] private Mascot mascot;

    private void Start()
    {
        AudioManager.Instance.Lowpass(false);
        AudioManager.Instance.Chorus(false);
        AudioManager.Instance.TargetPitch = 1f;
        
        Invoke(nameof(Greet), 1.5f);

        quitButton.onHover += () =>
        {
            mascot.Duck();
            bubble.Show("Bye...");
        };
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