using System;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public enum SheetType
{
    csv, tsv
}

[Serializable]
public struct SheetData
{
    [TextArea] public string url;
    public SheetType type;
    public char SplitSymbol => type == SheetType.csv ? ',' : '\t';

    public async UniTask Load(Action<char, string[]> SuccessCallback)
    {
        // 1. url 자르기
        // https://docs.google.com/spreadsheets/d/  1PRrsybn13kcrgR5zBiGWRSQKx0hf3lgdx73FJw7l2oo/  edit?   gid=132668514#gid=132668514

        string sheetId = url.Split("d/")[1].Split('/')[0];
        string gid = url.Split("gid=")[1].Split('&')[0].Split('#')[0];
        string format = type == SheetType.csv ? "csv" : "tsv";

        // 2. url 재설정
        url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format={format}&gid={gid}";

        // 3. 웹 요청
        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            Debug.Log("웹 요청 시작");
            await uwr.SendWebRequest();

            //  3-1. 실패면 에러
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"웹 데이터 로딩 실패: {uwr.error}");
                return;
            }
            //  3-2. 성공이면 데이터 반영하기
            string sheetData = uwr.downloadHandler.text;
            string[] lines = sheetData.Split('\n');

            SuccessCallback?.Invoke(SplitSymbol, lines); // 로딩 성공시 데이터 전달 및 실행
        }
    }
}
