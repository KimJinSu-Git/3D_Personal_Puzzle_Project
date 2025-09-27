# 🎮 Unity Inside 스타일 3D 퍼즐 플랫포머
🛠 개발 도구: Unity(C#), JetBrains Rider <br/>
📆 개발 기간: 25.06.09 ~ 25.06.22 (약 2주) <br/>
___
`Inside` 스타일의 3D 퍼즐 플랫포머를 목표로, **플레이어 상태 시스템, 수영·레버·사다리 상호작용, 적 AI 감지** 등의 퍼즐 메카닉을 구현했습니다. <br/>
몰입감 있는 연출과 퍼즐 상호작용의 흐름을 설계하는 데 집중하고자 하였습니다. <br/>
___
🔑 주요 구현 요소
* **플레이어 FSM**
  * Idle, Move, Crouch, Crawl, Jump, Push, Swim, Ladder, Death 등 세분화된 상태 구현
  * 플레이어 상태에 따라 Collider 크기 변경 👉 [PlayerController.cs](https://github.com/KimJinSu-Git/3D_Personal_Puzzle_Project/blob/main/Assets/Scripts/Player/PlayerController.cs#L256)
* **수영 시스템**
  * 수면/수중 상태 분리
  * 수중에 오래 잠수해 있을 경우 익사 처리
* **환경 상호작용**
  * Push 오브젝트 밀기, Lever 조작, 사다리 오르내리기
* **적 AI & 감지**
  * Spotlight 감지 시스템
  * 플레이어 상태(crouch/crawl/swim)에 따른 감지 여부 판별 👉 [SpotlightDetector.cs](https://github.com/KimJinSu-Git/3D_Personal_Puzzle_Project/blob/main/Assets/Scripts/Enemy/SpotlightDetector.cs#L39)
  * 감지 시 추격 or 공격 패턴 실행
* **사망 처리**
  * 사망 후 일정 시간 경과 시 GameResetEvent 호출
  * 기존의 Object들의 위치를 초기화시킨 후 리스폰 처리 👉 [GameResetEvent.cs](https://github.com/KimJinSu-Git/3D_Personal_Puzzle_Project/blob/main/Assets/Scripts/Manager/GameResetEvent.cs)
* **최종 연출**
  * 카메라 Follow → 줌아웃 → 페이드아웃 연출
  * "Demo Game Clear" UI 출력 후 타이틀로 전환
___
* **영상 바로가기** [Platformer.avi](https://drive.google.com/file/d/1V0_0wqTcIK181UsXvU2HkkI6AcapKUpX/view?usp=drive_link)
* **문서 바로가기** [Platformer.pdf](https://drive.google.com/file/d/1MhRaoZhiD35o1aBXtuJAhrldofdi4Vf6/view?usp=drive_link)
