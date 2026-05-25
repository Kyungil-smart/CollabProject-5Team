using UnityEngine;

// 직원 데이터 구글시트 연동용 SO
[CreateAssetMenu(fileName = "EmployeeSO", menuName = "Scriptable Objects/EmployeeSO")]
public class EmployeeSO : SheetDataSOBase
{
    [Header("[ 직원 기본 정보 ]")]
    public string    Name;
    public int       stat; // 프로퍼티3개로 분할???
    public Part      part;
    public MbtiFlags mbti;
    public HashTags  hashTags;
    public string    desc;

    // 계약금/급여
    public int contractMoney;
    public int salary;

    // [Header("[ 특성 ]")] 대표특성 / 보조특성 / 리크스 특성(임시 작성)
    //public enum mainTrait;
    //public enum subTrait;
    //public enum riskTrait;

    //[Header("[ 이미지 ]")] 추후 추가 예정

    public override void SetData(string[] data)
    {
        id = ParseInt(data[0]);
        Name = data[1];
        part = ParseEnum<Part>(data[2]);
        // 시트 순서가 나오지 않아서 나중에 완성
    }
}
