using System;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;
    [SerializeField] private WordDictionary wordDictionary;
    [SerializeField] private Field field;

    private List<Card> cards = new();

    private void Start()
    {
        for (var i = 0; i < 7; i++)
        {
            AddCard();
        }
    }

    public void AddCard()
    {
        var card = Instantiate(cardPrefab, Vector3.zero.WhereY(-5), Quaternion.identity);
        card.Setup(wordDictionary.GetRandomLetter());
        cards.Add(card);
        PositionCards();

        card.draggable.click += () =>
        {
            cards.Remove(card);
            PositionCards();
        };
        card.draggable.dropCancelled += () =>
        {
            cards.Add(card);
            PositionCards();
        };
        card.draggable.dropped += _ =>
        {
            field.AddCard(card);
            AddCard();
        };
    }
    
    private void PositionCards()
    {
        var basePos = transform.position + (cards.Count - 1) * 0.5f * Vector3.left;
        var index = 0;
        foreach (var c in cards)
        {
            if (!c.draggable.IsDragging)
            {
                Tweener.MoveToBounceOut(c.transform, basePos + index * Vector3.right, 0.3f);    
            }
            
            index++;
        }
    }

    private void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(KeyCode.A))
        {
            AddCard();
        }
    }
}