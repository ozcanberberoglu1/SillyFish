using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class FixAnimatorStates
{
    public static string Execute()
    {
        string result = "";
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController",
            new[] { "Assets/_Game/Animations/enemyFishs" });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) continue;

            bool changed = false;
            foreach (var layer in controller.layers)
            {
                foreach (var childState in layer.stateMachine.states)
                {
                    string oldName = childState.state.name;
                    if (oldName.Contains("Idle") && oldName != "Idle")
                    {
                        childState.state.name = "Idle";
                        changed = true;
                        result += $"  {oldName} -> Idle\n";
                    }
                    else if (oldName.Contains("Food") && oldName != "Food")
                    {
                        childState.state.name = "Food";
                        changed = true;
                        result += $"  {oldName} -> Food\n";
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(controller);
                result += $"Fixed: {System.IO.Path.GetFileName(path)}\n";
            }
        }

        AssetDatabase.SaveAssets();
        result += "\nAll animator states renamed.";
        return result;
    }
}
