# 의뢰별 재수락 정책 — 2026-10-30

의뢰별 `GuildContractOffer.nonRepeatable` 설정을 추가했다. 기본값 false는 재수락 가능이다. 인스펙터에는 UI Toolkit PropertyDrawer로 **재수락 불가능** 체크박스를 표시하며 직렬화 바인딩을 사용해 Undo와 씬 저장을 지원한다.

설정 위치: `Assets/Scenes/Guild.unity` → **Contract table** → **GuildContractTable** → **Offers** → 해당 의뢰 → **재수락 불가능**.

현재 스테이지 1 「푸른 저택의 인장」은 true로 저장했다. 나머지 준비 중 문서는 false이며 새 의뢰의 기본값도 false다. 옵션이 켜져 있어도 완료 전 최초 수락과 실패 후 재시도는 가능하다. 완료 이후에만 재수락을 막는다.

## 처리

- 의뢰의 콘텐츠 제공 여부 IsAvailable과 현재 수락 가능 여부 CanAccept를 분리했다. 완료한 재수락 불가 의뢰가 준비 중 문서로 오인되지 않는다.
- 완료 기록과 nonRepeatable이 함께 참일 때 재수락을 차단한다. 테이블 F/Enter, AcceptCurrent, 길드의 직접 Accept/Depart 호출이 같은 정책을 따른다.
- 완료한 의뢰 문서에는 **의뢰 완료 / 재수락 불가능**을 표시하고 수락 키 안내를 재수락 불가능 안내로 바꾼다. 목표·완료 이력은 계속 열람할 수 있다.
- 옵션을 끈 완료 의뢰는 다시 수락할 수 있다. 현재 수락 후에는 기존처럼 출구 F로 출발한다.
- 완료 기록의 저장 범위는 기존 현재 플레이 세션이다. 디스크 저장 기능은 이번 변경에 추가하지 않았다.

## 검증

- Unity 6000.3.19f1 컴파일 성공.
- 의뢰 테이블 PlayMode **11/11 통과**: 저장된 스테이지 1 옵션, 최초 수락→목표물 회수·탈출→귀환, 완료 문서, F·Enter와 직접 호출의 재수락 차단, 옵션 해제 후 재수락 허용.
- 길드 PlayMode **22/22 통과**: 최초 출발, 실패 재시작, 연습·초기화·주입·입력 회귀. 관련 검사 합계 **33/33 통과**.
- 실제 인스펙터 컨트롤 생성 결과의 라벨은 **재수락 불가능**, 스테이지 1 값은 true, 새 의뢰 기본값은 false임을 확인했다.
- 길드 씬 저장 시 기존 새 런타임 필드의 기본값도 Unity가 함께 직렬화했다. 저택 씬과 소리·AI 로직은 변경하지 않았다.
- 스테이징·커밋·푸시는 하지 않았다.

근거: `Artifacts/ContractRepeatPolicy/`의 `table-tests.json`, `guild-tests.json`, `inspector-configuration.json`, `inspector-label-verification.json`. `before/`는 이번 변경 직전 파일이다.
