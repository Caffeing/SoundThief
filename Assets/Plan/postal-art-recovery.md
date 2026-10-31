# 우체국 장식 복구 기록

## 복구 근거

원격 HMM의 `5a7dfc11` 커밋(살ㄹ려줘)에 우체국 에셋 적용 씬이 있다. 다음 커밋 `28256ad6`에서도 해당 씬은 동일하다. 이번 조사 전 로컬 HMM은 `0e72648d`였으므로 이 장식 변경을 포함하지 않았다. 원격 이력에서 장식 씬이 삭제됐다고 판단할 근거는 없다.

## 적용 내용

- 원본: `5a7dfc11:Assets/Scenes/Stages/PostalDepot/PostalDepot.unity`
- 현재 적용 씬: `Assets/Scenes/Stages/Stage3/PostalDepot.unity`
- 외형 그룹 128개, 렌더러 581개 복구. 기존 렌더러 214개의 표시 여부·재질을 원본 버전에 맞췄다.
- 추가 재질 `Manor postal parchment.mat`은 원본 GUID를 유지해 `Assets/Game/Stages/Stage3/Materials/`에 복구했다.
- 기존 Transform 월드 행렬, Collider 직렬화 데이터, MonoBehaviour 직렬화 데이터가 복구 전후 동일함을 확인했다. 현재 게임플레이 설정과 파일 구조를 유지한다.
- 복구한 외형의 메시·재질 누락 검사는 통과했다. 전체 플레이 및 재빌드는 하지 않았다.

원격 커밋 전체를 병합하거나 에셋 importer 설정 변경을 가져오지 않았다. 우체국 외형만 선택적으로 복구했다. 로컬 변경은 아직 커밋·푸시하지 않았다.

복구 전 씬과 조사 데이터는 `Logs/PostalRecovery/`에 보관했다.
