using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    // 암호화 Key
    private static readonly byte[] key = Encoding.UTF8.GetBytes("Dhw7pY3F4R2o9tS6");
    // 암호화 iv
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("Dtt7oG3F424o5r91");

    public const int MaxSaveSlots = 3;

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

    public void SaveGame(int slot)
    {
        if (slot < 0 || slot >= MaxSaveSlots) return;

        SaveData data = new SaveData();

        if(DateTimeManager.Instance != null)
        {
            DateTimeManager.Instance.ExportSaveData(data); // 날짜 정보
            Company.Instance.ExportCompanyData(data); // 회사 정보
            Company.Instance.curProject.ExportProjectData(data); // 프로젝트 정보
        }

        string keyName = GetSaveKey(slot);
        string jsonData = JsonUtility.ToJson(data, true);

        // JSON 문자열을 PlayerPrefs에 저장
        SaveEncryptedData(keyName, jsonData);
        Debug.Log("저장 완료");
    }

    public SaveData LoadGame(int slot)
    {
        if (slot < 0 || slot >= MaxSaveSlots) return null;

        string keyName = GetSaveKey(slot);
        string jsonData = LoadEncryptedData(keyName);

        if (!string.IsNullOrEmpty(jsonData))
        {
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(jsonData);

                if(DateTimeManager.Instance != null)
                {
                    DateTimeManager.Instance.ImportSaveData(data);
                }

                Debug.Log("불러오기 성공");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("저장된 세이터 없음.");
                return null;
            }

        }

        Debug.Log("저장된 데이터가 없습니다. 새 게임 시작");
        return null;
    }

    private string GetSaveKey(int slot)
    {
        return $"SaveSlot_{slot}";
    }

    // 특정 슬롯이 존재하는지 확인
    public bool HasSaveData(int slot)
    {
        if (slot < 0 || slot >= MaxSaveSlots) return false;

        string keyName = GetSaveKey(slot);
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(keyName));
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
        catch (Exception e)
        {
            Debug.Log("복호화 실패");
            return null;
        }
    }
}