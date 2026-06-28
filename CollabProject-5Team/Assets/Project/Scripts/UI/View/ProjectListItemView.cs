using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 진행 프로젝트 목록 아이템 프리팹 바인딩.
    /// Tab_InProgress.InProgressList Content에 동적 생성.
    /// 상태별 배경 스프라이트, 규모별 썸네일은 Inspector에서 지정.
    /// </summary>
    public sealed class ProjectListItemView : MonoBehaviour, IBindable<Project>, IBindable<ProjectCompleted>
    {
        [SerializeField] private Image _itemFrame;
        [SerializeField] private Image _projectIcon;
        [SerializeField] private TextMeshProUGUI _projectNumLabel;
        [SerializeField] private TextMeshProUGUI _projectNameLabel;
        [SerializeField] private TextMeshProUGUI _scaleValue;
        [SerializeField] private TextMeshProUGUI _statusValue;

        [Header("상태별 스프라이트")]
        [SerializeField] private Sprite _spriteInProgress;
        [SerializeField] private Sprite _spriteInService;
        [SerializeField] private Sprite _spriteServiceOver;

        [Header("규모별 썸네일 스프라이트")]
        [SerializeField] private Sprite _thumbnailSmall;
        [SerializeField] private Sprite _thumbnailMedium;
        [SerializeField] private Sprite _thumbnailLarge;

        [Header("업데이트 확정")]
        [SerializeField] private GameObject _overlay;
        [SerializeField] private GameObject _stamp;

        public void Bind(Project project)
        {
            _projectNameLabel.text = project.userNamed.Value;
            _scaleValue.text = GetScaleText(project.Scale);
            _projectIcon.sprite = GetThumbnail(project.Scale);
            _statusValue.text = "제작 중";
            _itemFrame.sprite = _spriteInProgress;

            _overlay.SetActive(false);
            _stamp.SetActive(false);
        }

        public void Bind(ProjectCompleted record)
        {
            _projectNameLabel.text = record.projectName;
            _scaleValue.text = GetScaleText(record.scale);
            _projectIcon.sprite = GetThumbnail(record.scale);

            bool isPending = record.isUpdatePending;
            _overlay.SetActive(isPending);
            _stamp.SetActive(isPending);

            if (record.isServiceOver)
            {
                _statusValue.text = "서비스 종료";
                _itemFrame.sprite = _spriteServiceOver;
                return;
            }

            _statusValue.text = "서비스 중";
            _itemFrame.sprite = _spriteInService;
        }

        public void SetNumber(int number)
        {
            _projectNumLabel.text = number.ToString();
        }

        private Sprite GetThumbnail(ProjectSize scale) => scale switch
        {
            ProjectSize.Small => _thumbnailSmall,
            ProjectSize.Medium => _thumbnailMedium,
            ProjectSize.Large => _thumbnailLarge,
            _ => null,
        };

        private string GetScaleText(ProjectSize scale) => scale switch
        {
            ProjectSize.Small => "소형",
            ProjectSize.Medium => "중형",
            ProjectSize.Large => "대형",
            _ => string.Empty,
        };
    }
}