using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Rendering;

public class PersonalOpinionList : MonoBehaviour
{
    public List<Employee> _personalOpinionEmployee = new List<Employee>();

    private PersonalOpinion _personalOpinion = new();

    [SerializeField] private GameObject _personalOpinionUI;
    [SerializeField] private GameObject _endPageUI;

    [Header("테스트용 직원 SO")]

    private int _count = 0;

    private void Start()
    {
        UpdatePersonalOpinion();
    }

    public void AddPersonalOpinion(Employee employee)
    {
        _personalOpinionEmployee.Add(employee);
    }

    public void UpdatePersonalOpinion()
    {
        if (_count >= _personalOpinionEmployee.Count)
        {
            EnterEndPage();
            _count = 0;
        }

        else
        {
            if (_personalOpinion != null)
            {
                _personalOpinion.SetOpinionData(_personalOpinionEmployee[_count]);
            }
            _count++;
        }
    }

    private void EnterEndPage()
    {
        _personalOpinionUI.gameObject.SetActive(false);
                _endPageUI.gameObject.SetActive(true);
    }
}