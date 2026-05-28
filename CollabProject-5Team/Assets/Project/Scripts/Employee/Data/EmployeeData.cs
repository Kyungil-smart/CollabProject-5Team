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

    public int hiringCost;   // 채용 비용 (골드)
    public int trainingCost; // 훈련 비용 (골드)

    public int grade; // 직원 등급 숫자 (1=S, 2=A, 3=B, 4=C)

    public Trait mainTrait;
    public Trait subTrait;
    public Trait riskTrait;
    public MbtiFlags mbtiParsed; string _mbtiStr;

    public override void SetData(string[] rowData)
    {
        id           = ParseInt(rowData[0]);
        Name         = rowData[1].Trim();
        role         = ParseEnum<Role>(rowData[2].Trim());
        ability      = ParseInt(rowData[3]);
        desire       = ParseInt(rowData[4]);
        fatigue      = ParseInt(rowData[5]);
        loyalty      = ParseInt(rowData[6]);
        hiringCost   = ParseInt(rowData[7]);
        trainingCost = ParseInt(rowData[8]);
        grade        = ParseInt(rowData[9]);
        mainTrait    = ParseEnum<Trait>(rowData[10].Trim());
        subTrait     = ParseEnum<Trait>(rowData[11].Trim());
        riskTrait    = ParseEnum<Trait>(rowData[12].Trim());
        _mbtiStr     = rowData[13].Trim();
        mbtiParsed   = ConvertMbtiStringToEnum(_mbtiStr);
    }
}

[Serializable]
public struct EmployeeMutableData // 가변 데이터
{
    public int ability;   // 주 능력치

    public int property1; // 주 능력치 기반으로 변화하는 디폴트 값
    public int property2;
    public int property3;

    public int bonus1; // property1에 더하는 추가 변동값
    public int bonus2;
    public int bonus3;

    public int desire;
    public int loyalty;
    public int fatigue;
}

