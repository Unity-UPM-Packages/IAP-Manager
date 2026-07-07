using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheLegends.Base.IAP
{
    [CustomEditor(typeof(IAPSettings))]
    public class IAPSettingsEditor : Editor
    {
        private static IAPSettings instance = null;

        public static IAPSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<IAPSettings>(IAPSettings.FileName);
                }

                if (instance != null)
                {
                    Selection.activeObject = instance;
                }
                else
                {
                    Directory.CreateDirectory(IAPSettings.ResDir);

                    instance = CreateInstance<IAPSettings>();

                    string assetPath = Path.Combine(IAPSettings.ResDir, IAPSettings.FileName);
                    string assetPathWithExtension = Path.ChangeExtension(assetPath, IAPSettings.FileExtension);
                    AssetDatabase.CreateAsset(instance, assetPathWithExtension);
                    AssetDatabase.SaveAssets();
                }

                return instance;
            }
        }

        [MenuItem("TripSoft/IAP Settings")]
        public static void OpenInspector()
        {
            if (Instance == null)
            {
                Debug.Log("Creat new IAP Settings");
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("isAutoInit"), new GUIContent("Auto Initialize IAP Manager"));
            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("skuIDs"), new GUIContent("SKU IDs"), true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("isUseValidator"), new GUIContent("Use Receipt Validator"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
