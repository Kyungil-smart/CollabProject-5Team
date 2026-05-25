using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class EmployeeSheetLoader : MonoBehaviour
{
    [Header("구글 시트")]
    [SerializeField] private SheetData _dataSheet;

    public  List<EmployeeImmutableData>  ParsedRawDataList => _parsedRawDataList;
    private List<EmployeeImmutableData> _parsedRawDataList = new List<EmployeeImmutableData>();

    private async void Awake()
    {
        await _dataSheet.Load(OnSheetLoadSuccess);
    }

    private void OnSheetLoadSuccess(char splitSymbol, string[] lines)
    {
        _parsedRawDataList.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] rowData = lines[i].Split(splitSymbol);

            EmployeeDataSO tempSO = ScriptableObject.CreateInstance<EmployeeDataSO>();
            tempSO.row = i + 1; 

            tempSO.SetData(rowData);

            _parsedRawDataList.Add(tempSO.data);
        }

        Debug.Log($"[구글 시트 연동 완료] 총 {_parsedRawDataList.Count}명의 직원 원본 데이터를 로드했습니다.");

        _EmployeeManager.Instance.InitializeDatabase(_parsedRawDataList);
    }
}
