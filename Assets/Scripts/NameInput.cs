using System;
using System.Collections;
using System.Collections.Generic;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Visuals;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class NameInput : MonoBehaviour
{
    public TMP_InputField field;
    public EffectCamera cam;
    [SerializeField] private SpeechBubble bubble;

    private void Start()
    {
        field.onValueChanged.AddListener(ToUpper);
        Invoke(nameof(FocusInput), 0.6f);
        Invoke(nameof(ShowHelp), 2f);
    }

    private void ShowHelp()
    {
        bubble.Show("The name will (only) be used for saving your score to (online leaderboards).");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            Save();
        }
    }

    private void FocusInput()
	{
        EventSystem.current.SetSelectedGameObject(field.gameObject, null);
        field.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    private void ToUpper(string value)
    {
        field.text = value;
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(field.text)) return;
        PlayerPrefs.SetString("PlayerName", field.text);
        PlayerPrefs.SetString("PlayerId", Guid.NewGuid().ToString());
        SceneChanger.Instance.ChangeScene("Language");
    }
}
