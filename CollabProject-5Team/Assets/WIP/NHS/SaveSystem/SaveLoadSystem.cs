using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    // 암호화 Key
    private static readonly byte[] key = Encoding.UTF8.GetBytes("Dhw7pY3F4R2o9tS6");
    // 암호화 iv
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("Dtt7oG3F424o5r91");

    public const int MaxSaveSlots = 3;

    public int? pendingLoadSlot;
    public string pendingPlayerName;
    public string pendingCompanyName;

    private static readonly JsonSerializerSettings jsonSettings = new()
    {
        Converters = { new StringEnumConverter() }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public bool SaveGame(int slot)
    {
        if (!IsValidSlot(slot)) return false;

        SaveData data = new SaveData();

        if (_EmployeeManager.Instance != null)
            _EmployeeManager.Instance.ExportEmployeeData(data);  // 직원 정보 저장

        if (Company.Instance != null)
        {
            Company.Instance.ExportCompanyData(data);        // 회사, 지난 프로젝트 정보 저장
            Company.Instance.ExportActiveProjectData(data);  // 진행 중 프로젝트 정보 저장
        }

        if (QuestManager.Instance != null)
            QuestManager.Instance.ExportQuestData(data);     // 퀘스트 정보 저장

        if (StoryQuestManager.Instance != null)
            StoryQuestManager.Instance.ExportStoryQuestData(data); // 스토리 퀘스트 진행 상태 저장

        if (DateTimeManager.Instance != null)
            DateTimeManager.Instance.ExportSaveData(data);   // 날짜 정보 저장

        data.realSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        string keyName = GetSaveKey(slot);
        string jsonData = SerializeSaveData(data);

        // JSON 문자열을 PlayerPrefs에 저장
        SaveEncryptedData(keyName, jsonData);
        Debug.Log("저장 완료");
        return true;
    }

    public SaveData GetSaveDataWithoutApply(int slot)
    {
        if (!IsValidSlot(slot)) return null;

        string keyName = GetSaveKey(slot);
        string jsonData = LoadEncryptedData(keyName);

        if (string.IsNullOrEmpty(jsonData)) return null;

        try
        {
            return DeserializeSaveData(jsonData);
        }
        catch (Exception)
        {
            Debug.LogError("불러오기 실패");
            return null;
        }
    }
    public bool LoadGame(SaveData data)
    {
        if (data == null) return false;

        try
        {
            if (_EmployeeManager.Instance != null)
                _EmployeeManager.Instance.ImportEmployeeData(data);  // 직원 정보 로드

            if (Company.Instance != null)
            {
                Company.Instance.ImportCompanyData(data);            // 회사, 지난 프로젝트 정보 로드
                Company.Instance.ImportActiveProjectData(data);      // 진행 중 프로젝트 정보 로드
            }

            if (QuestManager.Instance != null)
                QuestManager.Instance.ImportQuestData(data);         // 퀘스트 정보 로드

            if (StoryQuestManager.Instance != null)
                StoryQuestManager.Instance.ImportStoryQuestData(data); // 스토리 퀘스트 진행 상태 로드

            if (DateTimeManager.Instance != null)
                DateTimeManager.Instance.ImportSaveData(data);       // 날짜 정보 로드

            return true;
        }
        catch (Exception)
        {
            Debug.LogError("불러오기 실패");
            return false;
        }
    }

    public bool SetPendingLoad(int slot)
    {
        if (!HasSaveData(slot)) return false;

        pendingLoadSlot = slot;
        return true;
    }

    public bool TryConsumePendingLoad(out int slot, out SaveData data)
    {
        slot = -1;
        data = null;

        if (!pendingLoadSlot.HasValue) return false;

        slot = pendingLoadSlot.Value;
        pendingLoadSlot = null;
        data = GetSaveDataWithoutApply(slot);
        return data != null;
    }

    private string GetSaveKey(int slot)
    {
        return $"SaveSlot_{slot}";
    }

    private static string SerializeSaveData(SaveData data)
    {
        return JsonConvert.SerializeObject(data, Formatting.Indented, jsonSettings);
    }

    private static SaveData DeserializeSaveData(string jsonData)
    {
        return JsonConvert.DeserializeObject<SaveData>(jsonData, jsonSettings);
    }

    public bool HasSaveData(int slot)
    {
        if (!IsValidSlot(slot)) return false;

        string keyName = GetSaveKey(slot);
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(keyName));
    }

    private static bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < MaxSaveSlots;
    }

    public static void SaveEncryptedData(string keyName, string data)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using MemoryStream ms = new MemoryStream();
        using CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);

        byte[] bytes = Encoding.UTF8.GetBytes(data);
        cs.Write(bytes, 0, bytes.Length);
        cs.FlushFinalBlock();

        string encrypted = Convert.ToBase64String(ms.ToArray());
        PlayerPrefs.SetString(keyName, encrypted);
        PlayerPrefs.Save();
    }

    public static string LoadEncryptedData(string keyName)
    {
        string encryptedString = PlayerPrefs.GetString(keyName);
        if (string.IsNullOrEmpty(encryptedString))
            return null;

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            byte[] encryptedData = Convert.FromBase64String(encryptedString);

            using MemoryStream ms = new MemoryStream(encryptedData);
            using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader reader = new StreamReader(cs);

            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            Debug.Log("복호화 실패");
            return null;
        }
    }
}
