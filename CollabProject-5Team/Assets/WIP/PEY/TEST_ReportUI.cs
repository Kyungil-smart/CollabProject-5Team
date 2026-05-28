using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보고서 선택 UI 흐름 테스트
// Summary → Detail 패널 → Approve/Cancel → 다음 역할 또는 복귀
// 역할 순서: Planning → Art → Programmer
public class TEST_ReportUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject reportSummaryPannel;
    [SerializeField] GameObject reportDetailPannel;
    [SerializeField] GameObject dayPannel; // 낮 패널 (외부 연결)

    [Header("ReportSummary 패널 내부")]
    [SerializeField] TMP_Text summaryRoleText;   // "Planning Report" 등 파트 제목
    [SerializeField] Button   reportSummaryBtn;  // 보고서 요약 버튼
    [SerializeField] TMP_Text summaryReportName; // 보고서 이름
    [SerializeField] TMP_Text summaryEmploeName; // 직원 이름

    [Header("ReportDetail 패널 내부")]
    [SerializeField] TMP_Text detailRoleText;    // 파트 제목
    [SerializeField] TMP_Text detailEmploeName;  // 직원 이름
    [SerializeField] TMP_Text detailMainText;    // 보고서 본문
    [SerializeField] Button   approveBtn;
    [SerializeField] Button   cancelBtn;


    // 역할 진행 순서
    static readonly Role[] RoleOrder = { Role.PLANNER, Role.ARTIST, Role.PROGRAMMER };
    static readonly string[] RoleLabel = { "Planning Report", "Art Report", "Develop Report" };

    int          _roleIndex;   // 현재 처리 중인 역할 인덱스 (0~2)
    List<Report> _currentList; // 현재 역할의 보고서 목록
    int          _reportIndex; // 현재 역할 내에서 보고 있는 보고서 인덱스
    Report       _viewingReport;

    // ── 외부에서 호출 ────────────────────────────────────────

    // ProgressNight 후 이 메서드를 호출해 보고서 UI를 시작한다
    public void StartReportFlow()
    {
        _roleIndex = 0;
        ShowSummaryForCurrentRole();
    }

    // ── 역할별 Summary 구성 ──────────────────────────────────

    void ShowSummaryForCurrentRole()
    {
        // 역할 순서를 모두 소진했으면 승인 처리
        if (_roleIndex >= RoleOrder.Length)
        {
            FinishAllReports();
            return;
        }

        Role currentRole = RoleOrder[_roleIndex];
        _currentList = GetReportsByRole(currentRole);

        if (_currentList == null || _currentList.Count == 0)
        {
            // 해당 역할 보고서 없으면 다음 역할로 스킵
            _roleIndex++;
            ShowSummaryForCurrentRole();
            return;
        }

        _reportIndex = 0;
        RefreshSummaryPanel();

        reportSummaryPannel.SetActive(true);
        reportDetailPannel.SetActive(false);
        dayPannel?.SetActive(false);
    }

    // Summary 패널 내용을 현재 _reportIndex 보고서로 갱신 (재활용 가능)
    void RefreshSummaryPanel()
    {
        summaryRoleText.text = RoleLabel[_roleIndex];

        Report r = _currentList[_reportIndex];
        summaryReportName.text = r.so.title;
        summaryEmploeName.text = r.owner.so.Name;

        // 버튼 클릭: Detail로 전환
        reportSummaryBtn.onClick.RemoveAllListeners();
        reportSummaryBtn.onClick.AddListener(() => ShowDetail(r));
    }

    // ── Detail 패널 ──────────────────────────────────────────

    void ShowDetail(Report report)
    {
        _viewingReport = report;

        detailRoleText.text   = RoleLabel[_roleIndex];
        detailEmploeName.text = report.owner.so.Name;
        detailMainText.text   = report.so.contentNormal;

        reportSummaryPannel.SetActive(false);
        reportDetailPannel.SetActive(true);

        approveBtn.onClick.RemoveAllListeners();
        approveBtn.onClick.AddListener(OnApprove);

        cancelBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.AddListener(OnCancel);
    }

    void OnApprove()
    {
        // 현재 보고서를 선택으로 저장
        Company.Instance.curProject.SelectReport(_viewingReport);

        // 다음 역할로
        _roleIndex++;
        ShowSummaryForCurrentRole();
    }

    void OnCancel()
    {
        // Summary로 돌아감 (같은 보고서 목록 유지)
        reportDetailPannel.SetActive(false);
        reportSummaryPannel.SetActive(true);
    }

    // ── 완료 처리 ────────────────────────────────────────────

    void FinishAllReports()
    {
        Company.Instance.curProject.ApproveSelectedReports();

        reportSummaryPannel.SetActive(false);
        reportDetailPannel.SetActive(false);
        dayPannel?.SetActive(true);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    List<Report> GetReportsByRole(Role role)
    {
        var result = new List<Report>();
        foreach (Report r in Company.Instance.curProject.pendingReports)
        {
            if (r.role == role) result.Add(r);
        }
        return result;
    }
}
