using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRCar.Core;

/// <summary>
/// Escenario: ESTACIONAMIENTO EN PARALELO (CU2).
/// Calle de 10 m con veredas y edificios, autos estacionados a ambos lados y un hueco junto a la solera derecha.
/// El largo del hueco cambia con la dificultad elegida en el menú (ver ParallelParkingCourse).
/// El auto parte en el carril derecho, antes de la fila: hay que pasar de largo y entrar en reversa.
/// Menú: VRCar → 4. Crear escena: estacionamiento en paralelo
/// </summary>
public partial class SceneSetup
{
    public const string ParallelScenePath = "Assets/Scenes/Parking_Parallel.unity";

    // Calle (m). La solera derecha está en z = -StreetHalfWidth; el auto avanza hacia +X
    const float StreetHalfWidth = 5.0f;      // calzada de 10 m
    const float SidewalkWidth = 3.0f;
    const float CurbHeight = 0.20f;          // más alta que el despeje del auto (0.16 m): la solera sí frena
    const float StreetHalfLength = 70f;
    const float BayWidth = 2.3f;             // ancho de la línea paralela a la solera
    const float ParkedCarLength = 4.3f;      // medidas del auto estacionado (ver CreateParkedCar)
    const float ParkedCarWidth = 1.8f;
    const float ParkedCarSpacing = 0.8f;     // separación entre autos de la misma fila
    const int ParallelDefaultDifficulty = 1; // la escena se guarda en dificultad media

    [MenuItem("VRCar/4. Crear escena: estacionamiento en paralelo", priority = 4)]
    public static void SetupParallelScene() => CreateParallelScene(interactive: true);

