# Forsworn Director by soybean_alien. MIT License.
# Turns tools/DemoGif/frames/NN_HOLD.png (from record.ps1) into docs/media/demo.gif.
# HOLD is how many 0.65 s beats a frame stays on screen.
import sys
from pathlib import Path

from PIL import Image

here = Path(__file__).resolve().parent
frames = sorted((here / "frames").glob("*.png"))
if not frames:
    sys.exit("No frames; run record.ps1 first.")

beat_ms = 650
images, durations = [], []
for path in frames:
    hold = int(path.stem.split("_")[1])
    image = Image.open(path).convert("RGB")
    images.append(image.quantize(colors=192, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
    durations.append(hold * beat_ms)

out = here.parent.parent / "docs" / "media" / "demo.gif"
out.parent.mkdir(parents=True, exist_ok=True)
images[0].save(out, save_all=True, append_images=images[1:], duration=durations, loop=0, optimize=True)
print(f"{out}: {len(images)} frames, {sum(durations) / 1000:.1f} s, {out.stat().st_size // 1024} KB")
