# AI Assistant 🚀

AI Assistant is a powerful desktop application built with WPF and .NET 8 that brings your favorite AI models directly to your desktop. It integrates seamlessly with your workflow through global hotkeys, clipboard monitoring, screen capture, and built-in OCR capabilities. 

Recently, it has been transformed into a fully autonomous, agentic workspace IDE, allowing the AI to build projects, execute complex workflows, and automate browser tasks completely on its own!

## 🌟 Core Features

### Autonomous Agent Capabilities
- **Multi-Step Execution:** Fully automated background terminal and filesystem access directly from the chat prompt. The AI can execute multi-step plans, build projects, and write code without manual intervention.
- **Auto-Debugging:** If a background terminal command fails, the AI will automatically catch the `stderr` output, analyze the issue, and self-correct (up to 3 times in an auto-loop) without needing your input.
- **Auto-Testing & Verification:** The AI automatically detects your project's test framework (`npm test`, `dotnet test`, `pytest`, etc.) and runs it to verify its code.
- **Checkpointing:** Long-running tasks are check-pointed. If the IDE restarts, you will be prompted to resume incomplete background agent workflows exactly where they left off.

### Full Workspace IDE
- **VS Code Style Sidebar:** 6 dedicated panels for ultimate project control:
  - **File Explorer:** Native tree-view of your workspace.
  - **Search:** Lightning-fast project-wide text search.
  - **Git Manager:** Native UI for viewing staged/unstaged changes, committing, pushing, and pulling directly from the IDE.
  - **AI Agents:** Monitor running background tasks, review conversation history, and check your AI notification inbox.
  - **Artifacts Viewer:** View AI-generated implementation plans, reports, and persistent design documents.
  - **Scheduled Tasks:** View crons and scheduled tasks executing asynchronously.
- **Intelligent Code Editor:** Monaco Editor integration supporting `Ctrl+Shift+P` Command Palette actions! Highlight code and trigger instant AI commands like `Explain`, `Refactor`, or `Debug`.
- **1 Million Token Context:** Automatically aggregates and ingests your entire workspace's file contents into the AI's prompt for perfect codebase memory.

### Browser Automation Agent
- **Autonomous Web Navigation:** The AI can launch Chrome invisibly and browse the web, scrape data, and fill forms.
- **Visual Capture:** Built-in buttons for the AI to instantly take **📸 Screenshots** or **🎥 Record Videos** of web pages, feeding the data directly into your artifacts viewer.
- **Login Automation:** Securely store secrets in the settings panel so the AI can automatically log into complex dashboards on your behalf.

### Desktop Integration
- **Global Hotkeys:** 
  - `Ctrl + Shift + A`: Toggle the AI Assistant window instantly.
  - `Ctrl + Shift + F`: Trigger code fixing or context-aware actions based on your clipboard or screen.
- **Clipboard Monitoring:** Automatically detects when you copy code and makes it available to the AI.
- **Screen Capture & OCR:** Built-in native screen capture that uses Windows Native OCR (`Windows.Media.Ocr`) to extract text and send it to the AI.
- **Screen Share Privacy:** Advanced settings overlay utilizing Windows Display Affinity to completely hide the application window from screen recording software (OBS, Teams, Zoom).

## Prerequisites

To run and build this project, you will need:
- [Windows 10 or Windows 11](https://www.microsoft.com/windows)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Python 3.9+ (For the advanced Browser Automation Agent)
- Chrome Browser (For Browser Agent)
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (Included in modern Windows installations).

## Setup Process

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/veerachakradharpampanaboyina/Ai-Assistant.git
   cd Ai-Assistant
   ```

2. **Install Python Dependencies:**
   (Required for browser automation & media parsing)
   ```bash
   pip install playwright python-dotenv PyAutoGUI SpeechRecognition pyttsx3 gTTS
   playwright install chromium
   ```

3. **Restore & Build:**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the Application:**
   ```bash
   dotnet run
   ```

## Technologies Used

- **C# / .NET 8.0**
- **WPF (Windows Presentation Foundation) & WPF-UI**
- **Monaco Editor** (For code editing interface)
- **WebView2** (For embedding web-based AI chats)
- **Python / Playwright** (For Browser Agent integration via CDP)
- **NHotkey** & **Windows Native OCR**

## License

This project is open-source. Please see the LICENSE file for more details.
