using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Managers;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Field : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;
    [SerializeField] private WordDictionary wordDictionary;
    [SerializeField] private Color markColor;
    [SerializeField] private Score scoreDisplay;
    [SerializeField] private Appearer twistTitle;
    [SerializeField] private RectTransform twistHolder;
    [SerializeField] private TwistPanel twistPanelPrefab;
    [SerializeField] private Hand hand;
    [SerializeField] private Appearer showBoardButton;
    [SerializeField] private Transform twistHolderAndTitle;
    
    public bool CanAct { get; private set; }

    private readonly TileGrid<Card> grid = new(7, 7);
    private readonly List<WordMatch> words = new();

    private int move;
    private bool showingBoard;

    private void Start()
    {
        PlaceCard(new Vector3(-1f, -1f, 0));
        PlaceCard(new Vector3(1f, -1f, 0));
        PlaceCard(new Vector3(-1f, 1f, 0));
        PlaceCard(new Vector3(1f, 1f, 0));
    }

    public void ToggleBoardOrTwists()
    {
        showingBoard = !showingBoard;
        twistHolderAndTitle.gameObject.SetActive(!showingBoard);
        showBoardButton.text.text = showingBoard ? "SHOW TWISTS" : "SHOW BOARD";
    }

    private void PlaceCard(Vector3 pos)
    {
        var card = Instantiate(cardPrefab, pos, Quaternion.identity);
        card.Setup(wordDictionary.GetRandomLetter());
        card.draggable.DropLocked = true;
        card.hoverer.enabled = false;
        AddCard(card);
    }

    public void AddCard(Card card, bool check = true)
    {
        var p = card.draggable.GetRoundedPos();
        var x = Mathf.RoundToInt(p.x + 3);
        var y = Mathf.RoundToInt(-p.y + 3);
        grid.Set(card, x, y);

        move++;

        if (!check) return;
        StartCoroutine(Check(x, y));
    }
    
    IEnumerator Check(int x, int y)
    {
        hand.SetState(false);
        
        var rowCards = grid.GetRow(y).ToList();
        var colCards = grid.GetColumn(x).ToList();
        
        var rowsReversed = rowCards.ToList();
        rowsReversed.Reverse();
        var colsReversed = colCards.ToList();
        colsReversed.Reverse();
        
        var rowLetters = rowCards.Select(c => c ? c.Letter : " ").ToList();
        var colLetters = colCards.Select(c => c ? c.Letter : " ").ToList();
        
        var row = string.Join(string.Empty, rowLetters);
        var col = string.Join(string.Empty, colLetters);
        
        words.Clear();

        yield return CheckString(row, x, rowCards);
        yield return CheckString(col, y, colCards);
        yield return CheckString(Reverse(row), 6 - x, rowsReversed);
        yield return CheckString(Reverse(col), 6 - y, colsReversed);

        var multi = 1;

        foreach (var w in words.OrderBy(w => w.word.Length))
        {
            StartCoroutine(Announce(w, multi));
            
            foreach (var c in w.cards.ToList())
            {
                EffectManager.Instance.AddEffect(0, c.transform.position);
                c.Colorize(markColor);
                c.Shake(0.1f);
                yield return new WaitForSeconds(0.075f);
            }
            
            multi++;

            yield return new WaitForSeconds(0.5f);
        }

        yield return DoTwist();
    }

    private IEnumerator DoTwist()
    {
        if (move % 10 != 0)
        {
            hand.SetState(true);
            yield break;
        }
        
        foreach (Transform child in twistHolder)
        {
            Destroy(child.gameObject);
        }
        
        move = 0;
        
        yield return new WaitForSeconds(0.25f);
        
        twistTitle.Show();

        yield return new WaitForSeconds(1f);
        
        twistHolder.gameObject.SetActive(true);
        
        GetTwists().OrderBy(_ => Random.value).Take(3).ToList().ForEach(t =>
        {
            AddLettersTo(t);
            var twist = Instantiate(twistPanelPrefab, twistHolder);
            twist.Setup(t);
            twist.button.onClick.AddListener(() =>
            {
                ApplyTwist(t);
                twist.button.onClick.RemoveAllListeners();
            });
        });

        yield return new WaitForSeconds(0.5f);

        showBoardButton.Show();
    }

    private void ApplyTwist(Twist twist)
    {
        hand.SetState(true);
        
        Debug.Log($"Applying twist {twist.Type} with letters {twist.FirstLetter} and {twist.SecondLetter}");
        twistHolder.gameObject.SetActive(false);
        twistTitle.Hide();

        switch (twist.Type)
        {
            case TwistType.Replace:
                StartCoroutine(DestroyAll(twist.FirstLetter, twist.SecondLetter));
                break;
            case TwistType.Destroy:
                StartCoroutine(DestroyAll(twist.FirstLetter));
                break;
            case TwistType.AddCards:
                StartCoroutine(GiveExtraCards());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator DestroyAll(string letter, string replacement = null)
    {
        yield return new WaitForSeconds(0.5f);
        
        var cards = grid.All().Where(c => c && c.Letter == letter).ToList();
        var positions = cards.Select(c => c.transform.position);
        grid.Remove(cards);
        foreach (var c in cards)
        {
            c.Explode();
            yield return new WaitForSeconds(0.1f);
        }

        if (replacement == null) yield break;
        
        yield return new WaitForSeconds(0.4f);

        foreach (var p in positions)
        {
            var card = Instantiate(cardPrefab, p, Quaternion.identity);
            card.Setup(replacement);
            AddCard(card, false);
            
            var x = Mathf.RoundToInt(p.x + 3);
            var y = Mathf.RoundToInt(-p.y + 3);

            yield return Check(x, y);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator GiveExtraCards()
    {
        for (var i = 0; i < 3; i++)
        {
            hand.AddCard();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator Announce(WordMatch match, int multiplier)
    {
        var x = match.cards.Average(c => c.transform.position.x);
        var y = match.cards.Average(c => c.transform.position.y);
        const float diff = 0.3f;
        var score = Mathf.RoundToInt(Mathf.Pow(match.word.Length, 2));

        var pos = new Vector3(x, y, 0) + Vector3.down;
        
        EffectManager.AddTextPopup(match.word.ToUpper(), pos);
        
        if (grid.All().All(c => !c || c.Matched || match.cards.Contains(c)))
        {
            yield return new WaitForSeconds(0.05f);
            const string extraText = "<size=5>FULL MATCH BONUS</size>";
            pos += Vector3.down * 0.5f + new Vector3(Random.Range(-diff, diff), 0, 0);
            EffectManager.AddTextPopup(extraText, pos);
            multiplier *= 10;
        }
        
        yield return new WaitForSeconds(0.05f);
        
        yield return new WaitForSeconds(0.05f);
        var scoreText = $"<size=7>{score}</size><size=4> x {multiplier}</size>";
        pos += Vector3.down * 0.5f + new Vector3(Random.Range(-diff, diff), 0, 0);
        EffectManager.AddTextPopup(scoreText, pos);

        scoreDisplay.Add(score * multiplier);
    }

    private static string Reverse(string s)
    {
        var charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    IEnumerator CheckString(string text, int mustInclude, List<Card> cards)
    {
        if (text.Length < 3) yield break;
        
        for (var len = text.Length; len > 2; len--)
        {
            for (var start = 0; start <= text.Length - len; start++)
            {
                var word = text.Substring(start, len);
                if (wordDictionary.IsWord(word) && mustInclude >= start && mustInclude < start + len)
                {
                    words.Add(new WordMatch
                    {
                        word = word,
                        cards = cards.GetRange(start, len)
                    });
                }
            }

            yield return null;
        }
    }

    private static IEnumerable<Twist> GetTwists()
    {
        return new[]
        {
            new Twist(TwistType.Replace, "Immigrant blues", "Replace all [1] tiles with [2] tiles."),
            new Twist(TwistType.Destroy, "Delay the inevitable", "Destroy all [1] tiles."),
            new Twist(TwistType.AddCards, "Extra population", "Receive (3 extra) letter tiles.")
        };
    }

    private void AddLettersTo(Twist twist)
    {
        var randomLetter = wordDictionary.GetRandomLetter();
        var fieldLetter = grid.All().Where(c => c && c.Letter != randomLetter).OrderBy(_ => Random.value).First();
        twist.SetLetters(fieldLetter.Letter, randomLetter);
    }
}

internal struct WordMatch
{
    public string word;
    public IEnumerable<Card> cards;
}