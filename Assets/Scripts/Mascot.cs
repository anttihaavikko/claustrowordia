using System;
using AnttiStarterKit.Animations;
using UnityEngine;
using AnttiStarterKit.Extensions;

public class Mascot : MonoBehaviour
{
    [SerializeField] private SpeechBubble bubble;
    [SerializeField] private Transform mouth;

    private void Start()
    {
        bubble.onVocal += OpenMouth;
    }

    private void OpenMouth()
    {
        Tweener.ScaleToBounceOut(mouth, Vector3.one * 1.1f, 0.05f);
        this.StartCoroutine(() =>
        {
            Tweener.ScaleToQuad(mouth, Vector3.one * 0.4f, 0.1f);
        }, 0.1f);
    }
}