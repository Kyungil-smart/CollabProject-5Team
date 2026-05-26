using System;
using UnityEngine;

[System.Serializable]

public enum Part
{
    Develop, Planning, Art
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


[Flags] // 해시태그 플래그(중복 선택가능)
public enum HashTags
{
      None = 0,
       Shy = 1 << 0,  // 0001
    Active = 1 << 1,  // 0010
      Lazy = 1 << 2,  // 0100 
    Genius = 1 << 3   // 1000 
}

[CreateAssetMenu(fileName = "EmployeeData_", menuName = "Scriptable Objects/EmployeeData")]
public class EmployeeImmutableData : SheetDataSOBase
{
    public int      employeeID;
    public string employeeName;

    public string     partStr; 
    public string     mbtiStr; 
    public string hashTagsStr;

    public Part          partParsed;
    public MbtiFlags     mbtiParsed;
    public HashTags  hashTagsParsed;

    public int initProperty1;
    public int initProperty2;
    public int initProperty3;

    public override void SetData(string[] rowData)
    {
        this.id = ParseInt(rowData[0]);
        this.employeeID = this.id;

        this.employeeName = rowData[1].Trim();

        this.partStr      = rowData[2].Trim(); 
        this.mbtiStr      = rowData[3].Trim(); 
        this.hashTagsStr  = rowData[4].Trim();

        this.partParsed     = ParseEnum<Part>(partStr);
        this.mbtiParsed     = ConvertMbtiStringToEnum(mbtiStr);
        this.hashTagsParsed = ConvertHashTagsStringToEnum(hashTagsStr);

        this.initProperty1 = ParseInt(rowData[5]);
        this.initProperty2 = ParseInt(rowData[6]);
        this.initProperty3 = ParseInt(rowData[7]);
    }

    private MbtiFlags ConvertMbtiStringToEnum(string mbtiStr)
    {
        MbtiFlags result = MbtiFlags.INTP;
        string upperStr = mbtiStr.ToUpper().Trim();
        if (upperStr.Contains("J")) result |= MbtiFlags.J;
        if (upperStr.Contains("F")) result |= MbtiFlags.F;
        if (upperStr.Contains("S")) result |= MbtiFlags.S;
        if (upperStr.Contains("E")) result |= MbtiFlags.E;
        return result;
    }

    private HashTags ConvertHashTagsStringToEnum(string tagsStr)
    {
        HashTags result = HashTags.None;
        string[] tags = tagsStr.Split(',');
        foreach (var tag in tags)
        {
            if (System.Enum.TryParse(tag.Trim(), true, out HashTags parsedTag))
                result |= parsedTag;
        }
        return result;
    }
}

[System.Serializable]
public struct EmployeeMutableData // 가변 데이터
{
    public int property1;
    public int property2;
    public int property3;

    public int motivation;
    public int loyalty;
    public int fatigue;
}