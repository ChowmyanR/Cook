using TMPro;
using UnityEngine;

namespace Kitchen
{
    public class KitchenObject : MonoBehaviour
    {
        [SerializeField] private IngredientType ingredientType;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private GameObject visualCluster;

        public IngredientType IngredientType => ingredientType;

        public static KitchenObject Spawn(IngredientType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject go = new GameObject($"Item_{type}");
            go.transform.position = position;
            go.transform.rotation = rotation;
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }

            KitchenObject kitchenObject = go.AddComponent<KitchenObject>();
            kitchenObject.Init(type);

            return kitchenObject;
        }

        private void Init(IngredientType type)
        {
            ingredientType = type;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            UpdateVisuals();
            // Floating 3D text label removed - ingredients are visually distinguished by 3D shape and color
        }

        public void SetIngredientType(IngredientType newType)
        {
            ingredientType = newType;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            var info = IngredientDatabase.GetInfo(ingredientType);

            // Clean up any extra cluster children if switching types
            if (visualCluster != null)
            {
                Destroy(visualCluster);
                visualCluster = null;
            }

            // Material instance
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = info.visualColor;

            if (ingredientType == IngredientType.SlicedVegetables || ingredientType == IngredientType.SlicedTomato)
            {
                // Chopped vegetables: Cluster of light green small cubes!
                meshRenderer.enabled = false;

                visualCluster = new GameObject("ChoppedCubesCluster");
                visualCluster.transform.SetParent(transform, false);

                // Diced vegetable cluster: 4 base cubes + 1 top cube
                Vector3[] cubeOffsets = new Vector3[]
                {
                    new Vector3(-0.14f, 0.02f, -0.14f),
                    new Vector3(0.14f, 0.02f, -0.14f),
                    new Vector3(-0.14f, 0.02f, 0.14f),
                    new Vector3(0.14f, 0.02f, 0.14f),
                    new Vector3(0.0f, 0.20f, 0.0f)
                };

                foreach (var offset in cubeOffsets)
                {
                    GameObject subCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    subCube.transform.SetParent(visualCluster.transform, false);
                    subCube.transform.localPosition = offset;
                    subCube.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
                    Collider col = subCube.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    var rend = subCube.GetComponent<MeshRenderer>();
                    if (rend != null) rend.material = mat;
                }
            }
            else
            {
                meshRenderer.enabled = true;
                meshRenderer.material = mat;

                PrimitiveType primitiveType;
                Vector3 baseScale;

                switch (ingredientType)
                {
                    case IngredientType.Overcooked:
                        // Overcooked / Burned: Charcoal black patty
                        primitiveType = PrimitiveType.Cylinder;
                        baseScale = new Vector3(0.72f, 0.14f, 0.72f);
                        break;

                    case IngredientType.Vegetables:
                    case IngredientType.Tomato:
                        // Green big cube as specified
                        primitiveType = PrimitiveType.Cube;
                        baseScale = new Vector3(0.85f, 0.85f, 0.85f);
                        break;

                    case IngredientType.Cheese:
                    case IngredientType.SlicedCheese:
                        // Yellow cheese cube / block as specified
                        primitiveType = PrimitiveType.Cube;
                        baseScale = new Vector3(0.58f, 0.45f, 0.58f);
                        break;

                    case IngredientType.Bun:
                        primitiveType = PrimitiveType.Sphere;
                        baseScale = new Vector3(0.8f, 0.4f, 0.8f);
                        break;

                    case IngredientType.Meat:
                        // Pale red raw meat cylinder
                        primitiveType = PrimitiveType.Cylinder;
                        baseScale = new Vector3(0.72f, 0.16f, 0.72f);
                        break;

                    case IngredientType.CookedPatty:
                        // Sizzled dark brown cooked patty cylinder
                        primitiveType = PrimitiveType.Cylinder;
                        baseScale = new Vector3(0.76f, 0.20f, 0.76f);
                        break;

                    default:
                        primitiveType = PrimitiveType.Cylinder;
                        baseScale = new Vector3(0.72f, 0.18f, 0.72f);
                        break;
                }

                GameObject tempPrimitive = GameObject.CreatePrimitive(primitiveType);
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
                }
                Destroy(tempPrimitive);

                transform.localScale = baseScale;
            }
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
