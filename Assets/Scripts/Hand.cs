using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;
    [SerializeField] private WordDictionary wordDictionary;
    [SerializeField] private Field field;
    [SerializeField] private Transform dropPreview;

    private List<Card> cards = new();

    public int Size => cards.Count;

    private void Start()
    {
        StartCoroutine(DealCards());
    }

    private IEnumerator DealCards()
    {
        yield return new WaitForSeconds(0.5f);
        
        for (var i = 0; i < 7; i++)
        {
            AddCard();
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void SetState(bool canAct)
    {
        cards.ForEach(c => c.draggable.DropLocked = !canAct);
    }

    public void AddCard()
    {
        AudioManager.Instance.PlayEffectFromCollection(2, transform.position, 0.6f);
        var card = Instantiate(cardPrefab, Vector3.zero.WhereY(-5), Quaternion.identity);
        card.Setup(wordDictionary.GetRandomLetter());
        cards.Add(card);
        card.draggable.DropLocked = cards.First().draggable.DropLocked;
        PositionCards();
        AddListeners(card);
    }

    private void AddListeners(Card card)
    {
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
            field.AddCard(card, true, true);
            AudioManager.Instance.PlayEffectFromCollection(2, card.transform.position, 0.3f);

            if (!field.Undoing)
            {
                AddCard();   
            }
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
        var position = transform.position;
        AudioManager.Instance.PlayEffectFromCollection(2, position, 0.3f);
        
        var basePos = position + (cards.Count - 1) * 0.5f * Vector3.left;
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