using UnityEditor;
using UnityEngine;

/// <summary>
/// Camioneta "pickup" mediana construida con primitivas de Unity (sin assets externos, licencia propia).
/// Pensada para escala 1:1: las medidas de <see cref="Specs"/> son reales y sirven para calibrar
/// VehiclePhysics y ObstacleDetector.
///
/// Convención: origen en el piso (y = 0), +z hacia adelante, x = 0 en el eje del vehículo.
/// Al igual que SedanModel, el llamador ubica la raíz en el piso del auto.
///
/// Limitaciones conocidas (es un modelo de bloques, no una malla de diseño):
/// - Las ruedas son solo visuales: no giran ni se dirigen (VehicleVisuals no se conecta todavía).
/// - Los espejos son los mismos que se pasan a CreateMirrors (un solo juego, dentro del ancho declarado).
/// - El interior (tablero, asientos, volante) son cajas simples.
/// </summary>
public static class PickupModel
{
    /// <summary>Largo de diseño de la carrocería (m), antes de escalarla al largo de <see cref="Specs"/>.</summary>
    const float BodyLength = 5.25f;

    /// <summary>Medidas reales de referencia (m). Pickup compacta, pensada para caber en la plaza de 5,0 m.</summary>
    public static class Specs
    {
        public const float Length = 4.80f;        // largo total (paragolpes incluidos); cabe en la plaza más angosta (5,0 m)
        public const float Width = 2.20f;         // ancho con espejos (la carrocería mide 1,85 m)
        public static Vector3 Size => new Vector3(Width, Height, Length);
        public const float Height = 1.80f;        // alto total
        public const float Wheelbase = 2.90f;     // batalla (pickup compacta)
        public const float Track = 1.60f;         // trocha
        public const float TurningCircle = 11.0f; // diámetro de giro entre soleras (aprox.)
        public const float WheelRadius = 0.36f;   // radio de rodadura (neumático 265/65 R17 aprox.)
        public const float EyeHeight = 1.45f;     // altura de los ojos del conductor sentado
    }

    public struct Result
    {
        public Vector3 driverSeat;   // x, z relativos al centro del vehículo (y = piso)
        public float eyeHeight;      // sobre el piso
        public Vector3 mirrorLeft;   // espejo izquierdo (vidrio y carcasa), coordenadas del vehículo
        public Vector3 mirrorRight;  // espejo derecho
    }

    static readonly Color PaintColor = new Color(0.62f, 0.10f, 0.10f);
    static readonly Color TrimColor = new Color(0.24f, 0.24f, 0.25f);
    static readonly Color DarkColor = new Color(0.13f, 0.13f, 0.14f);
    static readonly Color FabricColor = new Color(0.30f, 0.30f, 0.33f);
    static readonly Color TireColor = new Color(0.08f, 0.08f, 0.08f);
    static readonly Color RimColor = new Color(0.55f, 0.55f, 0.58f);
    const string MaterialsFolder = "Assets/Materials";

