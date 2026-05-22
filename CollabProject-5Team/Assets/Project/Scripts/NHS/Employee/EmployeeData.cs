using System;

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

public class EmployeeRawData
{
    public int id;
    public string name;
    public string partStr;     // 엑셀의 "Develop"
    public string mbtiStr;     // 엑셀의 "J, F"
    public string hashTagsStr; // 엑셀의 "Active, Genius"
    public int intelligence;
    public float maxStamina;
}

