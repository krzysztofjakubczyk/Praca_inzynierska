#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Narzędzie diagnostyczne: zapisuje układ sceny (collidery, triggery, czujniki, waypointy, spawnery,
/// pola skryptów) do pliku SceneDump/&lt;scena&gt;.json. Uruchomienie: Tools > Praca > Zrzut układu sceny,
/// albo automatycznie po przeładowaniu skryptów, jeśli istnieje plik Temp/scene_dump_request.
/// </summary>
public static class SceneLayoutDumper
{
    private const string RequestFile = "Temp/scene_dump_request";
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    [InitializeOnLoadMethod]
    private static void AutoRun()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            Dump();
        };
    }

    [MenuItem("Tools/Praca/Zrzut układu sceny")]
    public static void Dump()
    {
        Scene scene = SceneManager.GetActiveScene();
        var sb = new StringBuilder();
        var prefabs = new List<GameObject>();
        sb.Append("{\"scene\":").Append(Q(scene.name)).Append(",\"playing\":").Append(Application.isPlaying ? "true" : "false");

        sb.Append(",\"objects\":[");
        bool first = true;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                bool interesting = go.GetComponent<Collider>() != null || HasProjectScript(go);
                if (!interesting) continue;
                if (go.GetComponent<CarController>() != null && Application.isPlaying) continue; // pojazdy osobno
                if (!first) sb.Append(',');
                first = false;
                AppendObject(sb, go, prefabs);
            }
        }
        sb.Append(']');

        // Płaskie powierzchnie (jezdnie, przejścia) do rysowania mapy
        sb.Append(",\"surfaces\":[");
        first = true;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Bounds b = r.bounds;
                if (b.size.y > 1.5f || b.size.x * b.size.z < 4f) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"name\":").Append(Q(r.name)).Append(",\"min\":").Append(V(b.min)).Append(",\"max\":").Append(V(b.max)).Append('}');
            }
        }
        sb.Append(']');

        // Pojazdy w trakcie symulacji (tylko w trybie Play)
        sb.Append(",\"vehicles\":[");
        first = true;
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append("{\"pos\":").Append(V(car.transform.position)).Append(",\"fwd\":").Append(V(car.transform.forward))
              .Append(",\"v\":").Append(F(car.CurrentSpeed)).Append(",\"reason\":").Append(Q(car.limitingReason.ToString()))
              .Append(",\"left\":").Append(car.isTurningLeft ? "true" : "false").Append('}');
        }
        sb.Append(']');

        // Prefaby pojazdów (rozmiary colliderów, ustawienia ruchu)
        sb.Append(",\"prefabs\":[");
        var done = new HashSet<GameObject>();
        first = true;
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject p = prefabs[i];
            if (p == null || !done.Add(p)) continue;
            foreach (Transform t in p.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<Collider>() == null && !HasProjectScript(t.gameObject)) continue;
                if (!first) sb.Append(',');
                first = false;
                AppendObject(sb, t.gameObject, null, p.name);
            }
        }
        sb.Append("]}");

        Directory.CreateDirectory("SceneDump");
        string path = Path.GetFullPath(Path.Combine("SceneDump", scene.name + ".json"));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"[Zrzut sceny] Zapisano {path}");
    }

    private static bool HasProjectScript(GameObject go)
    {
        foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Assembly.GetName().Name == "Assembly-CSharp") return true;
        }
        return false;
    }

    private static void AppendObject(StringBuilder sb, GameObject go, List<GameObject> prefabs, string prefabName = null)
    {
        Transform t = go.transform;
        sb.Append("{\"id\":").Append(go.GetInstanceID())
          .Append(",\"path\":").Append(Q(PathOf(t)))
          .Append(",\"active\":").Append(go.activeInHierarchy ? "true" : "false")
          .Append(",\"tag\":").Append(Q(go.tag))
          .Append(",\"layer\":").Append(go.layer)
          .Append(",\"pos\":").Append(V(t.position))
          .Append(",\"rot\":").Append(V(t.rotation.eulerAngles))
          .Append(",\"scale\":").Append(V(t.lossyScale))
          .Append(",\"fwd\":").Append(V(t.forward));
        if (prefabName != null) sb.Append(",\"prefab\":").Append(Q(prefabName));

        sb.Append(",\"colliders\":[");
        bool first = true;
        foreach (Collider c in go.GetComponents<Collider>())
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append("{\"type\":").Append(Q(c.GetType().Name)).Append(",\"trigger\":").Append(c.isTrigger ? "true" : "false")
              .Append(",\"enabled\":").Append(c.enabled ? "true" : "false")
              .Append(",\"bmin\":").Append(V(c.bounds.min)).Append(",\"bmax\":").Append(V(c.bounds.max));
            if (c is BoxCollider box)
            {
                Vector3 size = Vector3.Scale(box.size, t.lossyScale);
                sb.Append(",\"center\":").Append(V(t.TransformPoint(box.center)))
                  .Append(",\"size\":").Append(V(new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z))));
            }
            sb.Append('}');
        }
        sb.Append(']');

        sb.Append(",\"scripts\":[");
        first = true;
        foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;
            if (!first) sb.Append(',');
            first = false;
            sb.Append("{\"type\":").Append(Q(mb.GetType().Name)).Append(",\"enabled\":").Append(mb.enabled ? "true" : "false").Append(",\"fields\":{");
            AppendFields(sb, mb, prefabs);
            sb.Append("}}");
        }
        sb.Append("]}");
    }

    private static void AppendFields(StringBuilder sb, Object target, List<GameObject> prefabs)
    {
        var so = new SerializedObject(target);
        SerializedProperty it = so.GetIterator();
        bool enter = true, first = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.name == "m_Script") continue;
            if (!first) sb.Append(',');
            first = false;
            sb.Append(Q(it.name)).Append(':').Append(Value(it, prefabs, 0));
        }
    }

    private static string Value(SerializedProperty p, List<GameObject> prefabs, int depth)
    {
        if (p.isArray && p.propertyType != SerializedPropertyType.String)
        {
            var sb = new StringBuilder("[");
            int n = Mathf.Min(p.arraySize, 300);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Value(p.GetArrayElementAtIndex(i), prefabs, depth + 1));
            }
            return sb.Append(']').ToString();
        }

        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.longValue.ToString(Ci);
            case SerializedPropertyType.Boolean: return p.boolValue ? "true" : "false";
            case SerializedPropertyType.Float: return F(p.doubleValue);
            case SerializedPropertyType.String: return Q(p.stringValue);
            case SerializedPropertyType.Enum:
                return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumNames.Length ? Q(p.enumNames[p.enumValueIndex]) : p.intValue.ToString(Ci);
            case SerializedPropertyType.Vector3: return V(p.vector3Value);
            case SerializedPropertyType.LayerMask: return p.intValue.ToString(Ci);
            case SerializedPropertyType.ObjectReference:
            {
                Object o = p.objectReferenceValue;
                if (o == null) return "null";
                GameObject go = o as GameObject ?? (o as Component)?.gameObject;
                if (go != null)
                {
                    if (EditorUtility.IsPersistent(go))
                    {
                        prefabs?.Add(go);
                        return "{\"asset\":" + Q(go.name) + "}";
                    }
                    return "{\"ref\":" + go.GetInstanceID() + ",\"name\":" + Q(go.name) + "}";
                }
                return "{\"asset\":" + Q(o.name) + "}";
            }
            case SerializedPropertyType.Generic:
            {
                if (depth > 3) return "null";
                var sb = new StringBuilder("{");
                SerializedProperty copy = p.Copy();
                SerializedProperty end = p.GetEndProperty();
                bool enter = true, first = true;
                while (copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
                {
                    enter = false;
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(Q(copy.name)).Append(':').Append(Value(copy, prefabs, depth + 1));
                }
                return sb.Append('}').ToString();
            }
            default: return Q(p.propertyType.ToString());
        }
    }

    private static string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static string V(Vector3 v) => "[" + F(v.x) + "," + F(v.y) + "," + F(v.z) + "]";
    private static string F(double d) => double.IsInfinity(d) || double.IsNaN(d) ? "null" : d.ToString("0.###", Ci);

    private static string Q(string s)
    {
        if (s == null) return "null";
        var sb = new StringBuilder("\"");
        foreach (char ch in s)
        {
            if (ch == '"' || ch == '\\') sb.Append('\\').Append(ch);
            else if (ch < 32) sb.Append(' ');
            else sb.Append(ch);
        }
        return sb.Append('"').ToString();
    }
}
#endif
