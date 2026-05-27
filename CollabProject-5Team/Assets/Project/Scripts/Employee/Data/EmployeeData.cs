using System;
using UnityEngine;

public enum Rank
{
    S, A, B, C
}

public enum Part
{
    Develop, Planning, Art, Marketing, QA
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
    public string employeeName;

    [HideInInspector]
    public string mbtiStr;

    public Rank          rankParsed;
    public Part          partParsed;
    public MbtiFlags     mbtiParsed;

    public int contractGold;

    public override void SetData(string[] rowData)
    {
        this.id = ParseInt(rowData[0]);

        this.employeeName = rowData[1].Trim();

        this.rankParsed   = ParseEnum<Rank>(rowData[2].Trim());
        this.partParsed   = ParseEnum<Part>(rowData[3].Trim());

        this.mbtiStr      = rowData[4].Trim(); 
        this.mbtiParsed   = ConvertMbtiStringToEnum(mbtiStr);

        this.contractGold = ParseInt(rowData[5]);
    }
}

[Serializable]
public struct EmployeeMutableData // 가변 데이터
{
    public int baseStat;  // 주 능력치

    public int property1; // 주 능력치 기반으로 변화하는 디폴트 값
    public int property2;
    public int property3;

    public int bonus1; // property1에 더하는 추가 변동값
    public int bonus2;
    public int bonus3;

    public int motivation;
    public int loyalty;
    public int fatigue;
}

