using UnityEditor;

public static class GoogleSheetEditorMenu
{
    [MenuItem("구글시트/대화 Pool")]
    private static void LoadSheet1()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(1));
    }

    [MenuItem("구글시트/대화 Node")]
    private static void LoadSheet2()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(2));
    }
}
