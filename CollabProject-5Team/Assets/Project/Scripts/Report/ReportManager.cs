using System.Collections.Generic;
using UnityEngine;

public class ReportManager : MonoBehaviour
{
    public static ReportManager Instance;

    [Header("모든 보고서 (SO 할당)")]
    [SerializeField] List<ReportSO> _allReports = new();
    public IReadOnlyList<ReportSO> AllReports => _allReports;

    // (Trait,(startRepo첫주자보고서여부,grade)) → ReportSO 단일 조회용
    Dictionary<(Trait,(int, int)), ReportSO> _reportMap = new();

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    #endregion
        InitList();
    }

    // SO 리스트를 (Trait, (startRepo,grade)) 딕셔너리로 인덱싱
    void InitList()
    {
        _reportMap.Clear();
        foreach (ReportSO so in _allReports)
        {
            if (so == null) continue;
            _reportMap[(so.trait, (so.startRepo,so.grade))] = so;
        }
    }

    // 직원 Trait(main/sub/risk)과 grade로 ReportSO 1개 반환
    List<ReportSO> _candidateBuffer = new List<ReportSO>(3);
    Trait[] _traitBuffer = new Trait[3];
    public ReportSO GetReportsByTrait(Employee e, int grade, int startRepo)//startRepo: 1=1주차 전용, 0=이후 랜덤 적용
    {
        _traitBuffer[0] = e.so.mainTrait;
        _traitBuffer[1] = e.so.riskTrait;
        _traitBuffer[2] = e.so.subTrait;

        _candidateBuffer.Clear();

        foreach (Trait t in _traitBuffer)
        {
            if (_reportMap.TryGetValue((t, (startRepo,grade)), out var so))
                _candidateBuffer.Add(so);
        }

        if (_candidateBuffer.Count > 0)
        {
#if UNITY_EDITOR
            var sb = new System.Text.StringBuilder();
            sb.Append($"[ReportManager] {e.so.Name}({e.so.role}) grade={grade} 유효 후보 보고서: ");
            foreach (var so in _candidateBuffer)
                sb.Append($"[{so.title}({so.role}/{so.trait}/g{so.grade})] ");
            Debug.Log(sb.ToString());
#endif
            return _candidateBuffer[Random.Range(0, _candidateBuffer.Count)]; // 랜덤 한개 반환
        }

        return null;
    }
}

