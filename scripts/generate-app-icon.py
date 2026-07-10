"""Generate the multi-resolution Windows icon used by the WPF shell.

The generator intentionally uses only Python's standard library so the public
repository can regenerate the tracked ICO without a local Pillow dependency.
"""

from __future__ import annotations

import math
import struct
from pathlib import Path
from typing import Iterable, Sequence


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "src" / "ImprovisedEosl.Spike.SyncModal" / "Assets" / "App.ico"
SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)
SUPERSAMPLE = 4

TRANSPARENT = (0, 0, 0, 0)
BASE = (245, 236, 227, 255)
BASE_SHADOW = (211, 191, 176, 255)
PRIMARY = (90, 51, 37, 255)
ACCENT = (184, 121, 69, 255)
HIGHLIGHT = (255, 249, 239, 245)

Point = tuple[float, float]
Color = tuple[int, int, int, int]


class Canvas:
    def __init__(self, width: int, height: int) -> None:
        self.width = width
        self.height = height
        self.pixels: list[Color] = [TRANSPARENT] * (width * height)

    def blend_pixel(self, x: int, y: int, color: Color) -> None:
        if x < 0 or y < 0 or x >= self.width or y >= self.height:
            return
        sr, sg, sb, sa = color
        if sa == 0:
            return
        index = y * self.width + x
        dr, dg, db, da = self.pixels[index]
        alpha = sa / 255.0
        inverse = 1.0 - alpha
        out_a = sa + da * inverse
        if out_a <= 0:
            self.pixels[index] = TRANSPARENT
            return
        self.pixels[index] = (
            round((sr * sa + dr * da * inverse) / out_a),
            round((sg * sa + dg * da * inverse) / out_a),
            round((sb * sa + db * da * inverse) / out_a),
            round(out_a),
        )

    def fill_rect(self, x0: float, y0: float, x1: float, y1: float, color: Color) -> None:
        left = max(0, math.floor(x0))
        top = max(0, math.floor(y0))
        right = min(self.width, math.ceil(x1))
        bottom = min(self.height, math.ceil(y1))
        for y in range(top, bottom):
            for x in range(left, right):
                self.blend_pixel(x, y, color)

    def fill_ellipse(self, x0: float, y0: float, x1: float, y1: float, color: Color) -> None:
        left = max(0, math.floor(x0))
        top = max(0, math.floor(y0))
        right = min(self.width, math.ceil(x1))
        bottom = min(self.height, math.ceil(y1))
        cx = (x0 + x1) / 2.0
        cy = (y0 + y1) / 2.0
        rx = max((x1 - x0) / 2.0, 0.1)
        ry = max((y1 - y0) / 2.0, 0.1)
        for y in range(top, bottom):
            py = (y + 0.5 - cy) / ry
            for x in range(left, right):
                px = (x + 0.5 - cx) / rx
                if px * px + py * py <= 1.0:
                    self.blend_pixel(x, y, color)

    def fill_polygon(self, points: Sequence[Point], color: Color) -> None:
        if len(points) < 3:
            return
        min_y = max(0, math.floor(min(y for _, y in points)))
        max_y = min(self.height - 1, math.ceil(max(y for _, y in points)))
        for y in range(min_y, max_y + 1):
            scan_y = y + 0.5
            intersections: list[float] = []
            for index, (x1, y1) in enumerate(points):
                x2, y2 = points[(index + 1) % len(points)]
                if (y1 <= scan_y < y2) or (y2 <= scan_y < y1):
                    ratio = (scan_y - y1) / (y2 - y1)
                    intersections.append(x1 + ratio * (x2 - x1))
            intersections.sort()
            for start, end in zip(intersections[0::2], intersections[1::2]):
                for x in range(max(0, math.floor(start)), min(self.width, math.ceil(end))):
                    self.blend_pixel(x, y, color)

    def stroke_polyline(self, points: Sequence[Point], width: float, color: Color, closed: bool = False) -> None:
        if len(points) < 2:
            return
        pairs = list(zip(points, points[1:]))
        if closed:
            pairs.append((points[-1], points[0]))
        radius = width / 2.0
        radius_sq = radius * radius
        for start, end in pairs:
            x1, y1 = start
            x2, y2 = end
            min_x = max(0, math.floor(min(x1, x2) - radius))
            max_x = min(self.width - 1, math.ceil(max(x1, x2) + radius))
            min_y = max(0, math.floor(min(y1, y2) - radius))
            max_y = min(self.height - 1, math.ceil(max(y1, y2) + radius))
            dx = x2 - x1
            dy = y2 - y1
            length_sq = dx * dx + dy * dy
            for y in range(min_y, max_y + 1):
                for x in range(min_x, max_x + 1):
                    if length_sq == 0:
                        distance_sq = (x + 0.5 - x1) ** 2 + (y + 0.5 - y1) ** 2
                    else:
                        t = max(0.0, min(1.0, ((x + 0.5 - x1) * dx + (y + 0.5 - y1) * dy) / length_sq))
                        px = x1 + t * dx
                        py = y1 + t * dy
                        distance_sq = (x + 0.5 - px) ** 2 + (y + 0.5 - py) ** 2
                    if distance_sq <= radius_sq:
                        self.blend_pixel(x, y, color)


