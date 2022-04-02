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
    [SerializeField] private Transform dropPreview;

    private List<Card> cards = new();

    private void Start()
    {
        for (var i = 0; i < 7; i++)
        {
            AddCard();
        }
    }

    public void SetState(bool canAct)
    {
        cards.ForEach(c => c.draggable.DropLocked = !canAct);
    }

    public void AddCard()
    {
        var card = Instantiate(cardPrefab, Vector3.zero.WhereY(-5), Quaternion.identity);
        card.Setup(wordDictionary.GetRandomLetter());
        cards.Add(card);
        card.draggable.DropLocked = cards.First().draggable.DropLocked;
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
            dropPreview.gameObject.SetActive(false);
            field.AddCard(card);
            AddCard();
        };

        card.draggable.preview += ShowPreview;
        card.draggable.hidePreview += () => dropPreview.gameObject.SetActive(false);
    }

    private void ShowPreview(Draggable draggable)
    {
        dropPreview.gameObject.SetActive(true);
        // Tweener.MoveToBounceOut(dropPreview, draggable.GetRoundedPos(), 0.2f);
        dropPreview.transform.position = draggable.GetRoundedPos();
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