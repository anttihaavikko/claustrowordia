using System;
using System.Linq;
using UnityEngine;

public class Field : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;
    [SerializeField] private WordDictionary wordDictionary;

    private readonly TileGrid<Card> grid = new(7, 7);

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

        var rowLetters = grid.GetRow(y).Select(c => c ? c.Letter : " ").ToList();
        var colLetters = grid.GetColumn(x).Select(c => c ? c.Letter : " ").ToList();
        var row = string.Join(string.Empty, rowLetters).Trim();
        var col = string.Join(string.Empty, colLetters).Trim();
        
        Debug.Log($"Checking {row} => {wordDictionary.IsWord(row)}");
        Debug.Log($"Checking {col} => {wordDictionary.IsWord(col)}");
    }
}