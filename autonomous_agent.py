import time
import random
import math
import json
from playwright.sync_api import sync_playwright

import urllib.request
import subprocess
import os

class AutonomousAgent:
    def __init__(self):
        self.p = sync_playwright().start()
        cdp_url = 'http://127.0.0.1:9222'
        
        is_running = False
        try:
            urllib.request.urlopen(cdp_url + '/json', timeout=1)
            is_running = True
        except:
            pass

        if not is_running:
            exe_path = self.p.chromium.executable_path
            tmp_dir = os.path.join(os.environ.get('TEMP', 'C:\\temp'), 'ai_browser_profile')
            subprocess.Popen([exe_path, '--remote-debugging-port=9222', '--user-data-dir=' + tmp_dir], creationflags=subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP)
            time.sleep(3)
            
        self.browser = self.p.chromium.connect_over_cdp(cdp_url)
        contexts = self.browser.contexts
        self.page = contexts[0].pages[0] if contexts and contexts[0].pages else contexts[0].new_page()

        
    def goto(self, url):
        self.page.goto(url)
        self.page.add_init_script('''
            document.addEventListener('DOMContentLoaded', () => {
                if (document.getElementById('ai-overlay-blocker')) return;
                const o = document.createElement('div');
                o.id = 'ai-overlay-blocker';
                o.style.cssText = 'position:fixed;inset:0;z-index:999998;background:transparent;cursor:not-allowed;';
                document.body.appendChild(o);
                const c = document.createElement('div');
                c.id = 'ai-cursor';
                c.style.cssText = 'position:fixed;width:20px;height:20px;background:rgba(255,0,0,0.7);border-radius:50%;z-index:999999;pointer-events:none;transition:transform 0.4s cubic-bezier(0.25, 1, 0.5, 1);left:0;top:0;';
                document.body.appendChild(c);
            });
        ''')
        time.sleep(1)

    def _move_cursor(self, selector):
        try:
            box = self.page.locator(selector).bounding_box()
            if box:
                # Add random offset for human-like clicking within the button
                x = box['x'] + (box['width'] / 2) + random.uniform(-2, 2)
                y = box['y'] + (box['height'] / 2) + random.uniform(-2, 2)
                self.page.evaluate(f"document.getElementById('ai-cursor').style.transform = 'translate({x}px, {y}px)'")
                time.sleep(random.uniform(0.4, 0.7)) # Human reaction delay
        except Exception as e:
            print(f"Cursor move failed: {e}")

    def click(self, selector):
        self._move_cursor(selector)
        self.page.click(selector, force=True)
        time.sleep(random.uniform(0.1, 0.3))

    def double_click(self, selector):
        self._move_cursor(selector)
        self.page.dblclick(selector, force=True)

    def right_click(self, selector):
        self._move_cursor(selector)
        self.page.click(selector, button='right', force=True)

    def hover(self, selector):
        self._move_cursor(selector)
        self.page.hover(selector, force=True)

    def drag_and_drop(self, source_selector, target_selector):
        self._move_cursor(source_selector)
        self.page.drag_and_drop(source_selector, target_selector, force=True)

    def type(self, selector, text):
        self.click(selector)
        for char in text:
            self.page.keyboard.type(char)
            time.sleep(random.uniform(0.02, 0.1)) # Human-like typing speed
        time.sleep(random.uniform(0.1, 0.3))

    def press(self, key):
        self.page.keyboard.press(key)
        time.sleep(random.uniform(0.1, 0.3))

    def scroll(self, direction='down', amount=500):
        if direction == 'down':
            self.page.mouse.wheel(0, amount)
        elif direction == 'up':
            self.page.mouse.wheel(0, -amount)
        time.sleep(random.uniform(0.3, 0.6))

    def get_page_text(self):
        return self.page.evaluate('document.body.innerText')

    def get_links(self):
        return self.page.evaluate('''() => {
            return Array.from(document.querySelectorAll('a')).map(a => ({ text: a.innerText, href: a.href }));
        }''')

    def save_session(self, filepath):
        with open(filepath, 'w') as f:
            json.dump(self.browser.contexts[0].cookies(), f)

    def load_session(self, filepath):
        with open(filepath, 'r') as f:
            cookies = json.load(f)
            self.browser.contexts[0].add_cookies(cookies)

    def wait(self, ms):
        time.sleep(ms / 1000.0)

    def close(self):
        self.browser.close()
        self.p.stop()