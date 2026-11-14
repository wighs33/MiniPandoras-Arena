"""
창(프로필 등)의 그림을 코드로 그린다. 도트가 아니라 가장자리가 부드러운 그림이다(2026-11-13 사용자 결정).

    python Tools/Textures/ui_panels.py

4배로 크게 그린 뒤 줄여서 가장자리를 부드럽게 한다. 색은 2D판 UI 지침의 「밤하늘」「밤하늘 밝은」「달빛」이다.
결과는 Assets/MiniPandoras/Art/UI.
- panel.png: 창 바탕(128×128, 9-slice 40px). 밤하늘색 둥근 판에 옅은 달빛 테, 위가 조금 밝다.
- card.png: 창 안의 칸(128×128, 9-slice 32px). 밤하늘 밝은 색 둥근 판에 아주 옅은 테.
- card_focus.png: 고른 칸에 겹치는 흰 테(card와 같은 둥글기). 장면에서 등불색으로 물들인다.
- gem.png: 판도라 보석(128×128). 투기장 바닥 문장과 같은 마름모를 흰색 네 면으로 그려, 판도라 색으로 물들이면 면마다 밝기가 다르다.
- icon_close.png: 닫기(64×64). 끝이 둥근 흰 X.
"""

import os

from PIL import Image, ImageDraw, ImageFilter

SCALE = 4
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "UI")

NIGHT_SKY = (23, 42, 73)
NIGHT_SKY_LIGHT = (37, 53, 85)
MOONLIGHT = (243, 233, 216)


def rounded_mask(size, radius, inset=0):
    """둥근 네모 마스크(크게 그린 크기). size·radius·inset은 작은 그림의 px이다."""
    big = size * SCALE
    pad = inset * SCALE
    mask = Image.new("L", (big, big), 0)
    ImageDraw.Draw(mask).rounded_rectangle((pad, pad, big - 1 - pad, big - 1 - pad), radius=(radius - inset) * SCALE, fill=255)
    return mask


def scaled(mask, factor):
    return mask.point(lambda a: a * factor // 255)


def save(image, size, name):
    image.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, name))


def vertical_shade(size, color, top_lift, alpha):
    """위가 top_lift만큼 밝고 아래로 갈수록 원래 색이 되는 판."""
    big = size * SCALE
    image = Image.new("RGBA", (big, big))
    draw = ImageDraw.Draw(image)
    for y in range(big):
        lift = top_lift * max(0.0, 1 - y / (big * 0.5))
        rgb = tuple(int(c + (255 - c) * lift) for c in color)
        draw.line((0, y, big, y), fill=rgb + (alpha,))
    return image


def panel():
    size, radius = 128, 28
    rim = Image.new("RGBA", (size * SCALE, size * SCALE), MOONLIGHT + (0,))
    rim.putalpha(scaled(rounded_mask(size, radius), 80))
    body = vertical_shade(size, NIGHT_SKY, 0.06, 0)
    body.putalpha(scaled(rounded_mask(size, radius, inset=2), 240))
    save(Image.alpha_composite(rim, body), size, "panel.png")


def card():
    size, radius = 128, 20
    rim = Image.new("RGBA", (size * SCALE, size * SCALE), MOONLIGHT + (0,))
    rim.putalpha(scaled(rounded_mask(size, radius), 45))
    body = vertical_shade(size, NIGHT_SKY_LIGHT, 0.05, 0)
    body.putalpha(rounded_mask(size, radius, inset=2))
    save(Image.alpha_composite(rim, body), size, "card.png")


def card_focus():
    size, radius, width = 128, 20, 5
    ring = Image.new("L", (size * SCALE, size * SCALE), 0)
    ring.paste(rounded_mask(size, radius), mask=rounded_mask(size, radius))
    ring.paste(0, mask=rounded_mask(size, radius, inset=width))
    image = Image.new("RGBA", ring.size, (255, 255, 255, 0))
    image.putalpha(ring)
    save(image, size, "card_focus.png")


def gem():
    """마름모 하나를 가운데에서 네 면으로 나눠 왼쪽 위가 가장 밝고 오른쪽 아래가 가장 어둡게 칠한다(빛은 왼쪽 위에서)."""
    size = 128
    big = size * SCALE
    c = big / 2
    half_w, half_h = big * 0.34, big * 0.46
    top, right, bottom, left = (c, c - half_h), (c + half_w, c), (c, c + half_h), (c - half_w, c)
    # 가운데 꼭짓점을 조금 왼쪽 위로 두어 앞으로 튀어나온 면처럼 보이게 한다.
    middle = (c - big * 0.03, c - big * 0.05)
    image = Image.new("RGBA", (big, big), (255, 255, 255, 0))
    draw = ImageDraw.Draw(image)
    draw.polygon((top, middle, left), fill=(255, 255, 255, 255))
    draw.polygon((top, right, middle), fill=(214, 214, 214, 255))
    draw.polygon((left, middle, bottom), fill=(178, 178, 178, 255))
    draw.polygon((middle, right, bottom), fill=(132, 132, 132, 255))

    shadow = Image.new("RGBA", (big, big), (8, 20, 33, 0))
    shadow_mask = Image.new("L", (big, big), 0)
    offset = big * 0.025
    ImageDraw.Draw(shadow_mask).polygon(
        [(x + offset, y + offset * 2) for x, y in (top, right, bottom, left)], fill=150)
    shadow.putalpha(shadow_mask.filter(ImageFilter.GaussianBlur(big * 0.02)))
    save(Image.alpha_composite(shadow, image), size, "gem.png")


def close_icon():
    size, stroke = 64, 7
    big = size * SCALE
    image = Image.new("RGBA", (big, big), (255, 255, 255, 0))
    draw = ImageDraw.Draw(image)
    a, b = big * 0.24, big * 0.76
    width = stroke * SCALE
    for start, end in (((a, a), (b, b)), ((a, b), (b, a))):
        draw.line((start, end), fill=(255, 255, 255, 255), width=width)
        for x, y in (start, end):
            draw.ellipse((x - width / 2, y - width / 2, x + width / 2, y + width / 2), fill=(255, 255, 255, 255))
    save(image, size, "icon_close.png")


if __name__ == "__main__":
    panel()
    card()
    card_focus()
    gem()
    close_icon()
