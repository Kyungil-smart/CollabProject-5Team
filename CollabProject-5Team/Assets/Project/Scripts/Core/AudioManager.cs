using UnityEngine;
using UnityEngine.Audio;

public enum EAudioMixerType { Master, BGM, SFX }
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioMixer audioMixer;

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip _sfxAlert;
    [SerializeField] private AudioClip _sfxQuestTap;
    [SerializeField] private AudioClip _sfxClick;
    [SerializeField] private AudioClip _sfxNegative;
    [SerializeField] private AudioClip _sfxPositive;
    [SerializeField] private AudioClip _sfxQuestClear;

    bool[] isMute = new bool[3];
    float[] audioVolumes = new float[3];

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
    }
    public void SetAudioVolume(EAudioMixerType audioMixerType, float volume)
    {
        // 오디오 믹서의 값은 -80 ~ 0까지이기 때문에 0.0001 ~ 1의 Log10 * 20을 한다.
        // 따라서 슬라이더의 MinValue는 0.0001로 설정해야 한다
        audioMixer.SetFloat(audioMixerType.ToString(), Mathf.Log10(volume) * 20);
    }

    public void SetAudioMute(EAudioMixerType audioMixerType)
    {
        int type = (int)audioMixerType;
        if (!isMute[type]) // 뮤트 
        {
            isMute[type] = true;
            audioMixer.GetFloat(audioMixerType.ToString(), out float curVolume);
            audioVolumes[type] = curVolume;
            SetAudioVolume(audioMixerType, 0.001f);
        }
        else
        {
            isMute[type] = false;
            SetAudioVolume(audioMixerType, audioVolumes[type]);
        }
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (_bgmSource == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        if (_bgmSource == null) return;
        _bgmSource.Stop();
        _bgmSource.clip = null;
    }

    private float _lastSfxTime = -1f;
    private AudioClip _lastSfxClip;
    private const float SfxSameCooldown = 0.08f;

    public void PlaySFX(AudioClip clip)
    {
        if (_sfxSource == null || clip == null) return;
        if (clip == _lastSfxClip && Time.unscaledTime - _lastSfxTime < SfxSameCooldown) return;
        _lastSfxTime = Time.unscaledTime;
        _lastSfxClip = clip;
        _sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXAlert()      => PlaySFX(_sfxAlert);
    public void PlaySFXQuestTap()   => PlaySFX(_sfxQuestTap);
    public void PlaySFXClick()      => PlaySFX(_sfxClick);
    public void PlaySFXNegative()   => PlaySFX(_sfxNegative);
    public void PlaySFXPositive()   => PlaySFX(_sfxPositive);
    public void PlaySFXQuestClear() => PlaySFX(_sfxQuestClear);

    // 버튼 연결용 함수
    private void Mute()
    {
        AudioManager.Instance.SetAudioMute(EAudioMixerType.BGM);
    }
    private void ChangeVolume(float volume)
    {
        AudioManager.Instance.SetAudioVolume(EAudioMixerType.BGM, volume);
    }
}