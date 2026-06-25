using UnityEngine;

public class TutorialDynamicButtonRegistrar : MonoBehaviour
{
    [SerializeField] private string _tutorialButtonId; // 인스펙터에 적어줄 고유 ID (예: "MenuBtn_1")

    private void Start()
    {
        // 📢 생성되자마자 매니저에게 "저 여기 태어났어요!" 하고 자신을 등록합니다.
        Register();
    }

    private void OnEnable()
    {
        // 프리랩 생성이 아니라 오브젝트 활성화/비활성화 방식일 경우를 대비한 안전장치
        Register();
    }

    private void Register()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.RegisterDynamicButton(_tutorialButtonId, gameObject);
        }
    }
}