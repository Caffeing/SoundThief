using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SoundThief.Editor
{
    // Recreates Assets/Scenes/Stages/TaehyeonStage from primitives and the shared prefabs.
    // Footprint: x -18..18, z 0..48 (36m x 48m). Layer 8 is used only for floor, walls and lintels
    // (they block sight, sound blasts and the NavMesh bake); decor and interactables stay on the default layer.
    //
    //  z 48  +---------------+---------------+---------------+
    //        |  CLOCK ROOM   |  TARGET VAULT |  COURTYARD    |
    //  z 34  +------ . ------+------ . ------+------ . ------+   . = doorway
    //        |         CROSS-CORRIDOR (z 30..34)               |
    //  z 30  +------ . ------+-----(wide)-----+------ . ------+
    //        |  WEST ARCHIVE |  CENTRAL HALL |  EAST GALLERY |
    //  z 10  +---------------+----- door -----+---------------+   door = the one locked door
    //        |  COAT ROOM    |  VESTIBULE    |  STORAGE      |
    //  z 0   +---------------+  (start/exit) +---------------+
    //       x -18           -6              6               18
    public static class TaehyeonStageBuilder
    {
        public const string Folder = "Assets/Scenes/Stages/TaehyeonStage";
        public const string ScenePath = Folder + "/TaehyeonStage.unity";
        public const string DefinitionPath = Folder + "/TaehyeonStage Definition.asset";
        public const string NavMeshPath = Folder + "/TaehyeonStage NavMesh.asset";
        public const string StageId = "taehyeon_manor_seal";
        public const string KeyId = "taehyeon_key";
        public const string TargetId = "taehyeon_seal";
        const string Generated = "Assets/SoundThief/Generated/";
        const float WallHeight = 4, Thickness = 0.4f, Doorway = 2.4f;

        static Material stone, floor, wood, brass, cyan;
        static Transform geometry, decor, gameplay;

        [MenuItem("Sound Thief/Stage/Build Taehyeon Stage")]
        public static void Build()
        {
            stone = Mat("Midnight stone"); floor = Mat("Slate floor"); wood = Mat("Old oak"); brass = Mat("Warm brass"); cyan = Mat("Resonance");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            geometry = new GameObject("Geometry (layer 8)").transform;
            decor = new GameObject("Decor").transform;
            gameplay = new GameObject("Gameplay").transform;

            var context = InstallSystems();
            BuildArchitecture();
            BuildDecor();
            BuildGameplay(context);
            BakeNavMesh();
            context.Bind(); StageAuthoring.RecordContext(context);   // after the guard exists so Bind() wires it

            var sun = new GameObject("Moonlight").AddComponent<Light>();
            sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(55, -35, 0); sun.color = new Color(0.5f, 0.65f, 1); sun.intensity = 1.1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.3f, 0.36f, 0.48f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            StageAuthoring.RegisterScene(ScenePath);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("TAEHYEON_STAGE_BUILT: " + ScenePath);
        }

        // Batch entry: reopen the saved scene and check everything the deliverable promises.
        public static void Verify()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var context = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<StageContext>(true)).Single();
            var errors = context.Validate();
            foreach (var e in errors) Debug.LogError("VALIDATE: " + e);
            var all = context.SceneComponents<ThiefInteractable>();
            var grades = all.Where(i => i.kind == InteractionKind.Resonator).Select(i => i.relation.grade).OrderBy(g => g).ToArray();
            Check(errors.Count == 0, "StageContext.Validate reported errors");
            Check(context.definition && AssetDatabase.GetAssetPath(context.definition) == DefinitionPath, "definition not beside scene");
            Check(context.definition.requiredItemIds.SequenceEqual(new[] { TargetId }), "requiredItemIds != [" + TargetId + "]");
            Check(grades.SequenceEqual(new[] { 1, 1, 2, 3 }), "resonator grades: " + string.Join(",", grades));
            Check(all.Count(i => i.kind == InteractionKind.Door) == 1 && all.Count(i => i.kind == InteractionKind.Exit) == 1, "need exactly one door and one exit");
            Check(context.SceneComponents<GuardAgent>().Length == 1, "need exactly one guard");
            Check(EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled), "scene missing from build settings");
            Check(AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath), "NavMesh asset missing");
            Debug.Log("TAEHYEON_STAGE_VERIFIED: scene=" + ScenePath + " definition=" + AssetDatabase.GetAssetPath(context.definition)
                + " stageId=" + context.definition.stageId + " grades=" + string.Join(",", grades)
                + " buildSettings=" + EditorBuildSettings.scenes.Length + " scenes");
        }
        static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("Taehyeon stage verification failed: " + message); }

        // ---- shared systems --------------------------------------------------------------------------------

        static StageContext InstallSystems()
        {
            var systems = StageAuthoring.Spawn("StageSystems", Vector3.zero);
            var player = StageAuthoring.Spawn("PlayerRig", new Vector3(0, 0.05f, 3)).GetComponent<ThiefPlayer>();   // south vestibule, facing north
            var context = systems.GetComponent<StageContext>(); context.player = player;
            var definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(DefinitionPath);
            if (!definition) { definition = ScriptableObject.CreateInstance<StageDefinition>(); AssetDatabase.CreateAsset(definition, DefinitionPath); }
            definition.stageId = StageId;
            definition.title = "태현 저택의 인장";
            definition.briefing = "달빛 아래 잠든 대저택에 잠입하세요. 남쪽 보관실의 열쇠로 중앙 문을 열고, 서고·전시실·시계실의 소리를 다루며 경비를 피해 북쪽 금고의 태현 인장을 회수한 뒤 정문으로 돌아오세요.";
            definition.requiredItemIds = new[] { TargetId };
            definition.objectiveText = "열쇠로 중앙 문을 열고 북쪽 금고의 태현 인장을 회수하세요.";
            definition.exitText = "인장을 들고 남쪽 정문 출구에서 F로 탈출하세요.";
            definition.startImmediately = true;
            EditorUtility.SetDirty(definition);
            context.definition = definition;
            return context;
        }

        // ---- architecture (layer 8) ------------------------------------------------------------------------

        static void BuildArchitecture()
        {
            Cube("Floor", geometry, new Vector3(0, -0.25f, 24), new Vector3(36, 0.5f, 48), floor, 8, true);
            // outer shell
            WallZ("South outer wall", 0, -18.2f, 18.2f); WallZ("North outer wall", 48, -18.2f, 18.2f);
            WallX("West outer wall", -18, 0, 48); WallX("East outer wall", 18, 0, 48);
            // south band: coat room | vestibule | storage. The locked door at (0, 10) is the only way north.
            WallZ("Vestibule north wall", 10, -18, 18, (0, 2f));
            WallX("Vestibule west wall", -6, 0, 10, (5, Doorway)); WallX("Vestibule east wall", 6, 0, 10, (5, Doorway));
            // middle band: archive | hall | gallery, each side room with one hall doorway (offset so sight lines differ)
            WallX("Archive hall wall", -6, 10, 30, (15, Doorway)); WallX("Gallery hall wall", 6, 10, 30, (25, Doorway));
            WallZ("Cross-corridor south wall", 30, -18, 18, (-12, Doorway), (0, 6), (12, Doorway));
            // north band: clock room | vault | courtyard, entered only from the cross-corridor
            WallZ("Cross-corridor north wall", 34, -18, 18, (-12, Doorway), (0, Doorway), (12, Doorway));
            WallX("Clock room east wall", -6, 34, 48); WallX("Vault east wall", 6, 34, 48);
            WallZ("Vault baffle", 38, -3, 3);               // hides the plinth from the vault doorway
            WallZ("Gallery panel A", 17, 8, 11.5f); WallZ("Gallery panel B", 23, 12.5f, 16);
            WallZ("Clock room screen", 41, -17.8f, -15);
        }

        // ---- decor (default layer, no interaction) ---------------------------------------------------------

        static void BuildDecor()
        {
            // hall: carpet runner, pillars beside the aisles, wall lamps
            for (int i = 0; i < 5; i++) Cube("Hall carpet " + i, decor, new Vector3(0, 0.015f, 12 + i * 4), new Vector3(3, 0.025f, 3.6f), wood, 0, false);
            foreach (float z in new[] { 12f, 18f, 22f, 28f })
                foreach (float x in new[] { -4.6f, 4.6f }) Cube("Hall pillar", decor, new Vector3(x, 1.7f, z), new Vector3(0.8f, 3.4f, 0.8f), stone, 0, true);
            foreach (float z in new[] { 12f, 20f, 28f }) { Lamp(new Vector3(-5.6f, 2.8f, z)); Lamp(new Vector3(5.6f, 2.8f, z)); }
            Crates(new Vector3(-5f, 0, 10.9f), 0); Cube("Hall console", decor, new Vector3(5, 0.5f, 10.9f), new Vector3(2, 1, 0.6f), wood, 0, true);

            // vestibule + exit sign
            Cube("Vestibule rug", decor, new Vector3(0, 0.015f, 4.5f), new Vector3(4, 0.025f, 6), cyan, 0, false);
            Lamp(new Vector3(-5.6f, 2.8f, 2)); Lamp(new Vector3(5.6f, 2.8f, 2)); Lamp(new Vector3(0, 3, 9.6f));
            Painting(new Vector3(-5.78f, 2.1f, 8), -90); Painting(new Vector3(5.78f, 2.1f, 8), 90);
            Sign("EXIT", new Vector3(0, 2.4f, 0.3f), 180); Sign("MAIN HALL", new Vector3(0, 3.6f, 9.78f), 0);

            // coat room (SW): dead-end hiding nook
            Crates(new Vector3(-16.5f, 0, 1.5f), 10); Crates(new Vector3(-16.5f, 0, 8.5f), -5);
            Cube("Coat rack", decor, new Vector3(-11, 1.1f, 0.8f), new Vector3(4, 2.2f, 0.4f), wood, 0, true);
            Lamp(new Vector3(-12, 2.8f, 5)); Painting(new Vector3(-17.78f, 2, 5), -90);
            Sign("COAT ROOM", new Vector3(-5.78f, 3.6f, 5), -90);

            // storage (SE): key room
            Crates(new Vector3(16.3f, 0, 1.6f), 15); Crates(new Vector3(16.3f, 0, 8.4f), -12); Crates(new Vector3(9, 0, 1.4f), 0); Crates(new Vector3(12, 0, 9.0f), 25);
            Cube("Key plinth", decor, new Vector3(14.5f, 0.5f, 5), new Vector3(0.7f, 1, 0.7f), wood, 0, true);
            Lamp(new Vector3(12, 2.8f, 5)); Sign("STORAGE", new Vector3(5.78f, 3.6f, 5), 90);

            // west archive: shelf wall + island stacks, drawer cabinet
            for (int i = 0; i < 4; i++) Bookshelf(new Vector3(-17.5f, 0, 12.5f + i * 4.2f), 90);
            Bookshelf(new Vector3(-9.5f, 0, 19.5f), 0); Bookshelf(new Vector3(-9.5f, 0, 25.5f), 0);
            Cube("Archive reading table", decor, new Vector3(-13, 0.45f, 12.5f), new Vector3(2.4f, 0.9f, 1.2f), wood, 0, true);
            Lamp(new Vector3(-12, 3, 14)); Lamp(new Vector3(-12, 3, 24)); Lamp(new Vector3(-16, 3, 20));
            Sign("ARCHIVE", new Vector3(-5.78f, 3.6f, 15), -90);

            // east gallery: paintings, display plinths, panels
            for (int i = 0; i < 3; i++) Painting(new Vector3(17.78f, 2.1f, 14 + i * 6), 90);
            Painting(new Vector3(6.22f, 2.1f, 14), -90); Painting(new Vector3(11, 2.1f, 17.22f), 180); Painting(new Vector3(14, 2.1f, 22.78f), 0);
            Cube("Display plinth", decor, new Vector3(16.5f, 0.6f, 28), new Vector3(1.2f, 1.2f, 1.2f), stone, 0, true);
            Cube("Gallery bench", decor, new Vector3(9, 0.25f, 28.5f), new Vector3(2.4f, 0.5f, 0.7f), wood, 0, true);
            Lamp(new Vector3(12, 3, 13)); Lamp(new Vector3(12, 3, 26)); Lamp(new Vector3(16, 3, 20));
            Sign("GALLERY", new Vector3(5.78f, 3.6f, 25), 90);

            // cross-corridor
            for (int i = 0; i < 4; i++) Lamp(new Vector3(-13.5f + i * 9, 3, 32));
            Sign("CLOCK ROOM", new Vector3(-12, 3.6f, 33.78f), 0); Sign("VAULT", new Vector3(0, 3.6f, 33.78f), 0); Sign("COURTYARD", new Vector3(12, 3.6f, 33.78f), 0);
            Cube("Corridor runner", decor, new Vector3(0, 0.015f, 32), new Vector3(34, 0.025f, 1.6f), wood, 0, false);

            // clock room: grandfather clock body on the north wall
            Cube("Grandfather clock body", decor, new Vector3(-12, 1.6f, 47.4f), new Vector3(1.4f, 3.2f, 0.9f), wood, 0, true);
            Cube("Clock face", decor, new Vector3(-12, 2.6f, 46.93f), new Vector3(0.9f, 0.9f, 0.06f), brass, 0, false);
            Cube("Clock room desk", decor, new Vector3(-15, 0.45f, 38), new Vector3(2.6f, 0.9f, 1.2f), wood, 0, true);
            Cube("Clock room rug", decor, new Vector3(-12, 0.015f, 42.5f), new Vector3(5, 0.025f, 4), cyan, 0, false);
            Painting(new Vector3(-17.78f, 2, 44), -90); Painting(new Vector3(-6.22f, 2, 44), 90);
            Lamp(new Vector3(-8, 3, 36)); Lamp(new Vector3(-16, 3, 45)); Lamp(new Vector3(-8, 3, 46));

            // vault: pillars, brass floor inlay and plinth
            foreach (float x in new[] { -4.5f, 4.5f }) foreach (float z in new[] { 36.5f, 42f, 46.5f })
                Cube("Vault pillar", decor, new Vector3(x, 1.7f, z), new Vector3(0.7f, 3.4f, 0.7f), stone, 0, true);
            Cube("Vault inlay", decor, new Vector3(0, 0.015f, 43), new Vector3(3, 0.025f, 5), brass, 0, false);
            Cube("Seal plinth", decor, new Vector3(0, 0.6f, 45), new Vector3(1.3f, 1.2f, 1.3f), stone, 0, true);
            Painting(new Vector3(0, 2.4f, 47.78f), 0);
            Lamp(new Vector3(0, 3, 41)); Lamp(new Vector3(-3, 3, 46)); Lamp(new Vector3(3, 3, 46));

            // courtyard: fountain basin (resonator sits on the central column)
            Prim(PrimitiveType.Cylinder, "Fountain basin", decor, new Vector3(12, 0.25f, 41), new Vector3(4.4f, 0.25f, 4.4f), stone, 0, true);
            Prim(PrimitiveType.Cylinder, "Fountain water", decor, new Vector3(12, 0.52f, 41), new Vector3(3.8f, 0.01f, 3.8f), cyan, 0, false);
            Prim(PrimitiveType.Cylinder, "Fountain column", decor, new Vector3(12, 0.75f, 41), new Vector3(0.6f, 0.75f, 0.6f), brass, 0, false);
            Cube("Courtyard bench W", decor, new Vector3(8, 0.25f, 44), new Vector3(0.7f, 0.5f, 2.4f), wood, 0, true);
            Cube("Courtyard bench E", decor, new Vector3(16.3f, 0.25f, 44), new Vector3(0.7f, 0.5f, 2.4f), wood, 0, true);
            foreach (float x in new[] { 8f, 16f }) Lamp(new Vector3(x, 2.6f, 34.6f));
            Lamp(new Vector3(12, 3.2f, 46.6f));
        }

        // ---- gameplay: shared prefab instances -------------------------------------------------------------

        static void BuildGameplay(StageContext context)
        {
            var exit = Place("Exit", "Exit — front gate", new Vector3(0, 0.5f, 0.45f));
            Edit(exit, i => i.displayName = "저택 정문으로 탈출");

            var key = Place("Key", "Key — brass manor key", new Vector3(14.5f, 1.12f, 5));
            Edit(key, i => { i.itemId = KeyId; i.displayName = "황동 저택 열쇠"; });

            var door = Place("Door", "Door — central locked door", new Vector3(-1, 0, 10));   // hinge at the west edge of the 2m doorway
            Edit(door, i => { i.requiredItemId = KeyId; i.displayName = "중앙 홀의 잠긴 문"; });

            var seal = Place("Collectible", "Target — Taehyeon seal", new Vector3(0, 1.45f, 45));
            Edit(seal, i => { i.itemId = TargetId; i.displayName = "태현 가문의 인장"; });

            // Resonators: grade 1 / 1 / 2 / 3. Stands are decor; the shared prefab provides relation + emitter.
            Resonator("Resonator — archive drawer", new Vector3(-15.5f, 1.2f, 20), "문서 서랍", 1, new Vector3(1, 0.9f, 1.2f), wood);
            Resonator("Resonator — grandfather clock", new Vector3(-12, 1.3f, 46.6f), "괘종시계 추", 1, Vector3.zero, null);
            Resonator("Resonator — gallery vase", new Vector3(12, 1.95f, 20), "전시실 꽃병", 2, new Vector3(1, 1, 1), stone);
            Resonator("Resonator — courtyard fountain", new Vector3(12, 1.8f, 41), "중정 분수", 3, Vector3.zero, null);
            Prim(PrimitiveType.Cylinder, "Vase body", decor, new Vector3(12, 1.35f, 20), new Vector3(0.5f, 0.35f, 0.5f), brass, 0, false);

            // one guard asleep in the central hall (off the carpet line, facing the entrance door)
            var guard = Place("Guard", "Guard — central hall", new Vector3(3, 0, 19));
            guard.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        static GameObject Place(string prefab, string name, Vector3 position)
        {
            var obj = StageAuthoring.Spawn(prefab, position); obj.name = name; obj.transform.SetParent(gameplay, true);
            return obj;
        }
        static void Edit(GameObject obj, Action<ThiefInteractable> edit)
        {
            var item = obj.GetComponent<ThiefInteractable>(); edit(item);
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }
        static void Resonator(string name, Vector3 position, string medium, int grade, Vector3 standSize, Material standMaterial)
        {
            if (standMaterial) Cube(name + " stand", decor, new Vector3(position.x, standSize.y * 0.5f, position.z), standSize, standMaterial, 0, true);
            var obj = Place("Resonator", name, position);
            Edit(obj, i => i.displayName = medium);
            var relation = obj.GetComponent<SoundRelation>();
            relation.firstMedium = "손"; relation.secondMedium = medium; relation.grade = grade;
            PrefabUtility.RecordPrefabInstancePropertyModifications(relation);
            Light(position + Vector3.up * 0.9f, new Color(0.1f, 1, 0.8f), 4, 3);
        }

        static void BakeNavMesh()
        {
            Physics.SyncTransforms();
            var surface = geometry.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.layerMask = 1 << 8;
            surface.BuildNavMesh();
            if (!surface.navMeshData) throw new InvalidOperationException("NavMesh bake produced no data.");
            AssetDatabase.DeleteAsset(NavMeshPath);   // rebuild replaces the previous bake
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        }

        // ---- primitive helpers -----------------------------------------------------------------------------

        static Material Mat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Generated + name + ".mat");
            if (!m) throw new InvalidOperationException("Missing shared material: " + name + " (run Create Manor Prototype first)");
            return m;
        }
        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, int layer, bool collider, float yaw = 0)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent);
            obj.transform.position = position; obj.transform.rotation = Quaternion.Euler(0, yaw, 0); obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material; obj.layer = layer;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, int layer, bool collider, float yaw = 0)
            => Prim(PrimitiveType.Cube, name, parent, position, scale, material, layer, collider, yaw);

        // Wall running along z at a fixed x (a), or along x at a fixed z (b); gaps are (centre, width) doorways with a lintel.
        static void WallX(string name, float x, float z0, float z1, params (float c, float w)[] gaps) => Wall(name, true, x, z0, z1, gaps);
        static void WallZ(string name, float z, float x0, float x1, params (float c, float w)[] gaps) => Wall(name, false, z, x0, x1, gaps);
        static void Wall(string name, bool alongZ, float fixedAxis, float from, float to, (float c, float w)[] gaps)
        {
            float cursor = from; int n = 0;
            foreach (var gap in gaps.OrderBy(g => g.c))
            {
                Segment(name + " " + n++, alongZ, fixedAxis, cursor, gap.c - gap.w / 2, WallHeight / 2, WallHeight);
                Segment(name + " lintel", alongZ, fixedAxis, gap.c - gap.w / 2, gap.c + gap.w / 2, 3.6f, 0.8f);
                cursor = gap.c + gap.w / 2;
            }
            Segment(name + " " + n, alongZ, fixedAxis, cursor, to, WallHeight / 2, WallHeight);
        }
        static void Segment(string name, bool alongZ, float fixedAxis, float a, float b, float y, float height)
        {
            float mid = (a + b) / 2, length = b - a;
            if (length <= 0.01f) return;
            var position = alongZ ? new Vector3(fixedAxis, y, mid) : new Vector3(mid, y, fixedAxis);
            var scale = alongZ ? new Vector3(Thickness, height, length) : new Vector3(length, height, Thickness);
            Cube(name, geometry, position, scale, stone, 8, true);
        }

        static void Lamp(Vector3 position)
        {
            Light(position, new Color(1, 0.65f, 0.3f), 4, 8);
            Cube("Lamp bulb", decor, position, Vector3.one * 0.18f, brass, 0, false);
        }
        static void Light(Vector3 position, Color color, float intensity, float range)
        {
            var obj = new GameObject("Lamp light"); obj.transform.SetParent(decor); obj.transform.position = position;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
        }
        static void Crates(Vector3 floorPosition, float yaw)
        {
            Cube("Crate", decor, floorPosition + new Vector3(0, 0.5f, 0), Vector3.one, wood, 0, true, yaw);
            Cube("Crate", decor, floorPosition + new Vector3(0.95f, 0.4f, 0.2f), new Vector3(0.8f, 0.8f, 0.8f), wood, 0, true, yaw + 20);
            Cube("Crate (stacked)", decor, floorPosition + new Vector3(0.1f, 1.35f, 0), new Vector3(0.7f, 0.7f, 0.7f), wood, 0, true, yaw + 35);
        }
        static void Bookshelf(Vector3 floorPosition, float yaw)
        {
            Cube("Bookshelf", decor, floorPosition + Vector3.up * 1.5f, new Vector3(3.4f, 3, 0.7f), wood, 0, true, yaw);
            var rot = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < 3; i++)   // book rows on the +z face of the shelf
                Cube("Book row", decor, floorPosition + rot * new Vector3(0, 0.8f + i * 0.9f, 0.37f), new Vector3(3.1f, 0.5f, 0.05f), i % 2 == 0 ? stone : brass, 0, false, yaw);
        }
        static void Painting(Vector3 position, float yaw)   // readable by a viewer looking along Euler(0,yaw,0) * forward
        {
            Cube("Painting frame", decor, position, new Vector3(2.2f, 1.6f, 0.08f), brass, 0, false, yaw);
            Cube("Painting canvas", decor, position + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -0.03f), new Vector3(1.9f, 1.3f, 0.05f), stone, 0, false, yaw);
        }
        static void Sign(string text, Vector3 position, float yaw)   // readable by a viewer looking along Euler(0,yaw,0) * forward
        {
            var label = new GameObject("Sign — " + text).AddComponent<TextMeshPro>();
            label.transform.SetParent(decor); label.transform.position = position; label.transform.rotation = Quaternion.Euler(0, yaw, 0);
            label.text = text; label.fontSize = 3; label.alignment = TextAlignmentOptions.Center; label.color = new Color(0.4f, 0.95f, 0.85f);
            label.rectTransform.sizeDelta = new Vector2(4, 0.7f);
        }
    }
}
