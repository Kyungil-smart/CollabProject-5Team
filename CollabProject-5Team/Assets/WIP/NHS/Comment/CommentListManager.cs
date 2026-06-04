using UnityEngine;
using System.Collections.Generic;

public class CommentListManager : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private GameObject _uiPrefab;
    [SerializeField] private Transform  _contentTransform;

    private int _maxPoolCount = 30;
    private List<GameObject> _haveEmployeeList = new List<GameObject>();

    private void Awake()
    {
        for(int i=0;i<_maxPoolCount;i++)
        {
            GameObject employee = Instantiate(_uiPrefab, _contentTransform, false);
            employee.SetActive(true);
            _haveEmployeeList.Add(employee);
        }
    }

    public void RefreshList(int activeCount)
    {
        if (activeCount >= _haveEmployeeList.Count)
            activeCount = _haveEmployeeList.Count;

        for(int i=0;i<_haveEmployeeList.Count;i++)
        {
            if (i < activeCount)
                _haveEmployeeList[i].SetActive(true);
            else
                _haveEmployeeList[i].SetActive(false);
        }
    }
}