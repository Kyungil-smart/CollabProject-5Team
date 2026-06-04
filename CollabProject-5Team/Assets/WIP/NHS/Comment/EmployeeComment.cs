using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class EmployeeComment : MonoBehaviour
{
    [Header("내부 UI 요소들")]
    [SerializeField] private Image[] _characterImages;
    [SerializeField] private Image[] _partImages;
    
    [SerializeField] private TextMeshProUGUI        _nameText;
    [SerializeField] private TextMeshProUGUI     _loyaltyText;
    [SerializeField] private TextMeshProUGUI _FluctuatingText;
    [SerializeField] private TextMeshProUGUI     _commentText;

    private Employee _currentEmployee;

    public void SetUpCommentUI(Employee employee, string commentText, int loyaltyDelta)
    {
        _currentEmployee = employee;

           _nameText.text = _currentEmployee.so.Name;
        _loyaltyText.text = $"{_currentEmployee.MutableData.loyalty}";
        _commentText.text = commentText;

        // 2. 변동치 세팅 (예시: 이번 주 변동량)
        if (loyaltyDelta >= 0)
            _FluctuatingText.text = $"<color=#D32F2F>{loyaltyDelta} ( {loyaltyDelta} ▲ )</color>";
        else
            _FluctuatingText.text = $"<color=#1976D2>{_currentEmployee.MutableData.loyalty} ( {Mathf.Abs(loyaltyDelta)} ▼ )</color>";

        // 3. 부서(Role)에 따른 마크 이미지 활성화
        SetPartImage((int)_currentEmployee.so.role);

        // 4. 피로도 상태에 따른 초상화 이미지 활성화
        int fatigue = _currentEmployee.MutableData.fatigue;
        if (fatigue >= 80) SetCharacterImage(2);      // Critical
        else if (fatigue >= 40) SetCharacterImage(1); // Caution
        else SetCharacterImage(0);                    // Normal
    }

    private void SetPartImage(int index)
    {
        for (int i = 0; i < _partImages.Length; i++)
            _partImages[i].gameObject.SetActive(i == index);
    }

    private void SetCharacterImage(int index)
    {
        for (int i = 0; i < _characterImages.Length; i++)
            _characterImages[i].gameObject.SetActive(i == index);
    }
}