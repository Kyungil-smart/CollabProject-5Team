using System.Collections.Generic;
using UnityEngine;

public class ReportManager : MonoBehaviour
{
    public static ReportManager Instance;

    [Header("모든 보고서 (SO 할당)")]
    [SerializeField] List<ReportSO> _allReports = new();
    public IReadOnlyList<ReportSO> AllReports => _allReports;

    // (Part, ReportGrade) 빠른 조회용
    public Dictionary<(Part, ReportGrade), List<ReportSO>> reportMap = new();

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

    // SO 리스트를 딕셔너리로 인덱싱
    void InitList()
    {
        reportMap.Clear();
        foreach (ReportSO so in _allReports)
        {
            if (so == null) continue;
            var key = (so.part, so.grade);
            if (!reportMap.ContainsKey(key))
                reportMap[key] = new List<ReportSO>();
            reportMap[key].Add(so);
        }
    }

    // (Part, ReportGrade)로 보고서 목록 조회
    public List<ReportSO> GetReportList(Part part, ReportGrade grade)
    {
        var key = (part, grade);
        return reportMap.TryGetValue(key, out var list) ? list : null;
    }

    // (Part, ReportGrade)에서 랜덤 보고서 1개 반환
    public ReportSO GetRandomReport(Part part, ReportGrade grade)
    {
        List<ReportSO> list = GetReportList(part, grade);
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning($"[Report] {part}/{grade} 에 해당하는 보고서가 없습니다.");
            return null;
        }
        return list[Random.Range(0, list.Count)];
    }
}

