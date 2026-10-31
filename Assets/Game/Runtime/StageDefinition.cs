using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoundThief
{
    [CreateAssetMenu(menuName = "Sound Thief/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string stageId = "new-stage";
        public string title = "새 의뢰";
        [TextArea] public string briefing = "목표물을 회수한 뒤 출구로 돌아오세요.";
        public string[] requiredItemIds = { "objective" };
        public string objectiveText = "목표물을 회수하세요.";
        public string exitText = "출구에서 F로 탈출하세요.";
        public bool startImmediately = true;
    }

}
