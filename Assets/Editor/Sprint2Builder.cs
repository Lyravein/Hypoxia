using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hypoxia.EditorTools
{
    // Authoring pass for Sprint 2 (G1/B1/B2/E3). Adds objects to Hypoxia_Greybox.
    public static class Sprint2Builder
    {
        private const string Root = "Hypoxia_Greybox";
        private const string LitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat";

        private const string LevelName = "Sprint2_Level";

        [MenuItem("Hypoxia/Build Sprint 2 Level")]
        public static void Build()
        {
            Transform greybox = GameObject.Find(Root).transform;
            Transform previous = greybox.Find(LevelName);
            if (previous != null) Object.DestroyImmediate(previous.gameObject);

            GameObject level = new GameObject(LevelName);
            level.transform.SetParent(greybox, false);
            Transform root = level.transform;

            Material wall = AssetDatabase.LoadAssetAtPath<Material>(LitMaterial);

            // Prolog -> Sector 1 stays an open doorway; the sekat gate guards Sector 1 -> Sector 2 (GDD P2).
            BuildDermaga(root, wall);
            BuildLaboratory(root, wall);
            GateController gate = BuildGate(root, wall, "Gate_Sector2", new Vector3(30f, 2f, 0f), new Vector3(0.5f, 4f, 2f));
            BuildShaftAndStation(root, wall);
            BuildPuzzle(root, wall, gate);

            MarkStatic(root);
            // Unity 6 has no per-quality occlusion toggle: baked data + Camera.useOcclusionCulling is enough.
            StaticOcclusionCulling.Compute();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Sprint 2 level built.");
        }

        private static void BuildDermaga(Transform root, Material wall)
        {
            // Prolog stays roofless (GDD 9.1: dermaga berkabut saat senja), so the open-air
            // zone keeps covering it and exposure only starts once the player enters the lab.
            Box(root, "Ground_Dermaga", new Vector3(0f, 0f, 0f), new Vector3(16f, 0.5f, 16f), wall);

            GameObject openAir = GameObject.Find("Zone_OpenAir");
            if (openAir != null)
            {
                openAir.transform.position = new Vector3(0f, 2f, 0f);
                if (openAir.TryGetComponent(out BoxCollider box)) box.size = new Vector3(16f, 5f, 16f);
            }
        }

        private static void BuildLaboratory(Transform root, Material wall)
        {
            Box(root, "Ground_Lab", new Vector3(19f, 0f, 0f), new Vector3(22f, 0.5f, 16f), wall);
            Box(root, "Ceiling_Lab", new Vector3(19f, 4f, 0f), new Vector3(22f, 0.5f, 16f), wall);
            Box(root, "Wall_Lab_N", new Vector3(19f, 2f, 8f), new Vector3(22f, 4f, 0.5f), wall);
            Box(root, "Wall_Lab_S", new Vector3(19f, 2f, -8f), new Vector3(22f, 4f, 0.5f), wall);
            Box(root, "Wall_Lab_E_A", new Vector3(30f, 2f, 4.5f), new Vector3(0.5f, 4f, 7f), wall);
            Box(root, "Wall_Lab_E_B", new Vector3(30f, 2f, -4.5f), new Vector3(0.5f, 4f, 7f), wall);
            Box(root, "Wall_Lab_E_Lintel", new Vector3(30f, 3.5f, 0f), new Vector3(0.5f, 1f, 2f), wall);
            Box(root, "Bench_Microscope", new Vector3(15f, 0.6f, 5f), new Vector3(3f, 1.2f, 1f), wall);
            Box(root, "Cabinet_Archive", new Vector3(24f, 1.2f, 6f), new Vector3(2f, 2.4f, 0.8f), wall);
        }

        private static void BuildShaftAndStation(Transform root, Material wall)
        {
            Box(root, "Ground_Shaft", new Vector3(45f, 0f, 0f), new Vector3(30f, 0.5f, 6f), wall);
            Box(root, "Ceiling_Shaft", new Vector3(45f, 4f, 0f), new Vector3(30f, 0.5f, 6f), wall);
            Box(root, "Wall_Shaft_N", new Vector3(45f, 2f, 3f), new Vector3(30f, 4f, 0.5f), wall);
            Box(root, "Wall_Shaft_S", new Vector3(45f, 2f, -3f), new Vector3(30f, 4f, 0.5f), wall);
            Box(root, "Pillar_Shaft_1", new Vector3(40f, 2f, 1.5f), new Vector3(0.6f, 4f, 0.6f), wall);
            Box(root, "Pillar_Shaft_2", new Vector3(44f, 2f, -1.5f), new Vector3(0.6f, 4f, 0.6f), wall);
            Box(root, "Rail_Cart", new Vector3(48f, 0.6f, 0f), new Vector3(2.4f, 1f, 1.4f), wall);

            Box(root, "Ground_Station", new Vector3(66f, 0f, 0f), new Vector3(12f, 0.5f, 12f), wall);
            Box(root, "Ceiling_Station", new Vector3(66f, 5f, 0f), new Vector3(12f, 0.5f, 12f), wall);
            Box(root, "Wall_Station_N", new Vector3(66f, 2.5f, 6f), new Vector3(12f, 5f, 0.5f), wall);
            Box(root, "Wall_Station_S", new Vector3(66f, 2.5f, -6f), new Vector3(12f, 5f, 0.5f), wall);
            Box(root, "Wall_Station_E", new Vector3(72f, 2.5f, 0f), new Vector3(0.5f, 5f, 12f), wall);
            Box(root, "Blower_Main", new Vector3(70f, 2.5f, 0f), new Vector3(2f, 5f, 5f), wall);
        }

        private static GateController BuildGate(Transform root, Material wall, string name, Vector3 pos, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;

            GameObject visual = Box(go.transform, "GateVisual", Vector3.zero, size, wall);
            BoxCollider blocker = go.AddComponent<BoxCollider>();
            blocker.size = size;

            go.AddComponent<NetworkObject>();
            GateController gate = go.AddComponent<GateController>();
            SetReference(gate, "visual", visual);
            SetReference(gate, "blockingCollider", blocker);
            return gate;
        }

        private static void BuildPuzzle(Transform root, Material wall, GateController gate)
        {
            // P2 Tuas Listrik: three objects, only the middle one is the real lever.
            Vector3[] positions =
            {
                new Vector3(16f, 0f, -4f),
                new Vector3(19f, 0f, -4f),
                new Vector3(22f, 0f, -4f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject entity = new GameObject("Illusion_Entity_" + (i + 1));
                entity.transform.SetParent(root, false);
                entity.transform.position = positions[i];

                BoxCollider hitbox = entity.AddComponent<BoxCollider>();
                hitbox.center = new Vector3(0f, 1f, 0f);
                hitbox.size = new Vector3(1.2f, 2f, 1.2f);

                GameObject[] forms =
                {
                    Form(entity.transform, "Form_Normal", new Vector3(0.3f, 1.8f, 0.3f), new Color(0.55f, 0.55f, 0.58f), wall),
                    Form(entity.transform, "Form_Distorted", new Vector3(0.8f, 1.4f, 0.8f), new Color(0.85f, 0.6f, 0.15f), wall),
                    Form(entity.transform, "Form_Hallucinating", new Vector3(0.6f, 2.4f, 0.6f), new Color(0.9f, 0.25f, 0.2f), wall)
                };

                entity.AddComponent<NetworkObject>();
                IllusionEntity illusion = entity.AddComponent<IllusionEntity>();
                SetReferenceArray(illusion, "formsByBand", forms);
                SetBool(illusion, "isCorrectChoice", i == 1);
                SetReference(illusion, "gateToOpen", gate);
            }
        }

        private static GameObject Form(Transform parent, string name, Vector3 size, Color color, Material baseMaterial)
        {
            GameObject form = Box(parent, name, Vector3.zero, size, baseMaterial);
            form.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            Object.DestroyImmediate(form.GetComponent<Collider>());

            Material mat = new Material(baseMaterial) { name = name + "_Mat" };
            mat.color = color;
            form.GetComponent<Renderer>().sharedMaterial = mat;
            return form;
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void MarkStatic(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                // Puzzle entities and gates move or swap visuals at runtime.
                if (child.name.StartsWith("Illusion_") || child.name.StartsWith("Gate_")) continue;

                GameObjectUtility.SetStaticEditorFlags(child.gameObject,
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.ContributeGI);
            }
        }

        private static void SetReference(Object target, string name, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(name);
            if (prop == null)
            {
                Debug.LogError($"Missing serialized field '{name}' on {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferenceArray(Object target, string name, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(name);
            if (prop == null)
            {
                Debug.LogError($"Missing serialized field '{name}' on {target.GetType().Name}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string name, bool value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(name);
            if (prop == null)
            {
                Debug.LogError($"Missing serialized field '{name}' on {target.GetType().Name}");
                return;
            }
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
