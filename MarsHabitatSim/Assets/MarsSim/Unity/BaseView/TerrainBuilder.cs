using UnityEngine;

namespace MarsSim.UnityApp.BaseView
{
    /// <summary>
    /// Procedural Mars terrain: fBm relief with a graded, flattened settlement zone in the
    /// middle, crater dressing further out, and a noise-generated albedo so nothing here
    /// depends on imported assets.
    /// </summary>
    public sealed class TerrainBuilder : MonoBehaviour
    {
        public const float Extent = 3200f;   // total width, meters
        private const int Res = 300;         // vertices per side
        private const float FlatRadius = 260f;

        public static float HeightAt(float x, float z)
        {
            float h = 0;
            // Large-scale rolling relief.
            h += Mathf.PerlinNoise(x * 0.0008f + 31.7f, z * 0.0008f + 11.3f) * 26f;
            h += Mathf.PerlinNoise(x * 0.003f + 7.1f, z * 0.003f + 3.9f) * 7f;
            h += Mathf.PerlinNoise(x * 0.012f + 1.7f, z * 0.012f + 9.2f) * 1.6f;
            h -= 18f;

            // A few craters, deterministic positions away from the base.
            h += Crater(x, z, 620, 540, 130, 14);
            h += Crater(x, z, -780, 300, 170, 18);
            h += Crater(x, z, 240, -900, 200, 22);
            h += Crater(x, z, -420, -680, 90, 10);
            h += Crater(x, z, 980, -260, 110, 12);

            // Graded settlement zone: blend to a flat pad.
            float d = Mathf.Sqrt(x * x + z * z);
            float flat = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - FlatRadius) / 220f));
            return Mathf.Lerp(0f, h, flat);
        }

        private static float Crater(float x, float z, float cx, float cz, float radius, float depth)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz)) / radius;
            if (d > 1.6f) return 0;
            float bowl = -depth * Mathf.Exp(-d * d * 3.2f);
            float rim = depth * 0.35f * Mathf.Exp(-(d - 1.05f) * (d - 1.05f) * 14f);
            return bowl + rim;
        }

        private void Start()
        {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var verts = new Vector3[Res * Res];
            var uvs = new Vector2[Res * Res];
            for (int j = 0; j < Res; j++)
            for (int i = 0; i < Res; i++)
            {
                float x = (i / (Res - 1f) - 0.5f) * Extent;
                float z = (j / (Res - 1f) - 0.5f) * Extent;
                verts[j * Res + i] = new Vector3(x, HeightAt(x, z), z);
                uvs[j * Res + i] = new Vector2(i / (Res - 1f), j / (Res - 1f)) * 24f;
            }

            var tris = new int[(Res - 1) * (Res - 1) * 6];
            int t = 0;
            for (int j = 0; j < Res - 1; j++)
            for (int i = 0; i < Res - 1; i++)
            {
                int v = j * Res + i;
                tris[t++] = v; tris[t++] = v + Res; tris[t++] = v + 1;
                tris[t++] = v + 1; tris[t++] = v + Res; tris[t++] = v + Res + 1;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Terrain");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = BuildMaterial();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Material BuildMaterial()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = "MarsRegolith",
            };
            mat.SetFloat("_Smoothness", 0.02f);
            mat.SetFloat("_Metallic", 0f);
            mat.mainTexture = BuildAlbedo();
            mat.color = Color.white;
            return mat;
        }

        private static Texture2D BuildAlbedo()
        {
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat };
            var c1 = new Color(0.62f, 0.36f, 0.22f);  // ochre
            var c2 = new Color(0.48f, 0.26f, 0.16f);  // darker basalt-dust
            var c3 = new Color(0.72f, 0.46f, 0.30f);  // bright dust
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                float n2 = Mathf.PerlinNoise(x * 0.09f + 40, y * 0.09f + 17);
                float n3 = Mathf.PerlinNoise(x * 0.3f + 80, y * 0.3f + 51);
                var c = Color.Lerp(c2, c1, n);
                c = Color.Lerp(c, c3, n2 * 0.45f);
                c *= 0.92f + 0.16f * n3;
                px[y * size + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }
    }
}
