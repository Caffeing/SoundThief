# 프로토타입 검증 결과

검증일: 2026-10-28 | Unity 6000.3.19f1 | Windows | 씬: `Assets/Scenes/ManorPrototype.unity`

## 자동 검사

PlayMode **13/13 통과**, 실패 0, 건너뜀 0. 실행 시간 14.98초. 원본 결과는 `playmode-results.json`, 재실행 대상은 `SoundThief.Tests.ManorPlayModeTests`다. 매 검사에서 씬을 새로 로드하고 실험용 설정을 복제하여 프로젝트의 기본 수치를 변경하지 않는다.

| 검사 | 확인한 동작 | 결과 |
| --- | --- | --- |
| ExtractionMutesWholeRelationAndBlocksSecondCharge | 관계 무음·개문 후 소리 유지·추가 추출 차단·느린 속도 설정 | 통과 |
| InjectionAllowsIndependentOutputsAndRestoresOnDecay | 여러 대상 동시 주입·같은 대상 거절·거절 시 손 점유 보존·감쇠 복구 | 통과 |
| ThrowKeepsDeadlineAndExplodesOnceInAir | 투척 즉시 빈손·기존 기한·공중 현재 위치 단일 폭발·관계 복구 | 통과 |
| GroundCollisionDetonatesBeforeDeadline | 기한 전 물리 착지 폭발 | 통과 |
| HeldTimeoutStunsAndRestoresWithoutFailing | 손 만료 자기 기절·복구·의뢰 유지·추출/일반 상호작용 차단 | 통과 |
| BlastIsBlockedByWallButSoundStillWakesGuard | 벽 너머 청각과 벽에 막히는 폭발 기절 분리 | 통과 |
| PanicIgnoresSightSoundAndRestunUntilRecovery | 패닉 상태 유지·소리/재기절 무시·종료 후 재기절 허용 | 통과 |
| OnlyHigherGradeRetargetsAndSoundEndStopsInvestigation | 같거나 낮은 등급 목표 유지·높은 등급 교체·소리 종료 경계 | 통과 |
| SightTracksThenUsesLastVisiblePosition | 시야 추적·시야 상실 후 마지막 목격 위치 고정 | 통과 |
| PauseFreezesDeadlineAndContractDoesNot | 일시정지 시 보유 기한 정지·의뢰창 열어도 시간 진행 | 통과 |
| ObjectiveRequiresTheftAndExitThenRestartClearsEverything | 절도와 탈출 모두 필요·재시작 후 문/아이템/관계/진행 초기화 | 통과 |
| GuardContactImmediatelyRestartsWholeContract | 접촉 즉시 의뢰 전체 초기화 | 통과 |
| ShortInputEventsStillInteractAndMovementAdvances | 프레임 사이 짧은 키 이벤트 보존·실제 F 상호작용·W 전진 | 통과 |

## 실제 화면과 실행 확인

- Unity 에디터 컴파일 성공, 최종 실행 구간 콘솔 오류 0·경고 0.
- 서버에서 `Time.frameCount`가 631 → 645로 증가하는 것을 관찰하여 실제 프레임 진행 확인.
- Game 창 포커스 후 입력 도구의 F로 열쇠 획득 확인. Unity의 비활성 게임 창은 키보드 입력을 받지 않으므로 플레이 시 Game 창을 클릭한다.
- 관계 추출·무음 개문 상태에서 등급 2, 남은 보유 시간, 무음/복구 대기, 아이템 보유, 다음 목표 표시 확인. 이 화면의 상태 배치는 에디터에서 게임 기능을 호출해 재현했다.
- 화면: `Images/contract.png`(의뢰), `Images/manor.png`(입구), `Images/extraction.png`(추출·무음 개문).
- 확인 후 플레이 모드를 종료하고 저택 씬을 열어 두었다. 임시 플레이 상태는 저장하지 않는다.

## 확인 범위의 한계

자동 검사는 규칙의 회귀 검사이며 사람이 자연스럽게 처음부터 끝까지 조작한 완주·난이도 평가를 대신하지 않는다. 시야 검사에서는 추적과 마지막 목격 위치 고정을 검증했으며, 해당 위치 도착 후 경계와 안정 복귀의 긴 흐름은 수동 관찰이 남아 있다. 패닉 검사는 소리·재기절에 대한 상태 고정을 확인했고 플레이어를 정면에 둔 시야 자극 조합은 별도 수동 검증 대상이다. 정식 미술·공간 음향·투척 궤적·반응 예측 UI·배포 빌드·타 플랫폼 검증은 이번 범위에 포함하지 않았다.
