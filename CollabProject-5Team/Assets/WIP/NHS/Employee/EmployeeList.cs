using UnityEngine;
using System.Collections.Generic;

public class EmployeeList
{
    // 모든 직원 원본 전체목록
    public IReadOnlyDictionary<int, GameObject> allEmployeePrefabs => _allEmployeePrefabs;
    private Dictionary<int, GameObject> _allEmployeePrefabs = new Dictionary<int, GameObject>();

    // 아직 고용되지 않은 직원 목록
    public IReadOnlyDictionary<int, GameObject> leftEmployees => _leftEmployees;
    private Dictionary<int, GameObject> _leftEmployees = new Dictionary<int, GameObject>();

    public EmployeeList(GameObject[] prefabs)
    {
        _allEmployeePrefabs.Clear();
        _leftEmployees.Clear();

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;
            Employee obj = prefab.GetComponent<Employee>();
            if (obj == null || obj.so == null)
            {
                Debug.LogWarning($"[EmployeeList] 프리팹 {prefab.name}에 EmployeeObj 또는 ImmutableData가 없습니다.");
                continue;
            }
            int id = obj.so.id;
            _allEmployeePrefabs[id] = prefab;
            _leftEmployees[id]      = prefab;
        }
    }

    public void DeleteEmployee(int id)
    {
        if (_leftEmployees.ContainsKey(id))
            _leftEmployees.Remove(id);
        else
            Debug.LogWarning($"[경고] {id}번 지원자는 이미 고용되었거나 존재하지 않습니다.");
    }
}
