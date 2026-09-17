using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kitchen
{
    public enum StationType
    {
        None,
        Refrigerator,
        Stove,
        Table,
        Trash,
        CustomerWindow
    }

    [Serializable]
    public class IngredientInfo
    {
        public IngredientType type;
        public string displayName;
        public Color visualColor;
        public bool isPrepared;
        public bool requiresPreparation;
        public StationType prepStation;
        public IngredientType prepResult;
        public float prepDuration;

        public int scoreValue;

        public IngredientInfo(
            IngredientType type,
            string displayName,
            Color visualColor,
            bool isPrepared,
            bool requiresPreparation,
            StationType prepStation,
            IngredientType prepResult,
            float prepDuration = 3f,
            int scoreValue = 0)
        {
            this.type = type;
            this.displayName = displayName;
            this.visualColor = visualColor;
            this.isPrepared = isPrepared;
            this.requiresPreparation = requiresPreparation;
            this.prepStation = prepStation;
            this.prepResult = prepResult;
            this.prepDuration = prepDuration;
            this.scoreValue = scoreValue;
        }
    }

    public static class IngredientDatabase
    {
        private static readonly Dictionary<IngredientType, IngredientInfo> Ingredients =
            new Dictionary<IngredientType, IngredientInfo>
            {
                {
                    IngredientType.Meat,
                    new IngredientInfo(
                        IngredientType.Meat,
                        "Raw Meat",
                        new Color(0.92f, 0.48f, 0.48f), // Pale red color
                        isPrepared: false,
                        requiresPreparation: true,
                        prepStation: StationType.Stove,
                        prepResult: IngredientType.CookedPatty,
                        prepDuration: 6.0f,
                        scoreValue: 30
                    )
                },
                {
                    IngredientType.CookedPatty,
                    new IngredientInfo(
                        IngredientType.CookedPatty,
                        "Cooked Meat",
                        new Color(0.32f, 0.16f, 0.08f), // Dark brown color
                        isPrepared: true,
                        requiresPreparation: false,
                        prepStation: StationType.None,
                        prepResult: IngredientType.None,
                        scoreValue: 30
                    )
                },
                {
                    IngredientType.Overcooked,
                    new IngredientInfo(
                        IngredientType.Overcooked,
                        "Overcooked Food",
                        new Color(0.12f, 0.12f, 0.12f), // Charcoal burnt black
                        isPrepared: false, // Cannot be served!
                        requiresPreparation: false,
                        prepStation: StationType.None,
                        prepResult: IngredientType.None,
                        scoreValue: 0
                    )
                },
                {
                    IngredientType.Cheese,
                    new IngredientInfo(
                        IngredientType.Cheese,
                        "Cheese",
                        new Color(1.0f, 0.88f, 0.12f), // Yellow color
                        isPrepared: true,
                        requiresPreparation: false,
                        prepStation: StationType.Table,
                        prepResult: IngredientType.SlicedCheese,
                        prepDuration: 2.0f,
                        scoreValue: 10
                    )
                },
                {
                    IngredientType.SlicedCheese,
                    new IngredientInfo(
                        IngredientType.SlicedCheese,
                        "Cheese",
                        new Color(1.0f, 0.88f, 0.12f),
                        isPrepared: true,
                        requiresPreparation: false,
                        prepStation: StationType.None,
                        prepResult: IngredientType.None,
                        scoreValue: 10
                    )
                },
                {
                    IngredientType.Vegetables,
                    new IngredientInfo(
                        IngredientType.Vegetables,
                        "Vegetables",
                        new Color(0.12f, 0.65f, 0.20f), // Rich green color
                        isPrepared: false,
                        requiresPreparation: true,
                        prepStation: StationType.Table,
                        prepResult: IngredientType.SlicedVegetables,
                        prepDuration: 2.0f,
                        scoreValue: 20
                    )
                },
                {
                    IngredientType.SlicedVegetables,
                    new IngredientInfo(
                        IngredientType.SlicedVegetables,
                        "Chopped Vegetables",
                        new Color(0.60f, 0.95f, 0.45f), // Light green color
                        isPrepared: true,
                        requiresPreparation: false,
                        prepStation: StationType.None,
                        prepResult: IngredientType.None,
                        scoreValue: 20
                    )
                },
                {
                    IngredientType.Tomato,
                    new IngredientInfo(
                        IngredientType.Tomato,
                        "Vegetables",
                        new Color(0.12f, 0.65f, 0.20f),
                        isPrepared: false,
                        requiresPreparation: true,
                        prepStation: StationType.Table,
                        prepResult: IngredientType.SlicedVegetables,
                        prepDuration: 2.0f
                    )
                },
                {
                    IngredientType.SlicedTomato,
                    new IngredientInfo(
                        IngredientType.SlicedTomato,
                        "Chopped Vegetables",
                        new Color(0.60f, 0.95f, 0.45f),
                        isPrepared: true,
                        requiresPreparation: false,
                        prepStation: StationType.None,
                        prepResult: IngredientType.None
                    )
                },
                {
                    IngredientType.Bun,
                    new IngredientInfo(
                        IngredientType.Bun,
                        "Burger Bun",
                        new Color(0.93f, 0.74f, 0.45f),
                        isPrepared: true,
                        requiresPreparation: false,
                        prepStation: StationType.None,
                        prepResult: IngredientType.None
                    )
                }
            };

        public static IngredientInfo GetInfo(IngredientType type)
        {
            if (Ingredients.TryGetValue(type, out var info))
            {
                return info;
            }

            return new IngredientInfo(
                type,
                type.ToString(),
                Color.white,
                isPrepared: false,
                requiresPreparation: false,
                prepStation: StationType.None,
                prepResult: IngredientType.None
            );
        }

        public static bool CanPrepareAt(IngredientType type, StationType station)
        {
            var info = GetInfo(type);
            return info.requiresPreparation && info.prepStation == station && info.prepResult != IngredientType.None;
        }

        public static IngredientType GetPreparedResult(IngredientType type)
        {
            return GetInfo(type).prepResult;
        }
    }
}
