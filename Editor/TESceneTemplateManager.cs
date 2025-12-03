#if UNITY_EDITOR
using UnityEditor.SceneTemplate;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twinny.Editor
{
    public static class TESceneTemplateService 
    {
        public static SceneTemplateAsset GetTemplateByGUID2(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                UnityEngine.Debug.LogError($"[SceneTemplateLoader] Não achei asset pra GUID: {guid}");
                return null;
            }

            var asset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(path);
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"[SceneTemplateLoader] Asset no path existe, mas não é SceneTemplateAsset: {path}");
            }

            return asset;
        }

        public static Scene CreateTemplateScene(string sceneTemplateGUID, bool loadAdditively, string newSceneOutputPath = null) => CreateTemplateScene(GetTemplateByGUID2(sceneTemplateGUID),loadAdditively,newSceneOutputPath);
        public static Scene CreateTemplateScene(SceneTemplateAsset sceneTemplate, bool loadAdditively, string newSceneOutputPath = null)
        {


            var result = SceneTemplateService.Instantiate(sceneTemplate, loadAdditively, newSceneOutputPath);

            return result.scene;
        }
    }
}
#endif