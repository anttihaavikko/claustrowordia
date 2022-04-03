using System;
using AnttiStarterKit.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace AnttiStarterKit.Utils
{
    public class EscMenu : Appearer
    {
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