    /// <summary>
    /// Construye la camioneta dentro de <paramref name="vehicle"/>. <paramref name="size"/> es el bounding box
    /// del vehículo (el collider); el modelo se ubica con su piso en -size.y/2, igual que SedanModel.
    /// </summary>
    public static bool Build(GameObject vehicle, Vector3 size, out Result result)
    {
        result = default;
        var root = new GameObject("Visual");
        root.transform.SetParent(vehicle.transform, false);
        root.transform.localPosition = new Vector3(0f, -size.y / 2f, 0f);

        // Toda la carrocería se escala en Z para caber en la plaza; las ruedas van fuera, con la batalla real
        float k = Specs.Length / BodyLength;
        var body = new GameObject("Body").transform;
        body.SetParent(root.transform, false);
        body.localScale = new Vector3(1f, 1f, k);

        var paint = Mat("PickupPaint", PaintColor, 0.6f);
        var glass = Mat("PickupGlass", new Color(0.55f, 0.65f, 0.72f, 0.2f), 0.95f, transparent: true);
        var trim = Mat("PickupTrim", TrimColor, 0.1f);
        var dark = Mat("PickupDark", DarkColor, 0.3f);
        var fabric = Mat("PickupFabric", FabricColor, 0.05f);
        var tire = Mat("PickupTire", TireColor, 0.1f);
        var rim = Mat("PickupRim", RimColor, 0.4f);
        var light = Mat("PickupLight", new Color(0.95f, 0.95f, 0.85f), 0.8f);
        var tail = Mat("PickupTail", new Color(0.7f, 0.05f, 0.05f), 0.6f);

        const float hw = 0.925f; // mitad del ancho de carrocería (puertas ~1,90 m con el marco); los espejos quedan dentro de Specs.Width

        // ---------- Chasis y frente ----------
        Box("Chassis", body.transform, new Vector3(0, 0.40f, 0), new Vector3(1.7f, 0.22f, 5.0f), trim);
        Box("Hood", body.transform, new Vector3(0, 1.02f, 1.75f), new Vector3(1.85f, 0.10f, 1.30f), paint);
        Box("FrontFender", body.transform, new Vector3(0, 0.72f, 1.85f), new Vector3(1.85f, 0.45f, 1.30f), paint);
        Box("Grille", body.transform, new Vector3(0, 0.85f, 2.52f), new Vector3(1.40f, 0.30f, 0.06f), dark);
        Box("FrontBumper", body.transform, new Vector3(0, 0.50f, 2.56f), new Vector3(1.90f, 0.22f, 0.12f), trim);
        Box("RearBumper", body.transform, new Vector3(0, 0.50f, -2.56f), new Vector3(1.90f, 0.22f, 0.12f), trim);
        foreach (float x in new[] { -0.70f, 0.70f })
        {
            Box("Headlight", body.transform, new Vector3(x, 0.98f, 2.52f), new Vector3(0.28f, 0.10f, 0.04f), light);
            Box("TailLight", body.transform, new Vector3(x, 1.02f, -2.56f), new Vector3(0.28f, 0.12f, 0.04f), tail);
        }

        // ---------- Cabina (paredes finas, para ver el interior) ----------
        const float cabFront = 0.90f, cabRear = -1.10f;
        float cabLen = cabFront - cabRear;               // 2.0 m
        float cabCenterZ = (cabFront + cabRear) / 2f;
        Box("CabFloor", body.transform, new Vector3(0, 0.60f, cabCenterZ), new Vector3(1.75f, 0.04f, cabLen), trim);
        foreach (float side in new[] { -1f, 1f })
        {
            // Puerta: panel de carrocería (pintura) por fuera, y el mismo volumen se ve por dentro
            Box(side < 0 ? "DoorL" : "DoorR", body.transform, new Vector3(side * hw, 0.95f, cabCenterZ),
                new Vector3(0.05f, 0.70f, cabLen), paint);
            // Ventanilla lateral (transparente)
            Box(side < 0 ? "SideGlassL" : "SideGlassR", body.transform, new Vector3(side * (hw - 0.03f), 1.48f, cabCenterZ),
                new Vector3(0.02f, 0.46f, cabLen - 0.2f), glass);
            // Espejo lateral (caja)
            Box(side < 0 ? "MirrorL" : "MirrorR", body.transform, new Vector3(side * (hw + 0.09f), 1.15f, 0.70f),
                new Vector3(0.16f, 0.12f, 0.22f), paint);
        }
        // Techo (pintura) y parabrisas / luneta (vidrio)
        Box("Roof", body.transform, new Vector3(0, Specs.Height - 0.02f, cabCenterZ), new Vector3(1.80f, 0.04f, cabLen), paint);
        var windshield = Box("Windshield", body.transform, new Vector3(0, 1.50f, 0.50f), new Vector3(1.72f, 0.62f, 0.03f), glass);
        windshield.transform.localRotation = Quaternion.Euler(-38f, 0, 0);
        Box("RearWindow", body.transform, new Vector3(0, 1.50f, cabRear + 0.01f), new Vector3(1.72f, 0.46f, 0.02f), glass);
        Box("CabRearWall", body.transform, new Vector3(0, 0.92f, cabRear), new Vector3(1.75f, 0.60f, 0.04f), paint);

        // ---------- Caja (bed) ----------
        float bedZ0 = cabRear - 0.02f, bedZ1 = -2.55f;
        float bedCenterZ = (bedZ0 + bedZ1) / 2f, bedLen = bedZ0 - bedZ1;
        Box("BedFloor", body.transform, new Vector3(0, 0.90f, bedCenterZ), new Vector3(1.70f, 0.04f, bedLen), trim);
        foreach (float side in new[] { -1f, 1f })
            Box(side < 0 ? "BedSideL" : "BedSideR", body.transform, new Vector3(side * 0.85f, 1.18f, bedCenterZ),
                new Vector3(0.07f, 0.56f, bedLen), paint);
        Box("BedFrontWall", body.transform, new Vector3(0, 1.18f, bedZ0), new Vector3(1.70f, 0.56f, 0.05f), paint);
        Box("Tailgate", body.transform, new Vector3(0, 1.18f, bedZ1), new Vector3(1.70f, 0.56f, 0.05f), paint);

        // ---------- Ruedas (cilindros; eje X) ----------
        float wr = Specs.WheelRadius;
        float halfWb = Specs.Wheelbase / 2f, halfTr = Specs.Track / 2f;
        // Las ruedas son solo visuales (no giran ni se dirigen): VehicleVisuals no se conecta a esta camioneta todavía
        Wheel(root.transform, "WheelFL", new Vector3(-halfTr, wr, halfWb), wr, tire, rim);
        Wheel(root.transform, "WheelFR", new Vector3(halfTr, wr, halfWb), wr, tire, rim);
        Wheel(root.transform, "WheelRL", new Vector3(-halfTr, wr, -halfWb), wr, tire, rim);
        Wheel(root.transform, "WheelRR", new Vector3(halfTr, wr, -halfWb), wr, tire, rim);

        // ---------- Interior ----------
        var seat = new Vector3(-0.38f, 0f, -0.25f * k);
        float eye = Specs.EyeHeight;
        var interior = new GameObject("Interior").transform;
        interior.SetParent(body.transform, false);
        // Tablero
        Box("Dashboard", interior, new Vector3(0, 1.00f, 0.72f), new Vector3(1.70f, 0.30f, 0.34f), dark);
        Box("CenterConsole", interior, new Vector3(0, 0.78f, 0.05f), new Vector3(0.24f, 0.20f, 0.95f), dark);
        Box("Cluster", interior, new Vector3(seat.x, 1.02f, 0.52f), new Vector3(0.32f, 0.08f, 0.14f), dark);
        // Volante (aro en plano inclinado hacia el conductor) y columna
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = "SteeringWheel";
        Object.DestroyImmediate(wheel.GetComponent<Collider>());
        wheel.transform.SetParent(interior, false);
        wheel.transform.localPosition = new Vector3(seat.x, 1.00f, 0.34f);
        wheel.transform.localRotation = Quaternion.Euler(65f, 0, 0);
        wheel.transform.localScale = new Vector3(0.38f, 0.015f, 0.38f);
        wheel.GetComponent<Renderer>().sharedMaterial = rim;
        // Asientos delanteros y banca trasera
        foreach (float x in new[] { -0.38f, 0.38f })
        {
            string side = x < 0 ? "Driver" : "Passenger";
            Box(side + "SeatCushion", interior, new Vector3(x, 0.66f, -0.25f), new Vector3(0.50f, 0.12f, 0.50f), fabric);
            Box(side + "SeatBack", interior, new Vector3(x, 1.00f, -0.50f), new Vector3(0.50f, 0.55f, 0.12f), fabric);
        }
        Box("RearSeatCushion", interior, new Vector3(0, 0.66f, -0.85f), new Vector3(1.60f, 0.12f, 0.50f), fabric);
        Box("RearSeatBack", interior, new Vector3(0, 1.00f, -1.02f), new Vector3(1.60f, 0.55f, 0.12f), fabric);

        result = new Result
        {
            driverSeat = seat,
            eyeHeight = eye,
            // Espejos en coordenadas del vehículo: y = 1,15 m sobre el piso, z ya escalado
            mirrorLeft = new Vector3(-(hw + 0.09f), 1.15f - Specs.Height / 2f, 0.70f * k),
            mirrorRight = new Vector3(hw + 0.09f, 1.15f - Specs.Height / 2f, 0.70f * k),
        };
        return true;
    }

    /// <summary>Cilindro de rueda (eje X) con su pivote en el centro, apoyada en el suelo.</summary>
    static Transform Wheel(Transform parent, string name, Vector3 center, float radius, Material tire, Material rim)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = center;
        const float width = 0.27f;
        var tyre = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tyre.name = "Tyre";
        Object.DestroyImmediate(tyre.GetComponent<Collider>());
        tyre.transform.SetParent(pivot, false);
        tyre.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        tyre.transform.localScale = new Vector3(radius * 2f, width / 2f, radius * 2f);
        tyre.GetComponent<Renderer>().sharedMaterial = tire;
        var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "Rim";
        Object.DestroyImmediate(hub.GetComponent<Collider>());
        hub.transform.SetParent(pivot, false);
        hub.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        hub.transform.localScale = new Vector3(radius * 1.1f, width / 2f * 1.02f, radius * 1.1f);
        hub.GetComponent<Renderer>().sharedMaterial = rim;
        return pivot;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Material Mat(string name, Color color, float smoothness, bool transparent = false)
    {
        System.IO.Directory.CreateDirectory(MaterialsFolder);
        string path = $"{MaterialsFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetFloat("_Glossiness", smoothness);
        if (transparent)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