def scale_points(points: Iterable[Point], factor: float) -> list[Point]:
    return [(x * factor, y * factor) for x, y in points]


def scaled_rect(rect: tuple[float, float, float, float], factor: float) -> tuple[float, float, float, float]:
    x0, y0, x1, y1 = rect
    return x0 * factor, y0 * factor, x1 * factor, y1 * factor


def draw_icon(size: int) -> tuple[int, int, list[Color]]:
    factor = size * SUPERSAMPLE / 256.0
    canvas = Canvas(size * SUPERSAMPLE, size * SUPERSAMPLE)

    shadow = [(35, 42), (221, 31), (241, 61), (236, 219), (202, 241), (43, 236), (23, 202), (23, 68)]
    frame = [(28, 34), (218, 24), (238, 54), (232, 216), (199, 235), (39, 230), (18, 198), (17, 62)]
    top_bar = [(30, 35), (218, 25), (238, 55), (235, 74), (21, 79), (18, 62)]
    ribbon = [(171, 75), (210, 68), (219, 145), (190, 151)]

    canvas.fill_polygon(scale_points(shadow, factor), BASE_SHADOW)
    canvas.fill_polygon(scale_points(frame, factor), BASE)
    canvas.stroke_polyline(scale_points(frame, factor), 6 * factor, PRIMARY, closed=True)
    canvas.fill_polygon(scale_points(top_bar, factor), PRIMARY)
    canvas.fill_polygon(scale_points(ribbon, factor), ACCENT)

    for dot_x in (50, 70, 90):
        canvas.fill_ellipse(*scaled_rect((dot_x - 5, 51, dot_x + 5, 61), factor), HIGHLIGHT)

    # Abstract EOSL mark: a sturdy E-shaped page glyph with a warm improvised slash.
    for rect in (
        (61, 94, 86, 182),
        (78, 94, 176, 117),
        (78, 126, 158, 149),
        (78, 158, 184, 181),
    ):
        canvas.fill_rect(*scaled_rect(rect, factor), PRIMARY)
    canvas.fill_polygon(scale_points([(150, 92), (176, 83), (199, 182), (172, 190)], factor), ACCENT)
    canvas.stroke_polyline(scale_points([(152, 89), (174, 184)], factor), 5 * factor, HIGHLIGHT)

    return downsample(canvas, SUPERSAMPLE)


def downsample(canvas: Canvas, factor: int) -> tuple[int, int, list[Color]]:
    width = canvas.width // factor
    height = canvas.height // factor
    output: list[Color] = []
    area = factor * factor
    for y in range(height):
        for x in range(width):
            totals = [0, 0, 0, 0]
            for sy in range(factor):
                for sx in range(factor):
                    pixel = canvas.pixels[(y * factor + sy) * canvas.width + (x * factor + sx)]
                    totals[0] += pixel[0]
                    totals[1] += pixel[1]
                    totals[2] += pixel[2]
                    totals[3] += pixel[3]
            output.append(tuple(round(value / area) for value in totals))  # type: ignore[arg-type]
    return width, height, output


def encode_dib(width: int, height: int, pixels: Sequence[Color]) -> bytes:
    header = struct.pack(
        "<IIIHHIIIIII",
        40,
        width,
        height * 2,
        1,
        32,
        0,
        width * height * 4,
        0,
        0,
        0,
        0,
    )
    xor = bytearray()
    for y in range(height - 1, -1, -1):
        for x in range(width):
            r, g, b, a = pixels[y * width + x]
            xor.extend((b, g, r, a))

    mask_stride = ((width + 31) // 32) * 4
    and_mask = bytes(mask_stride * height)
    return header + bytes(xor) + and_mask


def encode_ico(images: Sequence[tuple[int, int, bytes]]) -> bytes:
    directory = bytearray(struct.pack("<HHH", 0, 1, len(images)))
    offset = 6 + 16 * len(images)
    payload = bytearray()
    for width, height, data in images:
        directory.extend(
            struct.pack(
                "<BBBBHHII",
                0 if width == 256 else width,
                0 if height == 256 else height,
                0,
                0,
                1,
                32,
                len(data),
                offset,
            )
        )
        payload.extend(data)
        offset += len(data)
    return bytes(directory + payload)


def main() -> None:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    images = []
    for size in SIZES:
        width, height, pixels = draw_icon(size)
        images.append((width, height, encode_dib(width, height, pixels)))
    OUTPUT.write_bytes(encode_ico(images))


if __name__ == "__main__":
    main()
