using UnityEngine;

namespace Dialogue
{
    /// <summary>
    /// 대화 시스템 테스트용 셋업 스크립트.
    /// </summary>
    public class TEST_DialogueSetup : MonoBehaviour
    {
        [Header("테스트 직원 SO")]
        [SerializeField] private EmployeeImmutableData _employeeSO;

        private void Start()
        {
            if (_employeeSO == null)
            {
                Debug.LogError("[TEST_DialogueSetup] _employeeSO가 연결되지 않았습니다. 인스펙터에서 SO를 연결해주세요.");
                return;
            }

            GameObject obj = new GameObject($"Employee_{_employeeSO.id}");
            Employee emp = obj.AddComponent<Employee>();
            emp.so = _employeeSO;
            emp.Init();

            _EmployeeManager.Instance.haveEmployees.AddEmployee(emp);

            Debug.Log($"[TEST] {_employeeSO.Name} 등록 완료 " +
                      $"| desire={emp.MutableData.desire} fatigue={emp.MutableData.fatigue}");
        }
    }
}