using System.Text;
using AnttiStarterKit.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class TwistPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleField, descField;
    public Button button;
    
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(Twist twist)
    {
        titleField.text = twist.Title;
        var sb = new StringBuilder(twist.Description);
        sb.Replace("[1]", $"<color=#F3DFA2>{twist.FirstLetter.ToUpper()}</color>");
        sb.Replace("[2]", $"<color=#F3DFA2>{twist.SecondLetter.ToUpper()}</color>");
        sb.Replace("(", "<color=#F3DFA2>");
        sb.Replace(")", "</color>");
        descField.text = sb.ToString();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
}