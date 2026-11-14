"""
투기장 모래 바닥 텍스처(이음매 없이 반복)를 코드로 그린다.

    python Tools/Textures/sand.py

셀셰이딩과 어울리게 얼룩을 세 단으로 끊고, 작은 자갈을 흩뿌린다. 결과는 Assets/MiniPandoras/Art/Textures/Sand.png.
"""

import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SIZE = 1024
SEED = 5
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "Textures", "Sand.png")

LIGHT = (237, 210, 164)
MIDDLE = (231, 200, 151)
DARK = (222, 188, 137)
PEBBLE = (196, 166, 128)
PEBBLE_LIGHT = (245, 228, 192)


def periodic_noise(rng, falloff, lowest, highest):
    """푸리에 공간에서 만든 노이즈라 가장자리가 맞물린다(바닥에 반복해 깔아도 이음매가 없다)."""
    freq = np.fft.fftfreq(SIZE) * SIZE
    fx, fy = np.meshgrid(freq, freq)
    radius = np.sqrt(fx ** 2 + fy ** 2)
    radius[0, 0] = 1.0
    amplitude = 1.0 / radius ** falloff
    amplitude[(radius < lowest) | (radius > highest)] = 0.0
    phase = rng.uniform(0, 2 * math.pi, (SIZE, SIZE))
    field = np.real(np.fft.ifft2(amplitude * np.exp(1j * phase)))
    return (field - field.min()) / (field.max() - field.min())


def main():
    rng = np.random.default_rng(SEED)
    patches = periodic_noise(rng, 1.8, 1, 12) * 0.85 + periodic_noise(rng, 1.2, 8, 40) * 0.15
    image = np.zeros((SIZE, SIZE, 3), dtype=np.float32)
    image[:] = MIDDLE
    image[patches > 0.58] = LIGHT
    image[patches < 0.40] = DARK
    picture = Image.fromarray(image.astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))

    draw = ImageDraw.Draw(picture)
    for _ in range(110):
        x, y = rng.uniform(0, SIZE, 2)
        r = rng.uniform(2.5, 6.5)
        squash = rng.uniform(0.55, 0.9)
        for dx in (-SIZE, 0, SIZE):
            for dy in (-SIZE, 0, SIZE):
                cx, cy = x + dx, y + dy
                draw.ellipse((cx - r, cy - r * squash, cx + r, cy + r * squash), fill=PEBBLE)
                draw.ellipse((cx - r * 0.55, cy - r * squash * 0.75, cx + r * 0.2, cy - r * squash * 0.1), fill=PEBBLE_LIGHT)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    picture.save(OUT)
    print("wrote", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
