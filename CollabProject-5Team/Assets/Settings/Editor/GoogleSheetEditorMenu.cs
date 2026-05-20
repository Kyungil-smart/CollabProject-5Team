using UnityEditor;

public static class GoogleSheetEditorMenu
{
    [MenuItem("구글시트/Test Sheet 1")]
    private static void LoadSheet1()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(1));
    }

    [MenuItem("구글시트/Test Sheet 2")]
    private static void LoadSheet2()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(2));
    }
}
