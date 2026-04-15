using System.Collections.Generic;
using UnityEngine;

namespace FrontStrike.UI.Core
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private List<UIScreen> screens;
        [SerializeField] private UIScreen initialScreen;

        private void Start()
        {
            foreach (var screen in screens)
            {
                screen.Hide();
            }

            if (initialScreen != null)
            {
                ShowScreen(initialScreen);
            }
        }

        public void ShowScreen(UIScreen targetScreen)
        {
            foreach (var screen in screens)
            {
                if (screen == targetScreen)
                    screen.Show();
                else
                    screen.Hide();
            }
        }
    }
}
