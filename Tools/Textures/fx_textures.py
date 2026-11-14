"""
스킬 효과 셰이더(Shaders/ToonFx.shader)가 쓰는 그림을 코드로 그린다. 모양 그림은 흰색에 알파가 모양이다.

    python Tools/Textures/fx_textures.py

- FxNoise.png: 이음매 없는 노이즈(256). 셰이더가 흘려 보내며 모양을 갉아 불꽃 결을 만든다.
- FxFlame.png: 불꽃 혀(128×256). 밑이 둥글고 끝이 뾰족하며, 밑 가운데가 가장 진하다.
- FxStreak.png: 불티·꼬리 줄(128×32). 머리(오른쪽)가 진하고 꼬리로 갈수록 사라진다.
- FxSoftCircle.png: 부드러운 원(128). 바닥 장판의 모양.
- FxRing.png: 가는 고리(256). 충격파와 고리 장판.
- FxWall.png: 원기둥 불벽(64×128). 아래가 진하고 위로 사라진다.
결과는 Assets/MiniPandoras/Art/Textures/Fx.
"""

import os

import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "MiniPandoras", "Art", "Textures", "Fx")
SEED = 11


def save_alpha(alpha, name):
    alpha = np.clip(alpha, 0.0, 1.0)
    rgba = np.zeros(alpha.shape + (4,), dtype=np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = (alpha * 255).astype(np.uint8)
    Image.fromarray(rgba).save(os.path.join(OUT, name))


def grid(width, height):
    """픽셀 가운데의 (x, y)를 0~1로."""
    x = (np.arange(width) + 0.5) / width
    y = (np.arange(height) + 0.5) / height
    return np.meshgrid(x, y)


def noise(size=256):
    """푸리에 공간에서 1/f로 거른 흰 노이즈. 주기적이라 이음매가 없다."""
    rng = np.random.default_rng(SEED)
    freq = np.fft.fftfreq(size) * size
    fx, fy = np.meshgrid(freq, freq)
    radius = np.sqrt(fx ** 2 + fy ** 2)
    radius[0, 0] = 1.0
    spectrum = np.fft.fft2(rng.standard_normal((size, size))) / radius ** 1.35
    # 아주 큰 덩어리와 잔 알갱이는 빼서 불꽃 결에 맞는 크기만 남긴다(잔 알갱이가 많으면 지저분해 보였다).
    spectrum[(radius < 2.0) | (radius > 20.0)] = 0
    field = np.real(np.fft.ifft2(spectrum))
    field = (field - field.min()) / (field.max() - field.min())
    image = (np.clip(field, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(image, mode="L").convert("RGBA").save(os.path.join(OUT, "FxNoise.png"))


def flame(width=128, height=256):
    x, y = grid(width, height)
    up = 1.0 - y  # 그림의 위가 불꽃 끝
    # 높이마다 반폭: 밑에서 둥글게 부풀었다가 끝으로 뾰족해진다.
    half_width = 0.5 * np.sqrt(np.clip(np.sin(np.pi * np.clip(up * 0.8 + 0.2, 0, 1)), 0, 1)) * (1.0 - up) ** 0.6
    across = np.abs(x - 0.5) / np.maximum(half_width, 1e-4)
    body = np.clip(1.0 - across ** 2, 0, 1)
    # 밑 가운데가 진하고 끝으로 갈수록 옅다(노이즈에 끝부터 갉힌다). 맨 밑은 잘린 선이 보이지 않게 흐린다.
    alpha = body * (1.0 - up * 0.75) * np.clip(up / 0.15, 0, 1) ** 0.5
    save_alpha(alpha, "FxFlame.png")


def streak(width=128, height=32):
    x, y = grid(width, height)
    across = np.exp(-((y - 0.5) / 0.22) ** 2)
    along = x ** 1.4
    save_alpha(across * along, "FxStreak.png")


def soft_circle(size=128):
    x, y = grid(size, size)
    r = np.sqrt((x - 0.5) ** 2 + (y - 0.5) ** 2) / 0.5
    save_alpha(np.clip(1.0 - r, 0, 1) ** 0.6, "FxSoftCircle.png")


def ring(size=256, radius=0.42, width=0.05):
    x, y = grid(size, size)
    r = np.sqrt((x - 0.5) ** 2 + (y - 0.5) ** 2)
    save_alpha(np.exp(-((r - radius) / width) ** 2), "FxRing.png")


def wall(width=64, height=128):
    x, y = grid(width, height)
    up = 1.0 - y
    save_alpha((1.0 - up) ** 1.3, "FxWall.png")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    noise()
    flame()
    streak()
    soft_circle()
    ring()
    wall()
    print("wrote", os.path.normpath(OUT))
