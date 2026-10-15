"""NoAdView 앱 아이콘(res/app.ico) 생성기.

파란 둥근 사각형 위에 펼친 책(두 장 보기)과 산·해 그림을 그린다.
사용법: python tools/make_icon.py res/app.ico
"""
import sys
from PIL import Image, ImageDraw

SIZE = 512


def draw(size=SIZE):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = size / 512

    # 배경: 세로 그라데이션 둥근 사각형
    bg = Image.new("RGBA", (size, size))
    bd = ImageDraw.Draw(bg)
    top, bottom = (40, 116, 220), (18, 60, 140)
    for y in range(size):
        t = y / (size - 1)
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        bd.line([(0, y), (size, y)], fill=c)
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, size - 1, size - 1], radius=int(110 * s), fill=255)
    img.paste(bg, (0, 0), mask)

    # 펼친 책 두 장
    page = (250, 250, 250, 255)
    edge = (200, 214, 235, 255)
    left = [(70 * s, 130 * s), (248 * s, 150 * s), (248 * s, 400 * s), (70 * s, 380 * s)]
    right = [(264 * s, 150 * s), (442 * s, 130 * s), (442 * s, 380 * s), (264 * s, 400 * s)]
    d.polygon(left, fill=page, outline=edge)
    d.polygon(right, fill=page, outline=edge)

    # 왼쪽 장: 산과 해
    d.ellipse([110 * s, 180 * s, 150 * s, 220 * s], fill=(255, 190, 40, 255))
    d.polygon([(90 * s, 350 * s), (150 * s, 250 * s), (190 * s, 310 * s), (215 * s, 280 * s), (235 * s, 355 * s)],
              fill=(60, 160, 110, 255))
    # 오른쪽 장: 만화 칸
    ink = (40, 70, 120, 255)
    w = max(2, int(8 * s))
    d.rectangle([290 * s, 175 * s, 415 * s, 260 * s], outline=ink, width=w)
    d.rectangle([290 * s, 275 * s, 345 * s, 360 * s], outline=ink, width=w)
    d.rectangle([360 * s, 275 * s, 415 * s, 360 * s], outline=ink, width=w)
    return img


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else "res/app.ico"
    big = draw(SIZE)
    sizes = [(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)]
    big.save(out, format="ICO", sizes=sizes)
    big.resize((256, 256), Image.LANCZOS).save(out.replace(".ico", ".png"))
    print("wrote", out)


if __name__ == "__main__":
    main()
