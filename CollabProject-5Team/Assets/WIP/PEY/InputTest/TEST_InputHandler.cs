using UnityEngine;

public class TEST_InputHandler : MonoBehaviour
{
    public InputReader input;
#if UNITY_EDITOR
    private void Reset()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:InputReader");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            input = UnityEditor.AssetDatabase.LoadAssetAtPath<InputReader>(path);
        }
    }
#endif
    private void OnEnable()
    {
        input.Enable();
        input.onTouchStarted  += HandleTouchStarted;
        input.onTouchCanceled += HandleTouchCanceled;
    }

    private void OnDisable()
    {
        input.onTouchStarted  -= HandleTouchStarted;
        input.onTouchCanceled -= HandleTouchCanceled;
    }

    private void HandleTouchStarted()
    {
        Debug.Log("[Touch] 터치 시작");
    }

    private void HandleTouchCanceled()
    {
        Debug.Log("[Touch] 터치 종료");
    }
}
