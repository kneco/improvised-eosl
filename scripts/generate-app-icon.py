"""Generate the multi-resolution Windows icon used by the WPF shell."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "src" / "ImprovisedEosl.Spike.SyncModal" / "Assets" / "App.ico"
FONT = Path("C:/Windows/Fonts/seguisb.ttf")
SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)


def create_master() -> Image.Image:
    scale = 4
    canvas = Image.new("RGBA", (256 * scale, 256 * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)

    # Style C reference: warm blue wordmark, soft improvised frame, amber tape.
    background = (232, 238, 248, 255)
    primary = (74, 144, 226, 255)
    accent = (255, 158, 68, 235)
    foreground = (44, 44, 44, 255)

    frame = [(20, 28), (232, 18), (246, 48), (240, 222), (202, 242), (38, 238), (14, 202), (12, 62)]
    frame = [(x * scale, y * scale) for x, y in frame]
    draw.polygon(frame, fill=background)
    draw.line(frame + [frame[0]], fill=primary, width=3 * scale, joint="curve")

    font = ImageFont.truetype(str(FONT), 108 * scale)
    wordmark = "IE"
    bounds = draw.textbbox((0, 0), wordmark, font=font)
    width = bounds[2] - bounds[0]
    draw.text(((128 * scale - width / 2), 52 * scale), wordmark, font=font, fill=primary)

    draw.rounded_rectangle((48 * scale, 198 * scale, 208 * scale, 208 * scale), radius=2 * scale, fill=accent)
    draw.polygon([(48 * scale, 198 * scale), (61 * scale, 203 * scale), (52 * scale, 209 * scale)], fill=accent)
    draw.polygon([(208 * scale, 198 * scale), (195 * scale, 203 * scale), (204 * scale, 209 * scale)], fill=accent)

    if FONT.exists():
        label_font = ImageFont.truetype(str(FONT), 11 * scale)
        label = "Improvised EOSL"
        label_bounds = draw.textbbox((0, 0), label, font=label_font)
        label_width = label_bounds[2] - label_bounds[0]
        draw.text((128 * scale - label_width / 2, 216 * scale), label, font=label_font, fill=foreground)

    return canvas


def main() -> None:
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    master = create_master()
    frames = [master.resize((size, size), Image.Resampling.LANCZOS) for size in SIZES]
    frames[-1].save(OUTPUT, format="ICO", append_images=frames[:-1], sizes=[(size, size) for size in SIZES])


if __name__ == "__main__":
    main()
