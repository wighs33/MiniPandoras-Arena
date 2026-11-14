"""
장판 바닥 빛 텍스처(흰 원)를 코드로 그린다. 가운데는 옅고 가장자리 가까이에서 밝다가 바깥으로 부드럽게 사라진다.

    python Tools/Textures/glow_disc.py

색은 재질(FireGlow)이 칠한다. 원의 가장자리는 한 변의 0.45 자리다(SkillZone의 glowEdge).
결과는 Assets/MiniPandoras/Art/Textures/GlowDisc.png.
"""

import os

import numpy as np
from PIL import Image

SIZE = 256
EDGE = 0.45
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "Textures", "GlowDisc.png")


def main():
    coords = (np.arange(SIZE) + 0.5) / SIZE - 0.5
    x, y = np.meshgrid(coords, coords)
    r = np.sqrt(x * x + y * y) / EDGE
    inside = 0.35 + 0.65 * np.clip(r, 0, 1) ** 3
    outside = np.exp(-((r - 1) / 0.08) ** 2)
    alpha = np.where(r <= 1, inside, outside)
    image = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
    image.putalpha(Image.fromarray((alpha * 255).astype(np.uint8)))
    image.save(OUT)
    print("wrote", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
