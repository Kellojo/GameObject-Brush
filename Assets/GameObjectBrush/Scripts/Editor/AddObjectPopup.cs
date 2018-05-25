using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameObjectBrush {

    /// <summary>
    /// Class that is responsible for the addition of new brushes to the list of brushObjects in the main editor windo class: "GameObjectBrushEditor"
    /// </summary>
    public class AddObjectPopup : EditorWindow {

        private GameObject obj2Add;
        public List<BrushObject> brushes;
        public EditorWindow parent;

        //initialize the popup window
        public static void Init(List<BrushObject> brushes, EditorWindow parent) {
            //create window
            AddObjectPopup window = ScriptableObject.CreateInstance<AddObjectPopup>();

            //cache the brushes from the main editor window for later use
            window.brushes = brushes;
            //cache the reference to the parent for later repaint
            window.parent = parent;

            //calculate window position (center of the parent window)
            float x = parent.position.x + (parent.position.width - 350) * 0.5f;
            float y = parent.position.y + (parent.position.height - 75) * 0.5f;
            window.position = new Rect(x, y, 350, 75);

            window.ShowPopup();
        }

        /// <summary>
        /// Creates the gui when called
        /// </summary>
        void OnGUI() {
            //create the "title" label
            EditorGUILayout.LabelField("Add GameObject to Brushes", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            //create the object field for the gameobject
            obj2Add = (GameObject) EditorGUILayout.ObjectField("GameObject", obj2Add, typeof(GameObject), false);

            //make sure we have some nice (?) spacing and all button next to each other (horizontally)
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            //Adds the gameobject to the brushes from the main window and closes the popup
            if (GUILayout.Button("Add")) {
                if (obj2Add != null) {
                    brushes.Add(new BrushObject(obj2Add));
                    parent.Repaint();
                }
                this.Close();
            }
            //close the popup
            if (GUILayout.Button("Cancel")) {
                this.Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}