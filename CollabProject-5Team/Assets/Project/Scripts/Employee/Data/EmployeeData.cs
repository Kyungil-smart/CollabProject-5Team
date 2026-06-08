using System;
using UnityEngine;

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

[CreateAssetMenu(fileName = "EmployeeData_", menuName = "Scriptable Objects/EmployeeData")]
public class EmployeeImmutableData : SheetDataSOBase
{
    public string Name;
    public Role role;

    public int ability; // 메인 능력치
    public int desire;  // 의욕
    public int fatigue; // 피로도
    public int loyalty; // 충성도

    public int hiringCost; // 계약금(퇴직금)
    public int weekSalary; // 주급

    public int grade; // 직원 등급 숫자 (1=S, 2=A, 3=B, 4=C)

    public Trait mainTrait;
    public Trait subTrait;
    public Trait riskTrait;
    public MbtiFlags mbtiParsed;
    public string style; // 칭호

    [TextArea] public string hireText;  // 자기소개서
    [TextArea] public string fireText;  // 해고시 텍스트1
    [TextArea] public string fireText2; // 해고시 텍스트2

    [Header("Art")]
    public Sprite iconNormal; // Normal 상태 초상화
    public Sprite iconCaution; // Caution 상태 초상화
    public Sprite iconCritical; // Critical 상태 초상화

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
        weekSalary   = ParseInt(rowData[8]);
        grade        = ParseInt(rowData[9]);
        mainTrait    = ParseKoreanTrait(rowData[10]);
        subTrait     = ParseKoreanTrait(rowData[11]);
        riskTrait    = ParseKoreanTrait(rowData[12]);
        mbtiParsed   = ConvertMbtiStringToEnum(rowData[13].Trim());
        style        = rowData[14].Trim();
        hireText     = rowData[15].Trim();
        fireText     = rowData[16].Trim();
        fireText2    = rowData[17].Trim();
    }
}

[Serializable]
public struct EmployeeMutableData // 가변 데이터
{
    [SerializeField] int _ability;
    public int ability
    {
        get
        {   // 충성도에 따른 능력치 보정
            float rate = _loyalty >= 81 ? 1.3f :
                         _loyalty >= 61 ? 1.15f :
                         _loyalty >= 41 ? 1.0f :
                         _loyalty >= 21 ? 0.85f : 0.7f; 
            return Mathf.Clamp((int)(_ability * rate), 0, 100);
        }
        set
        {
            // 성장 패널티: ability가 높을수록 상슥폭이 줄어듦
            int delta = value - _ability;
            if (delta > 0) // 증가일 때만 패널티 적용
            {
                float rate = _ability <= 40 ? 1.0f :
                             _ability <= 60 ? 0.8f :
                             _ability <= 80 ? 0.6f : 0.4f;
                delta = (int)(delta * rate);
            }
            _ability = Mathf.Clamp(_ability + delta, 0, 100);
        }
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

    [SerializeField] int _preDesire;
    public int preDesire
    {
        get => _preDesire;
        set => _preDesire = Mathf.Clamp(value, 0, 100);
    }
    [SerializeField] int _preLoyalty;
    public int preLoyalty
    {
        get => _preLoyalty;
        set => _preLoyalty = Mathf.Clamp(value, 0, 100);
    }
    [SerializeField] int _preFatigue;
    public int preFatigue
    {
        get => _preFatigue;
        set => _preFatigue = Mathf.Clamp(value, 0, 100);
    }
}