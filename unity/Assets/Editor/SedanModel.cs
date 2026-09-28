using UnityEditor;
using UnityEngine;
using VRCar.Vehicle;

/// <summary>
/// Auto por defecto: carrocería "Car" de Quaternius (CC0, poly.pizza) escalada a las medidas
/// exteriores de un Peugeot 308 + un interior simple construido aquí (tablero, volante, asientos,
/// consola, piso). Al ser simple es fácil de editar (p. ej. agregar la pantalla del sensor de retroceso).
/// - La carrocería es una malla cerrada: por dentro se ve gracias a una copia con las caras invertidas
/// - Los vidrios del modelo (material "Windows") se reemplazan por vidrio transparente
/// - Las ruedas se re-pivotean en su centro y se escalan uniformemente (no se deforman al girar)
/// </summary>
public static class SedanModel
{
    public const string ModelPath = "Assets/Models/PolyCars/QuaterniusCar.glb";
    const string MaterialsFolder = "Assets/Materials";

    // Medidas del modelo original (m, glTF)
    const float ModelWidth = 1.8074f, ModelLength = 4.2207f, ModelRoof = 1.1826f;
    // Alto real del 308 (sin antena)
    const float TargetHeight = 1.457f;

    public struct Result
    {
        public Vector3 driverSeat;   // x, z relativos al centro del auto
        public float eyeHeight;      // sobre el suelo
    }

