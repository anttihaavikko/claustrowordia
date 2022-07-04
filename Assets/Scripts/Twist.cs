using System;
using UnityEngine;

[Serializable]
public class Twist
{
    public TwistType type;
    public string title;
    public string description;
    public string firstLetter, secondLetter;
    public int index;

    public TwistType Type => type;
    public string Title => title;
    public string Description => description;

    public string FirstLetter => firstLetter;
    public string SecondLetter => secondLetter;
    public int Index => index;

    public Twist()
    {
    }

    public Twist(TwistType type, string title, string description)
    {
        this.type = type;
        this.title = title;
        this.description = description;
    }

    public void SetLetters(string first, string second)
    {
        firstLetter = first;
        secondLetter = second;
    }

    public void SetIndex(int i)
    {
        index = i;
    }
}

public enum TwistType
{
    Replace,
    Destroy,
    AddCards,
    SlideUp,
    SlideRight,
    SlideLeft,
    SlideDown,
    MoreMulti
}