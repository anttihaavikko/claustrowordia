using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using UnityEngine.Serialization;

public class Score : MonoBehaviour
{
    public List<TMP_Text> display, addition;
    public Appearer additionMyAppearer;

    private ulong score;
    private float shownScore;
    private int shownAddition;

    public ulong TotalScore => score;

    private void Update()
    {
        var scrollSpeed = Mathf.Abs(score - shownScore);
        shownScore = Mathf.MoveTowards(shownScore, score, Time.deltaTime * scrollSpeed * 2f);
        display.ForEach(t => t.text = ScoreString(shownScore));
    }

    public int Add(int amount)
    {
        shownAddition += amount;
        addition.ForEach(t => t.text = shownAddition > 0 ? "+" + shownAddition : shownAddition.ToString());
        additionMyAppearer.Show();
        
        CancelInvoke(nameof(HideAddition));
        Invoke(nameof(HideAddition), 2f);

        return amount;
    }

    private void HideAddition()
    {
        score += (ulong)shownAddition;
        additionMyAppearer.Hide();
        shownAddition = 0;
    }

    public static string ScoreString(float score)
    {
        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.NumberGroupSeparator = " ";
        return score.ToString("#,0", nfi);
    }
}
