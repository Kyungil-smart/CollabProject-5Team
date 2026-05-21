using UnityEngine;

public class _EmployeeManager : MonoBehaviour
{
    public static _EmployeeManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject); 
    }

}
