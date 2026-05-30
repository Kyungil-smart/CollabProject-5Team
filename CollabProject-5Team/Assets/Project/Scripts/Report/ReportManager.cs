using System.Collections.Generic;
using UnityEngine;

public class ReportManager : MonoBehaviour
{
    public static ReportManager Instance;

    [Header("모든 보고서 (SO 할당)")]
    [SerializeField] List<ReportSO> _allReports = new();
    public IReadOnlyList<ReportSO> AllReports => _allReports;

    // (Trait, grade) → ReportSO 단일 조회용
    Dictionary<(Trait, int), ReportSO> _reportMap = new();

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
        InitList();
    }

    // SO 리스트를 (Trait, grade) 딕셔너리로 인덱싱
    void InitList()
    {
        _reportMap.Clear();
        foreach (ReportSO so in _allReports)
        {
            if (so == null) continue;
            _reportMap[(so.trait, so.grade)] = so;
            // grade=0 키는 폴백용으로 덮어쓰기
            if (!_reportMap.ContainsKey((so.trait, 0)))
                _reportMap[(so.trait, 0)] = so;
        }
    }

    // 직원 Trait(main/sub/risk)과 grade로 ReportSO 1개 반환
    // 없으면 grade=0 폴백
    List<ReportSO> _candidateBuffer = new List<ReportSO>(3);
    Trait[] _traitBuffer = new Trait[3];
    public ReportSO GetReportsByTrait(Employee e, int grade)
    {
        _traitBuffer[0] = e.so.mainTrait;
        _traitBuffer[1] = e.so.subTrait;
        _traitBuffer[2] = e.so.riskTrait;

        _candidateBuffer.Clear();

        foreach (Trait t in _traitBuffer)
        {
            if (_reportMap.TryGetValue((t, grade), out var so))
                _candidateBuffer.Add(so);
        }

        if (_candidateBuffer.Count > 0)
            return _candidateBuffer[Random.Range(0, _candidateBuffer.Count)]; // 랜덤 한개 반환

        // 없으면 grade=0 폴백 시도
        foreach (Trait t in _traitBuffer)
        {
            if (_reportMap.TryGetValue((t, 0), out var so))
                _candidateBuffer.Add(so);
        }

        return _candidateBuffer.Count > 0
            ? _candidateBuffer[Random.Range(0, _candidateBuffer.Count)]
            : null;
    }
}