    public static void CreateParallelScene(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        CreateGround();
        var course = CreateParallelStreet(out Transform spot);
        AddTargetMarker(spot, new Vector3(-BayWidth / 2f, 0f, 0f));   // flecha sobre el centro del hueco
        CreateStreetSurroundings();

        // El auto parte en el carril derecho, antes de la fila de autos, mirando en el sentido de la marcha (+X)
        var vehicle = CreateVehicle();
        vehicle.transform.SetPositionAndRotation(
            new Vector3(-34f, VehicleSize.y / 2f + 0.01f, -1.45f), Quaternion.Euler(0f, 90f, 0f));   // centro del carril derecho
        var xrCamera = CreateXROrigin(vehicle.transform);
        CreateMirrors(vehicle.transform, xrCamera.transform);
        var hud = CreateHUD(vehicle.transform, xrCamera);

        var gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
        gameManager.AddComponent<ExperienceController>();

        var judgment = new GameObject("ParkingJudgment").AddComponent<ParallelParkingJudgment>();
        judgment.Configure(spot, ParallelParkingCourse.GapFor(ParallelDefaultDifficulty), vehicle.GetComponent<BoxCollider>(), hud);
        course.SetJudgment(judgment);
        course.Apply(ParallelDefaultDifficulty);
        EditorUtility.SetDirty(judgment);
        EditorUtility.SetDirty(course);

        vehicle.GetComponent<ExerciseReset>().resetMessage = "Ejercicio reiniciado: estaciona en paralelo";

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ParallelScenePath);
        RegisterScenesInBuild();
        Debug.Log($"✅ Escena creada: {ParallelScenePath}");
    }

    /// <summary>
    /// Calzada con línea central, veredas con solera, autos estacionados y el hueco objetivo.
    /// Devuelve el componente que ajusta el hueco según la dificultad, y el Transform del espacio objetivo.
    /// </summary>
    static ParallelParkingCourse CreateParallelStreet(out Transform spot)
    {
        var rootGO = new GameObject("Street");
        var root = rootGO.transform;
        var yellow = GetMaterial("LineYellow", new Color(1f, 0.8f, 0.1f));
        var sidewalkMat = GetMaterial("Sidewalk", new Color(0.55f, 0.55f, 0.53f));
        var carColors = new[]
        {
            new Color(0.75f, 0.1f, 0.1f), new Color(0.85f, 0.85f, 0.88f), new Color(0.1f, 0.1f, 0.12f),
            new Color(0.2f, 0.35f, 0.55f), new Color(0.5f, 0.5f, 0.52f), new Color(0.9f, 0.9f, 0.9f), new Color(0.25f, 0.3f, 0.2f)
        };

        // Veredas con solera (collider + tag Obstacle: el sensor las detecta y el auto no las sube)
        foreach (float side in new[] { -1f, 1f })
        {
            var sw = Box(side < 0 ? "Sidewalk_Right" : "Sidewalk_Left", root,
                new Vector3(0f, CurbHeight / 2f, side * (StreetHalfWidth + SidewalkWidth / 2f)),
                new Vector3(2f * StreetHalfLength, CurbHeight, SidewalkWidth), sidewalkMat, true);
            sw.tag = "Obstacle"; sw.isStatic = true;
            sw.AddComponent<VRCar.Obstacles.CurbSurface>();   // rozar la cuneta no cuenta como choque
        }

        // Línea central discontinua
        for (float x = -StreetHalfLength + 4f; x <= StreetHalfLength - 4f; x += 6f)
            Box("CenterDash", root, new Vector3(x, 0.003f, 0f), new Vector3(3f, 0.005f, 0.12f), yellow, false).isStatic = true;

        // Autos estacionados. Fila derecha (junto a la solera del ejercicio): mira hacia +X como el tráfico de ese lado.
        // Fila izquierda: mira hacia -X. Los autos pegados al hueco no tienen "desorden" para que la evaluación sea justa.
        float carZ = StreetHalfWidth - 0.3f - ParkedCarWidth / 2f;   // 30 cm de la solera
        float pitch = ParkedCarLength + ParkedCarSpacing;
        var front = new GameObject("FrontCars").transform; front.SetParent(root, false);
        var rear = new GameObject("RearCars").transform; rear.SetParent(root, false);

        for (int i = 0; i < 3; i++)   // delante del hueco: su parachoques trasero parte en x = 0
        {
            float jx = i == 0 ? 0f : Mathf.Sin(i * 7.1f) * 0.15f;
            float jyaw = i == 0 ? 0f : Mathf.Sin(i * 3.3f) * 1.5f;
            var paint = GetMaterial($"ParkedCar_{i % carColors.Length}", carColors[i % carColors.Length], 0.6f);
            CreateParkedCar(front, new Vector3(ParkedCarLength / 2f + i * pitch + jx, 0f, -carZ), 90f + jyaw, paint);
        }
        for (int i = 0; i < 4; i++)   // detrás del hueco: el grupo se mueve con la dificultad (origen = parachoques delantero del primero)
        {
            float jx = i == 0 ? 0f : Mathf.Sin(i * 5.3f) * 0.15f;
            float jyaw = i == 0 ? 0f : Mathf.Sin(i * 2.9f) * 1.5f;
            var paint = GetMaterial($"ParkedCar_{(i + 3) % carColors.Length}", carColors[(i + 3) % carColors.Length], 0.6f);
            CreateParkedCar(rear, new Vector3(-(ParkedCarLength / 2f + i * pitch) + jx, 0f, -carZ), 90f + jyaw, paint);
        }
        // Fila de enfrente
        int n = 0;
        for (float x = -45f; x <= 45f; x += 7.5f, n++)
        {
            if (n % 5 == 3) continue;   // algún hueco libre, como en la vida real
            var paint = GetMaterial($"ParkedCar_{(n + 1) % carColors.Length}", carColors[(n + 1) % carColors.Length], 0.6f);
            CreateParkedCar(root, new Vector3(x + Mathf.Sin(n * 4.1f) * 0.3f, 0f, carZ + Mathf.Sin(n * 1.7f) * 0.08f),
                270f + Mathf.Sin(n * 2.3f) * 1.5f, paint);
        }

        // Espacio objetivo: sobre la cara de la solera, forward = sentido de la calle (+X), right = hacia la solera
        var spotT = new GameObject("TargetSpot").transform;
        spotT.SetParent(root, false);
        spotT.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        float gap = ParallelParkingCourse.GapFor(ParallelDefaultDifficulty);
        var outer = Box("BayLine_Outer", spotT, new Vector3(-BayWidth, 0.003f, 0f), new Vector3(0.1f, 0.005f, gap), yellow, false);
        var frontLine = Box("BayLine_Front", spotT, new Vector3(-BayWidth / 2f, 0.003f, gap / 2f), new Vector3(BayWidth, 0.005f, 0.1f), yellow, false);
        var rearLine = Box("BayLine_Rear", spotT, new Vector3(-BayWidth / 2f, 0.003f, -gap / 2f), new Vector3(BayWidth, 0.005f, 0.1f), yellow, false);

        var course = rootGO.AddComponent<ParallelParkingCourse>();
        course.Configure(rear, spotT, outer.transform, frontLine.transform, rearLine.transform, -StreetHalfWidth, BayWidth);
        course.Apply(ParallelDefaultDifficulty);
        spot = spotT;
        return course;
    }

    /// <summary>
    /// Edificios, postes de luz, árboles y cierres de la calle: referencias de escala y profundidad (sin jugabilidad).
    /// Con gateX (escena del portón) el edificio de la derecha se interrumpe en esa abertura y no se plantan árboles delante.
    /// </summary>
    static void CreateStreetSurroundings(float gateX = float.NaN, float gateWidth = 0f)
    {
        var root = new GameObject("StreetSurroundings").transform;
        var wall = GetMaterial("BuildingWall", new Color(0.72f, 0.66f, 0.58f));
        var window = GetMaterial("BuildingWindow", new Color(0.25f, 0.32f, 0.4f), 0.9f);
        var pole = GetMaterial("LampPole", new Color(0.2f, 0.21f, 0.23f), 0.5f);
        var lamp = GetMaterial("LampHead", new Color(0.95f, 0.93f, 0.8f), 0.8f);
        var trunk = GetMaterial("TreeTrunk", new Color(0.35f, 0.25f, 0.15f));
        var leaves = GetMaterial("TreeLeaves", new Color(0.20f, 0.42f, 0.18f));
        float facadeZ = StreetHalfWidth + SidewalkWidth;   // cara de los edificios que mira a la calle

        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? "R" : "L";
            bool hasGate = side < 0f && !float.IsNaN(gateX);
            if (hasGate)
            {
                BuildingRun($"Building_{s}_West", -StreetHalfLength - 2f, gateX - gateWidth / 2f, 2f, 0.3f);
                BuildingRun($"Building_{s}_East", gateX + gateWidth / 2f, StreetHalfLength + 2f, 0.3f, 2f);
            }
            else BuildingRun($"Building_{s}", -StreetHalfLength - 2f, StreetHalfLength + 2f, 2f, 2f);

            void BuildingRun(string name, float x0, float x1, float trimStart, float trimEnd)
            {
                Box(name, root, new Vector3((x0 + x1) / 2f, 4.5f, side * (facadeZ + 1f)), new Vector3(x1 - x0, 9f, 2f), wall, true).isStatic = true;
                float w0 = x0 + trimStart, w1 = x1 - trimEnd;
                for (int f = 0; f < 3; f++)
                    Box($"Windows_{s}_{f}", root, new Vector3((w0 + w1) / 2f, 1.6f + f * 2.4f, side * (facadeZ - 0.01f)),
                        new Vector3(w1 - w0, 1.1f, 0.02f), window, false).isStatic = true;
            }

            // Postes de luz cada 14 m, con brazo hacia la calle
            for (float x = -63f; x <= 63f; x += 14f)
            {
                var p = Box($"LampPole_{s}", root, new Vector3(x, 2.5f, side * (StreetHalfWidth + 0.6f)), new Vector3(0.12f, 5f, 0.12f), pole, true);
                p.tag = "Obstacle"; p.isStatic = true;
                Box($"LampArm_{s}", root, new Vector3(x, 4.95f, side * (StreetHalfWidth + 0.1f)), new Vector3(0.08f, 0.08f, 1.0f), pole, false).isStatic = true;
                Box($"LampHead_{s}", root, new Vector3(x, 4.88f, side * (StreetHalfWidth - 0.35f)), new Vector3(0.3f, 0.08f, 0.5f), lamp, false).isStatic = true;
            }
            // Árboles entre los postes
            for (float x = -56f; x <= 56f; x += 14f)
            {
                if (hasGate && Mathf.Abs(x - gateX) < 3f) continue;   // no tapar el portón
                var t = Box($"TreeTrunk_{s}", root, new Vector3(x, 1.3f, side * (StreetHalfWidth + 1.6f)), new Vector3(0.3f, 2.6f, 0.3f), trunk, true);
                t.tag = "Obstacle"; t.isStatic = true;
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = $"TreeCrown_{s}";
                Object.DestroyImmediate(crown.GetComponent<Collider>());
                crown.transform.SetParent(root, false);
                crown.transform.localPosition = new Vector3(x, 3.9f, side * (StreetHalfWidth + 1.6f));
                crown.transform.localScale = new Vector3(3.2f, 3.0f, 3.2f);
                crown.GetComponent<Renderer>().sharedMaterial = leaves;
                crown.isStatic = true;
            }
        }
        // Edificios que cierran la calle en los dos extremos
        foreach (float side in new[] { -1f, 1f })
            Box(side < 0 ? "EndBlock_West" : "EndBlock_East", root, new Vector3(side * (StreetHalfLength + 2f), 4.5f, 0f),
                new Vector3(2f, 9f, 2f * (facadeZ + 2f)), wall, true).isStatic = true;
    }
}
