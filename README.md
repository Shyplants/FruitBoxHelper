# FruitBoxHelper

A WPF toy project that detects a "fruit box" region on-screen (via Emgu CV), recognizes digits inside apples (using Tesseract OCR), and simulates mouse interactions for demonstration.  
This project is released under **GPL v3** due to Emgu CV (GPL) dependencies.

---

## 1. 개요

- **언어 / 프레임워크**: C#, WPF (.NET 6 이상)
- **주요 라이브러리**:  
  - Emgu CV (OpenCV .NET Wrapper, GPL 버전)  
  - Tesseract (Apache 2.0)  
- **주요 기능**:  
  - 화면 스크린샷에서 녹색 사각형(“fruit box”) 영역 인식  
  - 사과 내부 숫자를 OCR(“Tesseract”)으로 파악  
  - Win32 API를 통해 마우스 자동 이동/클릭/드래그  
  - 투명한 WPF OverlayWindow로 시각 표시 (Debug 모드에서 동작)

![Playing](Demo/play.gif)

----------

## 2. 설치 및 빌드

1. **요구 사항**  
   - .NET 6 이상 (현재 프로젝트는 net8.0-windows 타겟을 사용 중)
   - Visual Studio 2022 등 최신 IDE

2. **NuGet 패키지**  
   - `Emgu.CV.Bitmap`, `Emgu.CV.runtime.windows` (Emgu CV)  
   - `Tesseract`, `Tesseract.Drawing`

3. **tessdata 폴더 및 .traineddata 파일**  
   - Tesseract OCR이 동작하려면, 실행 경로(예: `bin\Debug\net8.0-windows`)에 **tessdata** 폴더가 존재해야 합니다.
   - tessdata 폴더에 `eng.traineddata` 파일이 존재해야 합니다.

4. **빌드 / 실행**  
   - 저장소 **Clone** 후, `FruitBoxHelper.sln` 열기  
   - NuGet 패키지 복원 → 빌드(F5)  
   - 학습용 토이 프로젝트이므로, 실제 환경에서 동작 보장은 없음

----------

## 3. 주요 기능

- **보드 감지**  
  - 스크린샷을 분석, 특정 녹색 테두리(가로/세로 비율 1.53~1.55)의 사각형 검출 (Emgu CV)  
  - **주의**: Fruit box 경계 내에 다른 애플리케이션의 창이 있으면 올바른 감지가 어렵고, 경계 밖에 녹색 픽셀이 존재하면 오인식의 원인이 될 수 있습니다.
  
- **사과 숫자 인식**  
  - Tesseract로 각 사과 안의 숫자를 인식
- **마우스 자동화**  
  - Win32 API로 커서 이동/클릭/드래그를 시뮬레이션 (별도 스레드에서 진행)
- **Overlay**  
  - fruit box, 각 사과 클릭 포인트 등을 시각 표시 (Debug 모드에서 동작)
