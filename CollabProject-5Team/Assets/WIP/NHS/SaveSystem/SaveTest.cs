using UnityEngine;

public class SaveTest : MonoBehaviour
{
    void Update()
    {
        // S 키를 누르면 슬롯 1에 저장
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveLoadSystem.Instance.SaveGame(1);
        }

        // L 키를 누르면 슬롯 1 불러오기
        if (Input.GetKeyDown(KeyCode.L))
        {
            tempData data = SaveLoadSystem.Instance.LoadGame(1);

            if (data != null)
            {
                Debug.Log(Application.persistentDataPath);
                Debug.Log($"이름: {data.name} | 레벨: {data.level} | 골드: {data.gold}");
            }
            else
            {
                Debug.Log("불러오기 실패 또는 데이터 없음");
            }
        }

        // H 키를 누르면 슬롯에 데이터가 있는지 확인
        if (Input.GetKeyDown(KeyCode.H))
        {
            bool hasData = SaveLoadSystem.Instance.HasSaveData(2);
            Debug.Log($"슬롯 1 데이터 존재 여부: {hasData}");
        }

        if (Input.GetKeyDown(KeyCode.P))   // P 키 누르면 정보 출력
        {
            Debug.Log("저장 경로: " + Application.persistentDataPath);
            Debug.Log("Company Name: " + Application.companyName);
            Debug.Log("Product Name: " + Application.productName);

            for (int i = 1; i <= SaveLoadSystem.MaxSaveSlots; i++)
            {
                string key = $"SaveSlot_{i}";
                string value = PlayerPrefs.GetString(key);
                Debug.Log($"{key} 존재: {!string.IsNullOrEmpty(value)}");
            }
        }
    }
}