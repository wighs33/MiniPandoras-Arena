# 외부 에셋과 출처

| 에셋 | 위치 | 라이선스 | 출처 | 쓰는 곳 |
|---|---|---|---|---|
| Quaternius Universal Animation Library 1 (UAL1_Standard) | `Assets/ThirdParty/Quaternius/UniversalAnimationLibrary` | CC0 1.0 | https://quaternius.com (옛 3D판 MiniPandoras의 `Assets/ThirdParty`에서 복사, 2026-11-13) | 대기(Sword_Idle), 걷기(Walk_Loop), 달리기(Jog_Fwd_Loop) |
| Quaternius Universal Animation Library 2 (UAL2_Standard) | `Assets/ThirdParty/Quaternius/UniversalAnimationLibrary2` | CC0 1.0 | 같음 | 3연격(Sword_Regular_A·B·C)과 마무리 동작(_Rec) |
| MCP for Unity | `Packages/manifest.json` (git) | MIT | https://github.com/CoplayDev/unity-mcp | 에디터 자동화(게임 빌드에는 들어가지 않는다) |
| Pretendard (Bold, Regular) | `Assets/ThirdParty/Pretendard` | SIL Open Font License 1.1 (`LICENSE.txt`) | https://github.com/orioncactus/pretendard (2D판 MiniPandoras-2D의 `Assets/ThirdParty`에서 복사, 2026-11-13) | 대전 HUD의 이름·체력 숫자·피해 숫자(`Art/Fonts`의 SDF) |
| TextMesh Pro 기본 리소스 | `Assets/TextMesh Pro` | Unity Companion License | com.unity.ugui 2.0이 들여온다(2D판과 같은 자리) | TMP 셰이더와 설정 |

- 모션은 루트 모션이 없는 Standard 판이다. 휴머노이드 아바타로 리타깃해 큰 머리 인물에 입힌다(`Docs/ARCHITECTURE.md`).
- `Sword_Idle`은 원본이 반복 클립이 아니어서 `.meta`의 클립 설정에서 반복(loopTime)을 켰다. 원본 FBX는 고치지 않았다.

## 인물 Luna (사용자가 만든 아바타)

| 부분 | 만든 법 |
|---|---|
| 몸·머리카락 | 사용자가 Tripo로 생성 |
| 뼈대 | Character Creator 기본 뼈대(`CC_Base_*`, AccuRig) |
| 머리 장식, 드레스 세 벌, 표정 키, 머리카락 사슬 뼈, 텍스처 정리 | 사용자 요청으로 Claude가 Blender에서 다듬었다(2026-11-09, `Documents/Untitled_refined.blend`) |

- 이 프로젝트에는 `Tools/Blender/export_luna.py`가 내보낸 `Art/Models/Luna.fbx`와 `Art/Textures/Luna`만 들어 있다.
- 공개(저장소·빌드 배포)하기 전에 Tripo와 AccuRig의 이용 약관을 사용자가 확인한다.
- 2026-11-14에 찾아본 것(itch.io 공개 전, 약관 원문이 아니라 도움말·포럼이라 사용자가 원문으로 확인한다):
  - Tripo: 생성할 때의 요금제에 따라 다르다. 무료 요금제로 만든 모델은 공개 모델(CC BY 4.0)로 분류되고, 도움말은 무료 사용자의 결과물 권리를 Tripo가 가진다고 하며(약관 5.2.1) 상업적 이용은 유료 요금제에서만 된다고 한다. 유료 요금제로 만든 모델은 상업적 이용까지 된다. [도움말](https://www.tripo3d.ai/help/privacy-policy/can-i-use-models-commercially)
  - AccuRig: 앱은 무료다. Reallusion 콘텐츠를 하나도 쓰지 않은 자기 인물을 AccuRig로 뼈대만 입혀 게임에 쓰는 것은 된다는 Reallusion 커뮤니티 답변이 있다(공식 정책 문서는 아니다). Luna는 Reallusion 콘텐츠를 쓰지 않는다(뼈 이름만 `CC_Base_*`). [포럼](https://forum.reallusion.com/541812/Forum20.aspx)

## 직접 만든 것

| 그림 | 만든 법 |
|---|---|
| 투기장, 방망이, 악마의 가시 | `Tools/Blender`의 Python 스크립트(Claude가 썼다) |
| 모래 텍스처, 공격 아이콘 | `Tools/Textures`의 Python 스크립트(numpy, Pillow) |
| 이동 패드·버튼 원판·고리 그림 (`Art/UI`의 pad_base, pad_knob, disc_large, ring_large) | `Tools/Textures/touch_controls.py`(Pillow, Claude가 썼다). 2026-11-13에 2D판의 도트 그림을 매끈한 그림으로 바꿨다 |
| 체력 막대, HUD 어둠 띠, 알림 띠 (`Art/UI`의 bar_round, bar_trough, bar_shine, hud_shade, banner_band) | `Tools/Textures/ui_bars.py`(Pillow, Claude가 썼다) |
| 발밑 고리 (`Art/Textures/FootRing.png`, 불꽃 고리의 바닥 빛으로도 쓴다) | `Tools/Textures/foot_ring.py`(Pillow, numpy, Claude가 썼다) |
| 장판 바닥 빛 (`Art/Textures/GlowDisc.png`, 지금은 쓰지 않는다) | `Tools/Textures/glow_disc.py`(Pillow, numpy, Claude가 썼다) |
| 스킬 효과 그림 (`Art/Textures/Fx`: 노이즈, 불꽃 혀, 줄, 원, 고리, 벽) | `Tools/Textures/fx_textures.py`(Pillow, numpy, Claude가 썼다) |
| 스킬 효과 원기둥 (`Art/Models/FxCylinder.fbx`) | `Tools/Blender/build_fx_cylinder.py`(Claude가 썼다) |
| 툰 효과 셰이더 (`Shaders/ToonFx.shader`) | Claude가 썼다 |
| 화면 조작 그림과 공격 아이콘 | `Tools/Textures/touch_controls.py`, `attack_icon.py`(Pillow, Claude가 썼다) |
