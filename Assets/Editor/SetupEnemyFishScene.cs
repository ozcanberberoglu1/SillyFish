using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class SetupEnemyFishScene
{
    public static string Execute()
    {
        string result = "";

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) return "ERROR: No Canvas found!";
        result += "Canvas found.\n";

        // 1. Create RawImage for fish world rendering
        Transform existing = canvas.transform.Find("FishWorldRender");
        RawImage rawImg;
        if (existing == null)
        {
            var rawImgObj = new GameObject("FishWorldRender");
            rawImgObj.transform.SetParent(canvas.transform, false);
            rawImg = rawImgObj.AddComponent<RawImage>();

            var rt = rawImgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Between BG (index 0) and Fish (was index 1, now becomes 2)
            rawImgObj.transform.SetSiblingIndex(1);

            rawImg.raycastTarget = false;
            rawImg.color = Color.white;
            result += "FishWorldRender RawImage created.\n";
        }
        else
        {
            rawImg = existing.GetComponent<RawImage>();
            if (rawImg == null) rawImg = existing.gameObject.AddComponent<RawImage>();
            result += "FishWorldRender already exists.\n";
        }

        // 2. Find scene objects
        GameObject fishWorldObj = GameObject.Find("BalıkSpriteRenderer");
        if (fishWorldObj == null)
            fishWorldObj = GameObject.Find("BalikSpriteRenderer");
        result += fishWorldObj != null
            ? $"Fish world object found: {fishWorldObj.name}\n"
            : "WARNING: Fish world object (BalıkSpriteRenderer) not found!\n";

        GameObject fishCamObj = GameObject.Find("Fish Camera");
        Camera fishCam = fishCamObj != null ? fishCamObj.GetComponent<Camera>() : null;
        result += fishCam != null
            ? "Fish Camera found.\n"
            : "WARNING: Fish Camera not found!\n";

        // 3. Find GameManager and set serialized fields
        var gm = Object.FindObjectOfType<GameManager>();
        if (gm == null) return result + "ERROR: No GameManager found!";

        var so = new SerializedObject(gm);

        if (fishWorldObj != null)
        {
            var prop = so.FindProperty("fishWorldTransform");
            if (prop != null) prop.objectReferenceValue = fishWorldObj.transform;
            result += "  -> fishWorldTransform assigned.\n";
        }

        if (fishCam != null)
        {
            var prop = so.FindProperty("fishCamera");
            if (prop != null) prop.objectReferenceValue = fishCam;
            result += "  -> fishCamera assigned.\n";
        }

        var renderProp = so.FindProperty("fishRenderImage");
        if (renderProp != null) renderProp.objectReferenceValue = rawImg;
        result += "  -> fishRenderImage assigned.\n";

        // 4. Set up enemy spawn configs
        string[] prefabPaths = {
            "Assets/_Game/Prefabs/enemyFishs/enemyLv1/Lv1enemyFish.prefab",
            "Assets/_Game/Prefabs/enemyFishs/enemyLv2/Lv2enemyFish.prefab",
            "Assets/_Game/Prefabs/enemyFishs/enemyLv3/Lv3enemyFish.prefab",
            "Assets/_Game/Prefabs/enemyFishs/enemyLv4/Lv4enemyFish.prefab",
            "Assets/_Game/Prefabs/enemyFishs/enemyLv5/Lv5enemyFish.prefab",
        };

        int[] levels = { 1, 2, 3, 4, 5 };
        int[] counts = { 8, 6, 4, 3, 2 };
        float[] speeds = { 120f, 150f, 180f, 200f, 220f };
        float[] radii = { 300f, 400f, 500f, 600f, 700f };

        var configsProp = so.FindProperty("enemySpawnConfigs");
        if (configsProp != null)
        {
            configsProp.arraySize = prefabPaths.Length;
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null)
                {
                    result += $"  WARNING: Prefab not found: {prefabPaths[i]}\n";
                    continue;
                }

                var elem = configsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                elem.FindPropertyRelative("level").intValue = levels[i];
                elem.FindPropertyRelative("count").intValue = counts[i];
                elem.FindPropertyRelative("speed").floatValue = speeds[i];
                elem.FindPropertyRelative("detectionRadius").floatValue = radii[i];
                elem.FindPropertyRelative("chaseTime").floatValue = 2.5f;

                result += $"  Config[{i}]: Lv{levels[i]} x{counts[i]} speed={speeds[i]} radius={radii[i]} -> {prefab.name}\n";
            }
        }

        so.ApplyModifiedProperties();
        result += "GameManager properties saved.\n";

        // 5. Fix Idle animation loop settings
        string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip",
            new[] { "Assets/_Game/Animations/enemyFishs" });
        int fixedCount = 0;
        foreach (var guid in animGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("Idle")) continue;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
            {
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                fixedCount++;
                result += $"  Loop fixed: {System.IO.Path.GetFileName(path)}\n";
            }
        }
        result += $"Fixed loop on {fixedCount} Idle clips.\n";

        // 6. Save everything
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        result += "\nSetup complete! Scene saved.\n";
        return result;
    }
}
