using UnityEngine;

public class SaveTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveLoadSystem.Instance.SaveGame(0);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            SaveData data = SaveLoadSystem.Instance.LoadGame(0);

            if (data != null)
            {
                Debug.Log(Application.persistentDataPath);
            }
            else
            {
                Debug.Log("불러오기 실패 또는 데이터 없음");
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            bool hasData = SaveLoadSystem.Instance.HasSaveData(0);
            Debug.Log($"슬롯 1 데이터 존재 여부: {hasData}");
        }

        if (Input.GetKeyDown(KeyCode.P))
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