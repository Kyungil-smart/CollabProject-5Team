using System;
using System.IO;
using UnityEngine;

// 세이브 데이터
[Serializable]
public class SaveData
{
    //회사
    public Company company;

    // 진행중인 프로젝트
    public Project[] projects;

    // 직원
    public HaveEmployees haveEmployees;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string _savePath;

    private void Awake()
    {
        Instance = this;
        // 유니티 공식 안전 저장 경로 설정 (C:/Users/.../AppData/LocalLow/...)
        _savePath = Path.Combine(Application.persistentDataPath, "savefile.json");
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        // 데이터 채우기 예시
        // data.companyMoney = MoneyManager.Instance.CurrentMoney;
        // data.currentDay = DayManager.Instance.CurrentDay;

        // JSON 문자열로 변환 후 파일 쓰기
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_savePath, json);
        Debug.Log("게임 저장 완료!");
    }
}

public class Example : MonoBehaviour
{
    void Start()
    {
        if (!PlayerPrefs.HasKey("level"))
            PlayerPrefs.SetInt("level", 0);

        Debug.Log(String.Format("Level : {0}", PlayerPrefs.GetInt("level")));
    }
}