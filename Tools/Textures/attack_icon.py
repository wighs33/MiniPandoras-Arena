"""
공격 버튼 아이콘(방망이)을 코드로 그린다. 도트가 아니라 가장자리가 부드러운 그림이다(2026-11-13 사용자 결정).

    python Tools/Textures/attack_icon.py

왼쪽 아래 손잡이에서 오른쪽 위로 굵어지는 방망이를, 굵기가 다른 원을 축을 따라 촘촘히 찍어 그린다.
4배로 크게 그린 뒤 줄여서 가장자리를 부드럽게 한다. 색은 2D판 UI 지침의 「달빛」과 「밤」이다.
결과는 Assets/MiniPandoras/Art/UI/icon_attack.png.
"""

import os

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "UI", "icon_attack.png")
SIZE = 192
SCALE = 4
MOONLIGHT = (243, 233, 216, 255)
SHADE = (196, 182, 160, 255)
NIGHT = (8, 20, 33, 255)

HANDLE_END = (0.2, 0.8)
BARREL_END = (0.8, 0.2)


def thickness(t):
    """축의 t(0 손잡이 끝 ~ 1 몸통 끝)에서의 반지름(그림 폭에 대한 비율)."""
    if t < 0.05:
        return 0.06
    if t < 0.42:
        return 0.03
    if t < 0.78:
        return 0.03 + (t - 0.42) / 0.36 * 0.05
    return 0.08


def stamp(draw, grow, color, shift=(0.0, 0.0)):
    big = SIZE * SCALE
    steps = 400
    for i in range(steps + 1):
        t = i / steps
        x = (HANDLE_END[0] + (BARREL_END[0] - HANDLE_END[0]) * t + shift[0]) * big
        y = (HANDLE_END[1] + (BARREL_END[1] - HANDLE_END[1]) * t + shift[1]) * big
        r = (thickness(t) + grow) * big
        draw.ellipse((x - r, y - r, x + r, y + r), fill=color)


def main():
    big = SIZE * SCALE
    image = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    stamp(draw, 0.022, NIGHT)
    stamp(draw, 0.0, SHADE)
    # 왼쪽 위로 조금 비켜 밝은 면을 얹어 아래쪽에 그늘이 남게 한다.
    stamp(draw, -0.012, MOONLIGHT, shift=(-0.008, -0.008))
    image.resize((SIZE, SIZE), Image.LANCZOS).save(OUT)
    print("wrote", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
