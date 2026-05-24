using UnityEngine;
using System;

public class Employee : IClickable
{
    [Header("[ 직원 기본 정보 ]")]
    public int       id           { get; private set; } // 사번
    public string    employeeName { get; private set; } // 이름
    public Part      part         { get; private set; } // 하나만 고름
    public MbtiFlags mbti         { get; private set; } // 기본값 INTP
    public HashTags  hashTags     { get; private set; } // 여러 개 중복 가능

    [System.Serializable]
    public struct EmployeeStats
    {
        public int property1;
        public int property2;
        public int property3;

        public int motivation;
        public int loyalty;
        public int fatigue;
    }

    public EmployeeStats stats { get; private set; }

    public Employee(EmployeeRawData rawData)
    {
        this.id = rawData.id;
        this.employeeName = rawData.name;

        if (Enum.TryParse(rawData.partStr, true, out Part parsedPart))
            this.part = parsedPart;

        if (Enum.TryParse(rawData.mbtiStr, true, out MbtiFlags parsedMbti))
            this.mbti = parsedMbti;

        if (Enum.TryParse(rawData.hashTagsStr, true, out HashTags parsedTags))
            this.hashTags = parsedTags;

        this.stats = new EmployeeStats
        {
            property1  = 10,
            property2  = 10,
            property3  = 100,
            motivation = 50, 
            loyalty    = 50,    
            fatigue    = 0      
        };
    }
}