// A tiny console launcher. The UTF-8 <exe>.brew-shim sidecar holds the real exe path.
#include <windows.h>
#include <wchar.h>
#include <stdio.h>

static BOOL WINAPI ignore_ctrl(DWORD event) { return event == CTRL_C_EVENT || event == CTRL_BREAK_EVENT; }

int wmain(void) {
    wchar_t self[32768], sidecar[32768], target[32768];
    DWORD length = GetModuleFileNameW(NULL, self, 32768);
    if (!length || length >= 32756) return 126;
    swprintf(sidecar, 32768, L"%ls.brew-shim", self);
    HANDLE file = CreateFileW(sidecar, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    if (file == INVALID_HANDLE_VALUE) { fwprintf(stderr, L"brew shim: cannot read %ls\n", sidecar); return 126; }
    char bytes[131072]; DWORD count;
    BOOL read = ReadFile(file, bytes, sizeof(bytes), &count, NULL);
    CloseHandle(file);
    if (!read || !count || count >= sizeof(bytes)) return 126;
    char *newline = memchr(bytes, '\n', count);
    DWORD targetCount = newline ? (DWORD)(newline - bytes) : count;
    int chars = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, bytes, targetCount, target, 32767);
    if (!chars) return 126;
    target[chars] = 0;
    wchar_t prefixArgs[32768] = L"";
    if (newline) {
        int remaining = (int)(count - targetCount - 1);
        int n = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, newline + 1, remaining, prefixArgs, 32767);
        if (!n && remaining) return 126;
        prefixArgs[n] = 0;
    }
    // DLL resolution and script subprocesses also need their package's bin directory.
    wchar_t environmentPath[32768], oldPath[32768] = L"", targetDir[32768];
    wcscpy(targetDir, target);
    wchar_t *separator = wcsrchr(targetDir, L'\\');
    if (separator) *separator = 0;
    GetEnvironmentVariableW(L"PATH", oldPath, 32768);
    if (wcslen(targetDir) + wcslen(oldPath) + 2 < 32768) {
        swprintf(environmentPath, 32768, L"%ls;%ls", targetDir, oldPath);
        SetEnvironmentVariableW(L"PATH", environmentPath);
    }
    // Keep the original argument text, including quoting and backslashes.
    wchar_t *args = GetCommandLineW();
    BOOL quoted = FALSE;
    while (*args) {
        if (*args == L'"') quoted = !quoted;
        else if (!quoted && (*args == L' ' || *args == L'\t')) break;
        args++;
    }
    size_t size = wcslen(target) + wcslen(prefixArgs) + wcslen(args) + 5;
    wchar_t *command = HeapAlloc(GetProcessHeap(), 0, size * sizeof(wchar_t));
    if (!command) return 126;
    swprintf(command, size, L"\"%ls\" %ls%ls", target, prefixArgs, args);
    STARTUPINFOW startup = {0}; startup.cb = sizeof(startup);
    PROCESS_INFORMATION process = {0};
    SetConsoleCtrlHandler(ignore_ctrl, TRUE);
    BOOL ok = CreateProcessW(target, command, NULL, NULL, TRUE, 0, NULL, NULL, &startup, &process);
    HeapFree(GetProcessHeap(), 0, command);
    if (!ok) { fwprintf(stderr, L"brew shim: cannot launch %ls (error %lu)\n", target, GetLastError()); return 126; }
    CloseHandle(process.hThread);
    WaitForSingleObject(process.hProcess, INFINITE);
    DWORD code = 126; GetExitCodeProcess(process.hProcess, &code);
    CloseHandle(process.hProcess);
    return (int)code;
}
