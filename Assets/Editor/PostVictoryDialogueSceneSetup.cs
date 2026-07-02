using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PostVictoryDialogueSceneSetup
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string ControllerName = "PostVictoryDialogueController";

    [MenuItem("Tools/Luckless/Ensure Post Victory Dialogue")]
    public static void EnsureMainSceneDialogue()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        GameObject controllerObject = GameObject.Find(ControllerName);
        if (controllerObject == null)
        {
            controllerObject = new GameObject(ControllerName);
        }

        PostVictoryMainDialogueController controller = controllerObject.GetComponent<PostVictoryMainDialogueController>();
        if (controller == null)
        {
            controller = controllerObject.AddComponent<PostVictoryMainDialogueController>();
        }

        controller.EnsureDialogueHierarchy();
        EditorUtility.SetDirty(controllerObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Post victory dialogue hierarchy ensured in MainScene.");
    }
}
