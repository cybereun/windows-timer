# 윈도우 타이머 · UI 및 배포

## 제품명
- 한국어: 윈도우 타이머
- 실행 파일 / GitHub 저장소: WindowsTimer / windows-timer

## 화면 방향
3D 이머시브 참고 이미지에서 가져온 다크 브라운 레일, 크림 세라믹 패널, 테라코타 강조색, 촉감 있는 시계 오브젝트를 적용한다. 큰 한글 헤딩과 충분한 간격을 사용하며 예약 동작은 기존 동작을 유지한다.

## 기능
- Windows 종료 또는 재부팅, 몇 분 뒤 또는 날짜·시간 지정
- 빠른 시간 버튼, 남은 시간, 취소, 10분 연장
- 실행 60초 전 알림, 절전 복귀 후 60초 재안내
- 트레이 유지, 앱 종료 전 예약 확인, 강제 종료하지 않는 Windows 요청
- Noto Sans KR과 조형 이미지를 실행 파일에 포함

## 배포
- .NET Framework 4.x WinForms standalone EXE
- GitHub 저장소 `cybereun/windows-timer`, private visibility
- `v1.0.1` release: EXE, ZIP, SHA-256 manifest; selected button colors repaint immediately after clicks
