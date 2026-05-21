using UnityEngine;
using System;

// 전담 파트
public enum Part
{
    Develop,
    Planning,
    Art
}

// MBTI 플래그 (0000 = INTP가 디폴트)
[Flags]
public enum MbtiFlags
{
    INTP = 0,       // 0000
    J = 1 << 0,  // 0001
    F = 1 << 1,  // 0010
    S = 1 << 2,  // 0100
    E = 1 << 3   // 1000
}

// 해시태그 플래그 (중복 선택 가능하므로 비트 자리 분리)
[Flags]
public enum HashTags
{
    None = 0,
    Shy = 1 << 0,  // 0001
    Active = 1 << 1,  // 0010
    Lazy = 1 << 2,  // 0100 (예시 추가: 게으름)
    Genius = 1 << 3   // 1000 (예시 추가: 천재)
}

// 4. 직원 클래스
public class Employee
{
    [Header("[ 직원 기본 정보 ]")]
    public string employeeName;
    public Part part;                  // 하나만 고름
    public MbtiFlags mbti = MbtiFlags.INTP; // 기본값 INTP
    public HashTags hashTags = HashTags.None; // 여러 개 중복 가능

    public Employee(string name = "None", Part part = 0, MbtiFlags mbti = 0, HashTags tags = 0)
    {
         employeeName = name;
            this.part = part;
            this.mbti = mbti;
        this.hashTags = tags;
    }
}