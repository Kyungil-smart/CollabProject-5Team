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

public class EmployeeImmutableData // 불변 데이터
{
    public int             id;
    public string        name;
    public string     partStr; 
    public string     mbtiStr; 
    public string hashTagsStr;

    public int initProperty1;
    public int initProperty2;
    public int initProperty3;
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