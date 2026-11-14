"""
인물 발밑에 까는 고리 텍스처를 코드로 그린다(누가 나이고 누가 상대인지 알아보는 표시).

    python Tools/Textures/foot_ring.py

흰 고리 하나와 바깥으로 번지는 옅은 빛, 안쪽의 아주 옅은 바닥이다. 색은 재질(FootRingPlayer, FootRingRival)이 칠한다.
4배로 크게 그린 뒤 줄여서 가장자리를 부드럽게 한다. 결과는 Assets/MiniPandoras/Art/Textures/FootRing.png.
"""

import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SIZE = 256
SCALE = 4
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "Textures", "FootRing.png")


def circle_layer(big, radius, alpha, width=None):
    """크게 그린 크기의 L 마스크에 원(width가 없으면 채운 원)을 그린다."""
    layer = Image.new("L", (big, big), 0)
    box = (big / 2 - radius, big / 2 - radius, big / 2 + radius, big / 2 + radius)
    if width is None:
        ImageDraw.Draw(layer).ellipse(box, fill=alpha)
    else:
        ImageDraw.Draw(layer).ellipse(box, outline=alpha, width=round(width))
    return np.array(layer)


def main():
    big = SIZE * SCALE
    radius = big * 0.4
    glow = Image.fromarray(circle_layer(big, radius, 130, width=big * 0.05)).filter(ImageFilter.GaussianBlur(big * 0.035))
    floor = circle_layer(big, radius, 40)
    line = circle_layer(big, radius, 255, width=big * 0.028)
    alpha = np.maximum.reduce([np.array(glow), floor, line])

    image = Image.new("RGBA", (big, big), (255, 255, 255, 0))
    image.putalpha(Image.fromarray(alpha))
    image.resize((SIZE, SIZE), Image.LANCZOS).save(OUT)
    print("wrote", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