    public static bool Build(GameObject vehicle, Vector3 size, out Result result)
    {
        result = default;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (prefab == null) return false;

        float floorY = -size.y / 2f;
        var root = new GameObject("Visual");
        root.transform.SetParent(vehicle.transform, false);
        root.transform.localPosition = new Vector3(0f, floorY, 0f);

        var model = (GameObject)Object.Instantiate(prefab, root.transform);
        model.name = "CarModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        float sx = size.x / ModelWidth, sz = size.z / ModelLength, sy = TargetHeight / ModelRoof;
        model.transform.localScale = new Vector3(sx, sy, sz);
        foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        var glass = Mat("SedanGlass", new Color(0.55f, 0.65f, 0.72f, 0.18f), 0.95f, transparent: true);
        var trim = Mat("SedanInterior", new Color(0.24f, 0.24f, 0.25f), 0.1f);
        var paint = Mat("SedanPaint", new Color(0.15f, 0.33f, 0.72f), 0.65f);

        // Carrocería: pintura + vidrios transparentes + copia invertida para verla por dentro
        var body = Find(model.transform, "NormalCar1_Cube.012");
        if (body == null) { Object.DestroyImmediate(root); return false; }
        var bodyR = body.GetComponent<MeshRenderer>();
        var mats = bodyR.sharedMaterials;
        var innerMats = new Material[mats.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            string n = mats[i] != null ? mats[i].name : "";
            if (n.StartsWith("Windows")) { mats[i] = glass; innerMats[i] = glass; }
            else { if (n.StartsWith("Blue")) mats[i] = paint; innerMats[i] = trim; }
        }
        bodyR.sharedMaterials = mats;
        var inner = new GameObject("BodyInside");
        inner.transform.SetParent(body, false);
        // Por dentro: el techo (sobre la línea de las ventanas) va en un tapiz claro, el resto en plástico gris
        var headliner = Mat("SedanHeadliner", new Color(0.62f, 0.60f, 0.56f), 0.05f);
        var innerMesh = InvertedMesh(body.GetComponent<MeshFilter>().sharedMesh, 1.12f, mats, out int roofSub);
        var allInner = new Material[innerMesh.subMeshCount];
        for (int i = 0; i < allInner.Length; i++) allInner[i] = i < innerMats.Length ? innerMats[i] : headliner;
        inner.AddComponent<MeshFilter>().sharedMesh = innerMesh;
        inner.AddComponent<MeshRenderer>().sharedMaterials = allInner;

        // Ruedas: pivote en el centro, escala uniforme
        float sw = sy;
        Transform fl = Wheel(root.transform, model.transform, "NormalCar1_FrontLeftWheel_Cube.007", sw, false);
        Transform fr = Wheel(root.transform, model.transform, "NormalCar1_FrontRightWheel_Cube.008", sw, true);
        Transform rear = Wheel(root.transform, model.transform, "NormalCar1_BackWheels_Cube.011", sw, false);

        // ---------- Interior ----------
        var seat = new Vector3(-0.38f, 0f, -0.25f);
        float eye = 1.20f;
        var interior = new GameObject("Interior").transform;
        interior.SetParent(root.transform, false);
        var dark = Mat("SedanDash", new Color(0.13f, 0.13f, 0.14f), 0.3f);
        var fabric = Mat("SedanSeat", new Color(0.30f, 0.30f, 0.33f), 0.05f);

        Box("Floor", interior, new Vector3(0, 0.24f, -0.3f), new Vector3(1.55f, 0.04f, 2.6f), trim);
        Box("Dashboard", interior, new Vector3(0, 0.81f, 0.68f), new Vector3(1.62f, 0.22f, 0.50f), dark);
        Box("DashboardLower", interior, new Vector3(0, 0.62f, 0.78f), new Vector3(1.55f, 0.30f, 0.30f), dark);
        Box("Cowl", interior, new Vector3(0, 0.925f, 0.90f), new Vector3(1.55f, 0.02f, 0.25f), dark);
        var console = Box("CenterConsole", interior, new Vector3(0, 0.48f, 0.05f), new Vector3(0.24f, 0.22f, 0.95f), dark);
        Box("CenterStack", interior, new Vector3(0, 0.70f, 0.52f), new Vector3(0.28f, 0.30f, 0.18f), dark);
        // Pantalla central (aquí irá la cámara/sensor de retroceso)
        var screen = Box("CenterScreen", interior, new Vector3(0, 1.05f, 0.56f), new Vector3(0.26f, 0.15f, 0.02f),
            Mat("SedanScreen", new Color(0.02f, 0.03f, 0.05f), 0.9f));
        screen.transform.localRotation = Quaternion.Euler(-12f, 0, 0);

        foreach (float x in new[] { -0.38f, 0.38f })
        {
            string side = x < 0 ? "Driver" : "Passenger";
            Box(side + "SeatCushion", interior, new Vector3(x, 0.44f, -0.12f), new Vector3(0.52f, 0.13f, 0.52f), fabric);
            var back = Box(side + "SeatBack", interior, new Vector3(x, 0.78f, -0.44f), new Vector3(0.52f, 0.62f, 0.12f), fabric);
            back.transform.localRotation = Quaternion.Euler(-12f, 0, 0);
            Box(side + "Headrest", interior, new Vector3(x, 1.18f, -0.54f), new Vector3(0.26f, 0.18f, 0.10f), fabric);
        }
        // Paneles de puertas por dentro (bajo la línea de ventanas) con apoyabrazos
        var doorMat = Mat("SedanDoorPanel", new Color(0.20f, 0.20f, 0.22f), 0.15f);
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * 0.80f;
            Box(side < 0 ? "DoorPanelL" : "DoorPanelR", interior, new Vector3(x, 0.66f, -0.35f), new Vector3(0.05f, 0.56f, 2.1f), doorMat);
            Box(side < 0 ? "ArmrestL" : "ArmrestR", interior, new Vector3(x - side * 0.05f, 0.72f, -0.15f), new Vector3(0.08f, 0.05f, 0.45f), trim);
        }
        Box("RearSeatCushion", interior, new Vector3(0, 0.44f, -1.15f), new Vector3(1.40f, 0.14f, 0.50f), fabric);
        var rback = Box("RearSeatBack", interior, new Vector3(0, 0.78f, -1.44f), new Vector3(1.40f, 0.60f, 0.12f), fabric);
        rback.transform.localRotation = Quaternion.Euler(-14f, 0, 0);

