using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoomZero.App;
using RoomZero.Audio;
using RoomZero.Game;
using RoomZero.Interaction;
using RoomZero.Memory;
using RoomZero.Spatial;
using RoomZero.UI;
using RoomZero.Zero;

namespace RoomZero.EditorTools
{
    // Builds Bootstrap.unity + placeholder hologram prefabs so the full loop can be
    // played in the Editor with the keyboard. Re-run any time; it overwrites both.
    public static class SceneBuilder
    {
        const string Root = "Assets/RoomZero";
        const string ScenePath = Root + "/Scenes/Bootstrap.unity";
        const string PrefabDir = Root + "/Prefabs";
        const string MaterialPath = Root + "/Materials/Hologram.mat";

        [MenuItem("ROOMZERO/Build Test Scene")]
        public static void Build()
        {
            var holo = HologramMaterial();
            var prefabs = BuildPrefabs(holo);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, -2.4f), Quaternion.Euler(18f, 0f, 0f));
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var anchors = BuildRoom();

            var systems = new GameObject("ROOMZERO Systems");
            var bootstrap = systems.AddComponent<GameBootstrap>();
            var machine = systems.AddComponent<MissionStateMachine>();
            var client = systems.AddComponent<AgentClient>();
            var runner = systems.AddComponent<MissionRunner>();
            var scanner = systems.AddComponent<RoomScanner>();
            var anchorStore = systems.AddComponent<SpatialAnchorStore>();
            var safety = systems.AddComponent<SafetyGate>();
            var bridge = systems.AddComponent<ZeroClientBridge>();
            var telemetry = systems.AddComponent<LiveTelemetry>();
            var voice = systems.AddComponent<VoiceListener>();
            var wake = systems.AddComponent<WakeWatcher>();
            systems.AddComponent<PlayerMemory>();
            systems.AddComponent<WorldMemory>();

            var holoRoot = new GameObject("Holograms");
            var spawner = holoRoot.AddComponent<HologramSpawner>();

            var hud = new GameObject("Mission HUD");
            hud.transform.SetPositionAndRotation(new Vector3(0f, 2.1f, 1.2f), Quaternion.identity);
            var label = Text(hud, "ZERO is listening", 0.05f, Color.cyan);
            var hudComp = hud.AddComponent<MissionHud>();

            var adapt = new GameObject("Adaptation Visuals");
            var beam = adapt.AddComponent<LineRenderer>();
            beam.material = new Material(Shader.Find("Sprites/Default"));
            beam.startColor = beam.endColor = new Color(1f, 0.85f, 0.2f, 0.9f);
            beam.widthMultiplier = 0.01f;
            beam.enabled = false;
            var captionGo = new GameObject("Caption");
            captionGo.transform.SetParent(adapt.transform);
            captionGo.transform.position = new Vector3(0f, 1.62f, 1.2f);
            var caption = Text(captionGo, "", 0.035f, new Color(1f, 0.85f, 0.2f));
            captionGo.SetActive(false);
            var adaptComp = adapt.AddComponent<AdaptationVisuals>();

            var iris = new GameObject("Iris Voice");
            iris.AddComponent<AudioSource>();
            var irisVoice = iris.AddComponent<SpatialVoice>();

