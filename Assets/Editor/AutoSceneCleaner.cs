using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoSceneCleaner
{
    static AutoSceneCleaner()
    {
        EditorApplication.delayCall += ExecuteCleanup;
    }

    static void ExecuteCleanup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Ensure this only runs once per editor session to prevent infinite recompiles
        if (EditorPrefs.GetBool("AutoSceneCleaned_VRProject", false)) return;
        EditorPrefs.SetBool("AutoSceneCleaned_VRProject", true);

        bool changed = false;
        int missingScriptsRemoved = 0;

        // 1. Remove all missing scripts from all GameObjects in the scene
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                missingScriptsRemoved += removed;
                changed = true;
            }
        }

        if (missingScriptsRemoved > 0)
        {
            Debug.Log($"[AutoSceneCleaner] Successfully removed {missingScriptsRemoved} missing/broken script references from the scene.");
        }

        // 2. Erase the buggy "MainCamera" tag object if it lacks a real camera component
        try 
        {
            GameObject[] mainCams = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach(GameObject mc in mainCams) 
            {
                if (mc.GetComponent<Camera>() == null) 
                {
                    Debug.Log($"[AutoSceneCleaner] Kırık/Hatalı Main Camera objesi siliniyor: {mc.name}");
                    Object.DestroyImmediate(mc);
                    changed = true;
                }
            }
        }
        catch { }

        // Müşteri isteğine ek menü komutu (her zaman manuel tetiklenebilir)
        if (changed)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[AutoSceneCleaner] Sahne başarıyla temizlendi ve kaydedildi!");
        }
    }
}
