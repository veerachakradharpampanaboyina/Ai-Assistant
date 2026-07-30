import sys
import os
import pygame
import time
from gtts import gTTS

def play_text(file_path, lang='en'):
    try:
        # Read text from file (utf-8 to support all languages)
        with open(file_path, 'r', encoding='utf-8') as f:
            text = f.read().strip()
            
        if not text:
            return
            
        # gTTS often expects the language code before the dash (e.g. 'hi' instead of 'hi-IN')
        gtts_lang = lang.split('-')[0]
        
        # fallback for 'ta-IN' or 'te-IN' etc
        if gtts_lang == "en":
            gtts_lang = "en"
            
        tts = gTTS(text=text, lang=gtts_lang)
        
        # Save to temp mp3 with a unique name to prevent file locking issues
        import tempfile
        import uuid
        filename = os.path.join(tempfile.gettempdir(), f"temp_tts_{uuid.uuid4().hex}.mp3")
        tts.save(filename)
        
        # Initialize pygame mixer and play
        # Hide the pygame support prompt
        os.environ['PYGAME_HIDE_SUPPORT_PROMPT'] = '1'
        pygame.mixer.init()
        pygame.mixer.music.load(filename)
        pygame.mixer.music.play()
        
        # Wait until done playing
        while pygame.mixer.music.get_busy():
            time.sleep(0.1)
            
        pygame.mixer.quit()
        
        # Clean up
        if os.path.exists(filename):
            try:
                os.remove(filename)
            except:
                pass
                
    except Exception as e:
        print(f"GTTS ERROR: {e}")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python google_tts.py <file_path> <lang_code>")
        sys.exit(1)
        
    file_path = sys.argv[1]
    lang = sys.argv[2]
    play_text(file_path, lang)
