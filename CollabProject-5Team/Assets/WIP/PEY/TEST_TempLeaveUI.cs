using UnityEngine;
using TMPro;

public class TEST_TempLeaveUI : MonoBehaviour
{
    TMP_Text _text;

    void Awake()
    {
        _text = GetComponentInChildren<TMP_Text>(true);
    }

    void OnEnable()
    {
        _EmployeeManager.OnEmployeeLeft += Show;
    }

    void OnDisable()
    {
        _EmployeeManager.OnEmployeeLeft -= Show;
    }

    public void Show(string employeeName)
    {
        _text.text = $"{employeeName} 직원이 퇴사했습니다.";
        _text.gameObject.SetActive(true);
    }
}
