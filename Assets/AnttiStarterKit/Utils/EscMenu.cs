using System;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Managers;
using AnttiStarterKit.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace AnttiStarterKit.Utils
{
    public class EscMenu : Appearer
    {
        [SerializeField] private SoundCollection toggleSound;
        
        private bool visible;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (toggleSound)
            {
                AudioManager.Instance.PlayEffectFromCollection(toggleSound, Vector3.zero);
            }
            
            visible = !visible;

            if (visible)
            {
                Show();
                return;
            }

            Hide();
        }
    }
}