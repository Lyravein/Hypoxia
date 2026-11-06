using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypoxia
{
    public enum LSystemRulePreset
    {
        MossTendril, // Sulur lumut dinding tambang (aturan bercabang lebat)
        WallCrack    // Retakan dinding poros (aturan zig-zag/cabang tajam)
    }

    /// <summary>
    /// E5: Generator L-System prosedural berbasis aturan produksi string (Modul 2).
    /// Menghasilkan sulur lumut atau retakan dinding poros dengan parameter sudut dan panjang cabang
    /// yang dapat diatur via runtime slider. Deterministik di kedua klien.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class LSystemGenerator : MonoBehaviour
    {
        [Header("Rule Settings")]
        [SerializeField] private LSystemRulePreset preset = LSystemRulePreset.MossTendril;
        [Range(1, 4)]
        [SerializeField] private int iterations = 3;

        [Header("Geometry Parameters (Runtime Configurable)")]
        [Range(10f, 60f)]
        [SerializeField] private float branchAngle = 25f;
        [Range(0.2f, 3f)]
        [SerializeField] private float branchLength = 0.8f;
        [Range(0.02f, 0.2f)]
        [SerializeField] private float branchThickness = 0.05f;

        private Mesh mesh;
        private string cachedSentence;

        public float BranchAngle
        {
            get => branchAngle;
            set
            {
                if (!Mathf.Approximately(branchAngle, value))
                {
                    branchAngle = Mathf.Clamp(value, 10f, 60f);
                    BuildMesh();
                }
            }
        }

        public float BranchLength
        {
            get => branchLength;
            set
            {
                if (!Mathf.Approximately(branchLength, value))
                {
                    branchLength = Mathf.Clamp(value, 0.2f, 3f);
                    BuildMesh();
                }
            }
        }

        public LSystemRulePreset Preset
        {
            get => preset;
            set
            {
                if (preset != value)
                {
                    preset = value;
                    GenerateSentence();
                    BuildMesh();
                }
            }
        }

        private void Awake()
        {
            GenerateSentence();
            BuildMesh();
        }

        public static string Rewrite(string axiom, Dictionary<char, string> rules, int iter)
        {
            string current = axiom;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < iter; i++)
            {
                sb.Clear();
                foreach (char c in current)
                {
                    if (rules.TryGetValue(c, out string repl))
                    {
                        sb.Append(repl);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                current = sb.ToString();
            }
            return current;
        }

        public void GenerateSentence()
        {
            Dictionary<char, string> rules = new Dictionary<char, string>();
            string axiom;

            switch (preset)
            {
                case LSystemRulePreset.MossTendril:
                    axiom = "F";
                    // Aturan sulur lumut bercabang
                    rules['F'] = "FF+[+F-F-F]-[-F+F+F]";
                    break;

                case LSystemRulePreset.WallCrack:
                default:
                    axiom = "F";
                    // Aturan retakan bercabang tajam
                    rules['F'] = "F[+F]F[-F][F]";
                    break;
            }

            cachedSentence = Rewrite(axiom, rules, iterations);
        }

        public void BuildMesh()
        {
            if (string.IsNullOrEmpty(cachedSentence)) GenerateSentence();

            if (mesh == null)
            {
                mesh = new Mesh { name = "LSystem_Mesh" };
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            Stack<(Vector3 pos, Quaternion rot)> stateStack = new Stack<(Vector3, Quaternion)>();
            Vector3 currentPos = Vector3.zero;
            Quaternion currentRot = Quaternion.identity;

            foreach (char c in cachedSentence)
            {
                switch (c)
                {
                    case 'F':
                        Vector3 nextPos = currentPos + currentRot * (Vector3.up * branchLength);
                        AddCylinderSegment(currentPos, nextPos, branchThickness, verts, tris);
                        currentPos = nextPos;
                        break;
                    case '+':
                        currentRot *= Quaternion.Euler(0f, 0f, branchAngle);
                        break;
                    case '-':
                        currentRot *= Quaternion.Euler(0f, 0f, -branchAngle);
                        break;
                    case '[':
                        stateStack.Push((currentPos, currentRot));
                        break;
                    case ']':
                        if (stateStack.Count > 0)
                        {
                            var state = stateStack.Pop();
                            currentPos = state.pos;
                            currentRot = state.rot;
                        }
                        break;
                }
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static void AddCylinderSegment(Vector3 from, Vector3 to, float radius, List<Vector3> verts, List<int> tris)
        {
            int startIdx = verts.Count;
            Vector3 dir = (to - from).normalized;
            Vector3 side = Vector3.Cross(dir, Vector3.forward);
            if (side.sqrMagnitude < 0.001f) side = Vector3.Cross(dir, Vector3.right);
            side.Normalize();
            Vector3 normal = Vector3.Cross(dir, side).normalized;

            // 4-sided cross section segment
            Vector3 r = side * radius;
            Vector3 u = normal * radius;

            verts.Add(from + r);
            verts.Add(from + u);
            verts.Add(from - r);
            verts.Add(from - u);

            verts.Add(to + r);
            verts.Add(to + u);
            verts.Add(to - r);
            verts.Add(to - u);

            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                tris.Add(startIdx + i);
                tris.Add(startIdx + i + 4);
                tris.Add(startIdx + next);

                tris.Add(startIdx + next);
                tris.Add(startIdx + i + 4);
                tris.Add(startIdx + next + 4);
            }
        }
    }
}
