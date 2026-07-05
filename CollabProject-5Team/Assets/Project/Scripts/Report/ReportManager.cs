using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ReportManager : MonoBehaviour
{
    public static ReportManager Instance;

    [Header("모든 보고서 SO")]
    [SerializeField] List<ReportSO> _allReports = new();
    [SerializeField] List<ReportSO> _allSpyReports = new();
    public IReadOnlyList<ReportSO> AllReports => _allReports;

    // Trait/startRepo/grade 조합에 여러 ReportSO가 있을 수 있음
    Dictionary<(Trait trait, int startRepo, int grade), List<ReportSO>> _reportMap = new();
    Dictionary<(Role role, int startRepo, int grade), List<ReportSO>> _spyReportMap = new();

    readonly List<ReportSO> _candidateBuffer = new();
    readonly Trait[] _traitBuffer = new Trait[3];

    #region 싱글톤
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    #endregion
        InitList();
    }

    void InitList()
    {
        _reportMap.Clear();
        _spyReportMap.Clear();

        foreach (ReportSO so in _allReports)
        {
            var key = (so.trait, so.startRepo, so.grade);
            if (!_reportMap.TryGetValue(key, out var reports))
            {
                reports = new List<ReportSO>();
                _reportMap[key] = reports;
            }

            reports.Add(so);
        }

        foreach (ReportSO so in _allSpyReports)
        {
            var key = (so.role, so.startRepo, so.grade);
            if (!_spyReportMap.TryGetValue(key, out var reports))
            {
                reports = new List<ReportSO>();
                _spyReportMap[key] = reports;
            }

            reports.Add(so);
        }
    }

    public ReportSO GetReportsByTrait(Employee e, int grade, int startRepo)//startRepo: 1=1주차 전용, 0=이후 랜덤 적용
    {
        _traitBuffer[0] = e.so.mainTrait;
        _traitBuffer[1] = e.so.riskTrait;
        _traitBuffer[2] = e.so.subTrait;

        _candidateBuffer.Clear();

        foreach (Trait trait in _traitBuffer)
        {
            if (_reportMap.TryGetValue((trait, startRepo, grade), out var reports))
                _candidateBuffer.AddRange(reports);
        }

#if UNITY_EDITOR
        LogCandidates(e, grade, startRepo);
#endif

        return _candidateBuffer.Count > 0
            ? _candidateBuffer[Random.Range(0, _candidateBuffer.Count)] // 랜덤 한개 반환
            : null;
    }

    public ReportSO GetSpyReport(Employee e, int grade, int startRepo)
    {
        _candidateBuffer.Clear();

        if (_spyReportMap.TryGetValue((e.so.role, startRepo, grade), out var reports))
            _candidateBuffer.AddRange(reports);

#if UNITY_EDITOR
        LogSpyCandidates(e, grade, startRepo);
#endif

        return _candidateBuffer.Count > 0
            ? _candidateBuffer[Random.Range(0, _candidateBuffer.Count)]
            : null;
    }

#if UNITY_EDITOR
    void LogCandidates(Employee e, int grade, int startRepo)
    {
        var sb = new StringBuilder();
        sb.Append($"[RM] {e.so.Name}({e.so.role}) startRepo={startRepo} grade={grade} 유효 보고서 {_candidateBuffer.Count}개: ");

        foreach (ReportSO so in _candidateBuffer)
            sb.Append($"[{so.title}({so.role}/{so.trait}/start{so.startRepo}/g{so.grade})] ");

        Debug.Log(sb.ToString());
    }

    void LogSpyCandidates(Employee e, int grade, int startRepo)
    {
        var sb = new StringBuilder();
        sb.Append($"[RM][Spy] {e.so.Name}({e.so.role}) startRepo={startRepo} grade={grade} valid reports={_candidateBuffer.Count}: ");

        foreach (ReportSO so in _candidateBuffer)
            sb.Append($"[{so.title}({so.role}/start{so.startRepo}/g{so.grade})] ");

        Debug.Log(sb.ToString());
    }
#endif
}
