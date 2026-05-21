using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class AddressableTest : MonoBehaviour
{
    public AssetReferenceGameObject assetReference;
    public AssetReferenceGameObject[] roomObjs;

    //public AssetReferenceT<AudioClip> soundBGM;
    //public GameObject BGMObj;

    //public AssetReferenceSprite flagSprite;
    //public GameObject flagImage;

    public List<GameObject> assetList;

    private void Start()
    {
        InitAddressableAsync().Forget();
    }

    async UniTaskVoid InitAddressableAsync()
    {
        var handle = Addressables.InitializeAsync();
        await handle.ToUniTask();
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("Addressables initialized.");
        }
        else
        {
            Debug.LogError("Failed to initialize Addressables.");
        }
    }

    public void LoadAsset()
    {
        assetReference.InstantiateAsync().Completed += (handle) =>
        {
            assetList.Add(handle.Result);
        };

        for (int i = 0; i < roomObjs.Length; i++)
        {
            roomObjs[i].InstantiateAsync().Completed += (handle) =>
            {
                assetList.Add(handle.Result);
            };
        }

        //soundBGM.LoadAssetAsync().Completed += (clip) =>
        //{
        //    var bgmSound = BGMObj.GetComponent<AudioSource>();
        //    bgmSound.clip = clip.Result;
        //    bgmSound.loop = true;
        //    bgmSound.Play();
        //};

        //flagSprite.LoadAssetAsync().Completed += (img) =>
        //{
        //    var image = flagImage.GetComponent<Image>();
        //    image.sprite = img.Result;
        //};
    }

    public void UnloadAsset()
    {
        //Addressables.Release(soundBGM);
        //Addressables.Release(flagSprite);

        if (assetList.Count == 0) return;
        foreach (var obj in assetList)
        {
            Addressables.ReleaseInstance(obj);
        }
        assetList.Clear();
    }

    private void Update()
    {
        // 테스트용 입력. 1번 로드, 2번 언로드
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadAsset();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            UnloadAsset();
        }
    }
}
