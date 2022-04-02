using System;
using AnttiStarterKit.Animations;
using UnityEngine;

public class LanguageSelection : MonoBehaviour
{
    [SerializeField] private SpeechBubble bubble;

    private void Start()
    {
        Invoke(nameof(ShowHelp), 1.5f);
    }

    private void ShowHelp()
    {
        bubble.Show("This will (only) affect the (dictionary) used, not the game itself.");
    }

    public void PickLanguage(int index)
    {
        PlayerPrefs.SetInt("WordGridLanguage", index);
        SceneChanger.Instance.ChangeScene("Main");
    }
}