        // Volante: aro + 3 rayos + columna. Pivote con forward = eje de la columna (alejándose del conductor)
        var wheelCenter = new Vector3(seat.x, 0.93f, 0.22f);
        var pivot = new GameObject("SteeringWheelPivot").transform;
        pivot.SetParent(interior, false);
        pivot.localPosition = wheelCenter;
        pivot.localRotation = Quaternion.Euler(22f, 0, 0); // columna inclinada hacia abajo
        var ring = new GameObject("SteeringRing");
        ring.transform.SetParent(pivot, false);
        ring.AddComponent<MeshFilter>().sharedMesh = TorusMesh(0.18f, 0.019f);
        ring.AddComponent<MeshRenderer>().sharedMaterial = Mat("SedanWheelRim", new Color(0.09f, 0.09f, 0.10f), 0.45f);
        // Rayos a las 3, 9 y 6 (como un volante moderno) y centro redondo con airbag
        var rim = Mat("SedanWheelRim", new Color(0.09f, 0.09f, 0.10f), 0.45f);
        foreach (float a in new[] { 0f, 180f, 270f })
        {
            var spoke = Box("Spoke", pivot, Vector3.zero, new Vector3(0.12f, 0.035f, 0.018f), dark);
            spoke.transform.localRotation = Quaternion.Euler(0, 0, a);
            spoke.transform.localPosition = Quaternion.Euler(0, 0, a) * new Vector3(0.115f, 0, 0.01f);
        }
        var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "Airbag";
        Object.DestroyImmediate(hub.GetComponent<Collider>());
        hub.transform.SetParent(pivot, false);
        hub.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        hub.transform.localPosition = new Vector3(0, 0, 0.005f);
        hub.transform.localScale = new Vector3(0.13f, 0.025f, 0.11f);
        hub.GetComponent<Renderer>().sharedMaterial = rim;
        var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        column.name = "SteeringColumn";
        Object.DestroyImmediate(column.GetComponent<Collider>());
        column.transform.SetParent(interior, false);
        column.transform.localRotation = pivot.localRotation * Quaternion.Euler(90f, 0, 0);
        column.transform.localPosition = wheelCenter + pivot.localRotation * new Vector3(0, 0, 0.20f);
        column.transform.localScale = new Vector3(0.05f, 0.18f, 0.05f);
        column.GetComponent<Renderer>().sharedMaterial = dark;
        // Tablero de instrumentos detrás del volante
        Box("Cluster", interior, new Vector3(seat.x, 0.96f, 0.55f), new Vector3(0.32f, 0.08f, 0.14f), dark);

        var visuals = vehicle.AddComponent<VehicleVisuals>();
        visuals.frontLeft = fl; visuals.frontRight = fr; visuals.rearLeft = rear;
        visuals.steeringWheel = pivot;
        visuals.steerAxis = Vector3.up;
        visuals.spinAxis = Vector3.right;
        visuals.wheelRadius = 0.262f * sw;

