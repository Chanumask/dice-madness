using System.IO;
using DiceMadness.Core;
using DiceMadness.Dice;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DicePrototypeSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string DicePrefabPath = "Assets/Prefabs/Dice.prefab";
    private const string DiceMeshPath = "Assets/Prefabs/DiceMesh.asset";
    private const string DiceMaterialPath = "Assets/Materials/Dice.mat";
    private const string DiceAtlasTexturePath = "Assets/Materials/DiceAtlas.png";
    private const string DiceNormalTexturePath = "Assets/Materials/DiceAtlasNormal.png";
    private const string PlaySurfaceMaterialPath = "Assets/Materials/TraySurface.mat";
    private const string WallMaterialPath = "Assets/Materials/TrayWall.mat";
    private const string TableMaterialPath = "Assets/Materials/Table.mat";
    private const string DicePhysicsMaterialPath = "Assets/Materials/DicePhysics.asset";
    private const string LegacyDicePhysicsMaterialPath = "Assets/Materials/DicePhysics.physicsMaterial";
    private const int FaceTileSize = 512;
    private const int AtlasColumns = 3;
    private const int AtlasRows = 2;
    private const float DiceHalfExtent = 0.5f;
    private const float PipRadius = 0.118f;
    private const float PipEdgeSoftness = 0.0055f;
    private const float PipInsetDepth = 0.1f;

    private static readonly Color DiceBaseColor = new Color(0.975f, 0.968f, 0.945f, 1f);
    private static readonly Color DicePipColor = new Color(0.01f, 0.01f, 0.015f, 1f);
    private static readonly DiceFaceVisualDefinition[] D6FaceVisuals =
    {
        new DiceFaceVisualDefinition(1, Vector3.up, Vector3.forward, 0, 1, Vector2.zero),
        new DiceFaceVisualDefinition(2, Vector3.forward, Vector3.up, 1, 1, new Vector2(-1f, 1f), new Vector2(1f, -1f)),
        new DiceFaceVisualDefinition(3, Vector3.right, Vector3.up, 2, 1, new Vector2(-1f, 1f), Vector2.zero, new Vector2(1f, -1f)),
        new DiceFaceVisualDefinition(6, Vector3.down, Vector3.forward, 0, 0,
            new Vector2(-1f, 1f), new Vector2(-1f, 0f), new Vector2(-1f, -1f),
            new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(1f, -1f)),
        new DiceFaceVisualDefinition(5, Vector3.back, Vector3.up, 1, 0,
            new Vector2(-1f, 1f), Vector2.zero, new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, -1f)),
        new DiceFaceVisualDefinition(4, Vector3.left, Vector3.up, 2, 0,
            new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f)),
    };

    private struct DiceFaceVisualDefinition
    {
        public int value;
        public Vector3 normal;
        public Vector3 upHint;
        public int atlasColumn;
        public int atlasRow;
        public Vector2[] pipCoordinates;

        public DiceFaceVisualDefinition(
            int value,
            Vector3 normal,
            Vector3 upHint,
            int atlasColumn,
            int atlasRow,
            params Vector2[] pipCoordinates)
        {
            this.value = value;
            this.normal = normal;
            this.upHint = upHint;
            this.atlasColumn = atlasColumn;
            this.atlasRow = atlasRow;
            this.pipCoordinates = pipCoordinates;
        }
    }

    [InitializeOnLoadMethod]
    private static void ScheduleDiceVisualRefresh()
    {
        EditorApplication.delayCall += RefreshDiceVisualAssetsIfPossible;
    }

    // Builds the full prototype scene and prefab so the project opens ready to play.
    [MenuItem("Tools/Dice Prototype/Build Prototype")]
    public static void BuildPrototype()
    {
        EnsureFolders();
        DeleteLegacyPhysicsMaterial();

        Material diceMaterial = CreateOrUpdateDiceMaterial();
        Material playSurfaceMaterial = CreateOrUpdateFlatMaterial(PlaySurfaceMaterialPath, new Color(0.3f, 0.42f, 0.38f));
        Material wallMaterial = CreateOrUpdateFlatMaterial(WallMaterialPath, new Color(0.4f, 0.34f, 0.28f));
        Material tableMaterial = CreateOrUpdateFlatMaterial(TableMaterialPath, new Color(0.14f, 0.16f, 0.2f));
        PhysicsMaterial dicePhysicsMaterial = CreateOrUpdatePhysicsMaterial(DicePhysicsMaterialPath);
        Mesh diceMesh = CreateOrUpdateDiceMesh();

        GameObject dicePrefab = CreateOrUpdateDicePrefab(diceMaterial, diceMesh, dicePhysicsMaterial);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateDirectionalLight();
        CreateTray(playSurfaceMaterial, wallMaterial, tableMaterial, dicePhysicsMaterial);

        DiceRoller[] dice = CreateDiceInstances(dicePrefab);
        Canvas canvas = CreateCanvas();

        GameObject managerObject = new GameObject("DiceManager");
        DiceManager manager = managerObject.AddComponent<DiceManager>();
        manager.SetReferences(dice, null);
        PrototypeMenuSceneBuilder.BuildOrRefreshSceneUi(canvas, manager);

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void BuildPrototypeFromBatch()
    {
        BuildPrototype();
    }

    [MenuItem("Tools/Dice Prototype/Refresh Dice Visuals")]
    public static void RefreshDiceVisualAssets()
    {
        RefreshDiceVisualAssetsIfPossible();
    }

    private static void RefreshDiceVisualAssetsIfPossible()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RefreshDiceVisualAssetsIfPossible;
            return;
        }

        EnsureFolders();
        DeleteLegacyPhysicsMaterial();

        Material diceMaterial = CreateOrUpdateDiceMaterial();
        PhysicsMaterial dicePhysicsMaterial = CreateOrUpdatePhysicsMaterial(DicePhysicsMaterialPath);
        Mesh diceMesh = CreateOrUpdateDiceMesh();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(DicePrefabPath) != null)
        {
            UpdateExistingDicePrefab(diceMaterial, diceMesh, dicePhysicsMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Scripts");
        CreateFolder("Assets/Scripts", "Core");
        CreateFolder("Assets/Scripts", "Dice");
        CreateFolder("Assets", "Scenes");
        CreateFolder("Assets", "Prefabs");
        CreateFolder("Assets", "Materials");
        CreateFolder("Assets", "UI");
    }

    private static void DeleteLegacyPhysicsMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(LegacyDicePhysicsMaterialPath) != null)
        {
            AssetDatabase.DeleteAsset(LegacyDicePhysicsMaterialPath);
        }
    }

    private static void CreateFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Material CreateOrUpdateFlatMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = Path.GetFileNameWithoutExtension(path);
        material.shader = shader;
        material.color = color;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.45f);
        material.SetFloat("_Metallic", 0f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateOrUpdateDiceMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DiceMaterialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, DiceMaterialPath);
        }

        Texture2D atlasTexture = CreateOrUpdateDiceAtlasTexture(DiceAtlasTexturePath, false);
        Texture2D normalTexture = CreateOrUpdateDiceAtlasTexture(DiceNormalTexturePath, true);

        material.name = "Dice";
        material.shader = shader;
        material.color = Color.white;
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", atlasTexture);
        material.SetTexture("_MainTex", atlasTexture);
        material.SetTexture("_BumpMap", normalTexture);
        material.SetFloat("_BumpScale", 0.3f);
        material.SetFloat("_Smoothness", 0.14f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_SpecularHighlights", 0f);
        material.SetFloat("_EnvironmentReflections", 0f);
        material.EnableKeyword("_NORMALMAP");

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D CreateOrUpdateDiceAtlasTexture(string path, bool createNormalMap)
    {
        int width = AtlasColumns * FaceTileSize;
        int height = AtlasRows * FaceTileSize;
        Texture2D writableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, createNormalMap);
        writableTexture.name = Path.GetFileNameWithoutExtension(path);
        writableTexture.wrapMode = TextureWrapMode.Clamp;
        writableTexture.filterMode = FilterMode.Bilinear;
        writableTexture.anisoLevel = 0;

        Color[] pixels = createNormalMap
            ? BuildDiceNormalPixels(width, height)
            : BuildDiceAlbedoPixels(width, height);

        writableTexture.SetPixels(pixels);
        writableTexture.Apply(false, false);

        byte[] pngBytes = writableTexture.EncodeToPNG();
        File.WriteAllBytes(path, pngBytes);
        Object.DestroyImmediate(writableTexture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        ConfigureDiceTextureImporter(path, createNormalMap);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Color[] BuildDiceAlbedoPixels(int width, int height)
    {
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = DiceBaseColor;
        }

        for (int i = 0; i < D6FaceVisuals.Length; i++)
        {
            PaintDiceFaceAlbedo(pixels, width, height, D6FaceVisuals[i]);
        }

        return pixels;
    }

    private static Color[] BuildDiceNormalPixels(int width, int height)
    {
        Color[] pixels = new Color[width * height];
        Color neutral = EncodeNormal(Vector3.forward);

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = neutral;
        }

        for (int i = 0; i < D6FaceVisuals.Length; i++)
        {
            PaintDiceFaceNormal(pixels, width, height, D6FaceVisuals[i]);
        }

        return pixels;
    }

    private static void PaintDiceFaceAlbedo(Color[] pixels, int width, int height, DiceFaceVisualDefinition face)
    {
        GetTileBounds(face, out int startX, out int startY);

        for (int y = 0; y < FaceTileSize; y++)
        {
            for (int x = 0; x < FaceTileSize; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / FaceTileSize, (y + 0.5f) / FaceTileSize);
                Color color = ApplyFaceBackgroundShading(DiceBaseColor, uv);

                GetClosestPip(face, uv, out Vector2 pipDelta, out float pipDistance);
                float pipMask = GetPipMask(pipDistance);

                if (pipMask > 0f)
                {
                    float normalizedDistance = Mathf.Clamp01(pipDistance / PipRadius);
                    float bowl = Mathf.Pow(1f - normalizedDistance, 0.55f);
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(pipDistance - PipRadius) / 0.018f);
                    float directionalLight = Mathf.Clamp01(0.5f + (-pipDelta.y + pipDelta.x) * 1.6f);
                    Color cavityColor = Color.Lerp(DicePipColor, DiceBaseColor * 0.75f, directionalLight * 0.045f);

                    color = Color.Lerp(color, cavityColor, pipMask);
                    color += DiceBaseColor * (rim * directionalLight * 0.01f);
                    color -= DiceBaseColor * (bowl * 0.035f);
                }

                pixels[(startY + y) * width + startX + x] = ToSrgbColor(ClampColor(color));
            }
        }
    }

    private static void PaintDiceFaceNormal(Color[] pixels, int width, int height, DiceFaceVisualDefinition face)
    {
        GetTileBounds(face, out int startX, out int startY);

        for (int y = 0; y < FaceTileSize; y++)
        {
            for (int x = 0; x < FaceTileSize; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / FaceTileSize, (y + 0.5f) / FaceTileSize);
                GetClosestPip(face, uv, out Vector2 pipDelta, out float pipDistance);
                float pipMask = GetPipMask(pipDistance);

                Vector3 normal = Vector3.forward;

                if (pipMask > 0f)
                {
                    Vector2 direction = pipDistance > 0.0001f ? pipDelta / pipDistance : Vector2.zero;
                    float radialStrength = Mathf.Pow(1f - Mathf.Clamp01(pipDistance / PipRadius), 0.72f) * pipMask;
                    normal = new Vector3(
                        -direction.x * radialStrength * 0.35f,
                        -direction.y * radialStrength * 0.35f,
                        1f - radialStrength * PipInsetDepth).normalized;
                }

                pixels[(startY + y) * width + startX + x] = EncodeNormal(normal);
            }
        }
    }

    private static Color ApplyFaceBackgroundShading(Color baseColor, Vector2 uv)
    {
        float edge = Mathf.Min(Mathf.Min(uv.x, 1f - uv.x), Mathf.Min(uv.y, 1f - uv.y));
        float edgeShade = Mathf.Lerp(0.985f, 1f, Mathf.SmoothStep(0f, 0.14f, edge));
        float topLight = Mathf.Lerp(0.995f, 1.01f, uv.y);
        return baseColor * edgeShade * topLight;
    }

    private static float GetPipMask(float pipDistance)
    {
        return 1f - Mathf.SmoothStep(PipRadius - PipEdgeSoftness, PipRadius + PipEdgeSoftness, pipDistance);
    }

    private static void GetClosestPip(
        DiceFaceVisualDefinition face,
        Vector2 uv,
        out Vector2 closestDelta,
        out float closestDistance)
    {
        closestDelta = Vector2.zero;
        closestDistance = float.MaxValue;

        for (int i = 0; i < face.pipCoordinates.Length; i++)
        {
            Vector2 center = new Vector2(0.5f, 0.5f) + face.pipCoordinates[i] * 0.18f;
            Vector2 delta = uv - center;
            float distance = delta.magnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestDelta = delta;
            }
        }
    }

    private static void GetTileBounds(DiceFaceVisualDefinition face, out int startX, out int startY)
    {
        startX = face.atlasColumn * FaceTileSize;
        startY = face.atlasRow * FaceTileSize;
    }

    private static Color ClampColor(Color color)
    {
        color.r = Mathf.Clamp01(color.r);
        color.g = Mathf.Clamp01(color.g);
        color.b = Mathf.Clamp01(color.b);
        color.a = 1f;
        return color;
    }

    private static Color ToSrgbColor(Color color)
    {
        Color gammaColor = color.gamma;
        gammaColor.a = color.a;
        return gammaColor;
    }

    private static void ConfigureDiceTextureImporter(string path, bool isNormalTexture)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = !isNormalTexture;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.anisoLevel = 0;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Color EncodeNormal(Vector3 normal)
    {
        normal.Normalize();
        return new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
    }

    private static Mesh CreateOrUpdateDiceMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(DiceMeshPath);

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "DiceMesh";
            AssetDatabase.CreateAsset(mesh, DiceMeshPath);
        }

        mesh.name = "DiceMesh";
        mesh.Clear();

        Vector3[] vertices = new Vector3[D6FaceVisuals.Length * 4];
        Vector3[] normals = new Vector3[D6FaceVisuals.Length * 4];
        Vector2[] uvs = new Vector2[D6FaceVisuals.Length * 4];
        int[] triangles = new int[D6FaceVisuals.Length * 6];

        for (int faceIndex = 0; faceIndex < D6FaceVisuals.Length; faceIndex++)
        {
            DiceFaceVisualDefinition face = D6FaceVisuals[faceIndex];
            Quaternion faceRotation = Quaternion.LookRotation(face.normal, face.upHint);
            Vector3 right = faceRotation * Vector3.right;
            Vector3 up = faceRotation * Vector3.up;
            Vector3 center = face.normal * DiceHalfExtent;

            int vertexStart = faceIndex * 4;
            vertices[vertexStart + 0] = center - right * DiceHalfExtent - up * DiceHalfExtent;
            vertices[vertexStart + 1] = center - right * DiceHalfExtent + up * DiceHalfExtent;
            vertices[vertexStart + 2] = center + right * DiceHalfExtent + up * DiceHalfExtent;
            vertices[vertexStart + 3] = center + right * DiceHalfExtent - up * DiceHalfExtent;

            normals[vertexStart + 0] = face.normal;
            normals[vertexStart + 1] = face.normal;
            normals[vertexStart + 2] = face.normal;
            normals[vertexStart + 3] = face.normal;

            Rect uvRect = GetFaceUvRect(face.atlasColumn, face.atlasRow);
            uvs[vertexStart + 0] = new Vector2(uvRect.xMin, uvRect.yMin);
            uvs[vertexStart + 1] = new Vector2(uvRect.xMin, uvRect.yMax);
            uvs[vertexStart + 2] = new Vector2(uvRect.xMax, uvRect.yMax);
            uvs[vertexStart + 3] = new Vector2(uvRect.xMax, uvRect.yMin);

            int triangleStart = faceIndex * 6;
            triangles[triangleStart + 0] = vertexStart + 0;
            triangles[triangleStart + 1] = vertexStart + 2;
            triangles[triangleStart + 2] = vertexStart + 1;
            triangles[triangleStart + 3] = vertexStart + 0;
            triangles[triangleStart + 4] = vertexStart + 3;
            triangles[triangleStart + 5] = vertexStart + 2;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static Rect GetFaceUvRect(int atlasColumn, int atlasRow)
    {
        float padding = 2f / FaceTileSize;
        float cellWidth = 1f / AtlasColumns;
        float cellHeight = 1f / AtlasRows;
        float minX = atlasColumn * cellWidth + padding * cellWidth;
        float minY = atlasRow * cellHeight + padding * cellHeight;
        float maxX = (atlasColumn + 1) * cellWidth - padding * cellWidth;
        float maxY = (atlasRow + 1) * cellHeight - padding * cellHeight;
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static PhysicsMaterial CreateOrUpdatePhysicsMaterial(string path)
    {
        PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);

        if (material == null)
        {
            material = new PhysicsMaterial("DicePhysics");
            AssetDatabase.CreateAsset(material, path);
        }

        material.dynamicFriction = 0.85f;
        material.staticFriction = 0.85f;
        material.bounciness = 0.02f;
        material.frictionCombine = PhysicsMaterialCombine.Average;
        material.bounceCombine = PhysicsMaterialCombine.Minimum;

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateOrUpdateDicePrefab(Material diceMaterial, Mesh diceMesh, PhysicsMaterial dicePhysicsMaterial)
    {
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DicePrefabPath);

        if (existingPrefab != null)
        {
            UpdateExistingDicePrefab(diceMaterial, diceMesh, dicePhysicsMaterial);
            return AssetDatabase.LoadAssetAtPath<GameObject>(DicePrefabPath);
        }

        GameObject die = GameObject.CreatePrimitive(PrimitiveType.Cube);
        die.name = "Dice";
        ConfigureDiceObject(die, diceMaterial, diceMesh, dicePhysicsMaterial);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(die, DicePrefabPath);
        Object.DestroyImmediate(die);
        return prefab;
    }

    private static void UpdateExistingDicePrefab(Material diceMaterial, Mesh diceMesh, PhysicsMaterial dicePhysicsMaterial)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(DicePrefabPath);
        ConfigureDiceObject(prefabRoot, diceMaterial, diceMesh, dicePhysicsMaterial);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, DicePrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void ConfigureDiceObject(
        GameObject die,
        Material diceMaterial,
        Mesh diceMesh,
        PhysicsMaterial dicePhysicsMaterial)
    {
        while (die.transform.childCount > 0)
        {
            Object.DestroyImmediate(die.transform.GetChild(0).gameObject);
        }

        die.name = "Dice";
        die.transform.localScale = Vector3.one * 0.9f;

        MeshFilter meshFilter = EnsureComponent<MeshFilter>(die);
        MeshRenderer meshRenderer = EnsureComponent<MeshRenderer>(die);
        BoxCollider collider = EnsureComponent<BoxCollider>(die);
        Rigidbody rigidbody = EnsureComponent<Rigidbody>(die);

        meshFilter.sharedMesh = diceMesh;
        meshRenderer.sharedMaterial = diceMaterial;
        collider.size = Vector3.one;
        collider.center = Vector3.zero;
        collider.sharedMaterial = dicePhysicsMaterial;

        rigidbody.mass = 1.8f;
        rigidbody.linearDamping = 1f;
        rigidbody.angularDamping = 1.4f;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        EnsureComponent<DiceFaceReader>(die);
        EnsureComponent<DiceRoller>(die);

        EditorUtility.SetDirty(die);
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();

        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.12f, 0.16f);
        camera.fieldOfView = 55f;

        cameraObject.transform.position = new Vector3(0f, 7.5f, -8.5f);
        cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 0f));
    }

    private static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();

        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;

        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateTray(
        Material playSurfaceMaterial,
        Material wallMaterial,
        Material tableMaterial,
        PhysicsMaterial dicePhysicsMaterial)
    {
        GameObject trayRoot = new GameObject("DiceTray");

        CreateTrayPiece(
            "Table",
            trayRoot.transform,
            new Vector3(0f, -0.6f, 0f),
            new Vector3(16f, 0.7f, 12f),
            Quaternion.identity,
            tableMaterial,
            dicePhysicsMaterial);

        CreateTrayPiece(
            "Play Surface",
            trayRoot.transform,
            new Vector3(0f, -0.13f, 0f),
            new Vector3(11f, 0.25f, 8.5f),
            Quaternion.identity,
            playSurfaceMaterial,
            dicePhysicsMaterial);

        CreateTrayPiece(
            "Wall Front",
            trayRoot.transform,
            new Vector3(0f, 0.2f, 4.45f),
            new Vector3(11.8f, 0.55f, 0.55f),
            Quaternion.Euler(-4f, 0f, 0f),
            wallMaterial,
            dicePhysicsMaterial);

        CreateTrayPiece(
            "Wall Back",
            trayRoot.transform,
            new Vector3(0f, 0.2f, -4.45f),
            new Vector3(11.8f, 0.55f, 0.55f),
            Quaternion.Euler(4f, 0f, 0f),
            wallMaterial,
            dicePhysicsMaterial);

        CreateTrayPiece(
            "Wall Left",
            trayRoot.transform,
            new Vector3(-5.65f, 0.2f, 0f),
            new Vector3(0.55f, 0.55f, 9f),
            Quaternion.Euler(0f, 0f, -4f),
            wallMaterial,
            dicePhysicsMaterial);

        CreateTrayPiece(
            "Wall Right",
            trayRoot.transform,
            new Vector3(5.65f, 0.2f, 0f),
            new Vector3(0.55f, 0.55f, 9f),
            Quaternion.Euler(0f, 0f, 4f),
            wallMaterial,
            dicePhysicsMaterial);

        CreateInvisibleBarrier(
            "Barrier Front",
            trayRoot.transform,
            new Vector3(0f, 1.15f, 4.55f),
            new Vector3(11.9f, 2f, 0.7f));

        CreateInvisibleBarrier(
            "Barrier Back",
            trayRoot.transform,
            new Vector3(0f, 1.15f, -4.55f),
            new Vector3(11.9f, 2f, 0.7f));

        CreateInvisibleBarrier(
            "Barrier Left",
            trayRoot.transform,
            new Vector3(-5.75f, 1.15f, 0f),
            new Vector3(0.7f, 2f, 9.1f));

        CreateInvisibleBarrier(
            "Barrier Right",
            trayRoot.transform,
            new Vector3(5.75f, 1.15f, 0f),
            new Vector3(0.7f, 2f, 9.1f));
    }

    private static void CreateTrayPiece(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material,
        PhysicsMaterial dicePhysicsMaterial)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent);
        piece.transform.SetPositionAndRotation(position, rotation);
        piece.transform.localScale = scale;

        piece.GetComponent<Renderer>().sharedMaterial = material;
        piece.GetComponent<BoxCollider>().sharedMaterial = dicePhysicsMaterial;
    }

    private static void CreateInvisibleBarrier(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject barrier = new GameObject(name);
        barrier.transform.SetParent(parent);
        barrier.transform.localPosition = position;
        barrier.transform.localRotation = Quaternion.identity;
        barrier.transform.localScale = Vector3.one;

        BoxCollider collider = barrier.AddComponent<BoxCollider>();
        collider.size = scale;
        collider.center = Vector3.zero;
    }

    private static DiceRoller[] CreateDiceInstances(GameObject dicePrefab)
    {
        Vector3[] positions =
        {
            new Vector3(-2.4f, 1.1f, -1f),
            new Vector3(0f, 1.2f, 0f),
            new Vector3(2.4f, 1.1f, 1f),
        };

        Quaternion[] rotations =
        {
            Quaternion.Euler(8f, 0f, 0f),
            Quaternion.Euler(0f, 12f, 6f),
            Quaternion.Euler(-8f, -12f, 4f),
        };

        DiceRoller[] dice = new DiceRoller[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(dicePrefab);
            instance.name = $"Dice {i + 1}";
            instance.transform.SetPositionAndRotation(positions[i], rotations[i]);
            dice[i] = instance.GetComponent<DiceRoller>();
        }

        return dice;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvas;
    }
}
