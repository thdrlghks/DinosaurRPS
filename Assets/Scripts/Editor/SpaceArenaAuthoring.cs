using System;
using System.Collections.Generic;
using System.Linq;
using Environment;
using Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Builds the saved, URP-compatible environment for the final human battle.</summary>
public static class SpaceArenaAuthoring
{
    private const string AssetFolder = "Assets/MapAssets/HumanSpace";
    private const string RootName = "Human Space Arena";
    private static readonly Color Cyan = new Color(0.06f, 0.75f, 1f);
    private static readonly Color Magenta = new Color(0.9f, 0.22f, 0.7f);

    [MenuItem("Tools/Dinosaur RPS/Build Human Space Arena")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Open the target final-battle scene before building its arena.");

        EnsureFolder(AssetFolder);
        EnsureFolder(AssetFolder + "/Materials");
        EnsureFolder(AssetFolder + "/Meshes");
        foreach (var old in scene.GetRootGameObjects().Where(go => go.name == RootName))
            Object.DestroyImmediate(old);

        var manager = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<TournamentGameManager>(true)).FirstOrDefault();
        Animator player = null, opponent = null;
        if (manager != null)
        {
            var settings = new SerializedObject(manager);
            player = settings.FindProperty("_playerAnimator").objectReferenceValue as Animator;
            opponent = settings.FindProperty("_opponentAnimator").objectReferenceValue as Animator;
        }

        var center = player != null && opponent != null
            ? (player.transform.position + opponent.transform.position) * 0.5f
            : new Vector3(-1.8f, 0f, -1.8f);
        center.y = 0f;
        var root = new GameObject(RootName).transform;
        SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        root.position = center;

        var hull = Lit("Station Hull", new Color(0.075f, 0.11f, 0.17f), 0.72f, 0.56f);
        var deck = Lit("Titanium Deck", new Color(0.24f, 0.31f, 0.4f), 0.62f, 0.4f);
        var panel = Lit("Alternating Deck Panels", new Color(0.19f, 0.25f, 0.33f), 0.65f, 0.42f);
        var trim = Lit("Brushed Silver", new Color(0.48f, 0.58f, 0.69f), 0.72f, 0.5f);
        var dark = Lit("Recessed Deck", new Color(0.035f, 0.063f, 0.1f), 0.3f, 0.38f);
        var cyan = Emissive("Cyan Navigation", Cyan, 1.9f);
        var magenta = Emissive("Magenta Navigation", Magenta, 1.55f);
        var white = Emissive("Warm White Markings", new Color(0.68f, 0.85f, 0.95f), 1.1f);
        var mutedCyan = Emissive("Distant Cyan", new Color(0.07f, 0.27f, 0.4f), 1.0f);

