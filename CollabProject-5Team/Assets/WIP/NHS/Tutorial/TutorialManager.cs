using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using R3;

namespace Dialogue
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _dimOverlay;
        [SerializeField] private RectTransform _fingerPointer;

        private Dictionary<int, string> _tutorialSteps = new()
        {
            { 1005, "Btn_WorkStart" },
            { 1008, "Btn_EmployeeMenu" }
        };

        private Canvas           _tempCanvas;
        private GraphicRaycaster _tempRaycaster;
        private Button           _currentButton;

        void Start()
        {
            
        }
    }
}