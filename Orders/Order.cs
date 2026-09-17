using System;
using System.Collections.Generic;
using Kitchen;

namespace Orders
{
    [Serializable]
    public class Order
    {
        public int id;
        public string orderName;
        public List<IngredientType> requiredIngredients;
        public List<IngredientType> fulfilledIngredients;
        public List<bool> fulfilledIndices;
        public int baseScoreValue;
        public float timeElapsed;

        public int scoreValue => baseScoreValue;

        public Order(int id, string orderName, List<IngredientType> required, int scoreValue = 0)
        {
            this.id = id;
            this.orderName = orderName;
            this.requiredIngredients = new List<IngredientType>(required);
            this.fulfilledIngredients = new List<IngredientType>();
            this.fulfilledIndices = new List<bool>();
            for (int i = 0; i < this.requiredIngredients.Count; i++)
            {
                this.fulfilledIndices.Add(false);
            }
            this.timeElapsed = 0f;

            if (scoreValue > 0)
            {
                this.baseScoreValue = scoreValue;
            }
            else
            {
                this.baseScoreValue = CalculateBaseScore(this.requiredIngredients);
            }
        }

        public static int GetIngredientScoreValue(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Meat:
                case IngredientType.CookedPatty:
                    return 30;

                case IngredientType.Vegetables:
                case IngredientType.SlicedVegetables:
                case IngredientType.Tomato:
                case IngredientType.SlicedTomato:
                    return 20;

                case IngredientType.Cheese:
                case IngredientType.SlicedCheese:
                    return 10;

                default:
                    return 10;
            }
        }

        public static string GetOrderDisplayName(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.Meat:
                case IngredientType.CookedPatty:
                    return "Meat";

                case IngredientType.Vegetables:
                case IngredientType.SlicedVegetables:
                case IngredientType.Tomato:
                case IngredientType.SlicedTomato:
                    return "Vegetables";

                case IngredientType.Cheese:
                case IngredientType.SlicedCheese:
                    return "Cheese";

                default:
                    return IngredientDatabase.GetInfo(type).displayName;
            }
        }

        public static int CalculateBaseScore(List<IngredientType> ingredients)
        {
            int total = 0;
            foreach (var ing in ingredients)
            {
                total += GetIngredientScoreValue(ing);
            }
            return total;
        }

        public int GetFinalScore()
        {
            int secondsTaken = (int)Math.Floor(timeElapsed);
            return baseScoreValue - secondsTaken;
        }

        public bool IsFulfilled(int index)
        {
            EnsureFulfilledIndices();
            if (index >= 0 && index < fulfilledIndices.Count)
            {
                return fulfilledIndices[index];
            }
            return false;
        }

        private void EnsureFulfilledIndices()
        {
            if (fulfilledIndices == null)
            {
                fulfilledIndices = new List<bool>();
            }
            while (fulfilledIndices.Count < requiredIngredients.Count)
            {
                fulfilledIndices.Add(false);
            }
        }

        public bool TryDeliverIngredient(IngredientType ingredient)
        {
            EnsureFulfilledIndices();

            // Find an unfulfilled required ingredient that matches
            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                if (!fulfilledIndices[i] && MatchesRequirement(requiredIngredients[i], ingredient))
                {
                    fulfilledIndices[i] = true;
                    fulfilledIngredients.Add(requiredIngredients[i]);
                    return true;
                }
            }

            return false;
        }

        public bool IsComplete
        {
            get
            {
                EnsureFulfilledIndices();
                for (int i = 0; i < fulfilledIndices.Count; i++)
                {
                    if (!fulfilledIndices[i]) return false;
                }
                return requiredIngredients.Count > 0;
            }
        }

        public bool NeedsIngredient(IngredientType ingredient)
        {
            if (ingredient == IngredientType.None || ingredient == IngredientType.Overcooked)
            {
                return false;
            }

            EnsureFulfilledIndices();

            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                if (!fulfilledIndices[i] && MatchesRequirement(requiredIngredients[i], ingredient))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool MatchesRequirement(IngredientType required, IngredientType provided)
        {
            if (required == provided) return true;

            // Meat: CookedPatty (prepared) or Meat (collected raw)
            if ((required == IngredientType.Meat || required == IngredientType.CookedPatty) &&
                (provided == IngredientType.CookedPatty || provided == IngredientType.Meat))
            {
                return true;
            }

            // Cheese: Cheese or SlicedCheese
            if ((required == IngredientType.Cheese || required == IngredientType.SlicedCheese) &&
                (provided == IngredientType.Cheese || provided == IngredientType.SlicedCheese))
            {
                return true;
            }

            // Vegetables: SlicedVegetables (prepared) or Vegetables (collected raw)
            if ((required == IngredientType.Vegetables || required == IngredientType.SlicedVegetables ||
                 required == IngredientType.Tomato || required == IngredientType.SlicedTomato) &&
                (provided == IngredientType.SlicedVegetables || provided == IngredientType.SlicedTomato ||
                 provided == IngredientType.Vegetables || provided == IngredientType.Tomato))
            {
                return true;
            }

            return false;
        }
    }
}
