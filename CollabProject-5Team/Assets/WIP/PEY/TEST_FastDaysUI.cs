#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

public class TEST_FastDaysUI : MonoBehaviour
{
    Button _dayPlusButton;

    private void Awake()
    {
        _dayPlusButton = GetComponentInChildren<Button>();

        // 버튼 클릭 시 날짜를 빠르게 진행 OnClickEndDayButton 호출
        _dayPlusButton.onClick.AddListener(() =>
        {
            DateTimeManager.Instance.OnClickEndDayButton();
        });
    }
}
#endif