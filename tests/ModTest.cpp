#include "../windhawk/desktop-icons-toggle.wh.cpp"
#include <cstdio>
int main() {
    int before = Visibility();
    if (before < 0) { std::puts("FAIL: interactive desktop required"); return 1; }
    module = GetModuleHandleW(nullptr);
    stopEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    if (!stopEvent) return 1;
    worker = CreateThread(nullptr, 0, Run, nullptr, 0, nullptr);
    bool ok = worker != nullptr;
    for (int i = 0; i < 50 && !workerWindow.load(); ++i) Sleep(100);
    HWND h = workerWindow.load();
    if (!h) ok = false;
    if (h) {
        Sleep(200);
        PostMessageW(h, WM_HOTKEY, 1, 0);
        for (int i = 0; i < 30 && Visibility() == before; ++i) Sleep(100);
        if (Visibility() != !before) ok = false;
        // Unloading must restore the initial state after our toggle.
    }
    Wh_ModUninit();
    if (Visibility() != before || FindWindowW(kClass, nullptr)) ok = false;
    if (Visibility() != before) SetVisible(before != 0);
    std::puts(ok ? "PASS: tray worker, toggle, unload, visibility restoration" : "FAIL: mod lifecycle");
    return ok ? 0 : 1;
}
