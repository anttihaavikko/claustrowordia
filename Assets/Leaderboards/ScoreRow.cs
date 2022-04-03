using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Leaderboards
{
    public class ScoreRow : MonoBehaviour
    {
        public TMP_Text namePart, scorePart;
        public RawImage flag, langFlag;
    
        public void Setup(string nam, string sco, string locale, string level)
        {
            namePart.text = nam;
            scorePart.text = sco;
            FlagManager.SetFlag(flag, locale);
            
            var languages = new[] { "gb", "fi", "fr", "de", "es", "nl" };
            var first = level.Split(",").First();
            FlagManager.SetFlag(langFlag, languages[int.Parse(first) - 11]);
        }
    }
}
