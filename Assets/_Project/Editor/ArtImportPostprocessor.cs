using BTH.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BTH.Editor
{
    public sealed class ArtImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/") &&
                !assetPath.StartsWith("Assets/_Project/Art/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (importer.filterMode == FilterMode.Point &&
                importer.isReadable &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                !importer.mipmapEnabled)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        [MenuItem("Behind the Hero/1. Gắn GameBootstrap vào Scene hiện tại")]
        public static void AttachBootstrapToScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Object.FindAnyObjectByType<GameBootstrap>() == null)
            {
                var go = new GameObject("BTH_GameBootstrap");
                go.AddComponent<GameBootstrap>();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[Behind the Hero] Đã gắn BTH_GameBootstrap vào Scene.");
            }
        }

        [MenuItem("Behind the Hero/2. Chạy Game Ngay (Play Mode)")]
        public static void PlayGameNow()
        {
            EditorApplication.isPlaying = true;
        }
    }
}
