#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class SheetDataGeneratorEditor : EditorWindow
{
    private MonoScript targetScript; // Inspector에서 자식 클래스 스크립트 할당
    private int minId = 1;
    private int maxId = 10;
    private string savePath = "Assets/Project/DB";

    [MenuItem("Tools/Sheet Data 생성기")]
    public static void ShowWindow()
    {
        GetWindow<SheetDataGeneratorEditor>("Sheet Data 생성기");
    }

    private void OnGUI()
    {
        GUILayout.Label("SheetData 자식 클래스 일괄 생성 설정", EditorStyles.boldLabel);

        // MonoScript 필드를 통해 에디터에서 자식 클래스 스크립트 파일 참조받기
        targetScript = (MonoScript)EditorGUILayout.ObjectField("Target Script", targetScript, typeof(MonoScript), false);

        minId = EditorGUILayout.IntField("Min ID", minId);
        maxId = EditorGUILayout.IntField("Max ID", maxId);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        GUILayout.Space(10);

        if (GUILayout.Button("에셋 생성하기"))
        {
            GenerateAssets();
        }
    }

    private void GenerateAssets()
    {
        // 1. 유효성 검사
        if (targetScript == null)
        {
            Debug.LogError("대상 스크립트(Target Script)가 할당되지 않았습니다. SheetDataSOBase를 상속받는 자식 스크립트를 할당해주세요.");
            return;
        }

        if (minId > maxId)
        {
            Debug.LogError("Min ID는 Max ID보다 클 수 없습니다.");
            return;
        }

        // 2. 할당된 MonoScript로부터 System.Type 추출
        System.Type targetType = targetScript.GetClass();

        // 해당 타입이 SheetDataSOBase를 상속받았는지 확인
        if (!typeof(SheetDataSOBase).IsAssignableFrom(targetType))
        {
            Debug.LogError("할당된 스크립트가 SheetDataSOBase를 상속받는 유효한 클래스가 아닙니다.");
            return;
        }

        // 3. 디렉토리 생성 로직
        string fullPath = Path.Combine(Application.dataPath, savePath.Substring(7));
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }

        // 4. 순회하며 파일 생성
        int generateCount = 0;
        for (int i = minId; i <= maxId; i++)
        {
            // 추출한 Type을 사용하여 동적으로 스크립터블 오브젝트 생성
            ScriptableObject newSO = ScriptableObject.CreateInstance(targetType);

            // SheetDataSOBase로 캐스팅하여 공통 필드인 id 적용
            SheetDataSOBase sheetData = (SheetDataSOBase)newSO;
            sheetData.id = i;

            // 파일 이름을 id와 동일하게 지정 (예: 1.asset)
            string assetPath = $"{savePath}/{i}.asset";

            // 실제 유니티 프로젝트 경로에 에셋 생성
            AssetDatabase.CreateAsset(sheetData, assetPath);
            generateCount++;
        }

        // 5. 에셋 저장 및 갱신 적용
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"총 {generateCount}개의 {targetType.Name} 에셋이 성공적으로 생성되었습니다. (경로: {savePath})");
    }
}
#endif