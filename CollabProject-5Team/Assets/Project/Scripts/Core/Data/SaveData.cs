using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle;

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

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    // 암호화 Key
    private static readonly byte[] key = Encoding.UTF8.GetBytes("Dhw7pY3F4R2o9tS6");

    // 암호화 iv
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("Dtt7oG3F424o5r91");

    private string _savePath;

    private void Awake()
    {
        if(Instance != null & Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        string jsonData = JsonUtility.ToJson(data, true);

        // JSON 문자열을 PlayerPrefs에 저장
        SaveEncryptedData("GameSave", jsonData);
    }

    public SaveData LoadGame()
    {
        string jsonData = LoadEncryptedData("GameSave");

        if(!string.IsNullOrEmpty(jsonData))
        {
            SaveData data = JsonUtility.FromJson<SaveData>(jsonData);

            return data;
        }

        Debug.Log("저장된 데이터가 없습니다. 새 게임 시작");
        return null;
    }

    public static void SaveEncryptedData(string keyName, string data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                cs.Write(bytes, 0, bytes.Length);
                cs.FlushFinalBlock();

                string encrypted = Convert.ToBase64String(ms.ToArray());
                PlayerPrefs.SetString(keyName, encrypted);
                PlayerPrefs.Save();
            }
        }
    }

    public static string LoadEncryptedData(string keyName)
    {
        string encryptedString = PlayerPrefs.GetString(keyName);
        if (!string.IsNullOrEmpty(encryptedString))
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                //암호화 키와 초기화 벡터를 이용하여 복호화를 진행할 decryptor 생성
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // 데이터 복호화
                byte[] encryptedData = Convert.FromBase64String(encryptedString);
                byte[] decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                // 복호화된 데이터를 이용하여 저장된 데이터 반환
                return Encoding.UTF8.GetString(decryptedData);
            }
        }
        else
        {
            return null;
        }
    }
}