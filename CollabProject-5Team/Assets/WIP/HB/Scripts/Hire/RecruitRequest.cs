using UnityEngine;

public class RecruitRequest
{
    public Role TargetRole;     // 직군
    public int Count;           // 모집할 인원 수

    public RecruitRequest(Role role, int count)
    {
        TargetRole = role;
        Count = count;
    }
}
