using UnityEngine;
using System.Collections.Generic;

public class RecruitRequest
{
    public Role TargetRole;     // 직군
    public int Count;           // 모집할 인원 수
    public List<Employee> Applicants = new(); // 실제 지원자

    public RecruitRequest(Role role, int count)
    {
        TargetRole = role;
        Count = count;
    }
}
