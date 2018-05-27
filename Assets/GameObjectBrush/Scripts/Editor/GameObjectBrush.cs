using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GameObjectBrush {

    /// <summary>
    /// The main class of this extension/tool that handles the ui and the brush/paint functionality
    /// </summary>
    public class GameObjectBrushEditor : EditorWindow {

        //version variable
        private static string version = "v2.0";

        //some utility vars used to determine if the editor window is open
        public static GameObjectBrushEditor Instance { get; private set; }
        public static bool IsOpen {
            get { return Instance != null; }
        }

        //custom vars that hold the brushes, the current brush, the copied brush settings/details, the scroll position of the scroll view and all previously spawned objects
        private List<BrushObject> brushes = new List<BrushObject>();
        public BrushObject currentBrush = null;
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private Vector2 scrollViewScrollPosition = new Vector2();
        private BrushObject copy = null;

        /// <summary>
        /// Method that creates the window initially
        /// </summary>
        [MenuItem("Tools/GameObject Brush")]
        public static void ShowWindow() {
            //Show existing window instance. If one doesn't exist, make one.
            DontDestroyOnLoad(GetWindow<GameObjectBrushEditor>("GO Brush " + version));
        }

        public void OnGUI() {
            SerializedObject so = new SerializedObject(this);
            EditorGUIUtility.wideMode = true;

            if (currentBrush != null && currentBrush.brushObject != null) {
                EditorGUILayout.LabelField("Your Brushes (Current: " + currentBrush.brushObject.name + ")", EditorStyles.boldLabel);
            } else {
                EditorGUILayout.LabelField("Your Brushes", EditorStyles.boldLabel);
            }

            //scroll view
            scrollViewScrollPosition = EditorGUILayout.BeginScrollView(scrollViewScrollPosition, false, false);
            EditorGUILayout.BeginHorizontal();
            foreach(BrushObject brObj in brushes) {

                Color guiColor = GUI.backgroundColor;
                if (brObj == currentBrush) {
                    GUI.backgroundColor = Color.cyan;
                }

                GUIContent btnContent = new GUIContent(AssetPreview.GetAssetPreview(brObj.brushObject), "Select the " + brObj.brushObject.name + " brush");
                if (GUILayout.Button(btnContent, GUILayout.Width(100), GUILayout.Height(100))) {
                    currentBrush = brObj;
                }
                GUI.backgroundColor = guiColor;
            }

            //add button
            if (GUILayout.Button("+", GUILayout.Width(100), GUILayout.Height(100))) {
                AddObjectPopup.Init(brushes, this);
            }


            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            //gui below the scroll view
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Brush")) {
                AddObjectPopup.Init(brushes, this);
            }
            if (GUILayout.Button("Remove Current Brush")) {
                if (currentBrush != null) {
                    brushes.Remove(currentBrush);
                    currentBrush = null;
                }
            }
            if (GUILayout.Button("Clear Brushes")) {
                brushes.Clear();
                currentBrush = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Permanently Apply Spawned GameObjects (" + spawnedObjects.Count + ")")) {
                ApplyMeshedPermanently();
            }
            if (GUILayout.Button("Remove All Spawned GameObjects (" + spawnedObjects.Count + ")")) {
                RemoveAllSpawnedObjects();
            }

            //don't show the details of the current brush if we do not have selected a current brush
            if (currentBrush != null && currentBrush.brushObject != null) {
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Brush Details", EditorStyles.boldLabel);

                if (GUILayout.Button("Copy", GUILayout.MaxWidth(50))) {
                    copy = currentBrush;
                }
                if (GUILayout.Button("Paste", GUILayout.MaxWidth(50))) {
                    currentBrush.PasteDetails(copy);
                }
                if (GUILayout.Button("Reset", GUILayout.MaxWidth(50))) {
                    currentBrush.ResetDetails();
                }
                EditorGUILayout.EndHorizontal();

                currentBrush.density = EditorGUILayout.Slider("Density", currentBrush.density, 0f, 5f);
                currentBrush.brushSize = EditorGUILayout.Slider("Brush Size", currentBrush.brushSize, 0f, 25f);
                currentBrush.offsetFromPivot = EditorGUILayout.Vector3Field("Offset from Pivot", currentBrush.offsetFromPivot);
                currentBrush.rotOffsetFromPivot = EditorGUILayout.Vector3Field("Rotational Offset", currentBrush.rotOffsetFromPivot);


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Min and Max Scale");
                EditorGUILayout.MinMaxSlider(ref currentBrush.minScale, ref currentBrush.maxScale, 0.001f, 50);
                currentBrush.minScale = EditorGUILayout.FloatField(currentBrush.minScale);
                currentBrush.maxScale = EditorGUILayout.FloatField(currentBrush.maxScale);
                EditorGUILayout.EndHorizontal();

                currentBrush.alignToSurface = EditorGUILayout.Toggle("Align to Surface", currentBrush.alignToSurface);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(so.FindProperty("currentBrush").FindPropertyRelative("randomizeXRotation"), true);
                EditorGUILayout.PropertyField(so.FindProperty("currentBrush").FindPropertyRelative("randomizeYRotation"), true);
                EditorGUILayout.PropertyField(so.FindProperty("currentBrush").FindPropertyRelative("randomizeZRotation"), true);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(so.FindProperty("currentBrush").FindPropertyRelative("allowIntercollision"), true);



                EditorGUILayout.Space();
                EditorGUILayout.Space();


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
                if (GUILayout.Button("Copy", GUILayout.MaxWidth(50))) {
                    copy = currentBrush;
                }
                if (GUILayout.Button("Paste", GUILayout.MaxWidth(50))) {
                    currentBrush.PasteFilters(copy);
                }
                if (GUILayout.Button("Reset", GUILayout.MaxWidth(50))) {
                    currentBrush.ResetFilters();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Min and Max Slope");
                EditorGUILayout.MinMaxSlider(ref currentBrush.minSlope, ref currentBrush.maxSlope, 0, 360);
                currentBrush.minSlope = EditorGUILayout.FloatField(currentBrush.minSlope);
                currentBrush.maxSlope = EditorGUILayout.FloatField(currentBrush.maxSlope);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(so.FindProperty("currentBrush").FindPropertyRelative("layerFilter"), true);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(so.FindProperty("currentBrush").FindPropertyRelative("isTagFilteringEnabled"), true);
                if (currentBrush.isTagFilteringEnabled) {
                    currentBrush.tagFilter = EditorGUILayout.TagField("Tag Filter", currentBrush.tagFilter);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                so.ApplyModifiedProperties();
            }
        }
        void OnEnable() {
            SceneView.onSceneGUIDelegate += SceneGUI;
            Instance = this;
        }

        /// <summary>
        /// Delegate that handles Scene GUI events
        /// </summary>
        /// <param name="sceneView"></param>
        void SceneGUI(SceneView sceneView) {
            //don't do anything if the gameobject brush window is not open
            if (!IsOpen) {
                return;
            }

            //Draw Brush in the scene view
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (currentBrush != null && Physics.Raycast(ray, out hit)) {
                Color color = Color.cyan;
                color.a = 0.25f;
                Handles.color = color;
                Handles.DrawSolidArc(hit.point, hit.normal, Vector3.Cross(hit.normal, ray.direction), 360, currentBrush.brushSize);
                Handles.color = Color.white;
                Handles.DrawLine(hit.point, hit.point + hit.normal * 5);
            }

            //Check for the currently selected tool
            if (Tools.current != Tool.View) {
                //check if the bursh is used
                if (Event.current.rawType == EventType.MouseDown) {
                    //check used mouse button
                    if (Event.current.button == 0 && PlaceObjects()) {
                        Event.current.Use();
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    }
                    //check used mouse button
                    if (Event.current.button == 1) {
                        if (RemoveObjects()) {
                            Event.current.Use();
                        }
                    }
                }
                //check if the bursh is used
                if (Event.current.rawType == EventType.MouseDrag) {
                    //check used mouse button
                    if (Event.current.button == 0 && PlaceObjects()) {
                        Event.current.Use();
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    }
                    //check used mouse button
                    if (Event.current.button == 1) {
                        if (RemoveObjects()) {
                            Event.current.Use();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Places the objects
        /// returns true if objects were placed, false otherwise
        /// </summary>
        private bool PlaceObjects() {
            bool hasPlacedObjects = false;

            //loop as long as we have not reached the max ammount of objects to spawn per call/brush usage (calculated by currentBrush.density * currentBrush.brushSize)
            int spawnCount = Mathf.RoundToInt(currentBrush.density * currentBrush.brushSize);
            if (spawnCount < 1) {
                spawnCount = 1;
            }
            for (int i = 0; i < spawnCount; i++) {

                //create gameobjects of the given type if possible
                if (currentBrush.brushObject != null && IsOpen) {

                    //raycast from the scene camera to find the position of the brush and create objects there
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    ray.origin += new Vector3(Random.Range(0, currentBrush.brushSize), Random.Range(0, currentBrush.brushSize), Random.Range(0, currentBrush.brushSize));
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit)) {
                        //return if we are hitting an object that we have just spawned or don't if allowIntercollisionPlacement is enabled on the current brush
                        if (spawnedObjects.Contains(hit.collider.gameObject) && !currentBrush.allowIntercollision) {
                            continue;
                        }

                        //calculate the angle and abort if it is not in the specified range/filter
                        float angle = Vector3.Angle(Vector3.up, hit.normal);
                        if (angle < currentBrush.minSlope || angle > currentBrush.maxSlope) {
                            continue;
                        }

                        //check if the layer of the hit object is in our layermask filter
                        if (currentBrush.layerFilter != (currentBrush.layerFilter | (1 << hit.transform.gameObject.layer))) {
                            continue;
                        }

                        //check if tag filtering is active, if so check the tags
                        if (currentBrush.isTagFilteringEnabled && hit.transform.tag != currentBrush.tagFilter) {
                            continue;
                        }

                        //randomize position
                        Vector3 position = hit.point + currentBrush.offsetFromPivot;

                        //instantiate object
                        GameObject obj = Instantiate(currentBrush.brushObject, position, Quaternion.identity);
                        hasPlacedObjects = true;

                        //register created objects to the undo stack
                        Undo.RegisterCreatedObjectUndo(obj, "Created " + obj.name + " with brush");

                        //check if we should align the object to the surface we are "painting" on
                        if (currentBrush.alignToSurface) {
                            obj.transform.up = hit.normal;
                        }

                        //Randomize rotation
                        Vector3 rot = currentBrush.rotOffsetFromPivot;
                        if (currentBrush.randomizeXRotation)
                            rot.x = Random.Range(0, 360);
                        if (currentBrush.randomizeYRotation)
                            rot.y = Random.Range(0, 360);
                        if (currentBrush.randomizeZRotation)
                            rot.z = Random.Range(0, 360);

                        //apply rotation
                        obj.transform.Rotate(rot, Space.Self);

                        //randomize scale
                        float scale = Random.Range(currentBrush.minScale, currentBrush.maxScale);
                        obj.transform.localScale = new Vector3(scale, scale, scale);
                        
                        //Add object to list so it can be removed later on
                        spawnedObjects.Add(obj);
                    }
                }
            }

            return hasPlacedObjects;
        }
        /// <summary>
        /// remove objects that are in the brush radius around the brush.
        /// It returns true if it removed something, false otherwise
        /// </summary>
        private bool RemoveObjects() {
            bool hasRemovedSomething = false;

            //raycast to fin brush position
            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            List<GameObject> objsToRemove = new List<GameObject>();
            if (Physics.Raycast(ray, out hit)) {

                //loop over all spawned objects to find objects thar can be removed
                foreach (GameObject obj in spawnedObjects) {
                    if (obj != null && Vector3.Distance(obj.transform.position, hit.point) < currentBrush.brushSize) {
                        objsToRemove.Add(obj);
                    }
                }

                //delete the before found objects
                foreach (GameObject obj in objsToRemove) {
                    spawnedObjects.Remove(obj);
                    DestroyImmediate(obj);
                    hasRemovedSomething = true;
                }
                objsToRemove.Clear();
            }

            return hasRemovedSomething;
        }

        /// <summary>
        /// Applies all currently spawned objects, so they can not be removed by the brush
        /// </summary>
        private void ApplyMeshedPermanently() {
            spawnedObjects = new List<GameObject>();
        }
        /// <summary>
        /// Removes all spawned gameobjects that can be modified by the brush
        /// </summary>
        private void RemoveAllSpawnedObjects() {
            foreach(GameObject obj in spawnedObjects) {
                DestroyImmediate(obj);
            }
            spawnedObjects.Clear();
        }
    }

    /// <summary>
    /// Class that is responsible for holding information about a brush, such as the prefab/gameobject, size, density, etc.
    /// </summary>
    [System.Serializable]
    public class BrushObject {
        public GameObject brushObject;

        public bool allowIntercollision = false;
        public bool alignToSurface = false;
        public bool randomizeXRotation = false;
        public bool randomizeYRotation = true;
        public bool randomizeZRotation = false;
        [Range(0, 1)] public float density = 1f;
        [Range(0, 100)] public float brushSize = 5f;
        [Range(0, 10)] public float minScale = 0.5f;
        [Range(0, 10)] public float maxScale = 1.5f;
        [Tooltip("The offset applied to the pivot of the brushObject. This is usefull if you find that the placed GameObjects are floating/sticking in the ground too much.")] public Vector3 offsetFromPivot = Vector3.zero;
        [Tooltip("The offset applied to the rotation of the brushObject.")] public Vector3 rotOffsetFromPivot = Vector3.zero;


        /* filters */
        [Range(0, 360)] public float minSlope = 0f;
        [Range(0, 360)] public float maxSlope = 360f;
        public LayerMask layerFilter = ~0;

        public bool isTagFilteringEnabled = false;
        public string tagFilter = "";


        public BrushObject(GameObject obj) {
            this.brushObject = obj;
        }

        /// <summary>
        /// Pastes the details from another brush
        /// </summary>
        public void PasteDetails(BrushObject brush) {
            if (brush != null) {
                allowIntercollision = brush.allowIntercollision;
                alignToSurface = brush.alignToSurface;
                randomizeXRotation = brush.randomizeXRotation;
                randomizeYRotation = brush.randomizeYRotation;
                randomizeZRotation = brush.randomizeZRotation;
                density = brush.density;
                brushSize = brush.brushSize;
                minScale = brush.minScale;
                maxScale = brush.maxScale;
                offsetFromPivot = brush.offsetFromPivot;
                rotOffsetFromPivot = brush.rotOffsetFromPivot;
            }
        }
        /// <summary>
        /// Pastes the filters from another brush
        /// </summary>
        public void PasteFilters(BrushObject brush) {
            if (brush != null) {
                minSlope = brush.minSlope;
                maxSlope = brush.maxSlope;
                layerFilter = brush.layerFilter;
                isTagFilteringEnabled = brush.isTagFilteringEnabled;
                tagFilter = brush.tagFilter;
            }
        }

        /// <summary>
        /// Resets the filters on this bursh
        /// </summary>
        public void ResetFilters() {
            minSlope = 0f;
            maxSlope = 360f;
            layerFilter = ~0;
            isTagFilteringEnabled = false;
            tagFilter = "";
        }
        /// <summary>
        /// Resets the details of this brush
        /// </summary>
        public void ResetDetails() {
            allowIntercollision = false;
            alignToSurface = false;
            randomizeXRotation = false;
            randomizeYRotation = true;
            randomizeZRotation = false;
            density = 1f;
            brushSize = 5f;
            minScale = 0.5f;
            maxScale = 1.5f;
            offsetFromPivot = Vector3.zero;
            rotOffsetFromPivot = Vector3.zero;
        }
    }
}
