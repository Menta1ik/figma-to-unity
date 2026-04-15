using FrontStrike.UI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontStrike.UI.Screens
{
    public class LobbyScreen : UIScreen
    {
        [Header("Controls")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button armoryButton;

        protected override void Awake()
        {
            base.Awake();
            
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            Debug.Log("[Lobby] Play Clicked (uGUI Version)");
        }
    }
}
