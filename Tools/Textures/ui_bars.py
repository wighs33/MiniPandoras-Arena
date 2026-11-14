"""
대전 HUD의 그림 다섯 장을 코드로 그린다(도트가 아니라 가장자리가 부드러운 그림).

    python Tools/Textures/ui_bars.py

막대 그림은 128×64의 알약 모양이고, 유니티에서 9-slice(가장자리 32px)로 늘린다. 4배로 크게 그린 뒤 줄여서 가장자리를 부드럽게 한다.
- bar_round.png: 흰 알약. Image 색으로 물들여 채움·자국에 쓴다.
- bar_trough.png: 막대 홈. 짙은 밤색 바탕에 옅은 달빛 테(색이 그림에 들어 있다).
- bar_shine.png: 채움 위에 겹치는 윗면 빛. 위가 밝고 가운데로 갈수록 사라진다.
- hud_shade.png: 화면 위쪽에 까는 어둠 띠(64×256, 9-slice 없이 늘린다).
- banner_band.png: 화면 가운데 알림(준비·승리·패배) 뒤에 까는 어둠 띠(512×128). 가운데가 짙고 네 가장자리로 사라진다.
결과는 Assets/MiniPandoras/Art/UI.
"""

import os

from PIL import Image, ImageDraw

WIDTH, HEIGHT = 128, 64
SCALE = 4
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "UI")

# 2D판 UI 지침의 색: 밤 #081421, 달빛 #f3e9d8.
NIGHT = (8, 20, 33)
MOONLIGHT = (243, 233, 216)


def pill_mask(inset=0):
    """알약 모양의 흰 마스크(크게 그린 크기). inset만큼(작은 그림의 px) 안으로 들인다."""
    w, h = WIDTH * SCALE, HEIGHT * SCALE
    pad = inset * SCALE
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle((pad, pad, w - 1 - pad, h - 1 - pad), radius=(h - 2 * pad) // 2, fill=255)
    return mask


def save(image, name):
    image.resize((WIDTH, HEIGHT), Image.LANCZOS).save(os.path.join(OUT, name))


def round_bar():
    image = Image.new("RGBA", (WIDTH * SCALE, HEIGHT * SCALE), (255, 255, 255, 0))
    image.putalpha(pill_mask())
    save(image, "bar_round.png")


def trough():
    rim = Image.new("RGBA", (WIDTH * SCALE, HEIGHT * SCALE), MOONLIGHT + (0,))
    rim.putalpha(pill_mask().point(lambda a: a * 90 // 255))
    inside = Image.new("RGBA", rim.size, NIGHT + (0,))
    inside.putalpha(pill_mask(inset=3).point(lambda a: a * 205 // 255))
    save(Image.alpha_composite(rim, inside), "bar_trough.png")


def shine():
    w, h = WIDTH * SCALE, HEIGHT * SCALE
    fade = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(fade)
    for y in range(h // 2):
        draw.line((0, y, w, y), fill=int(150 * (1 - y / (h / 2)) ** 1.5))
    alpha = Image.new("L", (w, h), 0)
    alpha.paste(fade, mask=pill_mask(inset=4))
    image = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    image.putalpha(alpha)
    save(image, "bar_shine.png")


def top_shade():
    """화면 위쪽에 까는 어둠 띠(64×256). 위가 짙고 아래로 사라져 밝은 투기장 위에서도 HUD 글자가 읽힌다."""
    w, h = 64, 256
    alpha = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(alpha)
    for y in range(h):
        draw.line((0, y, w, y), fill=int(170 * (1 - y / h) ** 1.6))
    image = Image.new("RGBA", (w, h), NIGHT + (0,))
    image.putalpha(alpha)
    image.save(os.path.join(OUT, "hud_shade.png"))


def banner_band():
    w, h = 512, 128
    alpha = Image.new("L", (w, h), 0)
    pixels = alpha.load()
    for y in range(h):
        # 위아래는 가운데에서 멀수록, 좌우는 가장자리 30% 안에서 사라진다.
        vertical = max(0.0, 1 - abs(y - h / 2) / (h / 2)) ** 0.8
        for x in range(w):
            edge = min(x, w - 1 - x) / (w * 0.3)
            pixels[x, y] = int(175 * vertical * min(1.0, edge))
    image = Image.new("RGBA", (w, h), NIGHT + (0,))
    image.putalpha(alpha)
    image.save(os.path.join(OUT, "banner_band.png"))


if __name__ == "__main__":
    round_bar()
    trough()
    shine()
    top_shade()
    banner_band()
