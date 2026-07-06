using UnityEngine;
using System.Collections.Generic;
using GameDevTycoon.UI.Ingame;

public class CommentListManager : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private GameObject _uiPrefab;
    [SerializeField] private Transform _contentTransform;

    [SerializeField] private List<EmployeeCommentData> _commentSheetDatas = new List<EmployeeCommentData>();

    private int _maxPoolCount = 30;
    private List<EmployeeCommentItemView> _uiPoolList = new List<EmployeeCommentItemView>();

    private void Awake()
    {
        for (int i = 0; i < _maxPoolCount; i++)
        {
            GameObject go = Instantiate(_uiPrefab, _contentTransform, false);
            go.SetActive(false);

            _uiPoolList.Add(go.GetComponent<EmployeeCommentItemView>());
        }
    }

    private void OnEnable()
    {
        List<Employee> employees = null;

        if (Company.Instance.activeProjectCount.Value > 0)
        {
            employees = Company.Instance.curProject.GetAllEmployees();
        }

        if (employees == null)
        {
            Debug.LogWarning("[CommentListManager] 직원 목록을 찾을 수 없습니다.");
            return;
        }

        RefreshCommentList(employees, _commentSheetDatas);
    }

    public void RefreshCommentList(List<Employee> currentEmployees, List<EmployeeCommentData> commentSheetData)
    {
        int activeCount = Mathf.Min(currentEmployees.Count, _uiPoolList.Count);

        for (int i = 0; i < _uiPoolList.Count; i++)
        {
            if (i < activeCount)
            {
                Employee employee = currentEmployees[i];
                string commentText = FindMatchingComment(employee, commentSheetData);

                _uiPoolList[i].gameObject.SetActive(true);
                _uiPoolList[i].Bind(employee, commentText);
            }
            else
            {
                _uiPoolList[i].gameObject.SetActive(false);
            }
        }
    }

    private string FindMatchingComment(Employee employee, List<EmployeeCommentData> sheetData)
    {
        foreach (var data in sheetData)
        {
            if (data.target_role != employee.so.role) continue;

            if (!CheckSection(employee.MutableData.desire, data.trigger_desire)) continue;
            if (!CheckSection(employee.MutableData.fatigue, data.trigger_fatigue)) continue;
            if (!CheckSection(employee.MutableData.loyalty, data.trigger_loyalty)) continue;

            return data.comment_text;
        }

        return null;
    }

    private bool CheckSection(int actualValue, int sheetValue)
    {
        if (sheetValue == 50)
            return actualValue >= 50 && actualValue <= 100;
        else if (sheetValue == 0)
            return actualValue >= 0 && actualValue < 50;

        return actualValue >= sheetValue;
    }
}
