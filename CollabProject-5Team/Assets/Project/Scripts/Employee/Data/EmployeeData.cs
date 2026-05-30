using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum Role
{
    PROGRAMMER, PLANNER, ARTIST, MARKETING, QA
}

[Flags] // MBTI 플래그 (0000 = INTP가 디폴트)
public enum MbtiFlags
{
    INTP = 0,    // 0000
    J = 1 << 0,  // 0001
    F = 1 << 1,  // 0010
    S = 1 << 2,  // 0100
    E = 1 << 3   // 1000
}

// 칭호
public enum Style
{
    Rookie,     // 신입사원
    Senior,     // 선임
    Boss,       // 팀장
}

[CreateAssetMenu(fileName = "EmployeeData_", menuName = "Scriptable Objects/EmployeeData")]
public class EmployeeImmutableData : SheetDataSOBase
{
    public string Name;
    public Role role;

    public int ability; // 메인 능력치
    public int desire;  // 의욕
    public int fatigue; // 피로도
    public int loyalty; // 충성도

    public int hiringCost;   // 채용 비용 (골드)
    public int trainingCost; // 훈련 비용 (골드)

    public int grade; // 직원 등급 숫자 (1=S, 2=A, 3=B, 4=C)

    public Trait mainTrait;
    public Trait subTrait;
    public Trait riskTrait;
    public MbtiFlags mbtiParsed;
    public Style style; // 칭호

    public string hireText;  // 자기소개서
    public string fireText;  // 해고시 텍스트1
    public string fireText2; // 해고시 텍스트2

    public override void SetData(string[] rowData)
    {
        id           = ParseInt(rowData[0]);
        Name         = rowData[1].Trim();
        role         = ParseEnum<Role>(rowData[2]);
        ability      = ParseInt(rowData[3]);
        desire       = ParseInt(rowData[4]);
        fatigue      = ParseInt(rowData[5]);
        loyalty      = ParseInt(rowData[6]);
        hiringCost   = ParseInt(rowData[7]);
        trainingCost = ParseInt(rowData[8]);
        grade        = ParseInt(rowData[9]);
        mainTrait    = ParseEnum<Trait>(rowData[10]);
        subTrait     = ParseEnum<Trait>(rowData[11]);
        riskTrait    = ParseEnum<Trait>(rowData[12]);
        mbtiParsed = ConvertMbtiStringToEnum(rowData[13].Trim());
        style      = ParseEnum<Style>(rowData[14]);
        hireText   = rowData[15].Trim();
        fireText   = rowData[16].Trim();
        fireText2  = rowData[17].Trim();
    }
}

[Serializable]
public struct EmployeeMutableData // 가변 데이터
{
    [SerializeField] int _ability;
    public int ability
    {
        get => _ability;
        set => _ability = Mathf.Clamp(value, 0, 100);
    }

    public int property1; // 매 주차 보고서 승인 후 갱신되는 세부 능력치
    public int property2;
    public int property3;

    [SerializeField] int _desire;
    public int desire
    {
        get => _desire;
        set => _desire = Mathf.Clamp(value, 0, 100);
    }

    [SerializeField] int _loyalty;
    public int loyalty
    {
        get => _loyalty;
        set => _loyalty = Mathf.Clamp(value, 0, 100);
    }

    [SerializeField] int _fatigue;
    public int fatigue
    {
        get => _fatigue;
        set => _fatigue = Mathf.Clamp(value, 0, 100);
    }
}

