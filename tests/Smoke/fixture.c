#include <stdio.h>
#include <wchar.h>
__declspec(dllimport) int fixture_answer(void);
int wmain(int argc, wchar_t **argv) {
    for (int i = 1; i < argc; i++) wprintf(L"%ls\n", argv[i]);
    wchar_t input[128];
    if (fgetws(input, 128, stdin)) wprintf(L"%ls", input);
    return fixture_answer();
}
