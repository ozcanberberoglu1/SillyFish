using UnityEngine;
using UnityEditor;

public class iOSBuildSetup
{
    public static string Execute()
    {
        string result = "";

        var currentTarget = EditorUserBuildSettings.activeBuildTarget;
        result += $"Current platform: {currentTarget}\n";

        if (currentTarget != BuildTarget.iOS)
        {
            result += "Switching to iOS platform...\n";
            bool success = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.iOS, BuildTarget.iOS);
            if (success)
                result += "Successfully switched to iOS platform!\n";
            else
                result += "ERROR: Failed to switch to iOS. Make sure iOS Build Support module is installed in Unity Hub.\n";
        }
        else
        {
            result += "Already on iOS platform.\n";
        }

        PlayerSettings.companyName = "SillyFishStudio";
        PlayerSettings.productName = "SillyFish";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.sillyfishstudio.sillyfish");
        PlayerSettings.iOS.buildNumber = "1";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);

        AssetDatabase.SaveAssets();
        result += "All iOS settings configured successfully!\n";

        return result;
    }
}