            Set(runner, "spawner", spawner);
            Set(scanner, "client", client);
            SetArray(scanner, "editorAnchors", anchors);
            Set(spawner, "codePadPrefab", prefabs.codePad);
            Set(spawner, "wirePrefab", prefabs.wire);
            Set(spawner, "mapPrefab", prefabs.map);
            Set(spawner, "keycardPrefab", prefabs.token);
            Set(spawner, "circuitPrefab", prefabs.circuit);
            Set(spawner, "tokenPrefab", prefabs.token);
            Set(spawner, "fallbackOrigin", anchors[0]);
            Set(spawner, "scanner", scanner);
            Set(spawner, "anchors", anchorStore);
            Set(bridge, "client", client);
            Set(bridge, "scanner", scanner);
            Set(bridge, "runner", runner);
            Set(bridge, "machine", machine);
            Set(bridge, "safety", safety);
            Set(telemetry, "runner", runner);
            Set(telemetry, "client", client);
            Set(wake, "bridge", bridge);
            Set(wake, "voice", voice);
            Set(hudComp, "runner", runner);
            Set(hudComp, "client", client);
            Set(hudComp, "label", label);
            Set(adaptComp, "client", client);
            Set(adaptComp, "runner", runner);
            Set(adaptComp, "beam", beam);
            Set(adaptComp, "caption", caption);
            Set(irisVoice, "scanner", scanner);
            SetString(irisVoice, "homeAnchorId", "a3");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("ROOM//ZERO test scene built: " + ScenePath);
        }

        // ---------- room stand-in ----------

        static Transform[] BuildRoom()
        {
            var room = new GameObject("Editor Room (stand-in for MRUK)");
            var grey = new Material(Shader.Find("Standard")) { color = new Color(0.22f, 0.24f, 0.28f) };

            Box(room, "Floor", new Vector3(0f, -0.05f, 1f), new Vector3(4.2f, 0.1f, 3.6f), grey);
            var wall = Box(room, "Wall N", new Vector3(0f, 1.25f, 2.8f), new Vector3(4.2f, 2.5f, 0.1f), grey);
            var desk = Box(room, "Desk", new Vector3(-1.1f, 0.375f, 1.2f), new Vector3(1.2f, 0.75f, 0.6f), grey);
            var table = Box(room, "Table", new Vector3(1.0f, 0.36f, 0.9f), new Vector3(1.2f, 0.72f, 0.9f), grey);
            var shelf = Box(room, "Shelf", new Vector3(1.7f, 0.7f, 2.4f), new Vector3(0.8f, 1.4f, 0.4f), grey);

            // Anchor ids must match RoomScanner's editor fallback graph.
            return new[]
            {
                Anchor(desk, "a1", new Vector3(0f, 0.375f, 0f)),
                Anchor(table, "a2", new Vector3(0f, 0.36f, 0f)),
                Anchor(shelf, "a3", new Vector3(0f, 0.7f, -0.25f)),
                Anchor(wall, "a4", new Vector3(0f, 0.1f, -0.15f)),
            };
        }

        static Transform Box(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        static Transform Anchor(Transform furniture, string id, Vector3 worldOffset)
        {
            var a = new GameObject(id).transform;
            a.SetParent(furniture.parent);
            a.position = furniture.position + worldOffset;
            return a;
        }

        // ---------- hologram prefabs ----------

        struct Prefabs { public GameObject codePad, wire, map, circuit, token; }

        static Prefabs BuildPrefabs(Material holo)
        {
            return new Prefabs
            {
                codePad = SavePrefab(CodePad(holo), "HoloCodePad"),
                wire = SavePrefab(Simple<WirePuzzle>("HoloWire", PrimitiveType.Cylinder, new Vector3(0.06f, 0.2f, 0.06f), holo), "HoloWire"),
                map = SavePrefab(Simple<MapAlignPuzzle>("HoloMap", PrimitiveType.Cylinder, new Vector3(0.35f, 0.01f, 0.35f), holo, withArrow: true), "HoloMap"),
                circuit = SavePrefab(Simple<CircuitPuzzle>("HoloCircuit", PrimitiveType.Cube, new Vector3(0.3f, 0.3f, 0.04f), holo), "HoloCircuit"),
                token = SavePrefab(Simple<KeycardPuzzle>("HoloToken", PrimitiveType.Sphere, Vector3.one * 0.14f, holo), "HoloToken"),
            };
        }

        static GameObject Simple<T>(string name, PrimitiveType shape, Vector3 scale, Material holo, bool withArrow = false) where T : Component
        {
            var root = new GameObject(name);
            var body = GameObject.CreatePrimitive(shape);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = scale;
            body.GetComponent<Renderer>().sharedMaterial = holo;
            body.GetComponent<Collider>().isTrigger = true;
            if (withArrow)
            {
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.name = "North";
                arrow.transform.SetParent(root.transform, false);
                arrow.transform.localPosition = new Vector3(0f, 0.01f, 0.12f);
                arrow.transform.localScale = new Vector3(0.03f, 0.01f, 0.12f);
                arrow.GetComponent<Renderer>().sharedMaterial = holo;
                Object.DestroyImmediate(arrow.GetComponent<Collider>());
            }
            root.AddComponent<T>();
            return root;
        }

        static GameObject CodePad(Material holo)
        {
            var root = Simple<CodePadPuzzle>("HoloCodePad", PrimitiveType.Cube, new Vector3(0.28f, 0.2f, 0.02f), holo);
            char[] digits = { '2', '0', '4', '6' };
            for (int i = 0; i < digits.Length; i++)
            {
                var key = GameObject.CreatePrimitive(PrimitiveType.Cube);
                key.name = "Key " + digits[i];
                key.transform.SetParent(root.transform, false);
                key.transform.localPosition = new Vector3(-0.09f + i * 0.06f, 0f, -0.02f);
                key.transform.localScale = new Vector3(0.045f, 0.045f, 0.015f);
                key.GetComponent<Renderer>().sharedMaterial = holo;
                key.GetComponent<Collider>().isTrigger = true;
                var k = key.AddComponent<CodePadKey>();
                var so = new SerializedObject(k);
                so.FindProperty("digit").stringValue = digits[i].ToString();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return root;
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            var path = PrefabDir + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Material HologramMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null)
                return mat;
            mat = new Material(Shader.Find("Standard")) { name = "Hologram" };
            mat.color = new Color(0.2f, 0.9f, 1f, 0.85f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.1f, 0.45f, 0.5f));
            AssetDatabase.CreateAsset(mat, MaterialPath);
            return mat;
        }

        // ---------- helpers ----------

        static TextMesh Text(GameObject go, string text, float size, Color color)
        {
            var t = go.AddComponent<TextMesh>();
            t.text = text;
            t.characterSize = size;
            t.fontSize = 48;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = color;
            return t;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"ROOM//ZERO: {target.GetType().Name}.{field} not found"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"ROOM//ZERO: {target.GetType().Name}.{field} not found"); return; }
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Transform[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
