using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PersonalOpinion : MonoBehaviour
{
    [SerializeField] private Image  _profileImage;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _text;

    public void SetOpinionData(Employee employee, string commentText = "저는 일을 하고싶지 않아요\n돈만 받고 싶어요!")
    {
        _profileImage.sprite = employee.so.iconNormal;
                _name.text   = employee.so.Name;
    }
}