using UnityEditor;
using UnityEngine;

namespace FishTacoGames
{
    [CustomEditor(typeof(GreenThumbManager))]
    public class GreenThumbManagerEditor : Editor
    {
        private readonly string newTagName = "GreenThumb";
        private Color RandomGreen() => new(Random.Range(0.0f, 0.2f), Random.Range(0.3f, 0.6f), Random.Range(0.0f, 0.2f));
        private bool useNames = false;
        private int terrainsampleIndex = 0;
        private int cellSampleIndex = 0;
        public Texture2D sliderBack;
        public Texture2D sliderThumb;
        public Texture2D Title;
        private SerializedObject greenThumbGlobalObject;
        private SerializedProperty replacementListsProp;
        private void Awake()
        {
            CheckTag();
        }
        private void OnEnable()
        {
            GreenThumbGlobalData greenThumbGlobal = AssetDatabase.LoadAssetAtPath<GreenThumbGlobalData>("Assets/FishTacoGames/GreenThumb/Saving/GreenThumbGlobalData.asset");
            if (greenThumbGlobal != null)
            {
                greenThumbGlobalObject = new SerializedObject(greenThumbGlobal);
                replacementListsProp = greenThumbGlobalObject.FindProperty("replacementLists");
            }
        }
        public override void OnInspectorGUI()
        {

            GUIStyle labelStyle = new(EditorStyles.boldLabel)
            {
                fontSize = 12,
                fixedHeight = 130
            };

            labelStyle.normal.textColor = RandomGreen();
            GUILayout.Label(Title, labelStyle);
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Cannot edit during runtime.", EditorStyles.boldLabel);
                return;
            }
            if (greenThumbGlobalObject == null)
            {
                EditorGUILayout.LabelField("No Data, use the FishTacoGames dropdown menu to create data", EditorStyles.boldLabel);
                if (GUILayout.Button("Refresh"))
                {
                    GreenThumbGlobalData greenThumbGlobal = AssetDatabase.LoadAssetAtPath<GreenThumbGlobalData>("Assets/FishTacoGames/GreenThumb/Saving/GreenThumbGlobalData.asset");
                    if (greenThumbGlobal != null)
                    {
                        greenThumbGlobalObject = new SerializedObject(greenThumbGlobal);
                        replacementListsProp = greenThumbGlobalObject.FindProperty("replacementLists");
                    }
                }
                return;
            }
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);
            terrainsampleIndex = EditorGUILayout.IntSlider(terrainsampleIndex, 0, GreenThumbManager.Instance.greenThumbGlobal.terrainCount - 1);
            var terrainT = Terrain.activeTerrains[terrainsampleIndex].transform;
            EditorGUILayout.LabelField(terrainT.name + " at " + terrainT.position.ToString(), EditorStyles.label);
            terrainT.GetComponent<TerrainCollider>().enabled = true;
            var cache = terrainT.GetComponent<GreenThumbCellManager>();
            bool created = cache.transform.childCount != 0 && cache.transform.GetChild(0).childCount != 0;
            if (cache.greenThumbLocal.GreenCellProfiles != null && cache.greenThumbLocal.GreenCellProfiles.Count != 0)
            {
                cellSampleIndex = EditorGUILayout.IntSlider(cellSampleIndex, 0, cache.greenThumbLocal.GreenCellProfiles.Count - 1);
                EditorGUILayout.LabelField("Cell " + cellSampleIndex, EditorStyles.label);
                if (created && GreenThumbManager.Instance.greenThumbGlobal.disableCells)
                {
                    // here is how the editor script updates the cells state in realtime
                    var cellsToEnable = GreenThumbGridLogic.HandleCall(cache.greenThumbLocal.GreenCellProfiles[cellSampleIndex].cellBounds.center, cache.greenThumbLocal.terraindata1.size, terrainT.position);
                    for (int i = 0; i < cache.greenThumbLocal.GreenCellProfiles.Count; i++)
                    {
                        if (cache.greenThumbLocal.GreenCellProfiles[i].cellMesh == null || cache.greenThumbLocal.GreenCellProfiles[i].cellMesh.gameObject == null)
                            continue;
                        if (cellsToEnable.Contains(i))
                        {
                            cache.SetCellActive(i);
                        }
                        else
                            cache.SetCellInactive(i);
                    }
                }
                else if (created)
                {
                    for (int i = 0; i < cache.greenThumbLocal.GreenCellProfiles.Count; i++)
                    {
                        if (cache.greenThumbLocal.GreenCellProfiles[i].cellMesh == null || cache.greenThumbLocal.GreenCellProfiles[i].cellMesh.gameObject == null)
                            continue;
                        cache.SetCellActive(i);
                    }
                }
            }
            if (GUILayout.Button("Regenerate Local Cells"))
            {
                if (created)
                    cache.ClearCellsGlobal();
                cache.GenerateGrid();
            }
            if (GUILayout.Button("Regenerate Global Cells"))
            {
                foreach (var dataT in Terrain.activeTerrains)
                {
                    var dataC = dataT.GetComponent<GreenThumbCellManager>();
                    if (created)
                        dataC.ClearCellsGlobal();
                    dataC.GenerateGrid();
                }
            }
            if (GUILayout.Button("Destroy Global Cells") && created)
            {
                foreach (var dataT in Terrain.activeTerrains)
                {
                    dataT.GetComponent<GreenThumbCellManager>().ClearCellsGlobal();
                }
            }
            if (GUILayout.Button("Destroy Local Cells") && created)
            {
                cache.ClearCellsGlobal();
            }
            if (GUILayout.Button("Enable Local Cells") && created)
            {
                for (int i = 0; i < cache.greenThumbLocal.GreenCellProfiles.Count; i++)
                {
                    if (cache.greenThumbLocal.GreenCellProfiles[i].cellMesh == null || cache.greenThumbLocal.GreenCellProfiles[i].cellMesh.gameObject == null)
                        continue;
                    cache.SetCellActive(i);
                }
            }
            if (GUILayout.Button("Disable Local Cells") && created)
            {
                for (int i = 0; i < cache.greenThumbLocal.GreenCellProfiles.Count; i++)
                {
                    if (cache.greenThumbLocal.GreenCellProfiles[i].cellMesh == null || cache.greenThumbLocal.GreenCellProfiles[i].cellMesh.gameObject == null)
                        continue;
                    cache.SetCellInactive(i);
                }
            }


