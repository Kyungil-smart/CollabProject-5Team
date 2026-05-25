using System;
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

        data.    partParsed = ParseEnum<Part>            (data.partStr);
        data.    mbtiParsed = ConvertMBTIStringToEnum    (data.mbtiStr);
        data.hashTagsParsed = ConvertHashTagsStringToEnum(data.hashTagsStr);

        data.initProperty1 = ParseInt(rowData[5]);
        data.initProperty2 = ParseInt(rowData[6]);
        data.initProperty3 = ParseInt(rowData[7]);
    }

    private MbtiFlags ConvertMBTIStringToEnum(string mbtiStr)
    {
        MbtiFlags result = MbtiFlags.INTP;

        string upperStr = mbtiStr.ToUpper().Trim();

        if(upperStr.Contains('E')) result |= MbtiFlags.E;
        if(upperStr.Contains('S')) result |= MbtiFlags.S;
        if(upperStr.Contains('F')) result |= MbtiFlags.F;
        if(upperStr.Contains('J')) result |= MbtiFlags.J;

        return result;
    }

    private HashTags ConvertHashTagsStringToEnum(string tagsStr)
    {
        HashTags result = HashTags.None; // 0000 시작

        string[] tags = tagsStr.Split(',');
        foreach (var tag in tags)
        {
            if (Enum.TryParse(tag.Trim(), true, out HashTags parsedTag))
            {
                result |= parsedTag;
            }
        }
        return result;
    }
}
