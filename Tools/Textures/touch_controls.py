"""
화면 조작(이동 패드, 공격 버튼)의 그림을 코드로 그린다. 도트가 아니라 가장자리가 부드러운 그림이다(2026-11-13 사용자 결정).

    python Tools/Textures/touch_controls.py

4배로 크게 그린 뒤 줄여서 가장자리를 부드럽게 한다. 색은 2D판 UI 지침의 「밤하늘」「달빛」이다. 결과는 Assets/MiniPandoras/Art/UI.
- pad_base.png: 이동 패드 바탕. 어두운 원판에 달빛 테, 다이얼 선, 네 방향 꺾쇠.
- pad_knob.png: 이동 패드 손잡이. 위가 밝은 달빛 돌단추와 아래 그림자.
- disc_large.png, ring_large.png: 공격 버튼의 흰 원판과 흰 고리. 장면에서 Image 색으로 물들인다.
공격 버튼의 방망이 아이콘은 attack_icon.py가 그린다.
"""

import os

from PIL import Image, ImageDraw, ImageFilter

SCALE = 4
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "UI")

NIGHT_SKY = (23, 42, 73)
NIGHT_SKY_LIGHT = (37, 53, 85)
MOONLIGHT = (243, 233, 216)
MOONLIGHT_TOP = (255, 250, 240)
MOONLIGHT_BOTTOM = (214, 201, 180)
KNOB_RIM = (176, 160, 136)


def circle(draw, center, radius, **style):
    x, y = center
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), **style)


def disc_mask(size, radius):
    mask = Image.new("L", (size, size), 0)
    circle(ImageDraw.Draw(mask), (size / 2, size / 2), radius, fill=255)
    return mask


def vertical_gradient(size, top, bottom, alpha=255):
    image = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(image)
    for y in range(size):
        t = y / (size - 1)
        color = tuple(round(a + (b - a) * t) for a, b in zip(top, bottom))
        draw.line((0, y, size, y), fill=color + (alpha,))
    return image


def save(image, size, name):
    image.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, name))


def pad_base(size=384):
    big = size * SCALE
    center = (big / 2, big / 2)
    radius = big * 0.49
    image = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    # 손을 떼면 MovePadView가 패드 전체를 옅게 하므로 바탕은 불투명하게 칠한다.
    # 선형 색공간에서는 반투명이 계산보다 옅게 보여서, 밝은 모래 위에서 떠 보인다.
    body = vertical_gradient(big, NIGHT_SKY_LIGHT, NIGHT_SKY)
    image.paste(body, mask=disc_mask(big, radius))

    draw = ImageDraw.Draw(image)
    rim = big * 0.018
    circle(draw, center, radius - rim / 2, outline=MOONLIGHT + (150,), width=round(rim))
    circle(draw, center, radius * 0.62, outline=MOONLIGHT + (40,), width=round(big * 0.006))

    # 네 방향 꺾쇠: 끝이 둥근 두 획.
    arm = big * 0.05
    stroke = round(big * 0.016)
    color = MOONLIGHT + (190,)
    for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        tip = (center[0] + dx * radius * 0.8, center[1] + dy * radius * 0.8)
        back = (tip[0] - dx * arm, tip[1] - dy * arm)
        ends = ((back[0] - dy * arm, back[1] + dx * arm), (back[0] + dy * arm, back[1] - dx * arm))
        for end in ends:
            draw.line((end, tip), fill=color, width=stroke)
            circle(draw, end, stroke / 2, fill=color)
        circle(draw, tip, stroke / 2, fill=color)
    save(image, size, "pad_base.png")


def pad_knob(size=192):
    big = size * SCALE
    radius = big * 0.42
    image = Image.new("RGBA", (big, big), (0, 0, 0, 0))

    shadow = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    circle(ImageDraw.Draw(shadow), (big / 2, big / 2 + big * 0.035), radius, fill=(0, 0, 0, 110))
    image.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(big * 0.03)))

    rim = Image.new("RGBA", (big, big), KNOB_RIM + (255,))
    image.paste(rim, mask=disc_mask(big, radius))
    face = vertical_gradient(big, MOONLIGHT_TOP, MOONLIGHT_BOTTOM)
    image.paste(face, mask=disc_mask(big, radius - big * 0.025))

    # 손가락이 닿는 오목한 자리: 아래가 밝고 위가 어두운 얕은 원.
    dimple = vertical_gradient(big, MOONLIGHT_BOTTOM, MOONLIGHT_TOP)
    image.paste(dimple, mask=disc_mask(big, radius * 0.5))
    circle(ImageDraw.Draw(image), (big / 2, big / 2), radius * 0.5, outline=KNOB_RIM + (120,), width=round(big * 0.008))
    save(image, size, "pad_knob.png")


def white_disc(size=320):
    big = size * SCALE
    image = Image.new("RGBA", (big, big), (255, 255, 255, 0))
    image.putalpha(disc_mask(big, big / 2 - SCALE))
    save(image, size, "disc_large.png")


def white_ring(size=320):
    big = size * SCALE
    image = Image.new("RGBA", (big, big), (255, 255, 255, 0))
    width = big * 0.028
    mask = Image.new("L", (big, big), 0)
    circle(ImageDraw.Draw(mask), (big / 2, big / 2), big / 2 - SCALE - width / 2, outline=255, width=round(width))
    image.putalpha(mask)
    save(image, size, "ring_large.png")


if __name__ == "__main__":
    pad_base()
    pad_knob()
    white_disc()
    white_ring()
