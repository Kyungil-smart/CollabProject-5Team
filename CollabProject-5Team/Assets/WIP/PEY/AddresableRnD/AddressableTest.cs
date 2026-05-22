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

    //AsyncOperationHandle<IList<Material>> matHandle;
    //AsyncOperationHandle<IList<Shader>> shaderHandle;

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

            //await LoadShadersByLabelAsync("Shader");
            //await LoadMaterialsByLabelAsync("Mat");
            Invoke("LoadAsset", 1f);
        }
        else
        {
            Debug.LogError("Failed to initialize Addressables.");
        }
    }

    //private async UniTask LoadShadersByLabelAsync(string label)
    //{
    //    shaderHandle = Addressables.LoadAssetsAsync<Shader>(label, (shader) =>
    //    {
    //        Debug.Log($"셰이더 로드됨: {shader.name}");
    //    });

    //    await shaderHandle.ToUniTask();

    //    if (shaderHandle.Status == AsyncOperationStatus.Succeeded)
    //        Debug.Log($"'Shader' 라벨 셰이더 {shaderHandle.Result.Count}개 로드 완료");
    //    else
    //        Debug.LogError("셰이더 로드 실패");
    //}

    //private async UniTask LoadMaterialsByLabelAsync(string label)
    //{
    //    matHandle = Addressables.LoadAssetsAsync<Material>(label, (mat) =>
    //    {
    //        Debug.Log($"머테리얼 로드됨: {mat.name}");
    //    });

    //    await matHandle.ToUniTask();

    //    if (matHandle.Status == AsyncOperationStatus.Succeeded)
    //        Debug.Log($"'mat' 라벨 머테리얼 {matHandle.Result.Count}개 로드 완료");
    //    else
    //        Debug.LogError("머테리얼 로드 실패");
    //}

    //public void ReleaseShaders()
    //{
    //    if (shaderHandle.IsValid())
    //    {
    //        Addressables.Release(shaderHandle);
    //        Debug.Log("셰이더 릴리스 완료");
    //    }
    //}

    //public void ReleaseMaterials()
    //{
    //    if (matHandle.IsValid())
    //    {
    //        Addressables.Release(matHandle);
    //        Debug.Log("머테리얼 릴리스 완료");
    //    }
    //}

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
        //ReleaseShaders();
        //ReleaseMaterials();
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
