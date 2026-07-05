using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextSoundTweener : MonoBehaviour
{
    [Header("오디오 컴포넌트")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private List<AudioClip> _sampleSounds;

    [Header("사운드 피치 설정 (동숲 최적화)")]
    [SerializeField][Range(0.8f, 1.5f)] private float _baseOctave = 1.0f;
    [SerializeField][Range(0.0f, 0.2f)] private float _randomFactor = 0.08f;

    [Header("타이핑 설정")]
    [SerializeField][Range(1, 3)] private int _soundInterval = 2;

    private Coroutine _typingCoroutine;
    private TextMeshProUGUI _targetTextComponent;
    private string _currentFullDialogue;
    private System.Action _onCompleteCallback;

    private Dictionary<char, AudioClip> _soundDictionary = new Dictionary<char, AudioClip>();

    private void Awake()
    {
        InitializeSoundDictionary();

        if (_audioSource != null)
        {
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
        }
    }

    private void InitializeSoundDictionary()
    {
        _soundDictionary.Clear();
        if (_sampleSounds == null) return;

        foreach (AudioClip clip in _sampleSounds)
        {
            if (clip == null) continue;
            char letterKey = clip.name[0];
            if (!_soundDictionary.ContainsKey(letterKey))
            {
                _soundDictionary.Add(letterKey, clip);
            }
        }
    }

    public void DoType(TextMeshProUGUI textComponent, string fullDialogue, float typingSpeed, System.Action onComplete = null)
    {
        KillActiveTween();

        _targetTextComponent = textComponent;
        _targetTextComponent.text = "";
        _currentFullDialogue = fullDialogue;
        _onCompleteCallback = onComplete;

        _typingCoroutine = StartCoroutine(TypeTextRoutine(typingSpeed));
    }

    private IEnumerator TypeTextRoutine(float typingSpeed)
    {
        int soundCounter = 0;

        for (int i = 0; i < _currentFullDialogue.Length; i++)
        {
            char currentChar = _currentFullDialogue[i];
            _targetTextComponent.text += currentChar;

            if (currentChar != ' ' && currentChar != '.' && currentChar != ',' && currentChar != '!' && currentChar != '?')
            {
                soundCounter++;

                if (soundCounter % _soundInterval == 0)
                {
                    PlayAnimaleseSound(currentChar);
                }
            }
            else
            {
                if (_audioSource != null) _audioSource.Stop();
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        CompleteActiveTween();
    }

    private void PlayAnimaleseSound(char letter)
    {
        if (_audioSource == null) return;

        AudioClip targetClip = null;

        if (_soundDictionary.TryGetValue(letter, out var matchedClip))
        {
            targetClip = matchedClip;
        }
        else if (_sampleSounds != null && _sampleSounds.Count > 0)
        {
            targetClip = _sampleSounds[Random.Range(0, _sampleSounds.Count)];
        }

        if (targetClip != null)
        {
            _audioSource.pitch = _baseOctave + (Random.Range(-_randomFactor, _randomFactor));

            _audioSource.PlayOneShot(targetClip);
        }
    }

    public bool CompleteActiveTween()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;

            if (_audioSource != null) _audioSource.Stop();

            if (_targetTextComponent != null)
            {
                _targetTextComponent.text = _currentFullDialogue;
            }

            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
            return true;
        }
        return false;
    }

    public void KillActiveTween()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
            if (_audioSource != null) _audioSource.Stop();
        }
        _onCompleteCallback = null;
    }
}