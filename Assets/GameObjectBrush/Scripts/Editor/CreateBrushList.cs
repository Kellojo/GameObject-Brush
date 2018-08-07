using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameObjectBrush
{
    public static class CreateBrushList
    {
        //[MenuItem("Tools/Create List")]
        public static BrushList Create()
        {
            BrushList asset = ScriptableObject.CreateInstance<BrushList>();
            //Use Untiy's Application Path to find Assets folder and create or use Brushes folder
            string _FP = Application.dataPath + Path.DirectorySeparatorChar + "Brushes";
            Directory.CreateDirectory(_FP);
            AssetDatabase.CreateAsset(asset, "Assets/Brushes/BrushList.asset");
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
