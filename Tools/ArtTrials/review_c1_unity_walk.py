"""Encode the sixteen unmodified Unity Game captures as a review animation."""
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];out=ROOT/'ArtReview/C1-Walk'
frames=[]
for direction in 'dulr':
 for f in range(4):
  im=Image.open(out/f'unity-{direction}{f}.png').convert('RGB')
  assert im.size==(1920,1080)
  frames.append(im)
# Only format conversion to GIF; no crop, scale, compositing, or art alteration.
frames[0].save(out/'unity-walk.gif',save_all=True,append_images=frames[1:],duration=125,loop=0)
print('16 actual Unity frames encoded; 1920x1080, 8fps, dulr')
