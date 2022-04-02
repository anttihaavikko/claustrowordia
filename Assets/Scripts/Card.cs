using System;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;
using AnttiStarterKit.Visuals;
using TMPro;
using UnityEngine;

public class Card : MonoBehaviour
{
    public Draggable draggable;
    public CardHover hoverer;

    [SerializeField] private TMP_Text letterField;
    [SerializeField] private SpriteRenderer sprite, shineSprite;
    [SerializeField] private Color shadowColor;
    [SerializeField] private Transform wrapper;

    public string Letter { get; private set; }
    public bool Matched { get; private set; }

    private float shake;

    public void Setup(string letter)
    {
        Letter = letter;
        letterField.text = letter.ToUpper();

        draggable.dropped += Dropped;
        draggable.click += Clicked;
        draggable.dropCancelled += Cancelled;
    }

    public void Colorize(Color color)
    {
        Matched = true;
        sprite.color = color * shadowColor;
        shineSprite.color = color;
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
    
    public void Shake(float amount)
    {
        shake = amount;
    }

    private void Update()
    {
        if (shake > 0)
        {
            wrapper.localPosition = Vector3.zero.RandomOffset(shake);
            shake = Mathf.Max(0, shake - Time.deltaTime);
            return;
        }

        wrapper.localPosition = Vector3.zero;
    }

    public void Explode()
    {
        EffectManager.AddEffect(1, transform.position);
        EffectCamera.Effect(0.2f);
        gameObject.SetActive(false);
    }
}