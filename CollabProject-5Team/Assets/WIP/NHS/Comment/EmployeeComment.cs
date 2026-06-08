using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class EmployeeComment : MonoBehaviour
{
    [SerializeField] private Image _characterImage;
    [SerializeField] private Image[] _partImages;
    
    [SerializeField] private TextMeshProUGUI        _nameText;
    [SerializeField] private TextMeshProUGUI     _loyaltyText;
    [SerializeField] private TextMeshProUGUI _FluctuatingText;
    [SerializeField] private TextMeshProUGUI     _commentText;

    private Employee _currentEmployee;

    public void SetUpCommentUI(Employee employee, string commentText)
    {
        _currentEmployee = employee;

           _nameText.text = _currentEmployee.so.Name;
        _loyaltyText.text = $"{_currentEmployee.MutableData.loyalty}";
        _commentText.text = commentText;

        int loyaltyChanage = employee.MutableData.loyalty - employee.MutableData.preLoyalty;

        if (loyaltyChanage >= 0)
            _FluctuatingText.text = $"<color=#D32F2F>( {loyaltyChanage} ▲ )</color>";
        else
            _FluctuatingText.text = $"<color=#1976D2>( {Mathf.Abs(loyaltyChanage)} ▼ )</color>";

        SetPartImage((int)_currentEmployee.so.role);

        int fatigue = _currentEmployee.MutableData.fatigue;

        if      (fatigue >= 80)
            _characterImage.sprite = _currentEmployee.so.iconCritical;
        else if (fatigue >= 40)
            _characterImage.sprite = _currentEmployee.so.iconCaution; 
        else
            _characterImage.sprite = _currentEmployee.so.iconNormal;  
    }

    private void SetPartImage(int index)
    {
        for (int i = 0; i < _partImages.Length; i++)
            _partImages[i].gameObject.SetActive(i == index);
    }
}