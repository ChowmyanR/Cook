using Kitchen;
using UnityEngine;

namespace Stations
{
    public abstract class BaseStation : MonoBehaviour, IInteractable
    {
        [Header("Counter Setup")]
        [SerializeField] protected Transform holdPoint;

        protected KitchenObject currentItem;
        protected MeshRenderer[] meshRenderers;
        protected Color[] originalColors;

        public bool HasItem => currentItem != null;
        public KitchenObject CurrentItem => currentItem;

        protected virtual void Awake()
        {
            if (holdPoint == null)
            {
                // Create a default hold point above the counter
                GameObject hp = new GameObject("HoldPoint");
                hp.transform.SetParent(transform);

                Transform topChild = transform.Find("Tabletop") ?? transform.Find("Stovetop");
                if (topChild != null)
                {
                    hp.transform.position = topChild.position + Vector3.up * 0.4f;
                }
                else
                {
                    Collider col = GetComponent<Collider>();
                    float yOffset = col != null ? col.bounds.extents.y + 0.3f : 1.2f;
                    hp.transform.localPosition = new Vector3(0f, yOffset, 0f);
                }

                holdPoint = hp.transform;
            }

            meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }

        public virtual bool CanInteract(PlayerInteractor interactor)
        {
            return true;
        }

        public abstract void Interact(PlayerInteractor interactor);

        public virtual string GetInteractionPrompt(PlayerInteractor interactor)
        {
            return "Interact [E]";
        }

        public virtual bool PlaceItem(KitchenObject item)
        {
            if (HasItem || item == null)
            {
                return false;
            }

            currentItem = item;
            item.transform.SetParent(holdPoint);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            return true;
        }

        public virtual KitchenObject TakeItem()
        {
            if (!HasItem)
            {
                return null;
            }

            KitchenObject item = currentItem;
            currentItem = null;
            return item;
        }

        public virtual void SetSelected(bool isSelected)
        {
            // Visual highlight indicator can be applied here or via prompt
        }
    }
}
