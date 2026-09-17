using Kitchen;
using UnityEngine;

namespace Stations
{
    public class RefrigeratorStation : BaseStation
    {
        [Header("Refrigerator UI")]
        [SerializeField] private RefrigeratorUI refrigeratorUI;

        protected override void Awake()
        {
            base.Awake();
            FindOrSetupUI();
        }

        private void FindOrSetupUI()
        {
            if (refrigeratorUI == null)
            {
                refrigeratorUI = GetComponentInChildren<RefrigeratorUI>(true);
            }
        }

        public void SetRefrigeratorUI(RefrigeratorUI ui)
        {
            refrigeratorUI = ui;
        }

        public override bool CanInteract(PlayerInteractor interactor)
        {
            FindOrSetupUI();

            // If panel is already open, player can interact to close it
            if (refrigeratorUI != null && refrigeratorUI.IsOpen)
            {
                return true;
            }

            // Cannot open if player's hands are full
            return !interactor.IsHoldingItem;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            FindOrSetupUI();

            if (refrigeratorUI != null)
            {
                if (refrigeratorUI.IsOpen)
                {
                    refrigeratorUI.Close();
                }
                else if (!interactor.IsHoldingItem)
                {
                    refrigeratorUI.Open(interactor, this);
                }
            }
        }

        public override string GetInteractionPrompt(PlayerInteractor interactor)
        {
            FindOrSetupUI();

            if (refrigeratorUI != null && refrigeratorUI.IsOpen)
            {
                return "Pick [1] Meat, [2] Cheese, [3] Veggies, or Close [E]";
            }

            if (interactor.IsHoldingItem)
            {
                return "Hands full";
            }

            return "Open Refrigerator [E]";
        }
    }
}