        BuildDeck(root, hull, deck, panel, trim, dark, cyan, magenta, white);
        if (player != null) BuildFighterPad(root, "Player Launch Mark", player.transform.position - center, cyan, dark);
        if (opponent != null) BuildFighterPad(root, "Human Launch Mark", opponent.transform.position - center, magenta, dark);
        BuildOrbitals(root, hull, trim, cyan, magenta, mutedCyan);
        BuildEarth(root);
        BuildLighting(root);

        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/MapAssets/Space/Skybox.mat");
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.3f, 0.37f, 0.5f);
        RenderSettings.ambientEquatorColor = new Color(0.16f, 0.22f, 0.33f);
        RenderSettings.ambientGroundColor = new Color(0.07f, 0.09f, 0.15f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 0.45f;
        foreach (var camera in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>(true)))
        {
            // Hand render textures deliberately retain their short clipping distances.
            if (camera.targetTexture == null)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 300f);
            }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("SPACE_ARENA_BUILT: saved meshes/materials; scene remains open for final character/camera setup.");
    }

    private static void BuildDeck(Transform root, Material hull, Material deck, Material panel,
        Material trim, Material dark, Material cyan, Material magenta, Material white)
    {
        var platform = Empty("Orbital Combat Platform", root);
        var diskMesh = SaveMesh("DeckDisk", Ring(0f, 8.25f, 0.18f, 128));
        var floor = MeshObject("Walkable Titanium Deck", platform, diskMesh, deck, new Vector3(0f, -0.18f, 0f));
        floor.AddComponent<MeshCollider>().sharedMesh = diskMesh;
        MeshObject("Deep Station Hull", platform, SaveMesh("HullRing", Ring(0f, 8.45f, 0.8f, 128)), hull,
            new Vector3(0f, -0.98f, 0f));
        RingObject("Outer Silver Lip", platform, 8.12f, 8.48f, 0.09f, trim, -0.02f);
        RingObject("Continuous Cyan Edge", platform, 8.47f, 8.52f, 0.09f, cyan, -0.18f);
        RingObject("Underdeck Magenta Band", platform, 8.43f, 8.49f, 0.055f, magenta, -0.73f);
        RingObject("Combat Boundary", platform, 6.72f, 6.78f, 0.012f, cyan, 0.006f);
        RingObject("Inner Deck Seam", platform, 4.85f, 4.88f, 0.01f, dark, 0.006f);
        RingObject("Center Inlay", platform, 0f, 1.9f, 0.012f, dark, 0.006f);
        RingObject("Center Silver Ring", platform, 1.94f, 2.02f, 0.016f, trim, 0.008f);
        RingObject("Center Navigation Ring", platform, 2.08f, 2.105f, 0.012f, white, 0.009f);

        // Wedge panels add a readable industrial rhythm without texture noise.
        var panelMesh = SaveMesh("DeckPanelWedge", Ring(4.94f, 6.58f, 0.012f, 12, 21.5f));
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f;
            var piece = MeshObject("Deck Panel " + (i + 1).ToString("00"), platform,
                panelMesh, i % 2 == 0 ? panel : deck, new Vector3(0f, 0.007f, 0f));
            piece.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            var module = Empty("Perimeter Module " + (i + 1).ToString("00"), platform);
            module.localPosition = Radial(7.62f, angle, 0f);
            module.localRotation = Quaternion.Euler(0f, angle, 0f);
            Cube("Edge Armor", module, new Vector3(0f, -0.04f, 0f), new Vector3(0.8f, 0.17f, 0.5f), hull);
            Cube("Navigation Strip", module, new Vector3(0f, 0.052f, 0f), new Vector3(0.62f, 0.018f, 0.065f),
                i < 8 ? cyan : magenta);
            Cube("Outer Brace", module, new Vector3(0f, -0.52f, 0.5f), new Vector3(0.32f, 0.72f, 0.35f), trim);
        }

        for (int i = 0; i < 8; i++)
        {
            var seam = Cube("Radial Panel Seam " + i, platform, Radial(3.5f, i * 45f, 0.023f),
                new Vector3(0.018f, 0.008f, 2.45f), dark);
            seam.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
        }
        for (int i = 0; i < 4; i++)
        {
            var thruster = Empty("Suspension Engine " + (i + 1), platform);
            thruster.localPosition = Radial(6.1f, 45f + i * 90f, -1.35f);
            Cylinder("Engine Housing", thruster, Vector3.zero, new Vector3(0.75f, 0.55f, 0.75f), hull);
            Cylinder("Engine Core", thruster, new Vector3(0f, -0.55f, 0f), new Vector3(0.53f, 0.035f, 0.53f), cyan);
        }
    }

    private static void BuildFighterPad(Transform root, string name, Vector3 position, Material light, Material dark)
    {
        var pad = Empty(name, root);
        pad.localPosition = new Vector3(position.x, 0.026f, position.z);
        RingObject("Landing Circle", pad, 0.84f, 0.885f, 0.006f, light);
        for (int i = 0; i < 4; i++)
        {
            var marker = Cube("Approach Marker " + i, pad, Radial(1.02f, i * 90f, 0.003f),
                new Vector3(0.13f, 0.006f, 0.23f), light);
            marker.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
        }
    }

    private static void BuildOrbitals(Transform root, Material hull, Material trim, Material cyan,
        Material magenta, Material mutedCyan)
    {
        var orbit = Empty("Slow Orbital Dock", root);
        orbit.localPosition = new Vector3(0f, -2.8f, 0f);
        orbit.localRotation = Quaternion.Euler(7f, 0f, -5f);
        RingObject("Dock Outer Track", orbit, 11.7f, 11.94f, 0.16f, hull);
        RingObject("Dock Navigation Rail", orbit, 11.94f, 11.99f, 0.04f, mutedCyan, 0.11f);
        orbit.gameObject.AddComponent<SpaceArenaMotion>().Configure(new Vector3(0f, 0.7f, 0f));
        for (int i = 0; i < 4; i++)
        {
            var beacon = Empty("Dock Beacon " + (i + 1), orbit);
            beacon.localPosition = Radial(11.82f, 22f + i * 90f, 0f);
            Cube("Beacon Hull", beacon, Vector3.zero, new Vector3(0.7f, 0.6f, 1.4f), hull);
            Cube("Beacon Glass", beacon, new Vector3(0f, 0.32f, 0f), new Vector3(0.4f, 0.05f, 0.95f),
                i % 2 == 0 ? cyan : magenta);
        }

        var ring = Empty("Distant Station Halo", root);
        ring.localPosition = new Vector3(34f, 5f, -12f);
        ring.localRotation = Quaternion.Euler(18f, 20f, 63f);
        RingObject("Halo Structure", ring, 16.2f, 16.5f, 0.35f, hull);
        RingObject("Halo Rim", ring, 16.49f, 16.55f, 0.04f, mutedCyan, 0.32f);
        ring.gameObject.AddComponent<SpaceArenaMotion>().Configure(new Vector3(0f, -0.12f, 0f));
        for (int i = 0; i < 6; i++)
        {
            var brace = Cube("Halo Service Pod " + i, ring, Radial(16.3f, i * 60f, 0.2f),
                new Vector3(0.7f, 0.7f, 1.65f), trim);
            brace.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
        }

        var satellite = Empty("Survey Satellite", root);
        satellite.localPosition = new Vector3(30f, 10f, -19f);
        satellite.localRotation = Quaternion.Euler(18f, 30f, -22f);
        Cube("Satellite Core", satellite, Vector3.zero, new Vector3(1f, 1.5f, 1f), trim);
        Cube("Solar Array Spar", satellite, Vector3.zero, new Vector3(5.3f, 0.14f, 0.14f), hull);
        for (int side = -1; side <= 1; side += 2)
        {
            Cube("Solar Array " + side, satellite, new Vector3(side * 2f, 0f, 0f), new Vector3(2.3f, 0.06f, 2.8f), hull);
            for (int cell = 0; cell < 5; cell++)
                Cube("Solar Cell Line " + side + "-" + cell, satellite,
                    new Vector3(side * 2f, 0.035f, -1.05f + cell * 0.52f), new Vector3(2.12f, 0.012f, 0.025f), mutedCyan);
        }
        satellite.gameObject.AddComponent<SpaceArenaMotion>().Configure(new Vector3(0f, 0.45f, 0f), 0.25f, 22f);
    }

    private static void BuildEarth(Transform root)
    {
        // The older bundled Earth shader is built-in-pipeline only. Reuse its original
        // pre-shaded texture with URP Unlit so the planet is reliable in every battle camera.
        var earth = MaterialAsset("Earth URP", "Universal Render Pipeline/Unlit");
        earth.SetColor("_BaseColor", new Color(0.9f, 0.96f, 1f));
        earth.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MapAssets/Space/Earth_color_shadow.png"));
        EditorUtility.SetDirty(earth);
        var planetMesh = SaveMesh("PlanetSphere", Sphere(96, 48));
        var planet = MeshObject("Earth", root, planetMesh, earth, new Vector3(50f, 1f, 18f)).transform;
        planet.localScale = Vector3.one * 13f;
        planet.localRotation = Quaternion.Euler(0f, 145f, -19f);
        planet.gameObject.AddComponent<SpaceArenaMotion>().Configure(new Vector3(0f, 0.16f, 0f));
        var renderer = planet.GetComponent<Renderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        var atmosphereMaterial = MaterialAsset("Earth Atmosphere URP", "Universal Render Pipeline/Unlit");
        atmosphereMaterial.SetColor("_BaseColor", new Color(0.1f, 0.48f, 0.95f, 0.36f));
        atmosphereMaterial.SetFloat("_Surface", 1f);
        atmosphereMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        atmosphereMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        atmosphereMaterial.SetFloat("_ZWrite", 0f);
        atmosphereMaterial.SetFloat("_Cull", (float)CullMode.Front);
        atmosphereMaterial.SetOverrideTag("RenderType", "Transparent");
        atmosphereMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        atmosphereMaterial.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(atmosphereMaterial);
        var atmosphere = MeshObject("Thin Blue Atmosphere", planet, planetMesh, atmosphereMaterial, Vector3.zero);
        atmosphere.transform.localScale = Vector3.one * 1.012f;
        atmosphere.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        atmosphere.GetComponent<Renderer>().receiveShadows = false;
    }

    private static void BuildLighting(Transform root)
    {
        var key = Empty("Sun Key Light", root).gameObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(0.9f, 0.95f, 1f);
        key.intensity = 1.8f;
        key.transform.localRotation = Quaternion.Euler(42f, 125f, 0f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.65f;
        RenderSettings.sun = key;
        var fill = Empty("Earth Blue Fill", root).gameObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.33f, 0.65f, 1f);
        fill.intensity = 0.65f;
        fill.transform.localRotation = Quaternion.Euler(25f, 150f, 0f);
        fill.shadows = LightShadows.None;
    }

    private static Vector3 Radial(float radius, float degrees, float height)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians) * radius, height, Mathf.Cos(radians) * radius);
    }

    private static Transform Empty(string name, Transform parent)
    {
        var transform = new GameObject(name).transform;
        transform.SetParent(parent, false);
        return transform;
    }

    private static Transform Primitive(string name, PrimitiveType type, Transform parent, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go.transform;
    }

    private static Transform Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var transform = Primitive(name, PrimitiveType.Cube, parent, material);
        transform.localPosition = position;
        transform.localScale = scale;
        return transform;
    }

    private static Transform Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var transform = Primitive(name, PrimitiveType.Cylinder, parent, material);
        transform.localPosition = position;
        transform.localScale = scale;
        return transform;
    }

    private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 position)
    {
        var transform = Empty(name, parent);
        transform.localPosition = position;
        transform.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        transform.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        return transform.gameObject;
    }

    private static void RingObject(string name, Transform parent, float inner, float outer, float thickness,
        Material material, float height = 0f)
    {
        string meshName = "Ring_" + inner.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "_"
            + outer.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "_"
            + thickness.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetFolder + "/Meshes/" + meshName + ".asset");
        if (mesh == null) mesh = SaveMesh(meshName, Ring(inner, outer, thickness, 128));
        MeshObject(name, parent, mesh, material, new Vector3(0f, height, 0f));
    }

    // Closed annular prism. Separate vertices preserve crisp panel edges and flat deck normals.
    private static Mesh Ring(float inner, float outer, float thickness, int segments, float arcDegrees = 360f)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uv = new List<Vector2>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int start = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        for (int i = 0; i < segments; i++)
        {
            float a = arcDegrees * i / segments, b = arcDegrees * (i + 1) / segments;
            Vector3 ib = Radial(inner, a, 0f), ob = Radial(outer, a, 0f);
            Vector3 ib2 = Radial(inner, b, 0f), ob2 = Radial(outer, b, 0f);
            Vector3 up = Vector3.up * thickness;
            Quad(ib + up, ob + up, ob2 + up, ib2 + up);
            Quad(ib2, ob2, ob, ib);
            Quad(ob, ob2, ob2 + up, ob + up);
            if (inner > 0f) Quad(ib2, ib, ib + up, ib2 + up);
        }
        if (arcDegrees < 360f)
        {
            Quad(Radial(inner, 0f, 0f), Radial(outer, 0f, 0f), Radial(outer, 0f, thickness), Radial(inner, 0f, thickness));
            Quad(Radial(outer, arcDegrees, 0f), Radial(inner, arcDegrees, 0f),
                Radial(inner, arcDegrees, thickness), Radial(outer, arcDegrees, thickness));
        }
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh SaveMesh(string name, Mesh mesh)
    {
        mesh.name = name;
        string path = AssetFolder + "/Meshes/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh Sphere(int longitude, int latitude)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int y = 0; y <= latitude; y++)
        {
            float v = (float)y / latitude;
            float pitch = v * Mathf.PI;
            for (int x = 0; x <= longitude; x++)
            {
                float u = (float)x / longitude;
                float yaw = u * Mathf.PI * 2f;
                var vertex = new Vector3(Mathf.Sin(pitch) * Mathf.Sin(yaw), Mathf.Cos(pitch),
                    Mathf.Sin(pitch) * Mathf.Cos(yaw));
                vertices.Add(vertex);
                normals.Add(vertex);
                uv.Add(new Vector2(u, 1f - v));
            }
        }
        for (int y = 0; y < latitude; y++)
        for (int x = 0; x < longitude; x++)
        {
            int a = y * (longitude + 1) + x;
            int b = a + longitude + 1;
            triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material Lit(string name, Color color, float metallic, float smoothness)
    {
        var material = MaterialAsset(name, "Universal Render Pipeline/Lit");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Emissive(string name, Color color, float strength)
    {
        var material = Lit(name, color, 0.25f, 0.5f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * strength);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MaterialAsset(string name, string shaderName)
    {
        string path = AssetFolder + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidOperationException("Missing required shader: " + shaderName);
        if (material == null)
        {
            material = new Material(shader) { name = name, enableInstancing = true };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
}
