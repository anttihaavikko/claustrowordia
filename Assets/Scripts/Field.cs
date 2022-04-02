using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

public class Field : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;
    [SerializeField] private WordDictionary wordDictionary;
    [SerializeField] private Color markColor;

    private readonly TileGrid<Card> grid = new(7, 7);
    private readonly List<WordMatch> words = new();

    private void Start()
    {
        PlaceCard(new Vector3(-1f, -1f, 0));
        PlaceCard(new Vector3(1f, -1f, 0));
        PlaceCard(new Vector3(-1f, 1f, 0));
        PlaceCard(new Vector3(1f, 1f, 0));
    }

    private void PlaceCard(Vector3 pos)
    {
        var card = Instantiate(cardPrefab, pos, Quaternion.identity);
        card.Setup(wordDictionary.GetRandomLetter());
        card.draggable.DropLocked = true;
        card.hoverer.enabled = false;
        AddCard(card);
    }

    public void AddCard(Card card)
    {
        var p = card.draggable.GetRoundedPos();
        var x = Mathf.RoundToInt(p.x + 3);
        var y = Mathf.RoundToInt(-p.y + 3);
        grid.Set(card, x, y);

        StartCoroutine(Check(x, y));
    }
    
    IEnumerator Check(int x, int y)
    {
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
    }

    private IEnumerator Announce(WordMatch match, int multiplier)
    {
        var x = match.cards.Average(c => c.transform.position.x);
        var y = match.cards.Average(c => c.transform.position.y);
        const float diff = 0.3f;
        var score = Mathf.Pow(match.word.Length, 2);

        var pos = new Vector3(x, y, 0) + Vector3.down;
        
        EffectManager.AddTextPopup(match.word.ToUpper(), pos);
        
        if (grid.All().All(c => !c || c.Matched || match.cards.Contains(c)))
        {
            yield return new WaitForSeconds(0.05f);
            const string extraText = "<size=5>FULL MATCH BONUS</size><size=4> x 10</size>";
            pos += Vector3.down * 0.5f + new Vector3(Random.Range(-diff, diff), 0, 0);
            EffectManager.AddTextPopup(extraText, pos);
        }
        
        yield return new WaitForSeconds(0.05f);
        
        yield return new WaitForSeconds(0.05f);
        var scoreText = $"<size=7>{score}</size><size=4> x {multiplier}</size>";
        pos += Vector3.down * 0.5f + new Vector3(Random.Range(-diff, diff), 0, 0);
        EffectManager.AddTextPopup(scoreText, pos);
    }

    public static string Reverse(string s)
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
}

struct WordMatch
{
    public string word;
    public IEnumerable<Card> cards;
}