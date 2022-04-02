public class Twist
{
    public TwistType Type { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    
    public string FirstLetter { get; private set; }
    public string SecondLetter { get; private set; }

    public Twist(TwistType type, string title, string description)
    {
        Type = type;
        Title = title;
        Description = description;
    }

    public void SetLetters(string first, string second)
    {
        FirstLetter = first;
        SecondLetter = second;
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