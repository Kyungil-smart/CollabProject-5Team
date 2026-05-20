using UnityEngine;
using UnityEngine.InputSystem;
using static InputActionWide;
using System;

// 인풋 액션을 받고 이벤트를 실행하는 SO 입니다. 필요한 곳에 이 SO를 참조하여 사용
public class InputReader : ScriptableObject, IMobileActions
{
    public InputActionWide inputAction;

    // 터치 시작
    public event Action onTouchStarted;
    // 터치 종료
    public event Action onTouchCanceled;

    void Init()
    {
        if (inputAction != null) return;
        inputAction = new InputActionWide();
        inputAction.Mobile.SetCallbacks(this);
    }
    public void Enable()
    {
        Init();
        inputAction.Enable();
    }
    public void Disable()
    {
        inputAction?.Disable();
    }

    public void OnTouchscreen(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            onTouchStarted?.Invoke();
        }
        else if (context.canceled)
        {
            onTouchCanceled?.Invoke();
        }
    }
}
