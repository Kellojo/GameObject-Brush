 using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace GameObjectBrush {

    /// <summary>
    /// The main class of this extension/tool that handles the ui and the brush/paint functionality
    /// </summary>
    [InitializeOnLoad]
    public class GameObjectBrushEditor : EditorWindow
    {

        #region Properties

        //version variable
        private static string version = "v3.1";

        //colors
        public static Color red = ColorFromRGB(239, 80, 80);
        public static Color green = ColorFromRGB(93, 173, 57);
        public static Color yellow = ColorFromRGB(237, 199, 61);
        public static Color SelectedColor = ColorFromRGB(139, 150, 165);
        public static Color PrimarySelectedColor = ColorFromRGB(10, 153, 220);

        //some utility vars used to determine if the editor window is open
        public static GameObjectBrushEditor Instance { get; private set; }
        public static bool IsOpen
        {
            get { return Instance != null; }
        }

        //custom vars that hold the brushes, the current brush, the copied brush settings/details, the scroll position of the scroll view and all previously spawned objects
        public BrushList brushes;
        public List<BrushObject> currentBrushes = new List<BrushObject>();
        public string activeBrushList = "DefaultBrushSet";

        public BrushObject selectedBrush = null;                                    //The currently selected/viewes brush (has to be public in order to be accessed by the FindProperty method)
        //private List<GameObject> spawnedObjects = new List<GameObject>();  //moved to brush object!
        private Vector2 scrollViewScrollPosition = new Vector2();
        private BrushObject copy = null;

        private bool isErasingEnabled = true;
        private bool isPlacingEnabled = true;

        #endregion


        /// <summary>
        /// Method that creates the window initially
        /// </summary>
        [MenuItem("Tools/GameObject Brush")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
           DontDestroyOnLoad(EditorWindow.GetWindow<GameObjectBrushEditor>("GO Brush " + version));
        }

        void OnEnable()
        {
            if (EditorPrefs.HasKey(activeBrushList))
            {
                string objectPath = EditorPrefs.GetString(activeBrushList);
                brushes = AssetDatabase.LoadAssetAtPath(objectPath, typeof(BrushList)) as BrushList;
            }
            if (brushes == null || brushes.brushList == null)
            {
                CreateNewBrushList();
            }

            SceneView.onSceneGUIDelegate += SceneGUI;
            Instance = this;

            this.autoRepaintOnSceneChange = true;

        }

        public void OnGUI()
        {
            
            if (!Application.isPlaying)
            {
                SerializedObject so = new SerializedObject(this);
                EditorGUIUtility.wideMode = true;

                #region Header

                //select first object by default;
                if (brushes != null && brushes.brushList.Count > 0)
                {
                    if(currentBrushes == null || currentBrushes.Count < 1)
                    {
                        currentBrushes = new List<BrushObject>();
                        currentBrushes.Add(brushes.brushList[0]);
                        selectedBrush = currentBrushes[0];
                    }

                }

                if (currentBrushes != null && currentBrushes.Count > 0)
                {
                    EditorGUILayout.LabelField("Your Brushes - (Current: " + GetCurrentBrushesString() + ")", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Your Brushes", EditorStyles.boldLabel);
                }

                #endregion

                #region Scroll view 
                //scroll view
                scrollViewScrollPosition = EditorGUILayout.BeginScrollView(scrollViewScrollPosition, false, false);
                int rowLength = 1;
                int maxRowLength = Mathf.FloorToInt((this.position.width - 35) / 100);
                if (maxRowLength < 1) {
                    maxRowLength = 1;
                }

                foreach (BrushObject brObj in brushes.brushList)
                {
                    //check if row is longer than max row length
                    if (rowLength > maxRowLength) {
                        rowLength = 1;
                        EditorGUILayout.EndHorizontal();
                    }
                    //begin row if rowLength == 1
                    if (rowLength == 1) {
                        EditorGUILayout.BeginHorizontal();
                    }

                    //change color
                    Color guiColor = GUI.backgroundColor;
                    if (currentBrushes.Contains(brObj))
                    {
                        GUI.backgroundColor = SelectedColor;
                        if (selectedBrush == brObj)
                        {
                            GUI.backgroundColor = PrimarySelectedColor;
                        }
                    }

                    //Create the brush entry in the scroll view and check if the user clicked on the created button (change the currently selected/edited brush accordingly and add it to the current brushes if possible)
                    GUIContent btnContent = new GUIContent(AssetPreview.GetAssetPreview(brObj.brushObject), brObj.brushObject.name);
                    if (GUILayout.Button(btnContent, GUILayout.Width(100), GUILayout.Height(100)))
                    {
                        //Add and remove brushes from the current brushes list
                        if (Event.current.control && !currentBrushes.Contains(brObj))
                        {
                            currentBrushes.Add(brObj);
                        }
                        else if (currentBrushes.Contains(brObj))
                        {
                            currentBrushes.Remove(brObj);
                        }

                        //select the currently edited brush and deselect all selected brushes
                        if (!Event.current.control)
                        {
                            currentBrushes.Clear();
                            selectedBrush = brObj;
                            currentBrushes.Add(brObj);
                        }
                    }

                    GUI.backgroundColor = guiColor;
                    rowLength++;
                }

                //check if row is longer than max row length
                if (rowLength > maxRowLength) {
                    rowLength = 1;
                    EditorGUILayout.EndHorizontal();
                }
                //begin row if rowLength == 1
                if (rowLength == 1) {
                    EditorGUILayout.BeginHorizontal();
                }

                //add button
                if (GUILayout.Button("+", GUILayout.Width(100), GUILayout.Height(100)))
                {
                    AddObjectPopup.Init(brushes.brushList, this);
                }
                Color guiColorBGC = GUI.backgroundColor;

                //end horizontal and scroll view again
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();

                #endregion

                #region Actions Group
                //gui below the scroll view

                //The active BrushList asset
                brushes = EditorGUILayout.ObjectField(brushes, typeof(Object), true) as BrushList;

                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = green;
                if (GUILayout.Button(new GUIContent("Add Brush", "Add a new brush to the selection.")))
                {
                    AddObjectPopup.Init(brushes.brushList, this);
                }

                EditorGUI.BeginDisabledGroup(currentBrushes.Count == 0 || selectedBrush == null);
                GUI.backgroundColor = red;
                //remove selected brushes button
                if (GUILayout.Button(new GUIContent("Remove Current Brush(es)", "Removes the currently selected brush.")))
                {
                    if (currentBrushes != null)
                    {
                        foreach (BrushObject brush in currentBrushes)
                        {
                            brushes.brushList.Remove(brush);
                        }
                        currentBrushes = new List<BrushObject>();
                    }
                }
                EditorGUI.EndDisabledGroup();
                //remove all brushes button
                EditorGUI.BeginDisabledGroup(brushes.brushList.Count == 0);
                if (GUILayout.Button(new GUIContent("Clear Brushes", "Removes all brushes.")))
                {
                    RemoveAllBrushes();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = guiColorBGC;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                isPlacingEnabled = EditorGUILayout.Toggle(new GUIContent("Painting ebanled", "Should painting of gameobjects via left click be enabled?"), isPlacingEnabled);
                isErasingEnabled = EditorGUILayout.Toggle(new GUIContent("Erasing ebanled", "Should erasing of gameobjects via right click be enabled?"), isErasingEnabled);
                EditorGUILayout.EndHorizontal();
                guiColorBGC = GUI.backgroundColor;

                if (currentBrushes.Count>0)
                {
                    EditorGUI.BeginDisabledGroup(selectedBrush.spawnedObjects.Count == 0);
                
                    GUI.backgroundColor = green;
                    if (GUILayout.Button(new GUIContent("Permanently Apply Spawned GameObjects (" + selectedBrush.spawnedObjects.Count + ")", "Permanently apply the gameobjects that have been spawned with GO brush, so they can not be erased by accident anymore.")))
                    {
                        ApplyCachedObjects();
                    }


                    GUI.backgroundColor = red;
                    if (GUILayout.Button(new GUIContent("Remove All Spawned GameObjects (" + selectedBrush.spawnedObjects.Count + ")", "Removes all spawned objects from the scene that have not been applied before.")))
                    {
                        RemoveAllSpawnedObjects();
                    }
                    EditorGUI.EndDisabledGroup();
                }



                GUI.backgroundColor = guiColorBGC;

                #endregion

                #region Brush Details
                //don't show the details of the current brush if we do not have selected a current brush
                if (currentBrushes != null && selectedBrush != null && brushes.brushList.Count > 0 && currentBrushes.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = yellow;
                    EditorGUILayout.LabelField("Brush Details" + " - (" + selectedBrush.brushObject.name + ")", EditorStyles.boldLabel);
                    if (GUILayout.Button(new GUIContent("Copy", "Copies the brush."), GUILayout.MaxWidth(50)))
                    {
                        copy = selectedBrush;
                    }
                    EditorGUI.BeginDisabledGroup(copy == null);
                    if (GUILayout.Button(new GUIContent("Paste", "Pastes the details of the brush in the clipboard."), GUILayout.MaxWidth(50)))
                    {
                        selectedBrush.PasteDetails(copy);
                    }
                    GUI.backgroundColor = guiColorBGC;
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(new GUIContent("Reset", "Restores the defaults settings of the brush details."), GUILayout.MaxWidth(50)))
                    {
                        selectedBrush.ResetDetails();
                    }
                    EditorGUILayout.EndHorizontal();

                    selectedBrush.parentContainer = EditorGUILayout.ObjectField("Parent", selectedBrush.parentContainer, typeof(Transform), true) as Transform;
                    selectedBrush.density = EditorGUILayout.Slider(new GUIContent("Density", "Changes the density of the brush, i.e. how many gameobjects are spawned inside the radius of the brush."), selectedBrush.density, 0f, 5f);
                    selectedBrush.brushSize = EditorGUILayout.Slider(new GUIContent("Brush Size", "The radius of the brush."), selectedBrush.brushSize, 0f, 25f);
                    selectedBrush.offsetFromPivot = EditorGUILayout.Vector3Field(new GUIContent("Offset from Pivot", "Changes the offset of the spawned gameobject from the calculated position. This allows you to correct the position of the spawned objects, if you find they are floating for example due to a not that correct pivot on the gameobject/prefab."), selectedBrush.offsetFromPivot);
                    selectedBrush.rotOffsetFromPivot = EditorGUILayout.Vector3Field(new GUIContent("Rotational Offset", "Changes the rotational offset that is applied to the prefab/gameobject when spawning it. This allows you to current the rotation of the spawned objects."), selectedBrush.rotOffsetFromPivot);


                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Min and Max Scale", "The min and max range of the spawned gameobject. If they are not the same value a random value in between the min and max is going to be picked."));
                    EditorGUILayout.MinMaxSlider(ref selectedBrush.minScale, ref selectedBrush.maxScale, 0.001f, 50);
                    selectedBrush.minScale = EditorGUILayout.FloatField(selectedBrush.minScale);
                    selectedBrush.maxScale = EditorGUILayout.FloatField(selectedBrush.maxScale);
                    EditorGUILayout.EndHorizontal();

                    selectedBrush.alignToSurface = EditorGUILayout.Toggle(new GUIContent("Align to Surface", "This option allows you to align the instantiated gameobjects to the surface you are painting on."), selectedBrush.alignToSurface);

                    EditorGUILayout.BeginHorizontal();
                    selectedBrush.randomizeXRotation = EditorGUILayout.Toggle(new GUIContent("Randomize X Rotation", "Should the rotation be randomized on the x axis?"), selectedBrush.randomizeXRotation);
                    selectedBrush.randomizeYRotation = EditorGUILayout.Toggle(new GUIContent("Randomize Y Rotation", "Should the rotation be randomized on the y axis?"), selectedBrush.randomizeYRotation);
                    selectedBrush.randomizeZRotation = EditorGUILayout.Toggle(new GUIContent("Randomize Z Rotation", "Should the rotation be randomized on the z axis?"), selectedBrush.randomizeZRotation);
                    EditorGUILayout.EndHorizontal();

                    selectedBrush.allowIntercollision = EditorGUILayout.Toggle(new GUIContent("Allow Intercollision", "Should the spawned objects be considered for the spawning of new objects? If so, newly spawned objects can be placed on top of previously (not yet applied) objects."), selectedBrush.allowIntercollision);


                    EditorGUILayout.Space();
                    EditorGUILayout.Space();


                    GUI.backgroundColor = yellow;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Filters" + " - (" + selectedBrush.brushObject.name + ")", EditorStyles.boldLabel);
                    if (GUILayout.Button(new GUIContent("Copy", "Copies the brush."), GUILayout.MaxWidth(50)))
                    {
                        copy = selectedBrush;
                    }
                    EditorGUI.BeginDisabledGroup(copy == null);
                    if (GUILayout.Button(new GUIContent("Paste", "Pastes the filters of the brush in the clipboard."), GUILayout.MaxWidth(50)))
                    {
                        selectedBrush.PasteFilters(copy);
                    }
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = guiColorBGC;
                    if (GUILayout.Button(new GUIContent("Reset", "Restores the defaults settings of the brush filters."), GUILayout.MaxWidth(50)))
                    {
                        selectedBrush.ResetFilters();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Min and Max Slope", "The range of slope that is required for an object to be placed. If the slope is not in that range, no object is going to be placed."));
                    EditorGUILayout.MinMaxSlider(ref selectedBrush.minSlope, ref selectedBrush.maxSlope, 0, 360);
                    selectedBrush.minSlope = EditorGUILayout.FloatField(selectedBrush.minSlope);
                    selectedBrush.maxSlope = EditorGUILayout.FloatField(selectedBrush.maxSlope);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(so.FindProperty("selectedBrush").FindPropertyRelative("layerFilter"), true);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(so.FindProperty("selectedBrush").FindPropertyRelative("isTagFilteringEnabled"), true);
                    if (selectedBrush.isTagFilteringEnabled)
                    {
                        selectedBrush.tagFilter = EditorGUILayout.TagField(new GUIContent("Tag Filter", "Limits the painting to objects that have a specific tag on them."), selectedBrush.tagFilter);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    so.ApplyModifiedProperties();
                }

                //save AssetDatabase on any change
                if (GUI.changed)
                {
                    UpdateBrushList();
                }
                #endregion
            }
        }
        public void OnDestroy()
        {
            UpdateBrushList();
            SceneView.onSceneGUIDelegate -= SceneGUI;
        }
        /// <summary>
        /// Delegate that handles Scene GUI events
        /// </summary>
        /// <param name="sceneView"></param>
        void SceneGUI(SceneView sceneView)
        {

            //don't do anything if the gameobject brush window is not open
            if (!IsOpen)
            {
                return;
            }

            //Draw Brush in the scene view
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (isPlacingEnabled && currentBrushes != null && Physics.Raycast(ray, out hit))
            {
                Color color = Color.cyan;
                color.a = 0.25f;
                Handles.color = color;
                Handles.DrawSolidArc(hit.point, hit.normal, Vector3.Cross(hit.normal, ray.direction), 360, GetMaximumBrushSizeFromCurrentBrushes());
                Handles.color = Color.white;
                Handles.DrawLine(hit.point, hit.point + hit.normal * 5);
            }

            //Check for the currently selected tool
            if (Tools.current != Tool.View)
            {
                //check if the bursh is used
                if (Event.current.rawType == EventType.MouseDown)
                {
                    //check used mouse button
                    if (Event.current.button == 0 && PlaceObjects())
                    {
                        Event.current.Use();
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    }
                    //check used mouse button
                    if (Event.current.button == 1)
                    {
                        if (RemoveObjects())
                        {
                            Event.current.Use();
                        }
                    }
                }
                //check if the bursh is used
                if (Event.current.rawType == EventType.MouseDrag)
                {
                    //check used mouse button
                    if (Event.current.button == 0 && PlaceObjects())
                    {
                        Event.current.Use();
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    }
                    //check used mouse button
                    if (Event.current.button == 1)
                    {
                        if (RemoveObjects())
                        {
                            Event.current.Use();
                        }
                    }
                }
            }
        }

        #region GO Brush functionality methods


        /// <summary>
        /// Places the objects
        /// returns true if objects were placed, false otherwise
        /// </summary>
        private bool PlaceObjects()
        {
            //only paint if painting is ebanled
            if (!isPlacingEnabled)
            {
                return false;
            }

            bool hasPlacedObjects = false;

            foreach (BrushObject brush in currentBrushes)
            {

                //loop as long as we have not reached the max ammount of objects to spawn per call/brush usage (calculated by currentBrush.density * currentBrush.brushSize)
                int spawnCount = Mathf.RoundToInt(brush.density * brush.brushSize);
                if (spawnCount < 1)
                {
                    spawnCount = 1;
                }

                for (int i = 0; i < spawnCount; i++)
                {

                    //create gameobjects of the given type if possible
                    if (brush.brushObject != null && IsOpen)
                    {

                        //raycast from the scene camera to find the position of the brush and create objects there
                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        ray.origin += new Vector3(Random.Range(-brush.brushSize, brush.brushSize), Random.Range(-brush.brushSize, brush.brushSize), Random.Range(-brush.brushSize, brush.brushSize));
                        Vector3 startPoint = ray.origin;
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit))
                        {

                            //return if we are hitting an object that we have just spawned or don't if allowIntercollisionPlacement is enabled on the current brush
                            if (selectedBrush.spawnedObjects.Contains(hit.collider.gameObject) && !brush.allowIntercollision)
                            {
                                continue;
                            }

                            //calculate the angle and abort if it is not in the specified range/filter
                            float angle = Vector3.Angle(Vector3.up, hit.normal);
                            if (angle < brush.minSlope || angle > brush.maxSlope)
                            {
                                continue;
                            }

                            //check if the layer of the hit object is in our layermask filter
                            if (brush.layerFilter != (brush.layerFilter | (1 << hit.transform.gameObject.layer)))
                            {
                                continue;
                            }

                            //check if tag filtering is active, if so check the tags
                            if (brush.isTagFilteringEnabled && hit.transform.tag != brush.tagFilter)
                            {
                                continue;
                            }

                            //randomize position
                            Vector3 position = hit.point + brush.offsetFromPivot;

                            //instantiate prefab or clone object
                            GameObject obj;
                            if (brush.brushObject.gameObject.scene.name != null)
                            {
                                obj = Instantiate(brush.brushObject, position, Quaternion.identity);
                            }
                            else
                            {
                                obj = PrefabUtility.InstantiatePrefab(brush.brushObject) as GameObject;
                                obj.transform.position = position;
                                obj.transform.rotation = Quaternion.identity;
                            }

                            //check for parent container
                            if (brush.parentContainer != null)
                            {
                                obj.transform.parent = brush.parentContainer;
                            }

                            hasPlacedObjects = true;

                            //register created objects to the undo stack
                            Undo.RegisterCreatedObjectUndo(obj, "Created " + obj.name + " with brush");

                            //check if we should align the object to the surface we are "painting" on
                            if (brush.alignToSurface)
                            {
                                obj.transform.up = hit.normal;
                            }

                            //Randomize rotation
                            Vector3 rot = brush.rotOffsetFromPivot;
                            if (brush.randomizeXRotation)
                                rot.x = Random.Range(0, 360);
                            if (brush.randomizeYRotation)
                                rot.y = Random.Range(0, 360);
                            if (brush.randomizeZRotation)
                                rot.z = Random.Range(0, 360);

                            //apply rotation
                            obj.transform.Rotate(rot, Space.Self);

                            //randomize scale
                            float scale = Random.Range(brush.minScale, brush.maxScale);
                            obj.transform.localScale = new Vector3(scale, scale, scale);

                            //Add object to list so it can be removed later on
                            selectedBrush.spawnedObjects.Add(obj);
                        }
                    }
                }
            }

            return hasPlacedObjects;
        }
        /// <summary>
        /// remove objects that are in the brush radius around the brush.
        /// It returns true if it removed something, false otherwise
        /// </summary>
        private bool RemoveObjects()
        {

            //return if erasing is disabled
            if (!isErasingEnabled)
            {
                return false;
            }

            bool hasRemovedSomething = false;

            foreach (BrushObject brush in currentBrushes)
            {
                //raycast to fin brush position
                RaycastHit hit;
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                List<GameObject> objsToRemove = new List<GameObject>();
                if (Physics.Raycast(ray, out hit))
                {

                    //loop over all spawned objects to find objects thar can be removed
                    foreach (GameObject obj in selectedBrush.spawnedObjects)
                    {
                        if (obj != null && Vector3.Distance(obj.transform.position, hit.point) < brush.brushSize)
                        {
                            objsToRemove.Add(obj);
                        }
                    }

                    //delete the before found objects
                    foreach (GameObject obj in objsToRemove)
                    {
                        selectedBrush.spawnedObjects.Remove(obj);
                        DestroyImmediate(obj);
                        hasRemovedSomething = true;
                    }
                    objsToRemove.Clear();
                }
            }

            return hasRemovedSomething;
        }

        /// <summary>
        /// Applies all currently spawned objects, so they can not be removed by the brush
        /// </summary>
        private void ApplyCachedObjects()
        {
            selectedBrush.spawnedObjects = new List<GameObject>();
        }
        /// <summary>
        /// Removes all spawned gameobjects that can be modified by the brush
        /// </summary>
        private void RemoveAllSpawnedObjects()
        {
            foreach (GameObject obj in selectedBrush.spawnedObjects)
            {
                DestroyImmediate(obj);
            }
            selectedBrush.spawnedObjects.Clear();
        }

        /// <summary>
        /// Removes all brushes, resets the currently selected brushes etc.
        /// </summary>
        private void RemoveAllBrushes() {
            brushes.brushList.Clear();
            selectedBrush = null;
            currentBrushes = new List<BrushObject>();
            copy = null;
        }

        #endregion

        #region Misc


        /// <summary>
        /// Iterates over the list of current brushes and adds the name of each brush to a string.
        /// </summary>
        /// <returns></returns>
        private string GetCurrentBrushesString()
        {
            string brushes = "";
            foreach (BrushObject brush in currentBrushes)
            {
                if (brushes != "")
                {
                    brushes += " ,";
                }
                brushes += brush.brushObject.name;
            }
            return brushes;
        }
        /// <summary>
        /// Get the greatest brush size value from the current brushes list
        /// </summary>
        /// <returns></returns>
        private float GetMaximumBrushSizeFromCurrentBrushes()
        {
            float maxBrushSize = 0f;
            foreach (BrushObject brush in currentBrushes)
            {
                if (brush.brushSize > maxBrushSize)
                {
                    maxBrushSize = brush.brushSize;
                }
            }
            return maxBrushSize;
        }
        /// <summary>
        /// Generates a Color object by r g b values (Range 0, 256)
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color ColorFromRGB(int r, int g, int b) {
            return new Color((float)r / 256, (float)g / 256, (float)b / 256);
        }

        /// <summary>
        /// Saves BrushList to AssetDatabase
        /// </summary>
        void UpdateBrushList()
        {
            EditorUtility.SetDirty(brushes);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Creates a new BrushList.asset and links to it.
        /// </summary>
        void CreateNewBrushList()
        {
            brushes = CreateBrushList.Create();
            if (brushes)
            {
                brushes.brushList = new List<BrushObject>();
                string relPath = AssetDatabase.GetAssetPath(brushes);
                EditorPrefs.SetString(activeBrushList, relPath);
            }
        }

        /// <summary>
        /// Opens a system window dialog to choose a BrushList.assset
        /// </summary>
        void OpenBrushList()
        {
            string absPath = EditorUtility.OpenFilePanel("Select Brush List", "", "");
            if (absPath.StartsWith(Application.dataPath))
            {
                string relPath = absPath.Substring(Application.dataPath.Length - "Assets".Length);
                brushes = AssetDatabase.LoadAssetAtPath(relPath, typeof(BrushList)) as BrushList;
                if (brushes.brushList == null)
                    brushes.brushList = new List<BrushObject>();
                if (brushes)
                {
                    EditorPrefs.SetString(activeBrushList, relPath);
                }
            }
        }

        #endregion
    }
}

