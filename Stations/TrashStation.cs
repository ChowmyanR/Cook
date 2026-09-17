using System.Collections;
using Kitchen;
using TMPro;
using UnityEngine;

namespace Stations
{
    public class TrashStation : BaseStation
    {
        private GameObject feedbackObj;
        private TMP_Text feedbackText;
        private Coroutine feedbackCoroutine;
        private Vector3 originalScale;
        private Camera mainCam;

        protected override void Awake()
        {
            base.Awake();
            mainCam = Camera.main;
            originalScale = transform.localScale;
            CreateFeedbackVisual();
        }

        private void CreateFeedbackVisual()
        {
            feedbackObj = new GameObject("TrashFeedbackUI");
            feedbackObj.transform.SetParent(transform);
            feedbackObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);

            feedbackText = feedbackObj.AddComponent<TextMeshPro>();
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 3.6f;
            feedbackText.outlineColor = Color.black;
            feedbackText.outlineWidth = 0.3f;
            feedbackObj.SetActive(false);
        }

        private void LateUpdate()
        {
            if (feedbackObj != null && feedbackObj.activeSelf)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    feedbackObj.transform.rotation = mainCam.transform.rotation;
                }
            }
        }

        public override bool CanInteract(PlayerInteractor interactor)
        {
            return interactor != null && interactor.IsHoldingItem;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor)) return;

            KitchenObject item = interactor.DropItem();
            if (item != null)
            {
                string itemName = item.IngredientType == IngredientType.Overcooked
                    ? "Burnt Food"
                    : IngredientDatabase.GetInfo(item.IngredientType).displayName;

                bool isOvercooked = item.IngredientType == IngredientType.Overcooked;

                item.DestroySelf();

                ShowDisposedFeedback(itemName, isOvercooked);
                StartCoroutine(TrashPunchAnimation());
            }
        }

        private void ShowDisposedFeedback(string itemName, bool isOvercooked)
        {
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }

            feedbackCoroutine = StartCoroutine(FeedbackRoutine(itemName, isOvercooked));
        }

        private IEnumerator FeedbackRoutine(string itemName, bool isOvercooked)
        {
            if (feedbackObj == null || feedbackText == null) yield break;

            feedbackObj.SetActive(true);
            feedbackObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);

            if (isOvercooked)
            {
                feedbackText.text = $"<color=#FF4444><b>Trashed {itemName}!</b></color>";
            }
            else
            {
                feedbackText.text = $"<color=#FFAA44><b>Trashed {itemName}</b></color>";
            }

            float elapsed = 0f;
            float duration = 1.6f;
            Vector3 startPos = new Vector3(0f, 2.3f, 0f);
            Vector3 targetPos = new Vector3(0f, 3.2f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                feedbackObj.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                Color c = feedbackText.color;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                feedbackText.color = c;

                yield return null;
            }

            feedbackObj.SetActive(false);
        }

        private IEnumerator TrashPunchAnimation()
        {
            // Quick bounce punch effect on trash can
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float punch = Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.15f;
                transform.localScale = new Vector3(
                    originalScale.x * (1f + punch),
                    originalScale.y * (1f - punch * 0.5f),
                    originalScale.z * (1f + punch)
                );
                yield return null;
            }

            transform.localScale = originalScale;
        }

        public override string GetInteractionPrompt(PlayerInteractor interactor)
        {
            if (interactor == null) return "Trash Can";

            if (interactor.IsHoldingItem)
            {
                if (interactor.HeldItem.IngredientType == IngredientType.Overcooked)
                {
                    return "<color=#FF4444>Trash Overcooked Food [E]</color>";
                }

                string name = IngredientDatabase.GetInfo(interactor.HeldItem.IngredientType).displayName;
                return $"<color=#FF8866>Trash {name} [E]</color>";
            }

            return "Trash Can (Hold item to dispose)";
        }
    }
}
