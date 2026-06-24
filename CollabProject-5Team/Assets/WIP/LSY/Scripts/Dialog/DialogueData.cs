using System;
using UnityEngine;

namespace Dialogue
{
    public enum EmployeeDialogueState
    {
        Normal,
        Caution,
        Critical,
    }

    public struct StatDelta
    {
        public int employeeId;
        public int desireDelta;
        public int fatigueDelta;
        public int loyaltyDelta;
    }

    public struct DialogueStartPayload
    {
        public EmployeeDialogueState state;

        public int    employeeId;
        public string desc;     // 화자 이름 ("흰 고양이", "유저" 등)
        public string text;     // 대사 텍스트
        public bool   isChoice; // true면 선택지 표시
        public bool   isUser;   // true면 유저 대사 (초상화 숨김)
        public string choice01; // 선택지 1 텍스트
        public string choice02; // 선택지 2 텍스트
    }

    public class EmployeeDialogueViewData
    {
        public string desc; // 화자 이름
        public string text;
        public Sprite portrait;
    }

    public class ChoiceItemViewData
    {
        public string      text;
        public int         index;
        public Action<int> onSelected;
    }
}