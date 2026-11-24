using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRCar.Core;

/// <summary>
/// Escenario: ESTACIONAR EN PENDIENTE (CU2).
/// La calle del estacionamiento en paralelo con un tramo plano de partida y, desde la "bisagra" (x = SlopeHingeX),
/// una cuesta hacia +X. La inclinación (7 / 12 / 18 %) y el largo del hueco cambian con la dificultad elegida
/// en el menú; los aplican SlopeCourse y ParallelParkingCourse al iniciar.
///
/// Estructura (coordenadas de la calle: la calle va de x = -70 a +70, la solera derecha en z = -5):
///   FlatStart  → lo que queda antes de la bisagra: no se mueve (suelo, veredas, líneas, algún auto, edificios).
///   SlopeRoot  → su origen es la bisagra; se inclina en juego. Cuelga de él la calle ("Street", desplazada
///                para conservar sus coordenadas), el suelo de la cuesta y "Upright".
///   Upright    → edificios, postes y árboles de la cuesta: se inclinan con la calle pero se enderezan.
/// Nada bajo SlopeRoot puede ser estático (se mueve al iniciar).
/// Menú: VRCar → 5. Crear escena: estacionar en pendiente
/// </summary>
public partial class SceneSetup
{
    public const string SlopeScenePath = "Assets/Scenes/Parking_Slope.unity";
    const int SlopeDefaultDifficulty = 1;   // la escena se guarda en dificultad media
    const float SlopeHingeX = -40f;         // donde termina el plano y empieza la cuesta (coordenadas de la calle)
    const float SlopeJoin = 1f;             // el suelo y las veredas planas se prolongan 1 m bajo la cuesta: sin rendijas

    // Partida: sobre el tramo plano (12 m antes de la bisagra), en el carril derecho, mirando hacia +X
    // (propiedad y no campo: el orden de inicialización de campos estáticos entre archivos de una clase parcial no está definido)
    static Vector3 SlopeVehicleStart => new Vector3(-52f, VehicleSize.y / 2f + 0.01f, -1.45f);

    [MenuItem("VRCar/5. Crear escena: estacionar en pendiente", priority = 5)]
    public static void SetupSlopeScene() => CreateSlopeScene(interactive: true);

    public static void CreateSlopeScene(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        var flatRoot = new GameObject("FlatStart").transform;
        var slopeRoot = new GameObject("SlopeRoot").transform;
        slopeRoot.position = new Vector3(SlopeHingeX, 0f, 0f);   // la cuesta gira alrededor de este punto

        CreateSlopeGround(flatRoot, slopeRoot);
        var course = CreateParallelStreet(out Transform spot);
        course.transform.SetParent(slopeRoot, false);
        course.transform.localPosition = new Vector3(-SlopeHingeX, 0f, 0f);   // conserva las coordenadas de la calle
        SplitStreetAtHinge(course.transform, flatRoot);
        var upright = CreateSlopeSurroundings(slopeRoot, flatRoot);
        AddTargetMarker(spot, new Vector3(-BayWidth / 2f, 0f, 0f));   // flecha sobre el centro del hueco

        var vehicle = CreateVehicle();   // SlopeCourse lo coloca al iniciar
        var xrCamera = CreateXROrigin(vehicle.transform);
        CreateMirrors(vehicle.transform, xrCamera.transform);
        var hud = CreateHUD(vehicle.transform, xrCamera);

        var gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
        gameManager.AddComponent<ExperienceController>();

        var judgment = new GameObject("ParkingJudgment").AddComponent<ParallelParkingJudgment>();
        judgment.Configure(spot, ParallelParkingCourse.GapFor(SlopeDefaultDifficulty), vehicle.GetComponent<BoxCollider>(), hud);
        course.SetJudgment(judgment);
        course.Apply(SlopeDefaultDifficulty);

        var slope = new GameObject("SlopeCourse").AddComponent<SlopeCourse>();
        slope.Configure(slopeRoot, upright, vehicle.transform, SlopeVehicleStart, 90f, judgment);
        slope.Apply(SlopeDefaultDifficulty);   // deja la escena guardada ya inclinada y con el auto en su sitio

        // Lo que cuelga de SlopeRoot se mueve al iniciar: no puede ser estático (el batching estático lo dejaría atrás)
        foreach (var t in slopeRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;

        vehicle.GetComponent<ExerciseReset>().resetMessage = "Ejercicio reiniciado: estaciona en paralelo en la cuesta";
        EditorUtility.SetDirty(judgment);
        EditorUtility.SetDirty(course);
        EditorUtility.SetDirty(slope);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, SlopeScenePath);
        RegisterScenesInBuild();
        Debug.Log($"✅ Escena creada: {SlopeScenePath}");
    }

