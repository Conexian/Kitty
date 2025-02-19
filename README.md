# KittyConsole

KittyConsole is a simple console-based application written in C#. It's a learning project aimed at improving C# skills and experimenting with various console commands. While not intended to be particularly useful, the source code might serve as a helpful reference for others.

## Features & Commands

### Basic Commands:
- `help` | `?`                      - Show this help.
- `clear`                         - Clear the console screen.
- `meow <message>`                - Print a message.
- `art`                           - Display a cool kitty art.
- `beep <frequency> <duration>`   - Emit a beep sound at a specified frequency (Hz) for the given duration (ms).

### File & Encoding:
- `cat <file> [linesNumbers (true/false)] [maxLines]` - Print a file with optional line numbers and line limit.
- `open <path>`                   - Open a file or directory.
- `foldersize <path> [recursive (true/false)] [listFiles (true/false)]` - Show folder size.
- `batchrename <directoryPath> <searchPattern> <replacePattern> [recursive]` - Batch rename files in a directory.
- `hash <file> <algorithm>`       - Compute file hash using MD5, SHA1, or SHA256.
- `base64 <encode|decode> <text>` - Base64 encode or decode text.
- `encrypt <text>`                - Encrypt the provided text.
- `decrypt <text> <key> <iv>`     - Decrypt the provided text using a key and IV.

### Network & System:
- `sysinfo`                       - Show basic system information.
- `publicip`                      - Display your public IP address.
- `ping <address> [count] [delay] [size]` - Ping an address with optional count, delay, and packet size.
- `portscan <target> <start> <end>` - Scan target ports within the specified range.
- `commonports`                   - List common ports.
- `netstat [tcp|udp|all]`         - Show active network connections.
- `download <packageName> [silent (true/false)]` - Download a package using Chocolatey.

### Advanced:
- `readfile <name> <extension>`   - Read an embedded resource file.

## Usage
1. Download the source code from the repository.
2. Open `Kitty.sln` in Visual Studio.
3. Build and run the project.

OR
Download the release exe.

## Development & Updates
This project will receive frequent updates until I completely run out of ideas. Feel free to contribute or suggest new features!

![image](https://github.com/user-attachments/assets/3b78b93f-56a6-41bb-adbd-899c822a64e1)
