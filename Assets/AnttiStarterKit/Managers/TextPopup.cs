using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AnttiStarterKit.Managers
{
    public class TextPopup : MonoBehaviour
    {
        [SerializeField] private List<TMP_Text> texts;
        
        private Animator anim;
        private int defaultState;
        private float duration = 5f;

        private void Awake()
        {
            anim = GetComponent<Animator>();

            if (!anim) return;
            var info = anim.GetCurrentAnimatorStateInfo(0);
            defaultState = info.shortNameHash;
            duration = info.length;

            if (duration <= 0f)
            {
                Debug.LogError("Animation duration is 0");
            }
        }

        public void Play(string content)
        {
            texts.ForEach(t => t.text = content);
            Invoke(nameof(Done), duration);

            if (!anim) return;
            anim.Play(defaultState, -1, 0);
        }

        private void Done()
        {
            EffectManager.Instance.ReturnToPool(this);
        }
    }
}