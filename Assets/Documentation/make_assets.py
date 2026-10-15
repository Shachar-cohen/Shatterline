"""Original deterministic image and sound assets for the course project."""
from pathlib import Path
import math, random, struct, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

root = Path(__file__).parents[1] / 'Resources'
tex = root / 'Textures'; audio = root / 'Audio'
tex.mkdir(parents=True, exist_ok=True); audio.mkdir(parents=True, exist_ok=True)
styles = {
    'stone': ((97, 120, 135), 14, 'tile'), 'metal': ((98, 111, 119), 11, 'grid'),
    'armor': ((47, 67, 89), 8, 'grid'), 'hazard': ((148, 71, 46), 13, 'diagonal'),
    'marble': ((147, 151, 145), 18, 'veins'), 'rust': ((111, 74, 62), 22, 'spots'),
    'circuit': ((55, 92, 105), 10, 'wires'), 'cloth': ((57, 72, 90), 13, 'weave'),
    'glass': ((73, 147, 167), 8, 'grid'), 'masonry': ((117, 102, 91), 17, 'tile'),
    'sandstone': ((165, 126, 101), 14, 'veins'), 'industrial': ((67, 77, 83), 12, 'grid'),
    'emergency': ((129, 45, 52), 13, 'diagonal'), 'signal': ((50, 146, 169), 13, 'wires'),
}
for idx, (name, (base, noise, pattern)) in enumerate(styles.items()):
    rng = np.random.default_rng(idx + 773)
    fine = rng.normal(0, noise, (128, 128, 1))
    xx = np.arange(128)[None, :, None]; yy = np.arange(128)[:, None, None]
    grain = np.sin(xx / (2.5 + idx % 3) + yy / (8 + idx % 4)) * (3 + idx % 4)
    image = np.clip(np.array(base)[None, None, :] + fine + grain, 0, 255).astype('uint8')
    picture = Image.fromarray(image, 'RGB')
    ink = ImageDraw.Draw(picture, 'RGBA')
    if pattern == 'grid':
        for i in range(0, 128, 32):
            ink.line([(i, 0), (i, 127)], fill=(16, 25, 30, 53), width=2)
            ink.line([(0, i), (127, i)], fill=(16, 25, 30, 48), width=2)
            for j in range(0, 128, 32): ink.ellipse((i+4,j+4,i+6,j+6), fill=(210,215,205,95))
    elif pattern == 'tile':
        for j in range(0,128,32):
            ink.line([(0,j),(127,j)], fill=(15,20,30,80), width=2)
            for i in range((j//32%2)*32,128,64): ink.line([(i,j),(i,j+32)], fill=(15,20,30,72),width=2)
    elif pattern == 'diagonal':
        for i in range(-128,256,25):
            ink.polygon([(i,0),(i+12,0),(i+140,128),(i+128,128)],fill=(245,164,69,56))
    elif pattern == 'wires':
        for i in range(16,128,32):
            ink.line([(i,0),(i,50),(i+24,50),(i+24,127)],fill=(85,226,210,100),width=2)
    elif pattern == 'veins':
        rnd=random.Random(idx)
        for i in range(12):
            x=rnd.randrange(128); y=rnd.randrange(128)
            ink.line([(x,y),(x+rnd.randrange(-45,45),y+rnd.randrange(-45,45))],fill=(30,30,37,42),width=1)
    elif pattern == 'spots':
        rnd=random.Random(idx)
        for i in range(65):
            x=rnd.randrange(128); y=rnd.randrange(128); r=rnd.randrange(1,7)
            ink.ellipse((x-r,y-r,x+r,y+r),fill=(205,119,61,35))
    elif pattern == 'weave':
        for i in range(0,128,3): ink.line([(0,i),(127,i)],fill=(220,230,245,20))
    picture.save(tex / (name+'.png'))

rate=22050
sounds = {'blaster': (.15,190,.8), 'pickup': (.27,660,.55),
          'damage': (.26,117,.8), 'checkpoint': (.42,420,.53), 'dash': (.23,260,.5)}
for index,(name,(duration,pitch,volume)) in enumerate(sounds.items()):
    rng=random.Random(1984+index)
    samples=[]
    for i in range(int(duration*rate)):
        t=i/rate; envelope=math.exp(-t*(13 if name=='blaster' else 8))
        frequency=pitch*(1+.42*t if name in ('pickup','checkpoint') else 1-.2*t)
        v=(math.sin(2*math.pi*frequency*t)*.55+(rng.random()*2-1)*(.27 if name in ('blaster','damage') else .08))
        samples.append(struct.pack('<h',int(max(-1,min(1,v*volume*envelope))*32767)))
    with wave.open(str(audio/(name+'.wav')),'wb') as out:
        out.setnchannels(1);out.setsampwidth(2);out.setframerate(rate);out.writeframes(b''.join(samples))
print(len(styles),'textures',len(sounds),'sounds')