    /// <summary>Calzada y terreno: una losa plana de partida y otra que sube desde la bisagra (se inclina con SlopeRoot)</summary>
    static void CreateSlopeGround(Transform flatRoot, Transform slopeRoot)
    {
        // Material propio: el "Asphalt" del estacionamiento tiene otra repetición de textura y es compartido
        var asphalt = GetMaterial("AsphaltHill", Color.white, 0.1f);
        asphalt.mainTexture = AsphaltTexture();
        asphalt.mainTextureScale = new Vector2(StreetHalfLength + 4f, 10f);   // ~2 m por repetición en la cara superior
        float width = 2f * (StreetHalfWidth + SidewalkWidth + 2f);
        float west = -(StreetHalfLength + 4f), east = StreetHalfLength + 4f;

        // Plano: desde el extremo oeste hasta 1 m pasada la bisagra (queda bajo el arranque de la cuesta)
        float flatEnd = SlopeHingeX + SlopeJoin;
        Box("FlatGround", flatRoot, new Vector3((west + flatEnd) / 2f, -0.5f, 0f), new Vector3(flatEnd - west, 1f, width), asphalt, true);
        // Cuesta: coordenadas de SlopeRoot (origen en la bisagra), por eso empieza en x = 0
        float slopeLength = east - SlopeHingeX;
        Box("Hillside", slopeRoot, new Vector3(slopeLength / 2f, -0.5f, 0f), new Vector3(slopeLength, 1f, width), asphalt, true);
    }

    /// <summary>
    /// CreateParallelStreet arma la calle completa en horizontal. Aquí se parte en la bisagra: las veredas se rehacen
    /// (un tramo plano y uno que sube) y lo que queda antes de la bisagra (líneas, algún auto) pasa al tramo plano.
    /// </summary>
    static void SplitStreetAtHinge(Transform street, Transform flatRoot)
    {
        var sidewalkMat = GetMaterial("Sidewalk", new Color(0.55f, 0.55f, 0.53f));
        foreach (float side in new[] { -1f, 1f })
        {
            string n = side < 0 ? "Sidewalk_Right" : "Sidewalk_Left";
            var old = street.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            float z = side * (StreetHalfWidth + SidewalkWidth / 2f);

            // Cuesta: desde la bisagra hasta el extremo este (coordenadas de la calle)
            float slopeLen = StreetHalfLength - SlopeHingeX;
            var up = Box(n, street, new Vector3((SlopeHingeX + StreetHalfLength) / 2f, CurbHeight / 2f, z),
                new Vector3(slopeLen, CurbHeight, SidewalkWidth), sidewalkMat, true);
            up.tag = "Obstacle";
            up.AddComponent<VRCar.Obstacles.CurbSurface>();   // rozar la cuneta no cuenta como choque
            // Plano: desde el extremo oeste hasta 1 m pasada la bisagra
            float flatEnd = SlopeHingeX + SlopeJoin;
            var flat = Box(n + "_Flat", flatRoot, new Vector3((-StreetHalfLength + flatEnd) / 2f, CurbHeight / 2f, z),
                new Vector3(flatEnd + StreetHalfLength, CurbHeight, SidewalkWidth), sidewalkMat, true);
            flat.tag = "Obstacle";
            flat.AddComponent<VRCar.Obstacles.CurbSurface>();
        }

        // Lo que está por completo antes de la bisagra pasa al tramo plano (conserva su posición en el mundo)
        var toMove = new List<Transform>();
        foreach (Transform child in street)
        {
            if (child.name == "CenterDash" && child.localPosition.x < SlopeHingeX) toMove.Add(child);
            else if (child.name == "ParkedCar" && child.localPosition.x < SlopeHingeX - ParkedCarLength / 2f) toMove.Add(child);
        }
        foreach (var t in toMove) t.SetParent(flatRoot, true);
    }

