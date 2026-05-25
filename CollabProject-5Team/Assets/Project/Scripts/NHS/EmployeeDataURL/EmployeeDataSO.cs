using UnityEngine;

public class EmployeeDataSO : SheetDataSOBase
{
    public EmployeeImmutableData data = new EmployeeImmutableData();

    public override void SetData(string[] rowData)
    {
        this.id = ParseInt(rowData[0]); // TODO 웹의 데이터와 맞추기

        data.id          = this.id;
        data.name        = rowData[1].Trim();
        data.partStr     = rowData[2].Trim();
        data.mbtiStr     = rowData[3].Trim();
        data.hashTagsStr = rowData[4].Trim();

        data.initProperty1 = ParseInt(rowData[5]);
        data.initProperty2 = ParseInt(rowData[6]);
        data.initProperty3 = ParseInt(rowData[7]);
    }
}
