using UnityEditor;

public static class GoogleSheetEditorMenu
{
    [MenuItem("구글시트/대화 Pool")]
    static void LoadSheet1()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(1));
    }

    [MenuItem("구글시트/대화 Node")]
    static void LoadSheet2()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(2));
    }


    [MenuItem("구글시트/직원")]
    static void LoadSheet3()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(3));
    }
    [MenuItem("구글시트/코멘트 보고서")]
    static void LoadSheet4()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(4));
    }
    [MenuItem("구글시트/직군별 보고서")]
    static void LoadSheet5()
    {
        EditorUtility.OpenPropertyEditor(DataRequestSet.Get(5));
    }
}
