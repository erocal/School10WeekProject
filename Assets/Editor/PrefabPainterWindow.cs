using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class PrefabPainterWindow : EditorWindow
{
    private GameObject prefabToPlace;
    private bool placementMode = false;

    private bool alignToSurfaceNormal = true;
    private bool parentToHitObject = false;
    private bool fallbackToGroundPlane = true;
    private float yPlane = 0f;
    private Vector3 positionOffset = Vector3.zero;
    private Vector3 rotationOffset = Vector3.zero;

    [MenuItem("Tools/Prefab Painter")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPainterWindow>("Prefab Painter");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab 場景放置工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        prefabToPlace = (GameObject)EditorGUILayout.ObjectField(
            "要放置的 Prefab",
            prefabToPlace,
            typeof(GameObject),
            false
        );

        EditorGUILayout.Space();
        GUILayout.Label("放置選項", EditorStyles.boldLabel);

        alignToSurfaceNormal = EditorGUILayout.Toggle("貼齊表面法線", alignToSurfaceNormal);
        parentToHitObject = EditorGUILayout.Toggle("設為被點擊物件子物件", parentToHitObject);
        fallbackToGroundPlane = EditorGUILayout.Toggle("沒打到 Collider 時落到平面", fallbackToGroundPlane);
        yPlane = EditorGUILayout.FloatField("平面高度 Y", yPlane);

        positionOffset = EditorGUILayout.Vector3Field("位置偏移", positionOffset);
        rotationOffset = EditorGUILayout.Vector3Field("旋轉偏移", rotationOffset);

        EditorGUILayout.Space();

        GUI.backgroundColor = placementMode ? Color.green : Color.white;
        if (GUILayout.Button(placementMode ? "停止放置模式" : "開始放置模式", GUILayout.Height(30)))
        {
            placementMode = !placementMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "使用方法：\n" +
            "1. 指定 Prefab\n" +
            "2. 開啟放置模式\n" +
            "3. 到 Scene 視窗用左鍵點擊表面\n" +
            "4. 按 Esc 可快速退出放置模式",
            MessageType.Info
        );
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!placementMode || prefabToPlace == null)
            return;

        Event e = Event.current;
        if (e == null)
            return;

        // 讓 SceneView 事件優先由這個工具接管
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        // ESC 離開放置模式
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            placementMode = false;
            e.Use();
            Repaint();
            SceneView.RepaintAll();
            return;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 10000f);

        Vector3 previewPosition = Vector3.zero;
        Quaternion previewRotation = Quaternion.identity;
        Transform hitTransform = null;

        if (hasHit)
        {
            previewPosition = hit.point;
            previewRotation = alignToSurfaceNormal
                ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                : Quaternion.identity;

            hitTransform = hit.collider != null ? hit.collider.transform : null;
        }
        else if (fallbackToGroundPlane)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, yPlane, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                previewPosition = ray.GetPoint(enter);
                previewRotation = Quaternion.identity;
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        previewPosition += positionOffset;
        previewRotation *= Quaternion.Euler(rotationOffset);

        // Scene 視窗預覽
        DrawPreview(previewPosition, previewRotation);

        // 左鍵點擊放置（避免 Alt+左鍵影響 Scene 導航）
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            PlacePrefab(previewPosition, previewRotation, hitTransform);
            e.Use();
        }

        sceneView.Repaint();
    }

    private void DrawPreview(Vector3 position, Quaternion rotation)
    {
        if (prefabToPlace == null) return;

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(position, Vector3.up, 0.5f);
        Handles.ArrowHandleCap(
            0,
            position,
            rotation,
            1.0f,
            EventType.Repaint
        );

        // 顯示名稱
        Handles.Label(position + Vector3.up * 0.2f, $"預覽: {prefabToPlace.name}");
    }

    private void PlacePrefab(Vector3 position, Quaternion rotation, Transform hitTransform)
    {
        GameObject newObj = PrefabUtility.InstantiatePrefab(prefabToPlace, SceneManager.GetActiveScene()) as GameObject;

        if (newObj == null)
        {
            Debug.LogError("Prefab 放置失敗。");
            return;
        }

        Undo.RegisterCreatedObjectUndo(newObj, "Place Prefab");

        newObj.transform.position = position;
        newObj.transform.rotation = rotation;

        if (parentToHitObject && hitTransform != null)
        {
            Undo.SetTransformParent(newObj.transform, hitTransform, "Set Prefab Parent");
        }

        Selection.activeGameObject = newObj;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}