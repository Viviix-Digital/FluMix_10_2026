using UnityEngine;
using UnityEngine.UI;

namespace FluMix
{
    public class RulesView : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] Button continueButton;

        public void Bind(GameFlow gameFlow, Button button)
        {
            flow = gameFlow;
            continueButton = button;
            Wire();
        }

        void Awake()
        {
            Wire();
        }

        void Wire()
        {
            if (continueButton == null)
                return;
            continueButton.onClick.RemoveListener(Continue);
            continueButton.onClick.AddListener(Continue);
        }

        public void Continue()
        {
            FluMixSfx.Play("ui_confirm");
            // Fullscreen is triggered on Login/Register (user gesture), not Rules.
            if (flow != null)
                flow.StartRound();
        }
    }
}
