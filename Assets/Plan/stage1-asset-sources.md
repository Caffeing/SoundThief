# 스테이지 1 사용 에셋 기록

2026-10-29 · `Assets/Scenes/ManorPrototype.unity` · 브랜치 `taehyeon`

사용자 이미지의 청회색 석벽·어두운 목재·호박색 촛불·붉은 직물을 기준으로 구매한 Creator Bundle 및 아래 CC0 모델을 조합했다. 라이선스 원본은 각 ThirdParty 폴더에 보존한다.

| 팩 | 프로젝트 경로 | 적용 |
| --- | --- | --- |
| EmaceArt Slavica | `Assets/EmaceArt/Slavic World` | 문·가구·식기·석벽 모듈·랜턴, 서재 수납장/사다리/책, 벤치·쿠션·보관함, 경비 인물과 대기·걷기 |
| EmaceArt Harboria | `Assets/EmaceArt/Harboria` | 홀 기둥과 네 종류 책 묶음 |
| RG Poly Cute Dungeon | `Assets/Dungeon Megapack` | 석재 바닥·목재 천장과 보·아치·문틀·사슬·열쇠·식기·의자·방패, 동전과 잔을 조립한 황동 인장 |
| [Quaternius Animated Knight](https://quaternius.com/packs/knightcharacter.html) · CC0 | `Assets/ThirdParty/Quaternius/Knight` | 장식 갑옷 본체·닫힌 투구, Slavica 경비에 조합한 열린 투구와 견갑 |
| [Grab3D Grandfather Clock](https://grab3d.com/models/furniture/grandfather-clock/) · CC0 | `Assets/ThirdParty/Grab3D/GrandfatherClock` | 서재 태엽 시계, 원본 FBX·텍스처·라이선스·출처 manifest |
| [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds) · CC0 | `Assets/ThirdParty/Kenney/ImpactSounds` | 문·시계 틱·금속 물건·경비 발소리. 원본 효과음과 License.txt 보존 |
| [Kenney Furniture Kit](https://kenney.nl/assets/furniture-kit) · CC0 | `Assets/ThirdParty/Kenney/Furniture` | 테두리 있는 러그와 홀의 긴 러너 |

원본 패키지 전체 대신 필요한 모델과 연결 재질·텍스처를 선택해 가져왔다. 생성한 재질·결합 메시·이동망은 `Assets/SoundThief/Generated`에 둔다. 구입한 파일의 배포 범위는 원본 이용 조건을 따른다.

## 배치와 기능

보이는 기본 도형 81개를 에셋 외형으로 교체했다. 충돌용 도형과 경비 상태 글자·능력 피드백은 환경 외형과 구분한다. 새 수납장의 실제 형태에 맞춘 충돌과 이동망을 적용했다.

서재 곡선 책장 8개를 곡선형 2개와 직사각 수납장·낮은 장·좁은 선반·개방 선반으로 바꾸고 실제 선반 높이에 책을 배치했다. 현관 대기 공간·홀 휴식 공간·서재 보관 공간·식당 배식대를 생활 소품 묶음으로 채웠다. 통로와 열쇠·인장·문 앞은 비워 둔다. 이전 배치와 모델은 재편집할 수 있도록 비활성 상태로 보존했다.

시계는 실제 목재·금속 텍스처를 적용했고, 장식 갑옷은 포즈를 구운 고정 모델이다. 경비는 얼굴이 보이는 인물에 버건디/린넨 복장과 투구·견갑을 조합했다. 출구는 목재 문, 인장 받침대는 실제 식탁 모델을 사용한다.

배치 근거·참고 이미지·검증은 `stage1-environment-art.md`를 따른다. 가구·이동망 변경 후 전체 PlayMode 27/27 통과. 이후 잠입 개선에서 벽·문 청각 감쇠와 순간음/지속음 구분, 두 유인 위치와 관찰용 책장을 구현했다. 최신 결과는 stage1-core-loop-rework.md를 따른다. 투척은 계속 비활성이다.

Grab3D 원본 고지에는 Blender 스크립트 작성에 AI 보조를 사용했고 생성형 AI가 메시·텍스처를 직접 생성한 것은 아니라고 적혀 있다. 해당 `LICENSE.txt`와 manifest를 함께 보존한다.
