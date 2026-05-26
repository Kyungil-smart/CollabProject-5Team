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

    public string     mbtiStr; 

    public Part          partParsed;
    public Rank          rankParsed;
    public MbtiFlags     mbtiParsed;

    public int initProperty1;
    public int initProperty2;
    public int initProperty3;

    public override void SetData(string[] rowData)
    {
        this.id = ParseInt(rowData[0]);

        this.employeeName = rowData[1].Trim();

        this.partParsed   = ParseEnum<Part>(rowData[2].Trim());
        this.rankParsed   = ParseEnum<Rank>(rowData[3].Trim());

        this.mbtiStr      = rowData[4].Trim(); 
        this.mbtiParsed   = ConvertMbtiStringToEnum(mbtiStr);

        this.initProperty1 = ParseInt(rowData[5]);
        this.initProperty2 = ParseInt(rowData[6]);
        this.initProperty3 = ParseInt(rowData[7]);
    }
}

[Serializable]
public struct EmployeeMutableData // 가변 데이터
{
    public int stat; // 작업능력
    public int property1;
    public int property2;
    public int property3;

    public int motivation;
    public int loyalty;
    public int fatigue;
}

