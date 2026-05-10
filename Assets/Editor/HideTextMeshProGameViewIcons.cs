using System;
using System.Reflection;
using UnityEditor;

[InitializeOnLoad]
public static class HideTextMeshProGameViewIcons
{
    static HideTextMeshProGameViewIcons()
    {
        EditorApplication.delayCall += HideTextMeshProIcons;
        EditorApplication.playModeStateChanged += _ => EditorApplication.delayCall += HideTextMeshProIcons;
    }

    private static void HideTextMeshProIcons()
    {
        try
        {
            Type annotationUtility = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");

            if (annotationUtility == null)
            {
                return;
            }

            MethodInfo getAnnotations = annotationUtility.GetMethod(
                "GetAnnotations",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            MethodInfo setIconEnabled = annotationUtility.GetMethod(
                "SetIconEnabled",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new[] { typeof(int), typeof(string), typeof(int) },
                null);

            if (getAnnotations == null || setIconEnabled == null)
            {
                return;
            }

            Array annotations = getAnnotations.Invoke(null, null) as Array;

            if (annotations == null)
            {
                return;
            }

            foreach (object annotation in annotations)
            {
                Type annotationType = annotation.GetType();
                FieldInfo classIdField = annotationType.GetField("classID");
                FieldInfo scriptClassField = annotationType.GetField("scriptClass");

                if (classIdField == null || scriptClassField == null)
                {
                    continue;
                }

                string scriptClass = scriptClassField.GetValue(annotation) as string;

                if (string.IsNullOrEmpty(scriptClass) || !scriptClass.Contains("TextMeshPro"))
                {
                    continue;
                }

                int classId = (int)classIdField.GetValue(annotation);
                setIconEnabled.Invoke(null, new object[] { classId, scriptClass, 0 });
            }
        }
        catch
        {
        }
    }
}
