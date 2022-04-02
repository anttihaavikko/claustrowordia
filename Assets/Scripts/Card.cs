using System;
using TMPro;
using UnityEngine;

public class Card : MonoBehaviour
{
    public Draggable draggable;
    public CardHover hoverer;

    [SerializeField] private TMP_Text letterField;

    public string Letter { get; private set; }

    public void Setup(string letter)
    {
        Letter = letter;
        letterField.text = letter.ToUpper();

        draggable.dropped += Dropped;
        draggable.click += Clicked;
        draggable.dropCancelled += Cancelled;
    }

    private void Cancelled()
    {
        hoverer.enabled = true;
    }

    private void Clicked()
    {
        hoverer.Disable();
    }

    private void Dropped(Draggable obj)
    {
        hoverer.Disable();
        draggable.dropped -= Dropped;
        draggable.click -= Clicked;
        draggable.dropCancelled -= Cancelled;
    }
}