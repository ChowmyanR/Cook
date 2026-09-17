using System.Collections.Generic;
using Kitchen;
using Stations;
using UnityEngine;

namespace Orders
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [Header("Customer Windows")]
        [SerializeField] private CustomerWindowStation[] customerWindows;
        [SerializeField] private GameObject customerPrefab;

        private int orderIdCounter = 1;

        // Guaranteed delicious kitchen orders including Cheese, Meat, and Veggies!
        private static readonly IngredientType[][] OrderTemplates = new IngredientType[][]
        {
            new IngredientType[] { IngredientType.CookedPatty, IngredientType.Cheese },
            new IngredientType[] { IngredientType.CookedPatty, IngredientType.SlicedVegetables, IngredientType.Cheese },
            new IngredientType[] { IngredientType.Cheese, IngredientType.SlicedVegetables },
            new IngredientType[] { IngredientType.CookedPatty, IngredientType.SlicedVegetables },
            new IngredientType[] { IngredientType.CookedPatty, IngredientType.Cheese, IngredientType.Cheese },
            new IngredientType[] { IngredientType.SlicedVegetables, IngredientType.Cheese, IngredientType.CookedPatty }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (customerWindows == null || customerWindows.Length == 0)
            {
                customerWindows = FindObjectsByType<CustomerWindowStation>(FindObjectsSortMode.None);
            }

            InitializeAllOrders();
        }

        public void RegisterWindows(CustomerWindowStation[] windows, GameObject prefab)
        {
            customerWindows = windows;
            customerPrefab = prefab;
            InitializeAllOrders();
        }

        public void InitializeAllOrders()
        {
            if (customerWindows == null)
            {
                return;
            }

            for (int i = 0; i < customerWindows.Length; i++)
            {
                if (customerWindows[i] != null)
                {
                    customerWindows[i].SetWindowIndex(i);
                    if (customerPrefab != null)
                    {
                        customerWindows[i].SetCustomerPrefab(customerPrefab);
                    }
                    SpawnOrderForWindow(customerWindows[i], i);
                }
            }
        }

        public void SpawnOrderForWindow(CustomerWindowStation window, int initialSlot = -1)
        {
            if (window == null)
            {
                return;
            }

            List<IngredientType> chosenIngredients = new List<IngredientType>();

            if (initialSlot >= 0)
            {
                // Guaranteed orders at start, ensuring Cheese is prominently present on active tickets:
                // Window 0: Meat & Cheese
                // Window 1: Meat, Veggies & Cheese
                // Window 2: Cheese & Veggies
                // Window 3: Meat & Veggies
                switch (initialSlot % 4)
                {
                    case 0:
                        chosenIngredients.Add(IngredientType.CookedPatty);
                        chosenIngredients.Add(IngredientType.Cheese);
                        break;
                    case 1:
                        chosenIngredients.Add(IngredientType.CookedPatty);
                        chosenIngredients.Add(IngredientType.SlicedVegetables);
                        chosenIngredients.Add(IngredientType.Cheese);
                        break;
                    case 2:
                        chosenIngredients.Add(IngredientType.Cheese);
                        chosenIngredients.Add(IngredientType.SlicedVegetables);
                        break;
                    case 3:
                    default:
                        chosenIngredients.Add(IngredientType.CookedPatty);
                        chosenIngredients.Add(IngredientType.SlicedVegetables);
                        break;
                }
            }
            else
            {
                int randIndex = Random.Range(0, OrderTemplates.Length);
                chosenIngredients.AddRange(OrderTemplates[randIndex]);
            }

            int score = Order.CalculateBaseScore(chosenIngredients);
            string orderName = $"Order #{orderIdCounter}";

            Order newOrder = new Order(
                orderIdCounter++,
                orderName,
                chosenIngredients,
                score
            );

            window.AssignNewOrder(newOrder);
        }
    }
}
