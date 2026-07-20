using System;
using UnityEngine;

namespace PlayableAd
{
    public enum RunFlowState
    {
        Intro,
        Tutorial,
        MainRun,
        Boss,
        Result
    }

    public sealed class RunFlowController : MonoBehaviour
    {
        [SerializeField, InspectorName("Current State（当前流程状态）")] private RunFlowState currentState = RunFlowState.Intro;

        public event Action<RunFlowState, RunFlowState> StateChanged;

        public RunFlowState CurrentState => currentState;
        public bool IsGameplayActive => currentState == RunFlowState.Tutorial || currentState == RunFlowState.MainRun;

        public void ResetToIntro()
        {
            SetState(RunFlowState.Intro);
        }

        public void StartTutorial()
        {
            SetState(RunFlowState.Tutorial);
        }

        public void EnterMainRun()
        {
            if (currentState == RunFlowState.Tutorial || currentState == RunFlowState.Boss)
                SetState(RunFlowState.MainRun);
        }

        public void EnterBoss()
        {
            SetState(RunFlowState.Boss);
        }

        public void EnterResult()
        {
            SetState(RunFlowState.Result);
        }

        private void SetState(RunFlowState nextState)
        {
            if (currentState == nextState) return;
            RunFlowState previous = currentState;
            currentState = nextState;
            StateChanged?.Invoke(previous, nextState);
        }
    }
}