    /// <summary>
    /// Edificios (en tramos de 10 m, que bajan o suben como en un cerro), postes, árboles y cierres de la calle.
    /// Cada pieza es un grupo vacío apoyado en la calle con sus partes como hijas. Las que quedan antes de la bisagra
    /// van al tramo plano; las de la cuesta van a "Upright", que SlopeCourse endereza al inclinar.
    /// Devuelve el grupo "Upright" (hijo de slopeRoot, con las mismas coordenadas de la calle).
    /// </summary>
    static Transform CreateSlopeSurroundings(Transform slopeRoot, Transform flatRoot)
    {
        var upright = new GameObject("Upright").transform;
        upright.SetParent(slopeRoot, false);
        upright.localPosition = new Vector3(-SlopeHingeX, 0f, 0f);   // coordenadas de la calle, igual que "Street"
        var wall = GetMaterial("BuildingWall", new Color(0.72f, 0.66f, 0.58f));
        var window = GetMaterial("BuildingWindow", new Color(0.25f, 0.32f, 0.4f), 0.9f);
        var pole = GetMaterial("LampPole", new Color(0.2f, 0.21f, 0.23f), 0.5f);
        var lamp = GetMaterial("LampHead", new Color(0.95f, 0.93f, 0.8f), 0.8f);
        var trunk = GetMaterial("TreeTrunk", new Color(0.35f, 0.25f, 0.15f));
        var leaves = GetMaterial("TreeLeaves", new Color(0.20f, 0.42f, 0.18f));

        const float seg = 10f;         // largo de cada tramo de edificio
        const float foundation = 2.5f; // cuánto se entierra el tramo para que no queden huecos entre escalones
        const float height = 9f;       // alto sobre la calle
        float facadeZ = StreetHalfWidth + SidewalkWidth;   // cara de los edificios que mira a la calle

        // Pieza apoyada en la calle (coordenadas de la calle): al plano si está antes de la bisagra, si no a la cuesta
        Transform Unit(string name, Vector3 streetPos)
        {
            var u = new GameObject(name).transform;
            u.SetParent(streetPos.x < SlopeHingeX ? flatRoot : upright, false);
            u.localPosition = streetPos;
            return u;
        }

        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? "R" : "L";
            for (float x = -StreetHalfLength + seg / 2f; x < StreetHalfLength; x += seg)
            {
                var u = Unit($"BuildingSeg_{s}", new Vector3(x, 0f, side * facadeZ));
                Box("Wall", u, new Vector3(0f, (height - foundation) / 2f, side * 1f), new Vector3(seg, height + foundation, 2f), wall, true);
                for (int f = 0; f < 3; f++)
                    Box($"Windows_{f}", u, new Vector3(0f, 1.6f + f * 2.4f, -side * 0.01f), new Vector3(seg - 0.4f, 1.1f, 0.02f), window, false);
            }

            // Postes de luz cada 14 m, con brazo hacia la calle
            for (float x = -63f; x <= 63f; x += 14f)
            {
                var u = Unit($"Lamp_{s}", new Vector3(x, 0f, side * (StreetHalfWidth + 0.6f)));
                var p = Box("Pole", u, new Vector3(0f, 2.5f, 0f), new Vector3(0.12f, 5f, 0.12f), pole, true);
                p.tag = "Obstacle";
                Box("Arm", u, new Vector3(0f, 4.95f, -side * 0.5f), new Vector3(0.08f, 0.08f, 1.0f), pole, false);
                Box("Head", u, new Vector3(0f, 4.88f, -side * 0.95f), new Vector3(0.3f, 0.08f, 0.5f), lamp, false);
            }
            // Árboles entre los postes
            for (float x = -56f; x <= 56f; x += 14f)
            {
                var u = Unit($"Tree_{s}", new Vector3(x, 0f, side * (StreetHalfWidth + 1.6f)));
                var t = Box("Trunk", u, new Vector3(0f, 1.3f, 0f), new Vector3(0.3f, 2.6f, 0.3f), trunk, true);
                t.tag = "Obstacle";
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Crown";
                Object.DestroyImmediate(crown.GetComponent<Collider>());
                crown.transform.SetParent(u, false);
                crown.transform.localPosition = new Vector3(0f, 3.9f, 0f);
                crown.transform.localScale = new Vector3(3.2f, 3.0f, 3.2f);
                crown.GetComponent<Renderer>().sharedMaterial = leaves;
            }
        }
        // Edificios que cierran la calle en los dos extremos
        foreach (float side in new[] { -1f, 1f })
        {
            var u = Unit(side < 0 ? "EndBlock_Start" : "EndBlock_Top", new Vector3(side * (StreetHalfLength + 2f), 0f, 0f));
            Box("Wall", u, new Vector3(0f, (height - foundation) / 2f, 0f), new Vector3(2f, height + foundation, 2f * (facadeZ + 2f)), wall, true);
        }
        return upright;
    }
}
