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
    
    private Arcade arcade;

    private List<Card> cards = new();
    private Card held;
    private int prevIndex = -1;

    public int Size => cards.Count;

    public Field Field => field;
    
    private static Hand instance = null;
    public static Hand Instance {
        get { return instance; }
    }

    private void Awake()
    {
        // arcade.onReady += ArcadeReady;
        
        if (instance != null && instance != this) {
            Destroy (gameObject);
            return;
        }

        instance = this;
    }

    public void ArcadeReady(Arcade a)
    {
        arcade = a;
        field.Setup(arcade);
        arcade.AddCards(7);
    }

    public void SetState(bool canAct)
    {
        if (held)
        {
            held.draggable.DropLocked = !canAct;
        }
        cards.ForEach(c => c.draggable.DropLocked = !canAct);
    }
    
    public void SetPickState(bool canAct)
    {
        cards.ForEach(c => c.draggable.CanDrag = canAct);
    }

    public void AddCard(string letter)
    {
        AudioManager.Instance.PlayEffectFromCollection(2, transform.position, 0.6f);
        var card = Instantiate(cardPrefab, new Vector3(5, -5, 0), Quaternion.identity);
        card.Setup(letter);
        cards.Add(card);
        var sample = cards.First().draggable;
        card.draggable.DropLocked = sample.DropLocked;
        card.draggable.CanDrag = sample.CanDrag;
        PositionCards();
        AddListeners(card);
    }

    private void AddListeners(Card card)
    {
        card.draggable.click += () =>
        {
            PositionCards();
        };
        card.draggable.dropCancelled += () =>
        {
            cards.Add(held);
            if (field.Undoing)
            {
                field.ShowUndoArrow();
                return;
            }
            PositionCards();
            held = null;
        };
        card.draggable.dropped += _ =>
        {
            held = null;
            dropPreview.gameObject.SetActive(false);
            field.AddCard(card, true, true);
            AudioManager.Instance.PlayEffectFromCollection(2, card.transform.position, 0.3f);

            if (field.Undoing)
            {
                SetPickState(true);
            }

            if (!field.Undoing)
            {
                arcade.AddCards(1);
            }
        };
        card.draggable.picked += _ =>
        {
            held = card;
            field.HideUndoArrow();
            cards.Remove(card);
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
        if (field.Undoing) return;
        
        var position = transform.position;
        AudioManager.Instance.PlayEffectFromCollection(2, position, 0.3f);
        
        var basePos = position + (cards.Count - 1) * 0.5f * Vector3.left;
        var index = 0;
        foreach (var c in cards.OrderBy(c => c.transform.position.x))
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
        if (!held) return;
        
        var ordered = cards.OrderBy(c => c.transform.position.x).ToList();
        var index = ordered.IndexOf(held);

        if (index == prevIndex) return;
        
        prevIndex = index;
        PositionCards();
    }
}