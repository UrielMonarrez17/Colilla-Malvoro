"""Rebuild the two review scenes from the untouched originals (Python + Pillow).

The output is ordinary, editable uGUI scene objects. Runtime scripts only bind
events and update state; they do not build or replace the visual hierarchy.
Run from the Unity project root. This intentionally overwrites UIReview only.
"""
from pathlib import Path
import hashlib
import json
import math
import random
import re
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/FeralUI/Art'
OUT = ROOT / 'Assets/Scenes/UIReview'
PREVIEW = ROOT / 'Temp/FeralUI'
for p in (ART, OUT, PREVIEW):
    p.mkdir(parents=True, exist_ok=True)

def guid(path):
    p = ROOT / path
    meta = Path(str(p) + '.meta')
    if meta.exists():
        return re.search(r'guid: (\w+)', meta.read_text(encoding='utf-8')).group(1)
    value = hashlib.md5(('feral-ui-v1/' + str(path).replace('\\', '/')).encode()).hexdigest()
    kind = 'folderAsset: yes\n' if p.is_dir() else ''
    importer = 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n' if p.suffix == '.cs' else 'DefaultImporter:\n  externalObjects: {}\n'
    meta.write_text(f'fileFormatVersion: 2\nguid: {value}\n{kind}{importer}  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
    return value

def texture_meta(path):
    value = hashlib.md5(('feral-ui-v1/' + path).encode()).hexdigest()
    (ROOT / (path + '.meta')).write_text(f'''fileFormatVersion: 2
guid: {value}
TextureImporter:
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  alphaSource: 1
  alphaIsTransparency: 1
  textureType: 0
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  userData:
  assetBundleName:
  assetBundleVariant:
''', encoding='utf-8')

# Original geometric artwork, deliberately independent of paid/external art.
def artwork():
    w, h = 1600, 900
    im = Image.new('RGB', (w, h))
    px = im.load()
    for y in range(h):
        for x in range(w):
            glow = max(0, 1 - math.hypot((x-1180)/900, (y-280)/800))
            px[x, y] = (int(11+glow*23), int(24+glow*33), int(27+glow*32))
    d = ImageDraw.Draw(im)
    rng = random.Random(81)
    for _ in range(110):
        x, y = rng.randrange(620, 1580), rng.randrange(20, 450)
        d.ellipse((x, y, x+2, y+2), fill=(95, 117, 111))
    d.ellipse((1055, 110, 1335, 390), fill=(169, 172, 130))
    d.ellipse((1095, 128, 1345, 372), fill=(192, 188, 145))
    for layer, base, color in [(0, 690, '#253a3b'), (1, 795, '#172b2e'), (2, 900, '#102225')]:
        x = 580 if layer < 2 else 690
        while x < 1650:
            bw = rng.randrange(60, 130)
            bh = rng.randrange(170, 370) + layer*10
            top = base-bh
            d.rectangle((x, top, x+bw, base), fill=color)
            d.polygon([(x-6, top), (x+bw/2, top-rng.randrange(12, 45)), (x+bw+6, top)], fill=color)
            if layer == 1:
                for wx in range(x+17, x+bw-12, 25):
                    for wy in range(top+30, base-35, 45):
                        if rng.random() < .38: d.rectangle((wx, wy, wx+7, wy+13), fill='#967d50')
            x += bw + rng.randrange(9, 26)
    # A tiny observer on a foreground ledge gives the city its hamster scale.
    d.polygon([(710, 900), (950, 748), (1600, 806), (1600, 900)], fill='#0b171b')
    d.line([(950, 748), (1600, 806)], fill='#3e5551', width=3)
    d.ellipse((1120, 649, 1202, 764), fill='#ba915a')
    d.ellipse((1115, 613, 1197, 701), fill='#cea971')
    d.ellipse((1115, 605, 1142, 637), fill='#ba915a')
    d.ellipse((1168, 605, 1195, 637), fill='#ba915a')
    d.ellipse((1122, 612, 1136, 631), fill='#745747')
    d.ellipse((1175, 612, 1188, 631), fill='#745747')
    d.ellipse((1134, 688, 1189, 758), fill='#e0c69b')
    d.ellipse((1175, 643, 1182, 653), fill='#192528')
    d.polygon([(1194, 653), (1205, 658), (1194, 663)], fill='#745747')
    d.ellipse((1119, 751, 1150, 766), fill='#ba915a')
    d.ellipse((1170, 751, 1208, 766), fill='#ba915a')
    # Smooth dark left edge keeps menu typography readable.
    overlay = Image.new('RGBA', (w, h))
    od = ImageDraw.Draw(overlay)
    for x in range(900):
        a = int(220 * max(0, 1-x/900))
        od.line((x, 0, x, h), fill=(7, 17, 20, a))
    im = Image.alpha_composite(im.convert('RGBA'), overlay)
    im.save(ART / 'NightCity.png')
    texture_meta('Assets/FeralUI/Art/NightCity.png')
    for name in ['Madera', 'Metal', 'Desperdicios', 'Semillas', 'Goma']:
        icon = Image.new('RGBA', (128, 128))
        q = ImageDraw.Draw(icon)
        c, dark = '#ddbc82', '#8c7857'
        if name == 'Madera':
            q.polygon([(24,82),(82,24),(107,49),(49,107)], fill=c)
            q.line([(34,83),(85,32)], fill=dark, width=4)
            q.line([(48,93),(95,46)], fill=dark, width=4)
            q.line([(35,63),(53,45)], fill=dark, width=4)
        elif name == 'Metal':
            q.polygon([(20,54),(82,25),(108,42),(108,80),(48,107),(20,90)], fill=c)
            q.line([(20,54),(48,72),(108,42)], fill=dark, width=4)
            q.line([(48,72),(48,107)], fill=dark, width=4)
        elif name == 'Desperdicios':
            q.polygon([(27,45),(56,24),(76,42),(101,45),(91,101),(39,107),(20,76)], fill=c)
            q.line([(27,45),(59,60),(76,42),(69,87),(39,107)], fill=dark, width=4)
            q.line([(59,60),(41,76),(69,87),(91,101)], fill=dark, width=4)
        elif name == 'Semillas':
            for x,y in [(24,45),(61,22),(68,72)]:
                q.ellipse((x,y,x+30,y+44), fill=c)
                q.line([(x+15,y+8),(x+15,y+34)], fill=dark, width=3)
        else:
            q.ellipse((22,22,107,107), fill=c)
            q.ellipse((44,44,85,85), fill=(0,0,0,0))
            q.arc((29,29,100,100), 175, 340, fill=dark, width=4)
        icon.save(ART / (name + '.png'))
        texture_meta('Assets/FeralUI/Art/' + name + '.png')

IMAGE = 'fe87c0e1cc204ed48ad3b37840f39efc'
TEXT = '5f7201a12d95ffc409449d95f23cf332'
RAW = '1344c3c82d62a2a41a3576d8abb8e3ea'
BUTTON = '4e29b1a8efbd4b44bb3f3716e73f07ff'
SLIDER = '67db9e8f0e2ae9c40bc1e2b64352a6b4'
FONT = 'e3265ab4bf004d28a9537516768c1c75'
INK = '#eee9da'
MUTED = '#a4b6b2'
GOLD = '#ddbc82'
PANEL = '#132326'
CARD = '#1e2b2c'

def rgba(color):
    if isinstance(color, tuple): return color
    v = color.lstrip('#')
    return tuple(int(v[i:i+2], 16)/255 for i in (0,2,4)) + (1,)

def color_yaml(c):
    r,g,b,a = rgba(c)
    return f'{{r: {r:.6f}, g: {g:.6f}, b: {b:.6f}, a: {a:.6f}}}'

def ref(value): return '{fileID: ' + str(value) + '}'

class Scene:
    def __init__(self):
        self.next = 8100000000
        self.nodes = []
        self.docs = []

    def ident(self):
        self.next += 1
        return self.next

    def node(self, name, parent=None, box=(0,0,1600,900), fill=None, text=None, size=20, color=INK, bold=False, active=True, texture=None, align=0):
        n = dict(id=self.ident(), rect=self.ident(), name=name, parent=parent, box=box, children=[], components=[], active=active, fill=fill, text=text, size=size, color=color, bold=bold, texture=texture, align=align)
        self.nodes.append(n)
        if parent: parent['children'].append(n)
        if fill is not None or text is not None or texture:
            n['components'].append((222, self.ident(), 'CanvasRenderer', '  m_CullTransparentMesh: 1\n'))
            if text is not None:
                fields = self.graphic(color, False) + f'''  m_FontData:
    m_Font: {{fileID: 12800000, guid: {FONT}, type: 3}}
    m_FontSize: {size}
    m_FontStyle: {1 if bold else 0}
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: {size}
    m_Alignment: {align}
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: {json.dumps(text, ensure_ascii=False)}
'''
                n['graphic'] = self.mono(n, TEXT, fields)
            elif texture:
                fields = self.graphic('#ffffff', False) + f'  m_Texture: {{fileID: 2800000, guid: {guid(texture)}, type: 3}}\n  m_UVRect:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 1\n    height: 1\n'
                n['graphic'] = self.mono(n, RAW, fields)
            else:
                fields = self.graphic(fill, True) + '  m_Sprite: {fileID: 0}\n  m_Type: 0\n  m_PreserveAspect: 0\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: 1\n'
                n['graphic'] = self.mono(n, IMAGE, fields)
        return n

    def mono(self, n, script, fields):
        ident = self.ident()
        n['components'].append((114, ident, 'MonoBehaviour', f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n' + fields))
        return ident

    def graphic(self, color, raycast):
        return f'  m_Material: {{fileID: 0}}\n  m_Color: {color_yaml(color)}\n  m_RaycastTarget: {int(raycast)}\n  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}\n  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n'

    def selectable(self, target):
        return f'''  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 1.25, g: 1.25, b: 1.25, a: 1}}
    m_PressedColor: {{r: 0.7, g: 0.7, b: 0.7, a: 1}}
    m_SelectedColor: {{r: 1.25, g: 1.25, b: 1.25, a: 1}}
    m_DisabledColor: {{r: 0.5, g: 0.5, b: 0.5, a: 0.6}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.12
  m_SpriteState:
    m_HighlightedSprite: {{fileID: 0}}
    m_PressedSprite: {{fileID: 0}}
    m_SelectedSprite: {{fileID: 0}}
    m_DisabledSprite: {{fileID: 0}}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {ref(target)}
'''

    def button(self, parent, name, label, box, primary=False, label_name=None):
        n = self.node(name, parent, box, fill=GOLD if primary else CARD)
        self.mono(n, BUTTON, self.selectable(n['graphic']) + '  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n')
        x,y,w,h=box
        self.node(label_name or name+'Label', n, (22,0,w-44,h), text=label, size=22, color=PANEL if primary else INK, bold=primary, align=3)
        return n

    def label(self, parent, name, value, x,y,w,h,size=20,color=INK,bold=False,align=0):
        return self.node(name,parent,(x,y,w,h),text=value,size=size,color=color,bold=bold,align=align)

    def canvas(self, controller):
        root=self.node('Feral UI')
        root['canvas']=True
        root['components'].append((223,self.ident(),'Canvas','''  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 25
  m_SortingLayerID: 0
  m_SortingOrder: 100
  m_TargetDisplay: 0
'''))
        self.mono(root,'0cd44c1031e13a943bb63640046fad76','''  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {x: 1600, y: 900}
  m_ScreenMatchMode: 1
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
''')
        self.mono(root,'dc42784cf147c0c48a680349fa168899','  m_IgnoreReversedGraphics: 1\n  m_BlockingObjects: 0\n  m_BlockingMask:\n    serializedVersion: 2\n    m_Bits: 4294967295\n')
        self.mono(root,guid('Assets/FeralUI/Scripts/'+controller+'.cs'),'  openOnDirectPlay: 1\n' if controller=='FeralInventoryUI' else '')
        return root

    def save(self, original, output):
        docs=[]
        for n in self.nodes:
            common='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
            components=[n['rect']]+[c[1] for c in n['components']]
            docs.append(f'--- !u!1 &{n["id"]}\nGameObject:\n'+common+'  serializedVersion: 6\n  m_Component:\n'+''.join('  - component: '+ref(c)+'\n' for c in components)+f'  m_Layer: 5\n  m_Name: {n["name"]}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: {int(n["active"])}\n')
            x,y,w,h=n['box']
            center = n['parent'] is not None and n['parent'].get('canvas',False)
            if center: x-=800; y-=450
            anchor = 0.5 if center else 0
            ay = 0.5 if center else 1
            rect=common+'  m_GameObject: '+ref(n['id'])+'\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n'
            rect+='  m_Children:'+ ('\n'+''.join('  - '+ref(c['rect'])+'\n' for c in n['children']) if n['children'] else ' []\n')
            rect+='  m_Father: '+ref(n['parent']['rect'] if n['parent'] else 0)+'\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
            if n.get('sliderhandle'):
                rect+='  m_AnchorMin: {x: 0.8, y: 0.5}\n  m_AnchorMax: {x: 0.8, y: 0.5}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 22, y: 32}\n  m_Pivot: {x: 0.5, y: 0.5}\n'
            elif n.get('stretch'):
                rect+='  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 0, y: 0}\n  m_Pivot: {x: 0.5, y: 0.5}\n'
            else:
                rect+=f'  m_AnchorMin: {{x: {anchor}, y: {ay}}}\n  m_AnchorMax: {{x: {anchor}, y: {ay}}}\n  m_AnchoredPosition: {{x: {x}, y: {-y}}}\n  m_SizeDelta: {{x: {w}, y: {h}}}\n  m_Pivot: {{x: 0, y: 1}}\n'
            docs.append(f'--- !u!224 &{n["rect"]}\nRectTransform:\n'+rect)
            for classid, ident, kind, fields in n['components']:
                docs.append(f'--- !u!{classid} &{ident}\n{kind}:\n'+common+'  m_GameObject: '+ref(n['id'])+'\n'+fields)
        source=(ROOT/original).read_text(encoding='utf-8')
        pos=source.index('--- !u!1660057539')
        result=source[:pos]+''.join(docs)+source[pos:]+''.join('  - '+ref(n['rect'])+'\n' for n in self.nodes if n['parent'] is None)
        (ROOT/output).write_text(result,encoding='utf-8')
        guid(output)

    def preview(self, name, page=None):
        im=Image.new('RGBA',(1600,900),'#0b171b')
        d=ImageDraw.Draw(im)
        fontpath=ROOT/'Assets/TextMesh Pro/Fonts/LiberationSans.ttf'
        boxes={}
        visible={}
        for n in self.nodes:
            par=n['parent']
            x,y,w,h=n['box']
            if par:
                px,py,_,_=boxes[par['id']]; x+=px;y+=py
            boxes[n['id']]=(x,y,w,h)
            show=n['active']
            if page and n['name'] in ['Home','Sessions','Settings','Controls','Exit']: show=n['name']==page
            show=show and (not par or visible[par['id']])
            visible[n['id']]=show
            if not show: continue
            if n['fill']:
                c=tuple(round(v*255) for v in rgba(n['fill']))
                layer=Image.new('RGBA',im.size)
                ImageDraw.Draw(layer).rectangle((x,y,x+w,y+h),fill=c)
                im=Image.alpha_composite(im,layer);d=ImageDraw.Draw(im)
            if n['texture']:
                pic=Image.open(ROOT/n['texture']).convert('RGBA').resize((int(w),int(h)),Image.Resampling.LANCZOS)
                im.alpha_composite(pic,(int(x),int(y)));d=ImageDraw.Draw(im)
            if n['text'] is not None:
                font=ImageFont.truetype(str(fontpath),n['size'])
                lines=[]
                for paragraph in n['text'].split('\n'):
                    current=''
                    for word in paragraph.split(' '):
                        candidate=(current+' '+word).strip()
                        if current and d.textlength(candidate,font=font)>w: lines.append(current);current=word
                        else: current=candidate
                    lines.append(current)
                lh=n['size']*1.15
                yy=y+((h-len(lines)*lh)/2 if n['align'] in [3,4,5] else 0)
                for line in lines:
                    xx=x+((w-d.textlength(line,font=font))/2 if n['align'] in [1,4,7] else 0)
                    d.text((xx,yy),line,font=font,fill=n['color'],stroke_width=0)
                    yy+=lh
        im.convert('RGB').save(PREVIEW/(name+'.png'))

def line(s,p,name,x,y,w): s.node(name,p,(x,y,w,1),fill='#354644')

def menu():
    s=Scene();root=s.canvas('FeralMainMenu')
    s.node('BackdropFill',root,fill='#0b171b')['stretch']=True
    s.node('NightCity',root,texture='Assets/FeralUI/Art/NightCity.png')
    s.label(root,'Brand','F /',72,45,120,45,30,GOLD,True)
    s.label(root,'Edition','AVENTURA  /  SUPERVIVENCIA  /  SIGILO',180,55,700,35,16,MUTED)
    s.label(root,'Footer','FERAL   /   PROTOTIPO EN DESARROLLO',72,849,700,30,14,MUTED)
    s.label(root,'NavigationHint','Flechas: navegar     Enter: elegir     Esc: volver',985,849,545,30,15,MUTED)
    p=s.node('Home',root)
    s.label(p,'Overline','CUANDO LA CIUDAD CAMBIA, SOBREVIVE.',112,168,650,35,17,GOLD)
    s.label(p,'Title','FERAL',103,210,700,140,120,INK,True)
    s.label(p,'Tagline','Un pequeño héroe.\nUna ciudad que nunca duerme.',112,365,520,72,27,MUTED)
    for i,(name,label,primary) in enumerate([('Play','JUGAR    →',True),('SavedGames','Partidas',False),('AudioSettings','Configuración de audio',False),('Help','Cómo jugar',False),('Quit','Salir del juego',False)]):
        s.button(p,name,label,(112,481+i*62,440,52),primary)
    s.label(p,'ArtCaption','EXPLORA DE DÍA.\nESCÓNDETE DE NOCHE.',1004,459,490,90,23,INK,True)

    for name,title,subtitle in [('Sessions','Tu próxima historia','Prepara tu mochila. La ciudad te espera.'),('Settings','Configuración de audio','Ajusta el sonido antes de salir a explorar.'),('Controls','Aprende a sobrevivir','Tu mejor herramienta es saber cuándo escapar.'),('Exit','¿Dejar la ciudad?','Puedes regresar cuando estés listo.')]:
        p=s.node(name,root,active=False)
        s.node(name+'Shade',p,(72,130,875,675),fill=PANEL)
        s.label(p,name+'Eyebrow','FERAL  /  '+{'Sessions':'PARTIDAS','Settings':'AUDIO','Controls':'CONTROLES','Exit':'SALIR'}[name],112,167,760,25,16,GOLD)
        s.label(p,name+'Title',title,112,215,790,65,43,INK,True)
        s.label(p,name+'Subtitle',subtitle,112,286,760,45,21,MUTED)
        line(s,p,name+'Divider',112,351,795)
        s.button(p,'Back'+name,'←  Volver',(112,717,255,52))
        if name=='Sessions':
            s.button(p,'NewGame','NUEVA PARTIDA    →',(112,389,795,76),True)
            s.label(p,'SaveHeading','PARTIDAS GUARDADAS',112,500,760,25,16,GOLD)
            s.label(p,'NoSaves','Todavía no hay partidas guardadas.',112,547,760,34,25)
            s.label(p,'SessionStatus','El guardado y la carga estarán disponibles en una próxima versión.\nPor ahora, cada partida comienza desde cero.',112,599,755,80,19,MUTED)
        elif name=='Settings':
            s.label(p,'VolumeLabel','Volumen general',112,393,550,35,26)
            s.label(p,'VolumeValue','80%',808,397,90,35,23,GOLD)
            slider=s.node('MasterVolume',p,(112,454,795,48))
            s.node('VolumeTrack',slider,(0,19,795,10),fill='#354644')
            area=s.node('VolumeFillArea',slider,(12,19,771,10))
            fill=s.node('VolumeFill',area,(0,0,616,10),fill=GOLD)
            fill['stretch']=True
            handle_area=s.node('VolumeHandleArea',slider,(12,0,771,48))
            handle=s.node('VolumeHandle',handle_area,(606,8,22,32),fill=INK)
            handle['sliderhandle']=True
            s.mono(slider,SLIDER,s.selectable(handle['graphic'])+f'  m_FillRect: {ref(fill["rect"])}\n  m_HandleRect: {ref(handle["rect"])}\n  m_Direction: 0\n  m_MinValue: 0\n  m_MaxValue: 1\n  m_WholeNumbers: 0\n  m_Value: 0.8\n  m_OnValueChanged:\n    m_PersistentCalls:\n      m_Calls: []\n')
            s.button(p,'Mute','Silenciar / activar',(112,536,370,58))
            s.label(p,'AudioState','Sonido activado',520,551,380,38,21,GOLD)
            s.button(p,'ResetAudio','Restablecer',(550,717,357,52))
            s.label(p,'AudioNote','Tus preferencias se conservan al cerrar el juego.',112,641,760,35,19,MUTED)
        elif name=='Controls':
            controls=[('W A S D','Moverte'),('SHIFT','Correr · consume estamina'),('C','Agacharte y moverte con sigilo'),('ESPACIO','Dash · impulso para escapar'),('F','Revisar contenedor / usar mesa'),('I / TAB','Abrir y cerrar la mochila')]
            for i,(key,desc) in enumerate(controls):
                yy=383+i*48
                s.label(p,'Key'+str(i),key,112,yy,180,36,19,GOLD,True)
                s.label(p,'Control'+str(i),desc,323,yy,582,36,20)
        else:
            s.label(p,'ExitText','Salir cerrará el juego.\n\nNos vemos al amanecer.',112,410,760,130,28)
            s.button(p,'ConfirmQuit','SALIR DEL JUEGO',(112,593,795,70),True)
    s.save('Assets/Scenes/SampleScene.unity','Assets/Scenes/UIReview/Feral_MainMenu.unity')
    for page in ['Home','Sessions','Settings','Controls']:
        s.preview('Menu_'+page,page)

def inventory():
    s=Scene();root=s.canvas('FeralInventoryUI')
    hint=s.node('InventoryHint',root,(1210,30,325,54),active=False)
    s.button(hint,'OpenInventory','I / TAB   Mochila',(0,0,325,54))
    p=s.node('Inventory',root)
    shade=s.node('InventoryDim',p,fill=(.02,.05,.06,.88));shade['stretch']=True
    s.node('InventoryBoard',p,(64,56,1472,780),fill=PANEL)
    s.node('GoldRule',p,(64,56,1472,3),fill=GOLD)
    s.label(p,'InventoryEyebrow','FERAL  /  PREPÁRATE PARA LA NOCHE',104,89,940,28,16,GOLD)
    s.label(p,'InventoryTitle','Tu mochila',104,130,710,65,48,INK,True)
    s.label(p,'Total','0 MATERIALES REUNIDOS',109,204,740,28,17,MUTED)
    s.button(p,'CloseInventory','Continuar  [ I ]',(1215,102,280,56),True)
    line(s,p,'HeaderRule',104,251,1392)
    s.button(p,'FilterAll','[ Todos ]',(104,272,160,43),label_name='AllLabel')
    s.button(p,'FilterAvailable','Disponibles',(276,272,211,43),label_name='AvailableLabel')
    s.label(p,'MaterialCategory','MATERIALES',680,282,265,32,16,MUTED)
    materials=['Madera','Metal','Desperdicios','Semillas','Goma']
    for i,item in enumerate(materials):
        x=104+(i%3)*282;y=336+(i//3)*158
        card=s.button(p,'Card'+item,'',(x,y,264,140))
        s.node('Icon'+item,card,(16,12,68,68),texture='Assets/FeralUI/Art/'+item+'.png')
        s.label(card,'Count'+item,'00',178,19,65,52,38,GOLD)
        s.label(card,'Name'+item,item,20,94,230,35,23)
    s.label(p,'GatherTip','EXPLORA Y RECOLECTA\n\nF cerca de un contenedor\npara buscar materiales.',682,508,253,115,17,MUTED)
    s.label(p,'EmptyState','Tu mochila está vacía. Busca contenedores en la ciudad.',104,651,835,36,18,MUTED)
    s.node('DetailsPanel',p,(988,273,508,416),fill=CARD)
    s.label(p,'DetailsEyebrow','MATERIAL SELECCIONADO',1016,298,445,26,15,GOLD)
    s.label(p,'DetailTitle','Madera',1016,345,445,52,36,INK,True)
    s.label(p,'DetailAmount','EN TU MOCHILA  /  0',1016,412,445,28,17,GOLD)
    line(s,p,'DetailsRule',1016,461,449)
    s.label(p,'DetailBody','Tablas y fragmentos recuperados de la ciudad.\n\nMaterial requerido por las trampas 1 y 3.',1016,489,439,172,21,MUTED)
    # A separate recipe view keeps the primary inventory readable.
    s.label(p,'RecipesTitle','RECETAS DE LA MESA',104,703,530,26,15,GOLD)
    for i,(label,cost) in enumerate([('Trampa 1','1 madera + 1 metal'),('Trampa 2','1 goma + 1 desperdicio'),('Trampa 3','2 madera')]):
        x=104+i*282
        s.label(p,'RecipeName'+str(i),label+'  /  '+cost,x,739,274,27,15)
        s.label(p,'RecipeState'+str(i),'Faltan materiales',x,771,265,26,15,'#c2aa84')
    s.label(p,'RecipeNote','Consulta los requisitos. Fabrica en una mesa con F.',988,709,508,42,17,MUTED)
    s.button(p,'ReturnMenu','Volver al menú',(1196,769,300,43))
    s.label(p,'InventoryFooter','I / TAB  Mochila       ESC  Volver       Juego en pausa',104,851,1200,28,16,MUTED)
    # Modal placed last so its backdrop blocks pointer input to the inventory.
    c=s.node('ReturnConfirmation',p,active=False)
    s.node('ConfirmationShade',c,fill=(.015,.035,.04,.96))['stretch']=True
    s.node('ConfirmationCard',c,(365,238,870,420),fill=PANEL)
    s.label(c,'ReturnTitle','¿Volver al menú?',407,280,785,64,42,INK,True)
    s.label(c,'ReturnStatus','Se perderán los materiales y el estado de esta partida.\nEl prototipo todavía no guarda el progreso.',407,373,780,104,24,MUTED)
    s.button(c,'CancelReturn','Seguir explorando',(407,549,365,61),True)
    s.button(c,'ConfirmReturn','Volver al menú',(797,549,395,61))
    s.save('Assets/Scenes/Prototipo1.unity','Assets/Scenes/UIReview/Game.unity')
    s.preview('Inventory')

if __name__=='__main__':
    artwork()
    for directory in ['Assets/FeralUI','Assets/FeralUI/Scripts','Assets/FeralUI/Editor','Assets/FeralUI/Art','Assets/Scenes/UIReview']:
        guid(directory)
    for path in (ROOT/'Assets/FeralUI').rglob('*.cs'): guid(path.relative_to(ROOT))
    guid('Assets/Scripts/FeralInput.cs')
    menu();inventory()
    print('Created two editable uGUI scenes and layout previews in Temp/FeralUI.')
