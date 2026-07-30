import sys
import io

# Force stdout to use utf-8 so regional languages don't crash the charmap codec on Windows
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

import speech_recognition as sr
import json

def main():
    if len(sys.argv) > 1 and sys.argv[1] == "--list-mics":
        mics = sr.Microphone.list_microphone_names()
        print(json.dumps(mics))
        return

    lang = "en-US"
    mic_index = None
    wake_word = None

    for i in range(1, len(sys.argv)):
        if sys.argv[i].startswith("--lang="):
            lang = sys.argv[i].split("=")[1]
        elif sys.argv[i].startswith("--mic-index="):
            try:
                mic_index = int(sys.argv[i].split("=")[1])
            except ValueError:
                pass
        elif sys.argv[i].startswith("--wake-word="):
            wake_word = sys.argv[i].split("=", 1)[1].lower()

    # If no mic index specified, try to find a camera mic by default
    target_indices = []
    if mic_index is None:
        mics = sr.Microphone.list_microphone_names()
        target_indices = [idx for idx, name in enumerate(mics) if "Camera" in name or "camera" in name]
        target_indices.append(None) # Fallback to system default
    else:
        # Try the user selected mic first. If it fails due to PyAudio driver issues,
        # fallback to the system default instead of failing entirely.
        target_indices = [mic_index, None]

    r = sr.Recognizer()
    r.pause_threshold = 0.5
    
    recognized_text = None
    last_error = None
    
    for idx in target_indices:
        try:
            # We must catch the AttributeError thrown by SpeechRecognition's broken __exit__ 
            # if PyAudio fails to open the stream.
            source = sr.Microphone(device_index=idx)
            with source:
                r.adjust_for_ambient_noise(source, duration=0.5)
                
                if wake_word:
                    while True:
                        try:
                            audio = r.listen(source, timeout=None, phrase_time_limit=15)
                            raw_text = r.recognize_google(audio, language=lang).lower()
                            
                            import string
                            clean_text = raw_text.translate(str.maketrans('', '', string.punctuation))
                            
                            found_wake_word = None
                            for w in wake_word.split(','):
                                cw = w.strip().translate(str.maketrans('', '', string.punctuation))
                                if cw and cw in clean_text:
                                    found_wake_word = cw
                                    break
                            
                            if found_wake_word:
                                command = clean_text.split(found_wake_word, 1)[1].strip()
                                
                                if command:
                                    recognized_text = command
                                else:
                                    recognized_text = "__WAKE_WORD_ONLY__"
                                break
                        except sr.WaitTimeoutError:
                            continue
                        except sr.UnknownValueError:
                            continue
                        except sr.RequestError as e:
                            last_error = f"RequestError: {e}"
                            break
                else:
                    audio = r.listen(source, timeout=10, phrase_time_limit=15)
                    recognized_text = r.recognize_google(audio, language=lang)
                    
            # If we succeed, break out of loop
            if recognized_text or (wake_word and last_error): 
                break
        except Exception as e:
            if isinstance(e, AttributeError) and "'NoneType' object has no attribute 'close'" in str(e):
                last_error = "Microphone is unavailable or format is not supported by PyAudio."
            elif isinstance(e, sr.WaitTimeoutError):
                last_error = "Speech recognition timeout (no speech detected)."
            elif isinstance(e, sr.UnknownValueError):
                last_error = "Could not understand audio."
            else:
                last_error = str(e)
            continue
            
    if recognized_text == "__WAKE_WORD_ONLY__":
        import platform, os
        if platform.system() == "Windows":
            os.system('powershell -Command "Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak(\'How can I help you?\')"')
        
        # Now reopen mic and listen for the actual command
        recognized_text = None
        for idx in target_indices:
            try:
                source = sr.Microphone(device_index=idx)
                with source:
                    r.adjust_for_ambient_noise(source, duration=0.5)
                    audio = r.listen(source, timeout=10, phrase_time_limit=15)
                    recognized_text = r.recognize_google(audio, language=lang)
                if recognized_text:
                    break
            except Exception as e:
                if isinstance(e, sr.WaitTimeoutError):
                    last_error = "Speech recognition timeout after wake word (no command detected)."
                elif isinstance(e, sr.UnknownValueError):
                    last_error = "Could not understand the command after wake word."
                else:
                    last_error = str(e)
                continue

    if recognized_text:
        print(recognized_text)
    elif last_error:
        print(f"ERROR: {last_error}")
    else:
        print("ERROR: Could not capture or understand audio.")

if __name__ == "__main__":
    main()
