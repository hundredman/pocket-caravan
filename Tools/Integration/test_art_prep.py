import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtPrep'))
from prepare_art import prepare, estimate_grid, load_palette


class ArtPrepChecks(unittest.TestCase):
    palette=[(0,0,0),(255,255,255),(200,100,50)]

    def fixture(self):
        image=Image.new('RGBA',(3,2),(0,0,0,0))
        image.putdata([(195,105,55,255),(250,250,250,255),(0,0,0,0),
                       (3,3,3,127),(190,100,50,128),(0,0,0,0)])
        return image

    def test_nearest_whole_image_grid_roundtrip(self):
        original=self.fixture(); large=original.resize((12,8),Image.Resampling.NEAREST)
        result,report=prepare(large,(3,2),self.palette)
        self.assertEqual(report['gridStep'],4)
        self.assertEqual(result.getpixel((0,0)),(200,100,50,255))
        self.assertEqual(result.getpixel((1,0)),(255,255,255,255))
        self.assertTrue(report['paletteValid'] and report['binaryAlpha'])

    def test_native_image_needs_no_fake_upscale(self):
        result,report=prepare(self.fixture(),(3,2),self.palette)
        self.assertEqual(report['gridMode'],'native')
        self.assertEqual(result.size,(3,2))

    def test_uncertain_noise_refuses_automatic_reduction(self):
        image=Image.new('RGBA',(8,8));image.putdata([(i,i//2,50,255) for i in range(64)])
        self.assertIsNone(estimate_grid(image)['step'])
        with self.assertRaisesRegex(ValueError,'No reliable'):prepare(image,(4,4),self.palette)
        _,r=prepare(image,(4,4),self.palette,grid_step=2)
        self.assertEqual(r['gridMode'],'manual')

    def test_flat_image_requires_manual_grid(self):
        image=Image.new('RGBA',(8,8),'white')
        with self.assertRaisesRegex(ValueError,'No visible edges'):prepare(image,(4,4),self.palette)
        self.assertEqual(prepare(image,(4,4),self.palette,2)[0].size,(4,4))

    def test_alpha_threshold_boundary_and_rgb_hidden_under_transparency(self):
        result,_=prepare(self.fixture(),(3,2),self.palette)
        self.assertEqual(result.getpixel((0,1)),(0,0,0,0))
        self.assertEqual(result.getpixel((1,1)),(200,100,50,255))
        self.assertEqual(result.getpixel((2,0)),(0,0,0,0))

    def test_input_pixels_never_modified(self):
        original=self.fixture();before=original.tobytes();prepare(original,(3,2),self.palette)
        self.assertEqual(original.tobytes(),before)

    def test_no_cropping_or_aspect_distortion(self):
        with self.assertRaisesRegex(ValueError,'whole image'):prepare(self.fixture(),(2,2),self.palette,1)
        with self.assertRaises(ValueError):prepare(self.fixture(),(3,1),self.palette,2)

    def test_invalid_step_size_and_alpha(self):
        for step in (0,-1,1.5):
            with self.subTest(step=step),self.assertRaises(ValueError):prepare(self.fixture(),(3,2),self.palette,step)
        for threshold in (0,256):
            with self.subTest(threshold=threshold),self.assertRaises(ValueError):prepare(self.fixture(),(3,2),self.palette,1,threshold)
        with self.assertRaises(ValueError):prepare(self.fixture(),(0,2),self.palette)

    def test_invalid_palettes(self):
        for palette in ([],[(256,0,0)],[(0,0,0,0)]):
            with self.subTest(palette=palette),self.assertRaises(ValueError):prepare(self.fixture(),(3,2),palette)

    def test_palette_file_deduplicates_and_rejects_unknown_lines(self):
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder)/'palette.txt';path.write_text('#000000\n#ffffff\n#000000\n')
            self.assertEqual(load_palette(path),[(0,0,0),(255,255,255)])
            path.write_text('not a hex color\n')
            with self.assertRaises(ValueError):load_palette(path)

    def test_cli_roundtrip_report_and_no_source_overwrite(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);src=root/'source.png';out=root/'out.png';palette=root/'palette.txt';report=root/'report.json'
            self.fixture().save(src);before=src.read_bytes();palette.write_text('#000000\n#ffffff\n#c86432\n')
            args=[sys.executable,str(ROOT/'Tools/ArtPrep/prepare_art.py'),str(src),str(out),'--size','3','2','--palette',str(palette),'--report',str(report)]
            done=subprocess.run(args,capture_output=True,text=True);self.assertEqual(done.returncode,0,done.stderr)
            with Image.open(out) as image:self.assertEqual(image.size,(3,2));self.assertEqual({a for *_,a in image.convert('RGBA').get_flattened_data()},{0,255})
            self.assertEqual(json.loads(report.read_text())['approval'],'pending user visual review')
            self.assertEqual(src.read_bytes(),before)
            bad_report=args.copy();bad_report[-1]=str(src);self.assertNotEqual(subprocess.run(bad_report,capture_output=True).returncode,0)
            self.assertEqual(src.read_bytes(),before)
            args[3]=str(src);denied=subprocess.run(args,capture_output=True,text=True)
            self.assertNotEqual(denied.returncode,0);self.assertEqual(src.read_bytes(),before)

    def test_uncertain_cli_leaves_no_output(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);src=root/'source.png';out=root/'out.png';pal=root/'pal.txt'
            Image.new('RGBA',(8,8),'white').save(src);pal.write_text('#ffffff\n')
            result=subprocess.run([sys.executable,str(ROOT/'Tools/ArtPrep/prepare_art.py'),str(src),str(out),'--size','4','4','--palette',str(pal)],capture_output=True)
            self.assertNotEqual(result.returncode,0);self.assertFalse(out.exists())