            EditorGUILayout.LabelField("Range Options", EditorStyles.boldLabel);
            DrawRangeField("Grid Subdivision Level Global", ref GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal, 1, 50);
            DrawRangeField("Size of Detail Patch Removal", ref GreenThumbManager.Instance.greenThumbGlobal.sizeOfPatchesGlobal, 1, 10);
            // DrawRangeField("Render Physics Distance Global", ref GreenThumbManager.Instance.greenThumbGlobal.renderPhysicsDistanceGlobal, 1, 5);
            GreenThumbManager.Instance.greenThumbGlobal.extendPhysics = EditorGUILayout.Toggle("Use extended physics", GreenThumbManager.Instance.greenThumbGlobal.extendPhysics);
            //Nested Lists 
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(replacementListsProp, new GUIContent("Replacement Lists", "Each prototype name must match its target terrain tree prototype prefab name"), true);
            EditorGUILayout.LabelField("Other settings", EditorStyles.boldLabel);
            useNames = EditorGUILayout.Toggle("Automatic Naming", useNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (replacementListsProp != null && Terrain.activeTerrains != null)
                {
                    var data = Terrain.activeTerrains[terrainsampleIndex].GetComponent<Terrain>().terrainData;
                    int prototypeIndex = 0;
                    for (int i = 0; i < replacementListsProp.arraySize; i++)
                    {
                        SerializedProperty categoryProp = replacementListsProp.GetArrayElementAtIndex(i);
                        SerializedProperty mappingListProp = categoryProp.FindPropertyRelative("mappings");
                        SerializedProperty iDProp = categoryProp.FindPropertyRelative("categoryID");
                        string currentValue = iDProp.FindPropertyRelative("Name").stringValue;
                        iDProp.FindPropertyRelative("Index").intValue = i;
                        if (!useNames)
                        {
                            if (currentValue == "")
                                currentValue = "User Category";
                            iDProp.FindPropertyRelative("Name").stringValue = currentValue;
                        }
                        else if (useNames && data.treePrototypes.Length != 0)
                        {
                            var autoText = "Category " + GreenThumbManager.Instance.GetLetters().ToString();
                            iDProp.FindPropertyRelative("Name").stringValue = autoText;

                            for (int j = 0; j < mappingListProp.arraySize; j++)
                            {
                                SerializedProperty mappingProp = mappingListProp.GetArrayElementAtIndex(j);
                                mappingProp.FindPropertyRelative("prototypeName").stringValue = data.treePrototypes[prototypeIndex].prefab.name;
                                prototypeIndex++;
                                if (prototypeIndex >= data.treePrototypes.Length)
                                {
                                    prototypeIndex = 0;
                                }
                            }
                        }
                        if (GreenThumbManager.Instance.greenThumbGlobal.activePoolCount != GreenThumbManager.Instance.greenThumbGlobal.poolSizeGlobal)
                        {
                            GreenThumbManager.Instance.CreateReplacmentPool();
                        }
                    }
                }
            }
            GreenThumbManager.Instance.greenThumbGlobal.usePooling = EditorGUILayout.Toggle("Pool Objects For replacement", GreenThumbManager.Instance.greenThumbGlobal.usePooling);
            if (GreenThumbManager.Instance.greenThumbGlobal.usePooling)
            {
                EditorGUI.indentLevel++;
                DrawRangeField("Pool Count Per Category", ref GreenThumbManager.Instance.greenThumbGlobal.poolSizeGlobal, 10, 50);
                if (GreenThumbManager.Instance.greenThumbGlobal.activePoolCount != GreenThumbManager.Instance.greenThumbGlobal.poolSizeGlobal)
                {
                    GreenThumbManager.Instance.CreateReplacmentPool();
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                if (GreenThumbManager.Instance.transform.childCount != 0)
                    GreenThumbManager.Instance.DestroyPool();
            }

            GreenThumbManager.Instance.greenThumbGlobal.generateMeshForCells = EditorGUILayout.Toggle("Generate Mesh for Cells", GreenThumbManager.Instance.greenThumbGlobal.generateMeshForCells);
            GreenThumbManager.Instance.greenThumbGlobal.disableCells = EditorGUILayout.Toggle("Disable Cells At Runtime?", GreenThumbManager.Instance.greenThumbGlobal.disableCells);
            GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal = EditorGUILayout.Toggle("Debug Global", GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal);
            EditorGUILayout.Space();
            if (GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal)
                DrawGridVisualization(GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal, cellSampleIndex);
            if (GUI.changed)
            {
                GreenThumbManager.Instance.terrainsampleIndex = terrainsampleIndex;
                GreenThumbManager.Instance.cellSampleIndex = cellSampleIndex;
                greenThumbGlobalObject.ApplyModifiedProperties();
            }
        }
        void CheckTag()
        {
            SerializedObject tagManager = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProperty = tagManager.FindProperty("tags");

            bool tagAlreadyExists = false;

            // Check if the tag already exists
            for (int i = 0; i < tagsProperty.arraySize; i++)
            {
                SerializedProperty tag = tagsProperty.GetArrayElementAtIndex(i);
                if (tag.stringValue == newTagName)
                {
                    tagAlreadyExists = true;
                    break;
                }
            }

            // If the tag doesn't exist, add it
            if (!tagAlreadyExists)
            {
                tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
                SerializedProperty newTag = tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1);
                newTag.stringValue = newTagName;
                tagManager.ApplyModifiedProperties();
                Debug.Log("Tag '" + newTagName + "' created successfully!");
            }
        }

        void DrawRangeField(string label, ref int value, int minValue, int maxValue, bool includeZero = false)
        {
            GUILayout.BeginHorizontal();
            GUIStyle sliderStyle = new(GUI.skin.textField);
            sliderStyle.normal.background = sliderBack;
            GUIStyle thumbStyle = new(GUI.skin.horizontalSliderThumb);
            thumbStyle.normal.background = sliderThumb;
            EditorGUILayout.PrefixLabel(label);
            GUILayout.Label(value.ToString(), GUILayout.Width(40));
            EditorGUI.BeginChangeCheck();
            float newValue = GUILayout.HorizontalSlider(value, minValue, maxValue, sliderStyle, thumbStyle);
            if (EditorGUI.EndChangeCheck())
            {
                if (!includeZero)
                {
                    value = Mathf.RoundToInt(newValue);
                }
                else
                {
                    value = Mathf.FloorToInt(newValue);
                }
            }
            GUILayout.EndHorizontal();
        }
        // TODO: allow rectangular terrains
        void DrawGridVisualization(int gridSize, int playerCoord)
        {
            EditorGUILayout.LabelField("World Space Forward -->", EditorStyles.boldLabel);
            float cellSize = 500 / gridSize;
            int id = 0;
            for (int y = 0; y < gridSize; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < gridSize; x++)
                {
                    if (id == playerCoord)
                        GUI.backgroundColor = Color.red;
                    else
                        GUI.backgroundColor = RandomGreen();
                    GUILayout.Box(id.ToString(), GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                    id++;
                }
                EditorGUILayout.EndHorizontal();

            }
        }
    }
}