        result = new Result { driverSeat = seat, eyeHeight = eye };
        return true;
    }

    /// <summary>Auto estacionado (solo visual): misma carrocería con otro color; apoyado en y=0 del padre.</summary>
    public static bool BuildParked(Transform parent, Vector3 size, Material paint, Material glass)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (prefab == null) return false;
        var model = (GameObject)Object.Instantiate(prefab, parent);
        model.name = "CarModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = new Vector3(size.x / ModelWidth, size.y / ModelRoof, size.z / ModelLength);
        foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        foreach (var r in model.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                string n = mats[i] != null ? mats[i].name : "";
                if (n.StartsWith("Blue")) mats[i] = paint;
                else if (n.StartsWith("Windows")) mats[i] = glass;
            }
            r.sharedMaterials = mats;
            r.gameObject.isStatic = true;
        }
        return true;
    }

    static Transform Wheel(Transform root, Transform model, string name, float scale, bool flip)
    {
        var w = Find(model, name);
        if (w == null) return null;
        var mesh = w.GetComponent<MeshFilter>().sharedMesh;
        Vector3 c = mesh.bounds.center;
        Vector3 worldCenter = w.TransformPoint(c);
        var pivot = new GameObject(name + "_Pivot").transform;
        pivot.SetParent(root, false);
        Vector3 lp = root.InverseTransformPoint(worldCenter);
        lp.y = mesh.bounds.extents.y * scale; // apoyada en el suelo
        pivot.localPosition = lp;
        pivot.localScale = Vector3.one * scale;
        w.SetParent(pivot, false);
        w.localRotation = Quaternion.identity;
        w.localScale = Vector3.one;
        w.localPosition = -c;
        if (flip)
        {
            // Misma convención de giro que la rueda izquierda (VehicleVisuals usa -spin a la derecha)
            pivot.localRotation = Quaternion.Euler(0, 180f, 0);
            w.localRotation = Quaternion.Euler(0, 180f, 0);
            w.localPosition = Quaternion.Euler(0, 180f, 0) * -c;
        }
        return pivot;
    }

    /// <summary>
    /// Copia con caras invertidas (para ver la carrocería por dentro). Los triángulos que no son vidrio
    /// y quedan sobre roofY (unidades del modelo) se mueven a un submesh extra al final (techo interior).
    /// </summary>
    static Mesh InvertedMesh(Mesh src, float roofY, Material[] mats, out int roofSub)
    {
        var m = Object.Instantiate(src);
        m.name = src.name + "_Inside";
        var n = m.normals;
        for (int i = 0; i < n.Length; i++) n[i] = -n[i];
        m.normals = n;
        var v = m.vertices;
        int count = m.subMeshCount;
        var roof = new System.Collections.Generic.List<int>();
        var subs = new System.Collections.Generic.List<int>[count];
        for (int s = 0; s < count; s++)
        {
            var t = m.GetTriangles(s);
            bool glass = s < mats.Length && mats[s] != null && mats[s].name.StartsWith("SedanGlass");
            subs[s] = new System.Collections.Generic.List<int>();
            for (int i = 0; i < t.Length; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2];
                float cy = (v[a].y + v[b].y + v[c].y) / 3f;
                var dst = (!glass && cy > roofY) ? roof : subs[s];
                dst.Add(b); dst.Add(a); dst.Add(c); // invertido
            }
        }
        m.subMeshCount = count + 1;
        for (int s = 0; s < count; s++) m.SetTriangles(subs[s], s);
        m.SetTriangles(roof, count);
        roofSub = count;
        SaveMesh(m);
        return m;
    }

    static Mesh TorusMesh(float R, float r, int seg = 48, int side = 10)
    {
        var path = $"{MaterialsFolder}/SteeringRing_{R:F3}_{r:F3}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        var v = new Vector3[(seg + 1) * (side + 1)];
        var nrm = new Vector3[v.Length];
        var tris = new int[seg * side * 6];
        for (int i = 0; i <= seg; i++)
        {
            float u = i / (float)seg * Mathf.PI * 2f;
            var ring = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0f);
            for (int j = 0; j <= side; j++)
            {
                float w = j / (float)side * Mathf.PI * 2f;
                var dir = ring * Mathf.Cos(w) + Vector3.forward * Mathf.Sin(w);
                v[i * (side + 1) + j] = ring * R + dir * r;
                nrm[i * (side + 1) + j] = dir;
            }
        }
        int k = 0;
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < side; j++)
            {
                int a = i * (side + 1) + j, b = a + side + 1;
                tris[k++] = a; tris[k++] = a + 1; tris[k++] = b;
                tris[k++] = b; tris[k++] = a + 1; tris[k++] = b + 1;
            }
        var m = new Mesh { name = "SteeringRing", vertices = v, normals = nrm, triangles = tris };
        m.RecalculateBounds();
        System.IO.Directory.CreateDirectory(MaterialsFolder);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void SaveMesh(Mesh m)
    {
        System.IO.Directory.CreateDirectory(MaterialsFolder);
        string path = $"{MaterialsFolder}/{m.name.Replace('.', '_')}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
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

    static Transform Find(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = Find(c, name); if (r != null) return r; }
        return null;
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
            mat.SetFloat("_Mode", 3f); // Transparent
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
