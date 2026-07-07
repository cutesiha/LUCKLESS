using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PostVictoryDialogueSceneSetup
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string Victory2ScenePath = "Assets/Scenes/Victory2Scene.unity";
    private const string DialogueCanvasPrefabPath = "Assets/Prefabs/DialogueCanvas.prefab";
    private const string IdapenVictoryBackgroundPath = "Assets/Sprites/BackGround2/from-PixAI-2005774949346368261-0.png";
    private const string IdapenCharacterPath = "Assets/Sprites/Charaters2/idapen.png";
    private const string ControllerName = "PostVictoryDialogueController";
    private static readonly string[] GeneratedVictoryCanvasNames =
    {
        "Canvas1_IdapenVictory",
        "Canvas2_KarimHasanVictory",
        "Canvas3_DoctorOlaVictory"
    };

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

        GameObject dialogueCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueCanvasPrefabPath);
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("dialogueCanvasPrefab").objectReferenceValue = dialogueCanvasPrefab;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        controller.EnsureDialogueHierarchy();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(controllerObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Post victory dialogue hierarchy ensured in MainScene.");
    }

    [MenuItem("Tools/Luckless/Clean Generated Victory Story Canvases")]
    public static void CleanGeneratedVictoryStoryCanvases()
    {
        var scene = EditorSceneManager.OpenScene(Victory2ScenePath, OpenSceneMode.Single);
        EnsureVictory2StoryReferences(scene);
        int removedCount = 0;

        for (int i = 0; i < GeneratedVictoryCanvasNames.Length; i++)
        {
            GameObject canvasObject = FindSceneObject(scene, GeneratedVictoryCanvasNames[i]);
            if (canvasObject == null)
            {
                continue;
            }

            Object.DestroyImmediate(canvasObject);
            removedCount++;
        }

        if (removedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"Removed {removedCount} generated victory story canvas object(s) from Victory2Scene.");
    }

    [MenuItem("Tools/Luckless/Ensure Victory2 Story Setup")]
    public static void EnsureVictory2StorySetup()
    {
        var scene = EditorSceneManager.OpenScene(Victory2ScenePath, OpenSceneMode.Single);
        EnsureVictory2StoryReferences(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Victory2 story references ensured.");
    }

    private static void EnsureVictory2StoryReferences(UnityEngine.SceneManagement.Scene scene)
    {
        GameObject controllerObject = FindSceneObject(scene, "VictoryController");
        if (controllerObject == null)
        {
            return;
        }

        VictoryStorySceneController controller = controllerObject.GetComponent<VictoryStorySceneController>();
        if (controller == null)
        {
            return;
        }

        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(IdapenVictoryBackgroundPath);
        Sprite character = AssetDatabase.LoadAssetAtPath<Sprite>(IdapenCharacterPath);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("idapenBackgroundSprite").objectReferenceValue = background;
        serializedController.FindProperty("idapenCharacterSprite").objectReferenceValue = character;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static GameObject FindSceneObject(UnityEngine.SceneManagement.Scene scene, string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate != null && candidate.name == objectName && candidate.scene == scene)
            {
                return candidate;
            }
        }

        return null;
    }
}
