import os
import sys
import subprocess
import wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont

FFMPEG_EXE = r"C:\Users\PC Uriel\AppData\Roaming\Python\Python311\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe"
OUTPUT_DIR = r"D:\Uni\Gramaticas\Colilla-Malvoro\Assets\Prototipo3\Videos"

WIDTH = 1280
HEIGHT = 720
FPS = 30
SAMPLE_RATE = 44100

def get_font(size):
    try:
        return ImageFont.truetype("arial.ttf", size)
    except Exception:
        return ImageFont.load_default()

def get_bold_font(size):
    try:
        return ImageFont.truetype("arialbd.ttf", size)
    except Exception:
        return get_font(size)

def add_letterbox(draw, w, h):
    bar_h = int(h * 0.12)
    draw.rectangle([0, 0, w, bar_h], fill=(0, 0, 0))
    draw.rectangle([0, h - bar_h, w, h], fill=(0, 0, 0))

def save_wav(audio_data, wav_path):
    audio_int16 = np.clip(audio_data * 32767, -32767, 32767).astype(np.int16)
    with wave.open(wav_path, 'wb') as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        wf.writeframes(audio_int16.tobytes())

def create_video_with_audio(frames_generator, total_frames, audio_data, output_path):
    temp_wav = output_path.replace(".mp4", "_temp.wav")
    save_wav(audio_data, temp_wav)

    proc = subprocess.Popen([
        FFMPEG_EXE,
        "-y",
        "-loglevel", "error",
        "-f", "rawvideo",
        "-vcodec", "rawvideo",
        "-s", f"{WIDTH}x{HEIGHT}",
        "-pix_fmt", "rgb24",
        "-r", str(FPS),
        "-i", "-",
        "-i", temp_wav,
        "-c:v", "libx264",
        "-pix_fmt", "yuv420p",
        "-preset", "fast",
        "-crf", "20",
        "-c:a", "aac",
        "-b:a", "128k",
        "-shortest",
        output_path
    ], stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    for frame in frames_generator():
        proc.stdin.write(frame.tobytes())

    proc.stdin.close()
    proc.wait()

    if os.path.exists(temp_wav):
        os.remove(temp_wav)

    if proc.returncode != 0:
        print(f"FFmpeg failed for {output_path} with exit code {proc.returncode}")
    else:
        print(f"Generated: {output_path} ({os.path.getsize(output_path)} bytes)")

def generate_intro():
    output_path = os.path.join(OUTPUT_DIR, "Intro_FiltroCarmesi_Prototipo3.mp4")
    duration = 7.0
    total_frames = int(duration * FPS)

    font_title = get_bold_font(42)
    font_sub = get_font(24)
    font_quote = get_font(20)

    np.random.seed(42)
    skyline = np.random.randint(180, 420, size=WIDTH)
    skyline = np.convolve(skyline, np.ones(35)/35, mode='same')
    raindrops = [{"x": np.random.randint(0, WIDTH), "y": np.random.randint(0, HEIGHT), "speed": np.random.randint(18, 30)} for _ in range(120)]

    def frame_gen():
        for f in range(total_frames):
            t = f / FPS
            fade_in = min(1.0, t / 1.5)
            fade_out = max(0.0, min(1.0, (duration - t) / 1.5))
            alpha_global = fade_in * fade_out

            img = Image.new("RGB", (WIDTH, HEIGHT), color=(12, 10, 16))
            draw = ImageDraw.Draw(img)

            # Reddish fog
            fog_pulse = 0.5 + 0.3 * np.sin(t * 3.0)
            red_intensity = int(80 * fog_pulse * alpha_global)
            for y in range(HEIGHT // 2, HEIGHT):
                prog = (y - HEIGHT // 2) / (HEIGHT // 2)
                cur_r = int(red_intensity * prog)
                cur_b = int(25 * prog)
                draw.line([(0, y), (WIDTH, y)], fill=(cur_r, 5, cur_b))

            # Skyline
            for x in range(WIDTH):
                sy = int(HEIGHT - skyline[x] * 0.8)
                draw.line([(x, sy), (x, HEIGHT)], fill=(18, 14, 22))

            # Smokestack tower
            tower_x = int(WIDTH * 0.72)
            draw.rectangle([tower_x - 30, 150, tower_x + 30, HEIGHT], fill=(10, 8, 14))
            if int(t * 2) % 2 == 0:
                draw.ellipse([tower_x - 6, 144, tower_x + 6, 156], fill=(240, 30, 30))

            # Rain
            for drop in raindrops:
                drop["y"] = (drop["y"] + drop["speed"]) % HEIGHT
                drop["x"] = (drop["x"] + 2) % WIDTH
                draw.line([(drop["x"], drop["y"]), (drop["x"] - 3, drop["y"] - 12)], fill=(120, 140, 180))

            # Crimson Filter glowing
            filter_x = WIDTH // 2 + int(np.sin(t * 2.0) * 15)
            filter_y = HEIGHT - 180 + int(np.cos(t * 1.5) * 8)
            glow_r = int(50 + 15 * np.sin(t * 4.0))
            draw.ellipse([filter_x - glow_r, filter_y - glow_r, filter_x + glow_r, filter_y + glow_r], fill=(int(85 * fog_pulse), 5, 12))
            draw.rectangle([filter_x - 35, filter_y - 12, filter_x + 10, filter_y + 12], fill=(220, 35, 35))
            draw.rectangle([filter_x + 10, filter_y - 12, filter_x + 35, filter_y + 12], fill=(230, 220, 200))
            for s in range(5):
                sw_x = filter_x - 30 - s * 14 + int(np.sin(t * 4.0 + s) * 10)
                sw_y = filter_y - 10 - s * 18
                draw.ellipse([sw_x - 8, sw_y - 6, sw_x + 8, sw_y + 6], fill=(int(180 * alpha_global), 40, 70))

            # Text
            text_color = (int(255 * alpha_global), int(220 * alpha_global), int(220 * alpha_global))
            accent_color = (int(255 * alpha_global), int(50 * alpha_global), int(60 * alpha_global))
            draw.text((WIDTH // 2, 220), "MALVORO TOBACCO CORP.", font=font_title, fill=accent_color, anchor="mm")
            draw.text((WIDTH // 2, 270), "CRÓNICA I: EL FILTRO CARMESÍ", font=font_sub, fill=text_color, anchor="mm")

            if t > 1.8:
                n_fade = min(1.0, (t - 1.8) / 1.0) * fade_out
                narr_color = (int(200 * n_fade), int(200 * n_fade), int(210 * n_fade))
                draw.text((WIDTH // 2, 530), "«El humo del Aditivo Cero no adormecía a las bestias... las despertaba.»", font=font_quote, fill=narr_color, anchor="mm")
                draw.text((WIDTH // 2, 560), "Barnaby empuña su aguja y percutor. La conspiración en las cloacas arderá esta noche.", font=font_quote, fill=narr_color, anchor="mm")

            add_letterbox(draw, WIDTH, HEIGHT)
            yield img

    # Audio synthesis: rain noise + ambient drone
    t_audio = np.linspace(0, duration, int(duration * SAMPLE_RATE), endpoint=False)
    rain = np.random.normal(0, 0.04, len(t_audio))
    drone = 0.25 * np.sin(2 * np.pi * 55 * t_audio) + 0.15 * np.sin(2 * np.pi * 110 * t_audio)
    envelope = np.minimum(1.0, t_audio / 1.5) * np.minimum(1.0, (duration - t_audio) / 1.5)
    audio = (rain + drone) * envelope
    create_video_with_audio(frame_gen, total_frames, audio, output_path)

def generate_alert_ciervo():
    output_path = os.path.join(OUTPUT_DIR, "Alert_CiervoFeral_Prototipo3.mp4")
    duration = 5.0
    total_frames = int(duration * FPS)

    font_huge = get_bold_font(50)
    font_warn = get_bold_font(32)
    font_sub = get_font(24)

    def frame_gen():
        for f in range(total_frames):
            t = f / FPS
            glitch = (f % 6 == 0 or f % 7 == 0) and (t < 3.5)
            strobe = (int(t * 8) % 2 == 0)

            bg_col = (55, 0, 8) if strobe else (16, 4, 6)
            if glitch:
                bg_col = (100, 10, 20)

            img = Image.new("RGB", (WIDTH, HEIGHT), color=bg_col)
            draw = ImageDraw.Draw(img)

            stripe_w = 40
            offset = int(t * 60) % stripe_w
            for x in range(-stripe_w, WIDTH + stripe_w, stripe_w):
                draw.polygon([(x + offset, 90), (x + offset + 20, 90), (x + offset + 10, 120), (x + offset - 10, 120)], fill=(240, 180, 0))
                draw.polygon([(x - offset, 590), (x - offset + 20, 590), (x - offset + 10, 620), (x - offset - 10, 620)], fill=(240, 180, 0))

            cx, cy = WIDTH // 2, 350
            if glitch:
                cx += np.random.randint(-15, 15)

            antler_col = (255, 40, 50) if strobe else (210, 190, 180)
            draw.line([(cx - 20, cy), (cx - 80, cy - 80)], fill=antler_col, width=8)
            draw.line([(cx - 80, cy - 80), (cx - 150, cy - 130)], fill=antler_col, width=6)
            draw.line([(cx - 80, cy - 80), (cx - 110, cy - 170)], fill=antler_col, width=5)
            draw.line([(cx - 150, cy - 130), (cx - 200, cy - 180)], fill=antler_col, width=4)

            draw.line([(cx + 20, cy), (cx + 70, cy - 70)], fill=antler_col, width=8)
            draw.line([(cx + 70, cy - 70), (cx + 120, cy - 110)], fill=antler_col, width=5)
            draw.line([(cx + 120, cy - 110), (cx + 140, cy - 80)], fill=(255, 60, 60), width=3)
            draw.line([(cx + 70, cy - 70), (cx + 100, cy - 150)], fill=antler_col, width=4)

            eye_col = (255, 240, 30)
            draw.ellipse([cx - 40, cy + 20, cx - 20, cy + 35], fill=eye_col)
            draw.ellipse([cx + 20, cy + 20, cx + 40, cy + 35], fill=eye_col)

            draw.text((WIDTH // 2, 170), "¡ALERTA DE AMENAZA FERAL!", font=font_huge, fill=(255, 230, 40), anchor="mm")
            draw.text((WIDTH // 2, 230), "MINI-BOSS: CIERVO FERAL DE CORNAMENTA ASTILLADA", font=font_sub, fill=(255, 255, 255), anchor="mm")

            action_col = (255, 50, 50) if strobe else (255, 255, 100)
            draw.text((WIDTH // 2, 500), "¡PREPARA TU DASH CON [Q] O CLICK DERECHO!", font=font_warn, fill=action_col, anchor="mm")
            draw.text((WIDTH // 2, 545), "Haz que se estrelle contra los muros para aturdirlo.", font=font_sub, fill=(220, 220, 220), anchor="mm")

            add_letterbox(draw, WIDTH, HEIGHT)
            yield img

    t_audio = np.linspace(0, duration, int(duration * SAMPLE_RATE), endpoint=False)
    # Siren pitch modulation + sub rumble
    freq = 350 + 200 * np.sin(2 * np.pi * 2.0 * t_audio)
    phase = 2 * np.pi * np.cumsum(freq) / SAMPLE_RATE
    siren = 0.3 * np.sin(phase)
    sub = 0.3 * np.sin(2 * np.pi * 50 * t_audio)
    envelope = np.minimum(1.0, t_audio / 0.3) * np.minimum(1.0, (duration - t_audio) / 0.8)
    audio = (siren + sub) * envelope
    create_video_with_audio(frame_gen, total_frames, audio, output_path)

def generate_traicion_lulu():
    output_path = os.path.join(OUTPUT_DIR, "Traicion_Lulu_Prototipo3.mp4")
    duration = 6.0
    total_frames = int(duration * FPS)

    font_title = get_bold_font(42)
    font_quote = get_bold_font(28)
    font_sub = get_font(22)

    def frame_gen():
        for f in range(total_frames):
            t = f / FPS
            fade_in = min(1.0, t / 1.0)
            fade_out = max(0.0, min(1.0, (duration - t) / 1.0))
            alpha_global = fade_in * fade_out

            img = Image.new("RGB", (WIDTH, HEIGHT), color=(14, 12, 20))
            draw = ImageDraw.Draw(img)

            door_progress = min(1.0, t / 3.0)
            left_door_x = int(WIDTH * 0.5 * door_progress)
            right_door_x = WIDTH - left_door_x
            draw.rectangle([0, 100, left_door_x, HEIGHT - 100], fill=(28, 30, 38))
            draw.rectangle([right_door_x, 100, WIDTH, HEIGHT - 100], fill=(28, 30, 38))
            if left_door_x > 20:
                draw.line([(left_door_x, 100), (left_door_x, HEIGHT - 100)], fill=(220, 50, 50), width=6)
                draw.line([(right_door_x, 100), (right_door_x, HEIGHT - 100)], fill=(220, 50, 50), width=6)

            cx, cy = int(WIDTH * 0.65), 380
            draw.ellipse([cx - 40, cy - 30, cx + 40, cy + 50], fill=(20, 18, 26))
            draw.ellipse([cx - 25, cy - 70, cx + 25, cy - 25], fill=(24, 22, 30))
            draw.polygon([(cx - 25, cy - 60), (cx - 35, cy - 95), (cx - 10, cy - 65)], fill=(24, 22, 30))
            draw.polygon([(cx + 25, cy - 60), (cx + 35, cy - 95), (cx + 10, cy - 65)], fill=(24, 22, 30))
            draw.ellipse([cx - 16, cy - 50, cx - 6, cy - 40], fill=(190, 80, 255))
            draw.ellipse([cx + 6, cy - 50, cx + 16, cy - 40], fill=(190, 80, 255))
            draw.line([(cx + 8, cy - 35), (cx + 38, cy - 32)], fill=(240, 230, 200), width=3)
            draw.ellipse([cx + 35, cy - 36, cx + 43, cy - 28], fill=(255, 80, 30))

            for s in range(6):
                smk_x = cx + 40 + s * 8 + int(np.sin(t * 3.0 + s) * 12)
                smk_y = cy - 40 - s * 16
                draw.ellipse([smk_x - 6, smk_y - 6, smk_x + 6, smk_y + 6], fill=(130, 70, 170))

            t_col = (int(255 * alpha_global), int(70 * alpha_global), int(70 * alpha_global))
            draw.text((WIDTH // 2, 170), "TORRE MALVORO — PISO 44", font=font_title, fill=t_col, anchor="mm")

            if t > 1.2:
                q_alpha = min(1.0, (t - 1.2) / 0.8) * fade_out
                q_col = (int(255 * q_alpha), int(220 * q_alpha), int(230 * q_alpha))
                draw.text((WIDTH // 2, 510), "Lulú: «Lo lamento, Barnaby. Aquí no hay héroes.»", font=font_quote, fill=q_col, anchor="mm")
                draw.text((WIDTH // 2, 555), "«En esta ciudad... o devoras, o eres devorado.»", font=font_quote, fill=(int(255 * q_alpha), int(120 * q_alpha), int(140 * q_alpha)), anchor="mm")
                draw.text((WIDTH // 2, 595), "¡EL REACTOR CARMESÍ SE HA ACTIVADO! ¡SABOTEA SUS 3 NÚCLEOS!", font=font_sub, fill=(int(255 * q_alpha), int(255 * q_alpha), int(100 * q_alpha)), anchor="mm")

            add_letterbox(draw, WIDTH, HEIGHT)
            yield img

    t_audio = np.linspace(0, duration, int(duration * SAMPLE_RATE), endpoint=False)
    drone = 0.3 * np.sin(2 * np.pi * 42 * t_audio) + 0.1 * np.sin(2 * np.pi * 84 * t_audio)
    slam_time = 3.0
    slam_env = np.exp(-15 * np.maximum(0, t_audio - slam_time)) * (t_audio >= slam_time)
    slam = 0.5 * np.random.normal(0, 0.2, len(t_audio)) * slam_env
    envelope = np.minimum(1.0, t_audio / 1.0) * np.minimum(1.0, (duration - t_audio) / 1.0)
    audio = (drone + slam) * envelope
    create_video_with_audio(frame_gen, total_frames, audio, output_path)

def generate_outro_victoria():
    output_path = os.path.join(OUTPUT_DIR, "Outro_Victoria_Prototipo3.mp4")
    duration = 7.0
    total_frames = int(duration * FPS)

    font_title = get_bold_font(46)
    font_quote = get_bold_font(26)
    font_sub = get_font(22)

    def frame_gen():
        for f in range(total_frames):
            t = f / FPS
            fade_in = min(1.0, t / 1.5)
            fade_out = max(0.0, min(1.0, (duration - t) / 1.5))
            alpha_global = fade_in * fade_out

            img = Image.new("RGB", (WIDTH, HEIGHT), color=(20, 15, 30))
            draw = ImageDraw.Draw(img)

            for y in range(HEIGHT):
                prog = y / HEIGHT
                r = int((20 + 200 * prog * min(1.0, t / 3.0)) * alpha_global)
                g = int((15 + 110 * prog * min(1.0, t / 3.0)) * alpha_global)
                b = int((30 + 50 * (1 - prog)) * alpha_global)
                draw.line([(0, y), (WIDTH, y)], fill=(min(255, r), min(255, g), min(255, b)))

            sun_y = int(HEIGHT * 0.65 - min(100, t * 20))
            draw.ellipse([WIDTH // 2 - 80, sun_y - 80, WIDTH // 2 + 80, sun_y + 80], fill=(255, 230, 140))

            stack_x = int(WIDTH * 0.3)
            draw.rectangle([stack_x - 30, 260, stack_x + 30, HEIGHT], fill=(25, 20, 30))
            for s in range(8):
                pw_x = stack_x + int(np.sin(t * 2.0 + s) * 20) + s * 10
                pw_y = 250 - s * 22
                draw.ellipse([pw_x - 18, pw_y - 14, pw_x + 18, pw_y + 14], fill=(240, 240, 255))

            bx, by = int(WIDTH * 0.72), int(HEIGHT * 0.62)
            draw.ellipse([bx - 16, by - 24, bx + 16, by + 12], fill=(15, 10, 20))
            draw.ellipse([bx - 14, by - 32, bx - 6, by - 22], fill=(15, 10, 20))
            draw.ellipse([bx + 6, by - 32, bx + 14, by - 22], fill=(15, 10, 20))
            draw.line([(bx - 8, by - 36), (bx + 8, by + 16)], fill=(200, 180, 120), width=3)

            draw.text((WIDTH // 2, 170), "EL FILTRO CARMESÍ: NEUTRALIZADO", font=font_title, fill=(255, 240, 200), anchor="mm")
            draw.text((WIDTH // 2, 230), "VICTORIA — PROTOTIPO 3 COMPLETADO", font=font_quote, fill=(255, 200, 80), anchor="mm")

            if t > 1.5:
                q_alpha = min(1.0, (t - 1.5) / 1.0) * fade_out
                draw.text((WIDTH // 2, 530), "El reactor de Malvoro ha colapsado en cenizas blancas.", font=font_quote, fill=(int(240 * q_alpha), int(240 * q_alpha), int(250 * q_alpha)), anchor="mm")
                draw.text((WIDTH // 2, 570), "La metrópoli despierta a un nuevo aire... y Barnaby es finalmente libre.", font=font_sub, fill=(int(220 * q_alpha), int(220 * q_alpha), int(230 * q_alpha)), anchor="mm")

            add_letterbox(draw, WIDTH, HEIGHT)
            yield img

    t_audio = np.linspace(0, duration, int(duration * SAMPLE_RATE), endpoint=False)
    # Uplifting chords: Major triads (C major / F major / G major)
    chords = (
        0.18 * np.sin(2 * np.pi * 261.63 * t_audio) + # C4
        0.15 * np.sin(2 * np.pi * 329.63 * t_audio) + # E4
        0.15 * np.sin(2 * np.pi * 392.00 * t_audio) + # G4
        0.12 * np.sin(2 * np.pi * 523.25 * t_audio)   # C5
    )
    envelope = np.minimum(1.0, t_audio / 1.5) * np.minimum(1.0, (duration - t_audio) / 1.5)
    audio = chords * envelope
    create_video_with_audio(frame_gen, total_frames, audio, output_path)

if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    print("Generating Cinematics for Prototipo 3...")
    generate_intro()
    generate_alert_ciervo()
    generate_traicion_lulu()
    generate_outro_victoria()
    print("All cinematics generated successfully!")
