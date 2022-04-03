using System;
using AnttiStarterKit.Animations;
using UnityEngine;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;

public class Mascot : MonoBehaviour
{
    [SerializeField] private SpeechBubble bubble;
    [SerializeField] private Transform mouth;

    private Animator anim;
    
    private static readonly int DuckTrigger = Animator.StringToHash("duck");
    private static readonly int JumpTrigger = Animator.StringToHash("jump");

    private void Start()
    {
        anim = GetComponent<Animator>();
        bubble.onVocal += OpenMouth;
        bubble.onWord += () => AudioManager.Instance.PlayEffectFromCollection(0, mouth.position);
    }

    private void OpenMouth()
    {
        Tweener.ScaleToBounceOut(mouth, Vector3.one * 1.1f, 0.05f);
        this.StartCoroutine(() =>
        {
            Tweener.ScaleToQuad(mouth, Vector3.one * 0.4f, 0.1f);
        }, 0.1f);
    }

    private void Update()
    {
        if (!Application.isEditor) return;
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Duck();
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            Jump();
        }
    }

    public void Duck()
    {
        anim.SetTrigger(DuckTrigger);
    }

    public void Jump()
    {
        anim.SetTrigger(JumpTrigger);
    }
}