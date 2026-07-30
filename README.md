# AI Assistant

AI Assistant is a powerful desktop application built with WPF and .NET 8 that brings your favorite AI models directly to your desktop. It integrates seamlessly with your workflow through global hotkeys, clipboard monitoring, screen capture, and built-in OCR capabilities.

## Features

- **Multi-AI Support:** Access popular AI models directly through an integrated WebView2 browser (ChatGPT, Gemini, Claude, DeepSeek, Arena AI, Z.ai).
- **Autonomous AI Agent Mode:** Fully automated background terminal and filesystem access directly from the chat prompt. The AI can execute multi-step plans, build projects, run scripts, and fix code without manual intervention.
- **1 Million Token Workspace Context:** Automatically aggregates and ingests up to 1 million tokens of your workspace's file contents into the AI's prompt for perfect codebase memory and precision editing.
- **Screen Share Privacy:** Built-in settings overlay utilizing Windows Display Affinity layer concepts to completely hide the application window from screen recording software (OBS, Teams, Zoom, etc.).
- **Agent Execution Status:** Real-time animated status panel indicating when the AI is executing background terminal commands.
- **Hang-Free Optimization:** Fully asynchronous file I/O and process execution ensures the user interface remains snappy and never freezes during complex agent operations.
- **Global Hotkeys:** 
  - `Ctrl + Shift + A`: Toggle the AI Assistant window instantly.
  - `Ctrl + Shift + F`: Trigger code fixing or context-aware actions based on your clipboard or screen.
- **Clipboard Monitoring:** Automatically detects when you copy code or text and makes it available for the AI to process.
- **Screen Capture & OCR:** Built-in screen capture tool that uses Windows native OCR (`Windows.Media.Ocr`) to extract text from your screen and send it to the AI.
- **Fluent UI:** Modern, responsive design utilizing WPF-UI for a native Windows 11 look and feel.

## Prerequisites

To run and build this project, you will need:
- [Windows 10 or Windows 11](https://www.microsoft.com/windows)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recommended) or VS Code.
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (Included in most modern Windows installations by default).

## Setup Process

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/veerachakradharpampanaboyina/Ai-Assistant.git
   cd Ai-Assistant
   ```

2. **Restore Dependencies:**
   The project uses NuGet for package management. Restore the required packages (like `Microsoft.Web.WebView2`, `NHotkey.Wpf`, `WPF-UI`):
   ```bash
   dotnet restore
   ```

3. **Build the Project:**
   ```bash
   dotnet build
   ```

4. **Run the Application:**
   ```bash
   dotnet run
   ```
   *Alternatively, you can open `AIAssistant.csproj` in Visual Studio 2022 and press `F5` to build and launch the application.*

## Working Process

1. **Launch the App:** Upon starting, the app initializes the WebView2 component and loads the default AI provider (e.g., ChatGPT).
2. **Global Shortcuts:** You can minimize the app and continue your work. Press `Ctrl + Shift + A` at any time to bring the AI Assistant into focus.
3. **Clipboard Integration:** Whenever you copy text, the built-in clipboard monitor intercepts the copied content, making it instantly available to paste into your AI prompt.
4. **Screen Capture & OCR:** Use the capture feature to take a snippet of your screen. The application leverages Windows OCR to extract text from the image, which can then be used to query the AI (e.g., reading error messages from images or videos).
5. **Switching Providers:** Use the dropdown menu in the interface to seamlessly switch between ChatGPT, Gemini, Claude, and other providers without leaving the app.

## Technologies Used

- **C# / .NET 8.0**
- **WPF (Windows Presentation Foundation)**
- **WPF-UI** (For modern fluent design elements)
- **WebView2** (For embedding web-based AI chats)
- **NHotkey** (For global system-wide shortcuts)
- **Windows Native OCR** (For text extraction)

## License

This project is open-source. Please see the LICENSE file for more details.
