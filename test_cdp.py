import urllib.request
import subprocess
import time
from playwright.sync_api import sync_playwright
import os

cdp_url = 'http://127.0.0.1:9222'
is_running = False
try:
    urllib.request.urlopen(cdp_url + '/json', timeout=1)
    is_running = True
except:
    pass

with sync_playwright() as p:
    if not is_running:
        print("Launching Playwright Chromium...")
        exe_path = p.chromium.executable_path
        # Launch detached
        subprocess.Popen([exe_path, '--remote-debugging-port=9222', '--user-data-dir=' + os.path.join(os.environ['TEMP'], 'ai_browser_profile')], creationflags=subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP)
        time.sleep(3)

    print("Connecting to CDP...")
    browser = p.chromium.connect_over_cdp(cdp_url)
    contexts = browser.contexts
    page = contexts[0].pages[0] if contexts and contexts[0].pages else contexts[0].new_page()
    page.goto('https://example.com')
    print("Connected and navigated!")
    browser.close()
