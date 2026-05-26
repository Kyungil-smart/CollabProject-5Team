using UnityEngine;

public enum Trait // 직원 특성 임시로 작성!
{
    None,
    IdeaBank, DataAnalyst, TrendAgnostic
}


// 직원 데이터 구글시트 연동용 SO
[CreateAssetMenu(fileName = "EmployeeSO", menuName = "Scriptable Objects/EmployeeSO")]
public class EmployeeSO : SheetDataSOBase
{
    [Header("[ 직원 기본 정보 ]")]
    public string    Name;

    public int stat; // 작업능력 기본값:70
    public int property1; //   재미, 기술력, 비주얼 => 20(보정값) + 70(능력치)X0.5 = 55
    public int property2; // 창의성, 최적화, 연출력 => 20(보정값) + 70(능력치)X0.5 = 55
    public int property3; // 정교함, 버그제어, 구도 => 20(보정값) + 70(능력치)X0.5 = 55

    public Part      part;
    public MbtiFlags mbti;
    public HashTags  hashTags;
    public string    desc;

    // 계약금/급여
    public int contractMoney;
    public int salary;

    [Header("[ 특성 ]")] //대표특성 / 보조특성 / 리크스 특성(임시 작성)
    public Trait mainTrait;
    public Trait subTrait;
    public Trait riskTrait;

    //[Header("[ 이미지 ]")] 추후 추가 예정

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        Name = data[1].Trim();
        part = ParseEnum<Part>(data[2]);
        // 시트 순서가 나오지 않아서 나중에 완성
    }
}
