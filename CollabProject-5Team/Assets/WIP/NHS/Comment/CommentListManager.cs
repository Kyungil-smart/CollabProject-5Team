using UnityEngine;
using System.Collections.Generic;

public class CommentListManager : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private GameObject _uiPrefab;
    [SerializeField] private Transform  _contentTransform;

    [SerializeField] private List<EmployeeCommentData> _commentSheetDatas = new List<EmployeeCommentData>();

    private int _maxPoolCount = 30;
    private List<EmployeeComment> _uiPoolList = new List<EmployeeComment>();

    private List<Employee> _haveEmployeeList;

    private void Awake()
    {
        for (int i = 0; i < _maxPoolCount; i++)
        {
            GameObject employee = Instantiate(_uiPrefab, _contentTransform, false);
            employee.SetActive(false);

            _uiPoolList.Add(employee.GetComponent<EmployeeComment>());
        }
    }

    private void OnEnable()
    {
        if (_EmployeeManager.Instance == null || _EmployeeManager.Instance.haveEmployees == null)
        {
            Debug.LogWarning("[CommentListManager] _EmployeeManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        List<Employee> currentEmployees = _EmployeeManager.Instance.haveEmployees.haveEmployeeList;

        RefreshCommentList(currentEmployees, _commentSheetDatas);
    }

    public void RefreshCommentList(List<Employee> currentEmployees, List<EmployeeCommentData> commentSheetData)
    {
        int activeCount = currentEmployees.Count;
        if (activeCount >= _uiPoolList.Count)
            activeCount = _uiPoolList.Count;

        for (int i = 0; i < _uiPoolList.Count; i++)
        {
            if (i < activeCount)
            {
                Employee employee = currentEmployees[i];

                string matchingComment = FindMatchingComment(employee, commentSheetData);

                _uiPoolList[i].gameObject.SetActive(true);
                _uiPoolList[i].SetUpCommentUI(employee, matchingComment);
            }
            else
            {
                _uiPoolList[i].gameObject.SetActive(false);
            }
        }
    }

    private string FindMatchingComment(Employee employee, List<EmployeeCommentData> sheetData)
    {
        string defaultComment = "문제없습니다.";

        foreach (var data in sheetData)
        {
            if (data.target_role != employee.so.role) continue;

            if (!CheckSection(employee.MutableData.desire,  data.trigger_desire )) continue;
            if (!CheckSection(employee.MutableData.fatigue, data.trigger_fatigue)) continue;
            if (!CheckSection(employee.MutableData.loyalty, data.trigger_loyalty)) continue;

            return data.comment_text;
        }

        return defaultComment;
    }

    private bool CheckSection(int actualValue, int sheetValue)
    {
        if (sheetValue == 50)
        {
            // 50 이상 100 이하
            return actualValue >= 50 && actualValue <= 100;
        }
        else if (sheetValue == 0)
        {
            // 0 이상 50 미만
            return actualValue >= 0 && actualValue < 50;
        }

        return actualValue >= sheetValue;
    }
}