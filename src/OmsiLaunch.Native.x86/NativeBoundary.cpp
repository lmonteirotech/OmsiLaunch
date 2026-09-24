#include <windows.h>
#include <wchar.h>
#include <stdint.h>
#include <new>
#include <d3d9.h>

struct CanonicalMapDiagnostics {
    uintptr_t form;
    uintptr_t formVmt;
    uintptr_t stringArray;
    uintptr_t companionArray;
    int stringCount;
    int companionCount;
    int grundorfIndex;
    uintptr_t sourceData;
    int sourceLength;
    uintptr_t destinationSlot;
    uintptr_t destinationVariable;
    uintptr_t destinationBeforeData;
    int destinationBeforeLength;
    uintptr_t destinationAfterData;
    int destinationAfterLength;
    int destinationCodePage;
    int destinationElementSize;
    int helperCalled;
    int readbackValid;
    wchar_t sourceText[512];
    wchar_t destinationBeforeText[512];
    wchar_t destinationAfterText[512];
    int scanStage;
    int failedIndex;
    int readableEntryCount;
    wchar_t entrySummary[4096];
    unsigned int sourceHeader[4];
    unsigned char sourceBytes[64];
    unsigned int destinationBeforeHeader[4];
    unsigned char destinationBeforeBytes[64];
    unsigned int destinationAfterHeader[4];
    unsigned char destinationAfterBytes[64];
};

struct EntrypointDiagnostics {
    int status;
    int preflightStatus;
    int newSituationFingerprintOk;
    int setposGlobalReadable;
    int setposSlotReadable;
    int setposObjectReadable;
    int invocationCount;
    int originalInvoked;
    int formShowCalled;
    int buttonCalled;
    int presentedCount;
    int presentedIndex;
    int requestedPresentedIndex;
    int confirmedPresentedIndex;
    int itemIndexBefore;
    int itemIndexAfter;
    int identityDecodeStatus;
    int rawCount;
    int rawIndex;
    int rawDiagnosticStatus;
    int slotRestored;
    uintptr_t setposSelf;
    uintptr_t setposVmt;
    uintptr_t slotAddress;
    uintptr_t originalTarget;
    uintptr_t listBox;
    uintptr_t items;
    uintptr_t rawArrayHolder;
    uintptr_t rawArray;
    float map124;
    float map128;
    float map12C;
    float map130;
    float map134;
    float map138;
    float map13C;
    float map140;
    wchar_t presentedName[512];
    wchar_t rawName[512];
};

// Internal-only diagnostic contract for the profiled MakeVehicle primitive.
// It is intentionally pointer-bearing because it never crosses the public API.
struct NativeMakeVehicleDiagnostics {
    int status;
    int rawReturn;
    int beforeCount;
    int afterCount;
    int deltaCount;
    int copyReturn;
    uintptr_t progMan;
    uintptr_t roadVehicles;
    uintptr_t roadVehicleTypes;
    uintptr_t criticalSection;
    uintptr_t temporaryList;
    uintptr_t createdVehicle;
    uintptr_t createdVehicleVmt;
};

struct NativeD3DStatus {
    int status;
    uintptr_t slot;
    uintptr_t candidate;
    long queryInterfaceResult;
    long cooperativeLevelResult;
    unsigned long executionThreadId;
    unsigned long ownedDeviceReferences;
    unsigned long lifecycleState;
    unsigned long lifecycleTransition;
    unsigned long deviceGeneration;
    unsigned long liveTextureCount;
    unsigned long resetHookInstalled;
    unsigned long lastResetThreadId;
};

struct NativeD3DTextureResult {
    int status;
    long operationResult;
    unsigned __int64 handle;
    unsigned long lifecycleState;
    unsigned long deviceGeneration;
    unsigned long textureState;
    unsigned long width;
    unsigned long height;
    unsigned long format;
    unsigned long levels;
    unsigned long level;
    unsigned long levelWidth;
    unsigned long levelHeight;
    unsigned long executionThreadId;
};

namespace {
constexpr uintptr_t OmsiBase = 0x00400000;
constexpr uintptr_t OmsiEnd = 0x00C2B000;
// OmsiGlobals.Map for Omsi23004_692EBFBF. Keep this synchronized with the
// managed build profile; 0x00859D94 is not an OmsiMap slot in this build.
constexpr uintptr_t OmsiMapGlobal = 0x00861588;

bool Readable(const void* pointer, size_t bytes);
bool PlausibleOmsiObject(void* object);
bool ReadUnicodeString(wchar_t* value, wchar_t* output, int capacity);
void DirectSetPosStubBody(void* self);
void HeadlessStartShowModalBody(void* self);
void HeadlessStartShowThunkBody(void* self, unsigned char visibility);
void HeadlessStartShowModalStub();
void HeadlessStartShowThunk();

// Copied in behavior, not architecture, from the pinned OmsiHook invoker's
// BorlandFastCall helper. The MakeVehicle ABI was runtime-reconciled for this
// exact executable in MakeVehicle-Reconciliation-002.
__declspec(naked) int __cdecl BorlandFastCall(int function, int registerArguments, int totalArguments, ...) {
    __asm {
        push ebp
        mov ebp, esp
        sub esp, 10h
        push ebx
        push esi
        push edi
        mov ebx, [ebp+10h]
        sub ebx, [ebp+0Ch]
        mov esi, [ebp+10h]
        lea esi, [ebp+10h+esi*4]
    copy_stack_arguments:
        cmp ebx, 0
        jz load_register_arguments
        push [esi]
        sub esi, 4
        dec ebx
        jmp copy_stack_arguments
    load_register_arguments:
        mov ebx, [ebp+0Ch]
        cmp ebx, 1
        jl invoke_target
        mov eax, [ebp+14h]
        cmp ebx, 2
        jl invoke_target
        mov edx, [ebp+18h]
        cmp ebx, 3
        jl invoke_target
        mov ecx, [ebp+1Ch]
    invoke_target:
        call dword ptr [ebp+8]
        pop edi
        pop esi
        pop ebx
        add esp, 10h
        mov esp, ebp
        pop ebp
        ret
    }
}

bool Readable(const void* pointer, size_t bytes) {
    if (pointer == nullptr || bytes == 0) return false;
    MEMORY_BASIC_INFORMATION memory{};
    if (VirtualQuery(pointer, &memory, sizeof(memory)) != sizeof(memory)) return false;
    if (memory.State != MEM_COMMIT || (memory.Protect & (PAGE_GUARD | PAGE_NOACCESS)) != 0) return false;
    const auto start = reinterpret_cast<uintptr_t>(pointer);
    const auto end = start + bytes;
    const auto regionEnd = reinterpret_cast<uintptr_t>(memory.BaseAddress) + memory.RegionSize;
    return end >= start && end <= regionEnd;
}

volatile uintptr_t gSetposSlot = 0;
volatile uintptr_t gSetposOriginal = 0;
volatile EntrypointDiagnostics* gEntrypointDiagnostics = nullptr;
volatile int gEntrypointActive = 0;
volatile int gEntrypointInvocationCount = 0;
volatile int gEntrypointSuccess = 0;
volatile int gRequestedPresentedIndex = 1;
const wchar_t* gRequestedEntrypointIdentity = nullptr;
EntrypointDiagnostics gLastEntrypointDiagnostics{};

constexpr uintptr_t StartFormSlot = 0x008612F8;
constexpr uintptr_t StartFormVmt = 0x006759C8;
constexpr uintptr_t StartShowModalSlot = 0x00675AE8;
constexpr uintptr_t StartShowModalTarget = 0x00551D18;
constexpr uintptr_t ShowModalVisualCall = 0x00551C2F;
constexpr uintptr_t ShowModalVisualTarget = 0x0054CCA8;
constexpr uintptr_t StartFormShow = 0x00676FC4;
constexpr uintptr_t StartFormHide = 0x006785B4;
constexpr uintptr_t StartButtonDriverNewClick = 0x00676884;
constexpr uintptr_t StartSituationSelectionChanged = 0x00679250;
constexpr uintptr_t StartButton1Click = 0x006768F8;
constexpr uintptr_t RadioSetChecked = 0x0059BE58;
constexpr size_t StartModeNewMapOffset = 0x434;
constexpr size_t StartModeSelectedMapOffset = 0x438;
constexpr size_t StartModeSavedSituationOffset = 0x43C;
constexpr size_t StartSituationControlOffset = 0x3E4;
constexpr size_t StartSituationCollectionOffset = 0x3F8;
constexpr size_t StartModalResultOffset = 0x2C0;
constexpr size_t ItemIndexSetterVmtOffset = 0xFC;
volatile int gHeadlessStartPending = 0;
volatile int gHeadlessStartConsumed = 0;
volatile int gHeadlessFormShowExecuted = 0;
volatile int gHeadlessStartInstalled = 0;
volatile int gHeadlessModalResult = 0;

bool ReadStartForm(void** output) {
    if (output == nullptr) return false;
    *output = nullptr;
    auto slot = reinterpret_cast<void**>(StartFormSlot);
    if (!Readable(slot, sizeof(void*)) || *slot == nullptr || !Readable(*slot, sizeof(void*))) return false;
    auto form = *slot;
    if (*reinterpret_cast<uintptr_t*>(form) != StartFormVmt) return false;
    *output = form;
    return true;
}

bool ResolveSituationIndex(void* form, const wchar_t* requestedSituation, int* indexOutput) {
    if (form == nullptr || requestedSituation == nullptr || *requestedSituation == L'\0' || indexOutput == nullptr) return false;
    *indexOutput = -1;
    auto situations = *reinterpret_cast<wchar_t***>(reinterpret_cast<unsigned char*>(form) + StartSituationCollectionOffset);
    if (situations == nullptr || !Readable(situations - 1, sizeof(int))) return false;
    const int count = *(reinterpret_cast<int*>(situations) - 1);
    if (count <= 0 || count > 4096 || !Readable(situations, static_cast<size_t>(count) * sizeof(wchar_t*))) return false;
    for (int index = 0; index < count; ++index) {
        wchar_t decoded[512]{};
        if (ReadUnicodeString(situations[index], decoded, static_cast<int>(_countof(decoded))) && _wcsicmp(decoded, requestedSituation) == 0) {
            *indexOutput = index;
            return true;
        }
    }
    return false;
}

bool SetRadioChecked(void* radio, bool checked) {
    if (radio == nullptr || !Readable(radio, sizeof(uintptr_t))) return false;
    __asm {
        mov eax, radio
        mov dl, checked
        mov ecx, RadioSetChecked
        call ecx
    }
    return true;
}

bool SetSituationItemIndex(void* selector, int index) {
    if (selector == nullptr || index < 0 || !Readable(selector, sizeof(uintptr_t))) return false;
    const auto vmt = *reinterpret_cast<uintptr_t*>(selector);
    if (!Readable(reinterpret_cast<void*>(vmt + ItemIndexSetterVmtOffset), sizeof(uintptr_t))) return false;
    const auto setter = *reinterpret_cast<uintptr_t*>(vmt + ItemIndexSetterVmtOffset);
    if (setter < OmsiBase || setter >= OmsiEnd) return false;
    __asm {
        mov eax, selector
        mov edx, index
        mov ecx, setter
        call ecx
    }
    return true;
}

void InvokeStartModeChanged(void* form) {
    __asm {
        mov eax, form
        mov ecx, StartButtonDriverNewClick
        call ecx
    }
}

void InvokeSituationSelectionChanged(void* form) {
    __asm {
        mov eax, form
        mov edx, form
        mov ecx, StartSituationSelectionChanged
        call ecx
    }
}

void InvokeStartButton1Click(void* form) {
    __asm {
        mov eax, form
        mov ecx, StartButton1Click
        call ecx
    }
}

bool IsKnownNewSituation() {
    static const unsigned char expected[] = {
        0x80, 0x7D, 0x08, 0x00, 0x74, 0x3B,
        0xA1, 0x34, 0x8F, 0x85, 0x00, 0x8B, 0x00,
        0xE8, 0xF5, 0x46, 0xD2, 0xFF, 0x85, 0xC0,
        0x7E, 0x2B
    };
    return memcmp(reinterpret_cast<const void*>(0x006E60F5), expected, sizeof(expected)) == 0;
}

bool SetVmtSlot(uintptr_t slot, uintptr_t value) {
    DWORD oldProtection = 0;
    if (!VirtualProtect(reinterpret_cast<void*>(slot), sizeof(uintptr_t), PAGE_READWRITE, &oldProtection)) return false;
    *reinterpret_cast<volatile uintptr_t*>(slot) = value;
    FlushInstructionCache(GetCurrentProcess(), reinterpret_cast<void*>(slot), sizeof(uintptr_t));
    DWORD ignored = 0;
    VirtualProtect(reinterpret_cast<void*>(slot), sizeof(uintptr_t), oldProtection, &ignored);
    return true;
}

bool SetCodeBytes(uintptr_t address, const unsigned char* bytes, size_t size) {
    if (bytes == nullptr || size == 0 || !Readable(reinterpret_cast<void*>(address), size)) return false;
    DWORD oldProtection = 0;
    if (!VirtualProtect(reinterpret_cast<void*>(address), size, PAGE_EXECUTE_READWRITE, &oldProtection)) return false;
    memcpy(reinterpret_cast<void*>(address), bytes, size);
    FlushInstructionCache(GetCurrentProcess(), reinterpret_cast<void*>(address), size);
    DWORD ignored = 0;
    VirtualProtect(reinterpret_cast<void*>(address), size, oldProtection, &ignored);
    return true;
}

bool RestoreHeadlessVisualCall() {
    static const unsigned char original[] = { 0xE8, 0x74, 0xB0, 0xFF, 0xFF };
    return SetCodeBytes(ShowModalVisualCall, original, sizeof(original));
}

constexpr uintptr_t DownloadInternetTextures = 0x006E7164;
static const unsigned char DownloadInternetTexturesPrologue[] = { 0x55, 0x8B, 0xEC, 0xB9, 0x15 };
static volatile LONG gInternetTexturesSuppressed = 0;

bool InstallInternetTexturesSuppression() {
    if (!Readable(reinterpret_cast<void*>(DownloadInternetTextures), sizeof(DownloadInternetTexturesPrologue)) ||
        memcmp(reinterpret_cast<const void*>(DownloadInternetTextures), DownloadInternetTexturesPrologue, sizeof(DownloadInternetTexturesPrologue)) != 0) return false;
    const unsigned char replacement[] = { 0xC3, 0x90, 0x90, 0x90, 0x90 };
    if (!SetCodeBytes(DownloadInternetTextures, replacement, sizeof(replacement))) return false;
    gInternetTexturesSuppressed = 1;
    return true;
}

void RestoreInternetTexturesSuppression() {
    if (InterlockedExchange(&gInternetTexturesSuppressed, 0) != 0)
        SetCodeBytes(DownloadInternetTextures, DownloadInternetTexturesPrologue, sizeof(DownloadInternetTexturesPrologue));
}

bool InstallHeadlessVisualCall() {
    static const unsigned char original[] = { 0xE8, 0x74, 0xB0, 0xFF, 0xFF };
    if (!Readable(reinterpret_cast<void*>(ShowModalVisualCall), sizeof(original)) ||
        memcmp(reinterpret_cast<const void*>(ShowModalVisualCall), original, sizeof(original)) != 0) return false;
    unsigned char replacement[sizeof(original)]{};
    replacement[0] = 0xE8;
    const auto next = ShowModalVisualCall + sizeof(replacement);
    const auto target = reinterpret_cast<uintptr_t>(&HeadlessStartShowThunk);
    const LONG relative = static_cast<LONG>(target - next);
    memcpy(replacement + 1, &relative, sizeof(relative));
    return SetCodeBytes(ShowModalVisualCall, replacement, sizeof(replacement));
}

void RestoreHeadlessStartHooks() {
    SetVmtSlot(StartShowModalSlot, StartShowModalTarget);
    RestoreHeadlessVisualCall();
    gHeadlessStartPending = 0;
    gHeadlessStartConsumed = 0;
    gHeadlessStartInstalled = 0;
    gHeadlessFormShowExecuted = 0;
}

bool HeadlessFingerprint(void** form) {
    if (!ReadStartForm(form)) return false;
    if (!Readable(reinterpret_cast<void*>(StartShowModalSlot), sizeof(uintptr_t)) ||
        *reinterpret_cast<uintptr_t*>(StartShowModalSlot) != StartShowModalTarget) return false;
    static const unsigned char visualCall[] = { 0xE8, 0x74, 0xB0, 0xFF, 0xFF };
    return Readable(reinterpret_cast<void*>(ShowModalVisualCall), sizeof(visualCall)) &&
        memcmp(reinterpret_cast<const void*>(ShowModalVisualCall), visualCall, sizeof(visualCall)) == 0;
}

// Current PluginRuntime invokes only closed, profile-owned native operations.
// Build validation is intentionally separate from RuntimePlatform selection.
extern "C" __declspec(dllexport) int __cdecl NativeValidateBuild() {
    void* form = nullptr;
    return HeadlessFingerprint(&form) ? 1 : 0;
}

extern "C" __declspec(dllexport) int __cdecl NativeSuppressInternetTextures() {
    return NativeValidateBuild() && InstallInternetTexturesSuppression() ? 1 : 0;
}

extern "C" __declspec(dllexport) void __cdecl NativeRestoreInternetTextures() { RestoreInternetTexturesSuppression(); }

extern "C" __declspec(dllexport) void __cdecl NativeInstallMainThreadGateway() {
    // PluginRuntime owns scheduling. The native layer has no generic callback
    // registry and performs no work until a profile-owned semantic operation is queued.
}

extern "C" __declspec(dllexport) int __cdecl NativeArmHeadlessStart() {
    void* form = nullptr;
    if (!HeadlessFingerprint(&form)) return 0;
    if (!SetVmtSlot(StartShowModalSlot, reinterpret_cast<uintptr_t>(&HeadlessStartShowModalStub))) return 0;
    gHeadlessStartPending = 1;
    gHeadlessStartConsumed = 0;
    gHeadlessFormShowExecuted = 0;
    gHeadlessStartInstalled = 1;
    return 1;
}

void __declspec(naked) HeadlessStartShowModalStub() {
    __asm {
        pushad
        push eax
        call HeadlessStartShowModalBody
        add esp, 4
        popad
        mov eax, gHeadlessModalResult
        ret
    }
}

void __declspec(naked) HeadlessStartShowThunk() {
    __asm {
        pushad
        push edx
        push eax
        call HeadlessStartShowThunkBody
        add esp, 8
        popad
        ret
    }
}

void HeadlessStartShowThunkBody(void* self, unsigned char visibility) {
    void* current = nullptr;
    if (!gHeadlessStartConsumed || !gHeadlessStartPending || visibility != 1 || !ReadStartForm(&current) || current != self) {
        __asm {
            mov eax, self
            mov dl, visibility
            mov ecx, 0054CCA8h
            call ecx
        }
        return;
    }

    // Restore only after the live Tform_start has consumed the one-shot hook.
    RestoreHeadlessVisualCall();
    gHeadlessStartPending = 0;
    __asm {
        mov eax, self
        mov edx, self
        mov ecx, 00676FC4h
        call ecx
    }
    gHeadlessFormShowExecuted = 1;
}

void HeadlessStartShowModalBody(void* self) {
    void* current = nullptr;
    if (!gHeadlessStartInstalled || !ReadStartForm(&current) || current != self) {
        gHeadlessModalResult = 2;
        RestoreHeadlessStartHooks();
        return;
    }

    // The VMT hook is one-shot; arm the visibility call only inside this
    // exact Tform_start ShowModal invocation.
    SetVmtSlot(StartShowModalSlot, StartShowModalTarget);
    gHeadlessStartConsumed = 1;
    if (!InstallHeadlessVisualCall()) {
        gHeadlessStartConsumed = 0;
        gHeadlessStartInstalled = 0;
        gHeadlessStartPending = 0;
        gHeadlessModalResult = 2;
        return;
    }
    __asm {
        mov eax, self
        mov ecx, 00551D18h
        call ecx
        mov gHeadlessModalResult, eax
    }

    if (gHeadlessFormShowExecuted) {
        __asm {
            mov eax, self
            mov edx, self
            mov ecx, StartFormHide
            call ecx
        }
        gHeadlessFormShowExecuted = 0;
    }
    RestoreHeadlessStartHooks();
}

void RestoreSetposSlot() {
    if (gSetposSlot != 0 && gSetposOriginal != 0)
        SetVmtSlot(gSetposSlot, gSetposOriginal);
}

void CallFormShow(void* self) {
    __asm {
        mov eax, self
        mov edx, self
        mov ecx, 0067A6F0h
        call ecx
    }
}

int ListBoxItemsCount(void* listBox, void** itemsOut) {
    int result = -1;
    if (itemsOut == nullptr) return result;
    *itemsOut = nullptr;
    if (listBox == nullptr || !Readable(listBox, 0x28C)) return result;
    auto items = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(listBox) + 0x288);
    if (!PlausibleOmsiObject(items)) return result;
    *itemsOut = items;
    __asm {
        mov eax, items
        mov edx, [eax]
        call dword ptr [edx+14h]
        mov result, eax
    }
    return result;
}

int ListBoxItemIndex(void* listBox) {
    int result = -1;
    __asm {
        mov eax, listBox
        mov edx, [eax]
        call dword ptr [edx+0F8h]
        mov result, eax
    }
    return result;
}

bool SetListBoxItemIndex(void* listBox, int index) {
    __asm {
        mov eax, listBox
        mov edx, index
        mov ecx, [eax]
        call dword ptr [ecx+0FCh]
    }
    return true;
}

bool GetListBoxItemText(void* listBox, int index, wchar_t** text) {
    if (text == nullptr) return false;
    *text = nullptr;
    auto items = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(listBox) + 0x288);
    if (!PlausibleOmsiObject(items)) return false;
    __asm {
        mov eax, items
        mov edx, index
        lea ecx, text
        mov edi, [eax]
        call dword ptr [edi+0Ch]
    }
    return *text != nullptr;
}

bool ApplySelectedEntrypoint(void* setpos, EntrypointDiagnostics* diagnostics, int requestedIndex) {
    if (setpos == nullptr || diagnostics == nullptr) return false;
    diagnostics->setposSelf = reinterpret_cast<uintptr_t>(setpos);
    diagnostics->setposVmt = *reinterpret_cast<uintptr_t*>(setpos);
    if (diagnostics->setposVmt != 0x00679EB8 || *reinterpret_cast<unsigned char*>(reinterpret_cast<unsigned char*>(setpos) + 0x3A0) != 1) {
        diagnostics->status = 2;
        return false;
    }

    CallFormShow(setpos);
    diagnostics->formShowCalled = 1;
    auto listBox = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(setpos) + 0x398);
    diagnostics->listBox = reinterpret_cast<uintptr_t>(listBox);
    if (!PlausibleOmsiObject(listBox)) {
        diagnostics->status = 3;
        return false;
    }
    void* items = nullptr;
    const int presentedCount = ListBoxItemsCount(listBox, &items);
    diagnostics->items = reinterpret_cast<uintptr_t>(items);
    if (items == nullptr) {
        diagnostics->status = 4;
        return false;
    }
    diagnostics->presentedCount = presentedCount;
    diagnostics->presentedIndex = requestedIndex;
    diagnostics->requestedPresentedIndex = requestedIndex;
    if (gRequestedEntrypointIdentity != nullptr && *gRequestedEntrypointIdentity != L'\0') {
        int resolvedIndex = -1;
        for (int index = 0; index < presentedCount; ++index) {
            wchar_t* text = nullptr;
            if (!GetListBoxItemText(listBox, index, &text) || text == nullptr) continue;
            if (lstrcmpiW(text, gRequestedEntrypointIdentity) != 0) continue;
            if (resolvedIndex >= 0) {
                diagnostics->status = 19; // Identity is ambiguous in this map's presented list.
                return false;
            }
            resolvedIndex = index;
        }
        if (resolvedIndex < 0) {
            diagnostics->status = 20; // Identity is absent from the presented list.
            return false;
        }
        requestedIndex = resolvedIndex;
        diagnostics->presentedIndex = resolvedIndex;
        diagnostics->requestedPresentedIndex = resolvedIndex;
    }
    if (requestedIndex < 0 || requestedIndex >= presentedCount) {
        diagnostics->status = 4;
        return false;
    }

    wchar_t* presentedName = nullptr;
    if (GetListBoxItemText(listBox, requestedIndex, &presentedName) && presentedName != nullptr)
        wcsncpy_s(diagnostics->presentedName, _countof(diagnostics->presentedName), presentedName, _TRUNCATE);

    diagnostics->itemIndexBefore = ListBoxItemIndex(listBox);
    SetListBoxItemIndex(listBox, requestedIndex);
    diagnostics->itemIndexAfter = ListBoxItemIndex(listBox);
    diagnostics->confirmedPresentedIndex = diagnostics->itemIndexAfter;
    if (diagnostics->itemIndexAfter != requestedIndex) {
        diagnostics->status = 15;
        return false;
    }
    // The VMT item-text getter is not stable across all VCL listbox backing
    // implementations. This profiled form field is the selected Unicode text.
    if (diagnostics->presentedName[0] == L'\0') {
        auto selectedText = *reinterpret_cast<wchar_t**>(reinterpret_cast<unsigned char*>(setpos) + 0x3A4);
        ReadUnicodeString(selectedText, diagnostics->presentedName, _countof(diagnostics->presentedName));
    }

    auto button = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(setpos) + 0x390);
    if (!PlausibleOmsiObject(button)) {
        diagnostics->status = 11;
        return false;
    }
    __asm {
        mov eax, setpos
        mov edx, button
        mov ecx, 0067A2E8h
        call ecx
    }
    diagnostics->buttonCalled = 1;
    auto rawIndexGlobal = reinterpret_cast<int**>(0x00858A50);
    if (!Readable(rawIndexGlobal, sizeof(void*)) || *rawIndexGlobal == nullptr || !Readable(*rawIndexGlobal, sizeof(int))) {
        diagnostics->status = 12;
        return false;
    }
    diagnostics->rawIndex = **rawIndexGlobal;
    auto rawGlobal = reinterpret_cast<void**>(0x00858F34);
    auto rawHolder = (Readable(rawGlobal, sizeof(void*)) ? *rawGlobal : nullptr);
    auto raw = (rawHolder != nullptr && Readable(rawHolder, sizeof(void*)))
        ? *reinterpret_cast<void**>(rawHolder)
        : nullptr;
    diagnostics->rawArrayHolder = reinterpret_cast<uintptr_t>(rawHolder);
    if (raw == nullptr || !Readable(reinterpret_cast<unsigned char*>(raw) - 4, sizeof(int))) {
        diagnostics->rawDiagnosticStatus = 1;
    } else {
        const int rawCount = *reinterpret_cast<int*>(reinterpret_cast<unsigned char*>(raw) - 4);
        diagnostics->rawArray = reinterpret_cast<uintptr_t>(raw);
        diagnostics->rawCount = rawCount;
        if (diagnostics->rawIndex < 0 || diagnostics->rawIndex >= rawCount || diagnostics->rawIndex >= 4096) {
            diagnostics->rawDiagnosticStatus = 2;
        } else {
            auto rawRecord = reinterpret_cast<unsigned char*>(raw) + diagnostics->rawIndex * 0x30;
            auto rawName = *reinterpret_cast<wchar_t**>(rawRecord + 0x2C);
            diagnostics->identityDecodeStatus = ReadUnicodeString(rawName, diagnostics->rawName, _countof(diagnostics->rawName)) ? 1 : 0;
        }
    }
    auto mapGlobal = reinterpret_cast<void**>(OmsiMapGlobal);
    auto map = (Readable(mapGlobal, sizeof(void*)) ? *mapGlobal : nullptr);
    if (PlausibleOmsiObject(map) && Readable(map, 0x144)) {
        auto mapBytes = reinterpret_cast<unsigned char*>(map);
        diagnostics->map124 = *reinterpret_cast<float*>(mapBytes + 0x124);
        diagnostics->map128 = *reinterpret_cast<float*>(mapBytes + 0x128);
        diagnostics->map12C = *reinterpret_cast<float*>(mapBytes + 0x12C);
        diagnostics->map130 = *reinterpret_cast<float*>(mapBytes + 0x130);
        diagnostics->map134 = *reinterpret_cast<float*>(mapBytes + 0x134);
        diagnostics->map138 = *reinterpret_cast<float*>(mapBytes + 0x138);
        diagnostics->map13C = *reinterpret_cast<float*>(mapBytes + 0x13C);
        diagnostics->map140 = *reinterpret_cast<float*>(mapBytes + 0x140);
    }
    diagnostics->status = 0;
    return true;
}

void __declspec(naked) DirectSetPosStub() {
    __asm {
        pushad
        push eax
        call DirectSetPosStubBody
        add esp, 4
        popad
        mov eax, 1
        ret
    }
}

void DirectSetPosStubBody(void* self) {
    if (!gEntrypointActive || ++gEntrypointInvocationCount != 1) {
        if (gEntrypointDiagnostics != nullptr) gEntrypointDiagnostics->status = 16;
        RestoreSetposSlot();
        return;
    }
    RestoreSetposSlot();
    gEntrypointSuccess = ApplySelectedEntrypoint(self, const_cast<EntrypointDiagnostics*>(gEntrypointDiagnostics), gRequestedPresentedIndex) ? 1 : 0;
}

bool PlausibleOmsiObject(void* object) {
    if (!Readable(object, sizeof(void*))) return false;
    const auto vmt = *reinterpret_cast<uintptr_t*>(object);
    return vmt >= OmsiBase && vmt < OmsiEnd && Readable(reinterpret_cast<void*>(vmt), sizeof(void*));
}

void* ResolveObject(uintptr_t globalAddress) {
    auto global = reinterpret_cast<void***>(globalAddress);
    if (!Readable(global, sizeof(void**)) || *global == nullptr || !Readable(*global, sizeof(void*))) return nullptr;
    return **global;
}

namespace {
constexpr uintptr_t MakeVehicleProgManLiveSlot = 0x00862F28;
constexpr uintptr_t MakeVehicleRoadVehiclesSlot = 0x00861508;
constexpr uintptr_t MakeVehicleRoadVehicleTypesSlot = 0x008615A8;
constexpr uintptr_t MakeVehicleTempListVmt = 0x0074802C;
constexpr uintptr_t MakeVehicleCreate = 0x0074A0E0;
constexpr uintptr_t MakeVehicleCall = 0x0070A250;
constexpr uintptr_t MakeVehicleCopy = 0x0074A240;
constexpr uintptr_t PlaceRandomBusCall = 0x00708F8C;
constexpr uintptr_t DelphiGetMem = 0x00404614;
constexpr size_t MakeVehicleCriticalSectionOffset = 0x1B4;
constexpr size_t RoadVehicleItemsOffset = 0x28;
constexpr size_t PointerListDataOffset = 0x04;
constexpr size_t PointerListCountOffset = 0x08;
constexpr int MaximumRoadVehicles = 10000;

bool SnapshotRoadVehicles(void* roadVehicles, uintptr_t** items, int* count) {
    if (items == nullptr || count == nullptr || !PlausibleOmsiObject(roadVehicles)) return false;
    *items = nullptr;
    *count = 0;
    auto wrapper = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(roadVehicles) + RoadVehicleItemsOffset);
    if (wrapper == nullptr || !Readable(wrapper, 0x0C)) return false;
    const int itemCount = *reinterpret_cast<int*>(reinterpret_cast<unsigned char*>(wrapper) + PointerListCountOffset);
    auto data = *reinterpret_cast<uintptr_t**>(reinterpret_cast<unsigned char*>(wrapper) + PointerListDataOffset);
    if (itemCount < 0 || itemCount > MaximumRoadVehicles || (itemCount > 0 && (data == nullptr || !Readable(data, static_cast<size_t>(itemCount) * sizeof(uintptr_t))))) return false;
    auto copy = itemCount == 0 ? nullptr : new (std::nothrow) uintptr_t[itemCount];
    if (itemCount > 0 && copy == nullptr) return false;
    for (int index = 0; index < itemCount; ++index) copy[index] = data[index];
    *items = copy;
    *count = itemCount;
    return true;
}

bool IsBeforeVehicle(uintptr_t candidate, const uintptr_t* before, int beforeCount) {
    for (int index = 0; index < beforeCount; ++index) if (before[index] == candidate) return true;
    return false;
}

// This mirrors OmsiHook's AllocateString(false): a Delphi AnsiString data
// pointer backed by the OMSI allocator. The path is passed to MakeVehicle;
// ownership is deliberately not reclaimed here because the reference call
// leaves its path allocation with the native operation.
char* AllocateMakeVehiclePath(const wchar_t* value) {
    if (value == nullptr || *value == L'\0') return nullptr;
    const int bytes = WideCharToMultiByte(1252, WC_NO_BEST_FIT_CHARS, value, -1, nullptr, 0, nullptr, nullptr);
    if (bytes <= 1) return nullptr;
    const int allocation = bytes + 12;
    const auto header = reinterpret_cast<unsigned char*>(static_cast<uintptr_t>(BorlandFastCall(DelphiGetMem, 1, 1, allocation)));
    if (header == nullptr || !Readable(header, static_cast<size_t>(allocation))) return nullptr;
    BOOL usedDefault = FALSE;
    if (WideCharToMultiByte(1252, WC_NO_BEST_FIT_CHARS, value, -1, reinterpret_cast<char*>(header + 12), bytes, nullptr, &usedDefault) == 0 || usedDefault != FALSE) return nullptr;
    *reinterpret_cast<short*>(header) = 1252;
    *reinterpret_cast<short*>(header + 2) = 1;
    *reinterpret_cast<int*>(header + 4) = 1;
    *reinterpret_cast<int*>(header + 8) = bytes - 1;
    return reinterpret_cast<char*>(header + 12);
}
}

// Executes only the basic, reconciled MakeVehicle sequence. It accepts no
// player assignment, paint, HOF, fleet or registration behavior. Temp list
// cleanup is performed by CopyTempListIntoMainList for this build, as proven
// by MakeVehicle-Reconciliation-002.
extern "C" __declspec(dllexport) int __cdecl NativeMakeVehicleBasic(
    const wchar_t* busIdentity, NativeMakeVehicleDiagnostics* diagnostics) {
    if (diagnostics == nullptr) return 1;
    ZeroMemory(diagnostics, sizeof(*diagnostics));
    diagnostics->status = 1;
    if (busIdentity == nullptr || *busIdentity == L'\0' || !NativeValidateBuild()) return diagnostics->status;

    auto progMan = *reinterpret_cast<void**>(MakeVehicleProgManLiveSlot);
    auto roadVehicles = *reinterpret_cast<void**>(MakeVehicleRoadVehiclesSlot);
    auto roadVehicleTypes = *reinterpret_cast<void**>(MakeVehicleRoadVehicleTypesSlot);
    diagnostics->progMan = reinterpret_cast<uintptr_t>(progMan);
    diagnostics->roadVehicles = reinterpret_cast<uintptr_t>(roadVehicles);
    diagnostics->roadVehicleTypes = reinterpret_cast<uintptr_t>(roadVehicleTypes);
    if (!PlausibleOmsiObject(progMan)) { diagnostics->status = 2; return diagnostics->status; }
    if (!PlausibleOmsiObject(roadVehicles) || roadVehicleTypes == nullptr || !Readable(roadVehicleTypes, sizeof(void*))) { diagnostics->status = 3; return diagnostics->status; }

    uintptr_t* before = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &before, &diagnostics->beforeCount)) { diagnostics->status = 4; return diagnostics->status; }
    char* path = AllocateMakeVehiclePath(busIdentity);
    if (path == nullptr) { delete[] before; diagnostics->status = 5; return diagnostics->status; }

    auto criticalSection = reinterpret_cast<CRITICAL_SECTION*>(reinterpret_cast<unsigned char*>(progMan) + MakeVehicleCriticalSectionOffset);
    if (!Readable(criticalSection, sizeof(CRITICAL_SECTION))) { delete[] before; diagnostics->status = 6; return diagnostics->status; }
    diagnostics->criticalSection = reinterpret_cast<uintptr_t>(criticalSection);
    int temporaryList = 0;
    bool lockHeld = false;
    __try {
        EnterCriticalSection(criticalSection);
        lockHeld = true;
        temporaryList = BorlandFastCall(MakeVehicleCreate, 2, 2, MakeVehicleTempListVmt, 1);
        diagnostics->temporaryList = static_cast<uintptr_t>(temporaryList);
        if (temporaryList == 0 || !PlausibleOmsiObject(reinterpret_cast<void*>(static_cast<uintptr_t>(temporaryList)))) { diagnostics->status = 7; __leave; }
        diagnostics->rawReturn = BorlandFastCall(MakeVehicleCall, 3, 25,
            progMan, temporaryList, roadVehicleTypes,
            0, 0, 0, 0, 0, 0, 0,
            -1, 1, 0, 2, 0,
            0, 0, 0, 0, -1, 0,
            0, 0, 0, path);
        // CopyTempListIntoMainList owns the temp-list finalization for this
        // exact build. Do not add a destructor or FreeMem call here.
        diagnostics->copyReturn = BorlandFastCall(MakeVehicleCopy, 2, 2, roadVehicles, temporaryList);
        temporaryList = 0;
    } __finally {
        if (lockHeld) LeaveCriticalSection(criticalSection);
    }

    uintptr_t* after = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &after, &diagnostics->afterCount)) { delete[] before; diagnostics->status = 8; return diagnostics->status; }
    uintptr_t created = 0;
    int delta = 0;
    for (int index = 0; index < diagnostics->afterCount; ++index) {
        if (!IsBeforeVehicle(after[index], before, diagnostics->beforeCount)) { created = after[index]; ++delta; }
    }
    delete[] before;
    delete[] after;
    diagnostics->deltaCount = delta;
    diagnostics->createdVehicle = created;
    if (delta == 0) { diagnostics->status = 9; return diagnostics->status; }
    if (delta != 1) { diagnostics->status = 10; return diagnostics->status; }
    if (!PlausibleOmsiObject(reinterpret_cast<void*>(created))) { diagnostics->status = 11; return diagnostics->status; }
    diagnostics->createdVehicleVmt = *reinterpret_cast<uintptr_t*>(created);
    diagnostics->status = 0;
    return 0;
}

// Distinct from MakeVehicle: OMSI owns the AI/random placement policy and the
// returned integer is preserved as diagnostics only, never a public identity.
extern "C" __declspec(dllexport) int __cdecl NativePlaceRandomBus(
    int aiType, int group, int type, int scheduled, int tour, int line,
    int* rawReturn, int* beforeCount, int* afterCount) {
    if (rawReturn == nullptr || beforeCount == nullptr || afterCount == nullptr || !NativeValidateBuild()) return 1;
    *rawReturn = 0; *beforeCount = 0; *afterCount = 0;
    auto progMan = *reinterpret_cast<void**>(MakeVehicleProgManLiveSlot);
    auto roadVehicles = *reinterpret_cast<void**>(MakeVehicleRoadVehiclesSlot);
    if (!PlausibleOmsiObject(progMan) || !PlausibleOmsiObject(roadVehicles)) return 2;
    uintptr_t* before = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &before, beforeCount)) return 3;
    const int result = BorlandFastCall(PlaceRandomBusCall, 3, 11,
        progMan, aiType, group, 0, 0, 1, type, scheduled != 0, 0, tour, line);
    delete[] before;
    uintptr_t* after = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &after, afterCount)) return 4;
    delete[] after;
    *rawReturn = result;
    return 0;
}

bool ReadUnicodeString(wchar_t* value, wchar_t* output, int capacity) {
    if (value == nullptr || output == nullptr || capacity < 2 ||
        !Readable(value - 2, sizeof(int))) return false;
    const int length = *(reinterpret_cast<int*>(value) - 1);
    if (length <= 0 || length >= capacity ||
        !Readable(value, static_cast<size_t>(length) * sizeof(wchar_t))) return false;
    wmemcpy_s(output, capacity, value, length);
    output[length] = L'\0';
    return true;
}

// Tform_start is an auto-created form in this known build. The slot is
// validated at runtime; no heap pointer is persisted.
void* ResolveStartForm() {
    auto slot = reinterpret_cast<void**>(0x008612F8);
    if (!Readable(slot, sizeof(void*))) return nullptr;
    void* form = *slot;
    if (!Readable(form, sizeof(void*))) return nullptr;
    const auto vmt = *reinterpret_cast<uintptr_t*>(form);
    return vmt == 0x006759C8 && Readable(reinterpret_cast<void*>(vmt), sizeof(void*))
        ? form : nullptr;
}

bool ResolveMapSource(const wchar_t* requestedMap, void** formOutput, wchar_t** sourceOutput, wchar_t* decoded, int capacity,
    CanonicalMapDiagnostics* diagnostics = nullptr) {
    if (requestedMap == nullptr || formOutput == nullptr || sourceOutput == nullptr || decoded == nullptr || capacity < 2) return false;
    *formOutput = nullptr;
    *sourceOutput = nullptr;
    decoded[0] = L'\0';

    auto form = ResolveStartForm();
    if (diagnostics != nullptr) {
        diagnostics->form = reinterpret_cast<uintptr_t>(form);
        diagnostics->formVmt = form != nullptr ? *reinterpret_cast<uintptr_t*>(form) : 0;
        diagnostics->scanStage = form == nullptr ? 1 : 2;
        diagnostics->entrySummary[0] = L'\0';
        diagnostics->failedIndex = -1;
    }
    if (form == nullptr) return false;
    auto strings = *reinterpret_cast<wchar_t***>(reinterpret_cast<unsigned char*>(form) + 0x3F0);
    auto companions = *reinterpret_cast<int**>(reinterpret_cast<unsigned char*>(form) + 0x3F4);
    if (diagnostics != nullptr) {
        diagnostics->stringArray = reinterpret_cast<uintptr_t>(strings);
        diagnostics->companionArray = reinterpret_cast<uintptr_t>(companions);
        diagnostics->scanStage = (strings == nullptr || companions == nullptr) ? 3 : 4;
    }
    if (strings == nullptr || companions == nullptr ||
        !Readable(strings - 1, sizeof(int)) || !Readable(companions - 1, sizeof(int))) return false;
    const int stringCount = *(reinterpret_cast<int*>(strings) - 1);
    const int companionCount = *(reinterpret_cast<int*>(companions) - 1);
    if (diagnostics != nullptr) {
        diagnostics->stringCount = stringCount;
        diagnostics->companionCount = companionCount;
        diagnostics->scanStage = 5;
    }
    if (stringCount <= 0 || stringCount > 4096 || companionCount != stringCount ||
        !Readable(strings, static_cast<size_t>(stringCount) * sizeof(wchar_t*)) ||
        !Readable(companions, static_cast<size_t>(companionCount) * sizeof(int))) return false;

    size_t summaryLength = 0;
    for (int i = 0; i < stringCount; ++i) {
        wchar_t* candidate = strings[i];
        wchar_t path[512]{};
        const bool readable = ReadUnicodeString(candidate, path, static_cast<int>(_countof(path)));
        if (readable && diagnostics != nullptr) {
            ++diagnostics->readableEntryCount;
            const auto remaining = _countof(diagnostics->entrySummary) - summaryLength;
            if (remaining > 1) {
                const int written = _snwprintf_s(diagnostics->entrySummary + summaryLength, remaining,
                    _TRUNCATE, L"[%d]=%ls;", i, path);
                if (written > 0) summaryLength += static_cast<size_t>(written);
            }
        }
        if (!readable && diagnostics != nullptr) diagnostics->failedIndex = i;
        if (readable && _wcsicmp(path, requestedMap) == 0) {
            wcsncpy_s(decoded, capacity, path, _TRUNCATE);
            *formOutput = form;
            *sourceOutput = candidate;
            if (diagnostics != nullptr) {
                diagnostics->grundorfIndex = i;
                diagnostics->sourceData = reinterpret_cast<uintptr_t>(candidate);
                diagnostics->sourceLength = *(reinterpret_cast<int*>(candidate) - 1);
                wcsncpy_s(diagnostics->sourceText, _countof(diagnostics->sourceText), path, _TRUNCATE);
                diagnostics->scanStage = 6;
            }
            return true;
        }
    }
    if (diagnostics != nullptr) diagnostics->scanStage = 7;
    return false;
}

}

extern "C" __declspec(dllexport) int __cdecl NativeAssignCanonicalMapDetailed(
    wchar_t* selectedMap, int selectedMapCapacity, CanonicalMapDiagnostics* diagnostics);

void ClearCanonicalMapDiagnostics(CanonicalMapDiagnostics* diagnostics) {
    if (diagnostics != nullptr) {
        ZeroMemory(diagnostics, sizeof(*diagnostics));
    }
}

bool ReadUnicodeStringBounded(wchar_t* value, wchar_t* output, int capacity, int* lengthOutput) {
    if (lengthOutput != nullptr) *lengthOutput = 0;
    if (!ReadUnicodeString(value, output, capacity)) return false;
    if (lengthOutput != nullptr) *lengthOutput = static_cast<int>(wcslen(output));
    return true;
}

bool ReadAnsiStringBounded(unsigned char* value, wchar_t* output, int capacity, int* lengthOutput,
    int* codePageOutput, int* elementSizeOutput) {
    if (lengthOutput != nullptr) *lengthOutput = 0;
    if (codePageOutput != nullptr) *codePageOutput = 0;
    if (elementSizeOutput != nullptr) *elementSizeOutput = 0;
    if (value == nullptr || output == nullptr || capacity < 2 ||
        !Readable(value - 12, 12)) return false;
    const unsigned int metadata = *(reinterpret_cast<unsigned int*>(value) - 3);
    const unsigned int codePage = metadata & 0xffff;
    const unsigned int elementSize = metadata >> 16;
    const int length = *reinterpret_cast<int*>(value - 4);
    if (codePageOutput != nullptr) *codePageOutput = static_cast<int>(codePage);
    if (elementSizeOutput != nullptr) *elementSizeOutput = static_cast<int>(elementSize);
    if (elementSize != 1 || codePage != 0x04e4 || length <= 0 || length >= capacity ||
        !Readable(value, static_cast<size_t>(length))) return false;
    const int converted = MultiByteToWideChar(1252, 0, reinterpret_cast<const char*>(value), length,
        output, capacity - 1);
    if (converted <= 0) return false;
    output[converted] = L'\0';
    if (lengthOutput != nullptr) *lengthOutput = length;
    return true;
}

void CaptureRawString(void* value, unsigned int* header, unsigned char* bytes) {
    if (header != nullptr) ZeroMemory(header, 4 * sizeof(unsigned int));
    if (bytes != nullptr) ZeroMemory(bytes, 64);
    if (value == nullptr) return;
    auto data = reinterpret_cast<unsigned char*>(value);
    if (header != nullptr && Readable(data - 16, 16))
        memcpy(header, data - 16, 16);
    if (bytes != nullptr && Readable(data, 64))
        memcpy(bytes, data, 64);
}

extern "C" __declspec(dllexport) void __cdecl NativeClose(void* self) {
    // Delphi register convention: Self is supplied in EAX; target is fingerprint-gated.
    __asm {
        mov eax, self
        mov ecx, 008298A4h
        call ecx
    }
}

// 0=found, 1=form unresolved, 2=source collection unresolved, 3=not found.
extern "C" __declspec(dllexport) int __cdecl NativeResolveCanonicalMapSource(
    void** form, wchar_t* sourcePath, int sourcePathCapacity) {
    if (form == nullptr || sourcePath == nullptr || sourcePathCapacity < 2) return 1;
    wchar_t* source = nullptr;
    if (ResolveStartForm() == nullptr) return 1;
    if (!ResolveMapSource(L"maps\\Grundorf\\global.cfg", form, &source, sourcePath, sourcePathCapacity)) {
        auto start = ResolveStartForm();
        if (start == nullptr) return 1;
        auto strings = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(start) + 0x3F0);
        auto companions = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(start) + 0x3F4);
        if (strings == nullptr || companions == nullptr) return 2;
        return 3;
    }
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl NativeAssignMapDetailed(
    const wchar_t* requestedMap, wchar_t* selectedMap, int selectedMapCapacity, CanonicalMapDiagnostics* diagnostics);

// Compatibility wrapper for the original Grundorf lab operation.
extern "C" __declspec(dllexport) int __cdecl NativeAssignCanonicalMap(
    wchar_t* selectedMap, int selectedMapCapacity) {
    return NativeAssignMapDetailed(L"maps\\Grundorf\\global.cfg", selectedMap, selectedMapCapacity, nullptr);
}

extern "C" __declspec(dllexport) int __cdecl NativeAssignMapDetailed(
    const wchar_t* requestedMap, wchar_t* selectedMap, int selectedMapCapacity, CanonicalMapDiagnostics* diagnostics) {
    ClearCanonicalMapDiagnostics(diagnostics);
    void* form = nullptr;
    wchar_t* source = nullptr;
    if (requestedMap == nullptr || selectedMap == nullptr || selectedMapCapacity < 2) return 1;
    wchar_t sourcePath[512]{};
    if (!ResolveMapSource(requestedMap, &form, &source, sourcePath, static_cast<int>(_countof(sourcePath)), diagnostics)) return 2;

    if (diagnostics != nullptr) {
        diagnostics->form = reinterpret_cast<uintptr_t>(form);
        diagnostics->formVmt = *reinterpret_cast<uintptr_t*>(form);
        diagnostics->sourceData = reinterpret_cast<uintptr_t>(source);
        diagnostics->sourceLength = *(reinterpret_cast<int*>(source) - 1);
        wcsncpy_s(diagnostics->sourceText, _countof(diagnostics->sourceText), sourcePath, _TRUNCATE);
        CaptureRawString(source, diagnostics->sourceHeader, diagnostics->sourceBytes);
    }

    auto destinationSlot = reinterpret_cast<wchar_t***>(0x008591A4);
    if (!Readable(destinationSlot, sizeof(wchar_t**)) || *destinationSlot == nullptr) return 3;
    if (diagnostics != nullptr) {
        diagnostics->destinationSlot = reinterpret_cast<uintptr_t>(destinationSlot);
        diagnostics->destinationVariable = reinterpret_cast<uintptr_t>(*destinationSlot);
        diagnostics->destinationBeforeData = reinterpret_cast<uintptr_t>(**destinationSlot);
        ReadUnicodeStringBounded(**destinationSlot, diagnostics->destinationBeforeText,
            static_cast<int>(_countof(diagnostics->destinationBeforeText)),
            &diagnostics->destinationBeforeLength);
        CaptureRawString(**destinationSlot, diagnostics->destinationBeforeHeader,
            diagnostics->destinationBeforeBytes);
    }
    __asm {
        mov eax, destinationSlot
        mov eax, [eax]
        mov edx, source
        xor ecx, ecx
        mov ebx, 004092CCh
        call ebx
    }
    if (diagnostics != nullptr) diagnostics->helperCalled = 1;

    wchar_t* assigned = **destinationSlot;
    if (diagnostics != nullptr) {
        diagnostics->destinationAfterData = reinterpret_cast<uintptr_t>(assigned);
        CaptureRawString(assigned, diagnostics->destinationAfterHeader,
            diagnostics->destinationAfterBytes);
        diagnostics->readbackValid = ReadAnsiStringBounded(reinterpret_cast<unsigned char*>(assigned),
            diagnostics->destinationAfterText, static_cast<int>(_countof(diagnostics->destinationAfterText)),
            &diagnostics->destinationAfterLength, &diagnostics->destinationCodePage,
            &diagnostics->destinationElementSize) ? 1 : 0;
    }
    if (!ReadAnsiStringBounded(reinterpret_cast<unsigned char*>(assigned), selectedMap, selectedMapCapacity,
        nullptr, nullptr, nullptr) ||
        _wcsicmp(selectedMap, requestedMap) != 0) return 4;
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl NativeAssignCanonicalMapDetailed(
    wchar_t* selectedMap, int selectedMapCapacity, CanonicalMapDiagnostics* diagnostics) {
    return NativeAssignMapDetailed(L"maps\\Grundorf\\global.cfg", selectedMap, selectedMapCapacity, diagnostics);
}

// 0=ready, 1=ProgMan unresolved, 2=selected map unresolved, 3=map mismatch,
// 4=world already active, 5=AI config owner unresolved.
extern "C" __declspec(dllexport) int __cdecl NativeResolveWorld(
    void** progMan, void** aiConfig, wchar_t* selectedMap, int selectedMapCapacity, const wchar_t* expectedMap) {
    if (progMan == nullptr || aiConfig == nullptr || selectedMap == nullptr || selectedMapCapacity < 2) return 1;
    *progMan = nullptr;
    *aiConfig = nullptr;
    selectedMap[0] = L'\0';

    void* resolvedProgMan = ResolveObject(0x00858BDC);
    if (!PlausibleOmsiObject(resolvedProgMan)) return 1;

    auto mapSlotGlobal = reinterpret_cast<wchar_t***>(0x008591A4);
    if (!Readable(mapSlotGlobal, sizeof(wchar_t**)) || *mapSlotGlobal == nullptr || !Readable(*mapSlotGlobal, sizeof(wchar_t*))) return 2;
    auto mapData = reinterpret_cast<unsigned char*>(**mapSlotGlobal);
    if (!ReadAnsiStringBounded(mapData, selectedMap, selectedMapCapacity, nullptr, nullptr, nullptr) ||
        expectedMap == nullptr || _wcsicmp(selectedMap, expectedMap) != 0) return 3;

    if (ResolveObject(OmsiMapGlobal) != nullptr) return 4;

    void* resolvedAiConfig = ResolveObject(0x0085912C);
    if (!PlausibleOmsiObject(resolvedAiConfig)) return 5;

    *progMan = resolvedProgMan;
    *aiConfig = resolvedAiConfig;
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl NativeResolveCanonicalWorld(
    void** progMan, void** aiConfig, wchar_t* selectedMap, int selectedMapCapacity) {
    return NativeResolveWorld(progMan, aiConfig, selectedMap, selectedMapCapacity,
        L"maps\\Grundorf\\global.cfg");
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldCloseMap(void* progMan) {
    __asm {
        mov eax, progMan
        mov ecx, 006E57B0h
        call ecx
    }
}

extern "C" __declspec(dllexport) int __cdecl NativeWorldNewSituation(void* progMan) {
    unsigned char success = 0;
    __asm {
        push 1
        mov eax, progMan
        xor edx, edx
        mov cl, 1
        mov ebx, 006E5C0Ch
        call ebx
        mov success, al
    }
    return success;
}

extern "C" __declspec(dllexport) int __cdecl NativeWorldNewSituationDirect(
    void* progMan, int presentedIndex, EntrypointDiagnostics* diagnostics) {
    if (diagnostics == nullptr) return 0;
    ZeroMemory(diagnostics, sizeof(*diagnostics));
    gRequestedPresentedIndex = presentedIndex;
    const uintptr_t slotAddress = 0x00679FD8;
    auto setposGlobal = reinterpret_cast<void**>(0x00859BC8);
    diagnostics->newSituationFingerprintOk = IsKnownNewSituation() ? 1 : 0;
    diagnostics->setposGlobalReadable = Readable(setposGlobal, sizeof(void*)) ? 1 : 0;
    diagnostics->setposSlotReadable = diagnostics->setposGlobalReadable && *setposGlobal != nullptr &&
        Readable(*setposGlobal, sizeof(void*)) ? 1 : 0;
    if (!diagnostics->newSituationFingerprintOk) {
        diagnostics->status = 1;
        diagnostics->preflightStatus = 1;
        return 0;
    }
    if (!diagnostics->setposGlobalReadable || !diagnostics->setposSlotReadable) {
        diagnostics->status = 1;
        diagnostics->preflightStatus = 2;
        return 0;
    }
    // NewSituation resolves 0x00859BC8 to the global form slot, then reads the live form object.
    auto setposSlot = *setposGlobal;
    auto setpos = *reinterpret_cast<void**>(setposSlot);
    diagnostics->setposObjectReadable = setpos != nullptr && Readable(setpos, sizeof(void*)) ? 1 : 0;
    if (setpos == nullptr || !Readable(setpos, sizeof(void*))) {
        diagnostics->status = 1;
        diagnostics->preflightStatus = 3;
        return 0;
    }
    const uintptr_t vmt = *reinterpret_cast<uintptr_t*>(setpos);
    diagnostics->setposSelf = reinterpret_cast<uintptr_t>(setpos);
    diagnostics->setposVmt = vmt;
    diagnostics->slotAddress = slotAddress;
    if (vmt != 0x00679EB8 || slotAddress != vmt + 0x120 ||
        !Readable(reinterpret_cast<void*>(slotAddress), sizeof(uintptr_t)) ||
        *reinterpret_cast<uintptr_t*>(slotAddress) != 0x00551D18) {
        diagnostics->status = 2;
        diagnostics->originalTarget = Readable(reinterpret_cast<void*>(slotAddress), sizeof(uintptr_t))
            ? *reinterpret_cast<uintptr_t*>(slotAddress) : 0;
        return 0;
    }
    diagnostics->originalTarget = 0x00551D18;
    gSetposSlot = slotAddress;
    gSetposOriginal = 0x00551D18;
    gEntrypointDiagnostics = diagnostics;
    gEntrypointInvocationCount = 0;
    gEntrypointSuccess = 0;
    gEntrypointActive = 1;
    if (!SetVmtSlot(slotAddress, reinterpret_cast<uintptr_t>(&DirectSetPosStub))) {
        gEntrypointActive = 0;
        gEntrypointDiagnostics = nullptr;
        diagnostics->status = 3;
        return 0;
    }
    unsigned char success = 0;
    __try {
        __asm {
            push 1
            mov eax, progMan
            xor edx, edx
            mov cl, 1
            mov ebx, 006E5C0Ch
            call ebx
            mov success, al
        }
    } __finally {
        RestoreSetposSlot();
        gEntrypointActive = 0;
        gEntrypointDiagnostics = nullptr;
    }
    diagnostics->invocationCount = gEntrypointInvocationCount;
    diagnostics->slotRestored = Readable(reinterpret_cast<void*>(slotAddress), sizeof(uintptr_t)) &&
        *reinterpret_cast<uintptr_t*>(slotAddress) == 0x00551D18 ? 1 : 0;
    if (!diagnostics->slotRestored) {
        diagnostics->status = 17;
        return 0;
    }
    if (gEntrypointInvocationCount != 1 || !gEntrypointSuccess) return 0;
    diagnostics->status = success != 0 ? 0 : 18;
    return success != 0 ? 1 : 0;
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldCalcAiSum(void* aiConfig) {
    __asm {
        mov eax, aiConfig
        mov ecx, 00720F74h
        call ecx
    }
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldSetAiVehicles(void* progMan) {
    __asm {
        mov eax, progMan
        mov ecx, 00709350h
        call ecx
    }
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldSetTime(void* progMan) {
    __asm {
        mov eax, progMan
        mov dl, 1
        mov ecx, 00707B94h
        call ecx
    }
}

// Applies OMSI's profiled TProgMan.SetTime after the in-process adapter
// updates validated clock scalars, preserving dependent time invariants.
extern "C" __declspec(dllexport) int __cdecl NativeRuntimeApplyTime() {
    void* progMan = ResolveObject(0x00858BDC);
    if (!PlausibleOmsiObject(progMan)) return 0;
    NativeWorldSetTime(progMan);
    return 1;
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldInitializeScheduledAi(void* progMan) {
    __asm {
        mov eax, progMan
        mov ecx, 0070D160h
        call ecx
    }
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldFinalizeStart() {
    auto transitionSlot = reinterpret_cast<unsigned char**>(0x00858DA8);
    if (Readable(transitionSlot, sizeof(unsigned char*)) && *transitionSlot != nullptr && Readable(*transitionSlot, 1)) {
        **transitionSlot = 0;
    }
    auto start = ResolveStartForm();
    if (start != nullptr && Readable(reinterpret_cast<unsigned char*>(start) + 0x2C0, sizeof(unsigned long))) {
        *reinterpret_cast<unsigned long*>(reinterpret_cast<unsigned char*>(start) + 0x2C0) = 1;
    }
}

// Closed semantic operation used by PluginRuntime. No address, VMT slot, or
// raw OMSI object is exposed outside this build-profile-gated native boundary.
// Result: 0=success; non-zero values identify the first failed native stage.
extern "C" __declspec(dllexport) int __cdecl NativeStartNewMap(
    const wchar_t* mapIdentity, int presentedIndex, const wchar_t* entrypointIdentity) {
    void* startForm = nullptr;
    if (!ReadStartForm(&startForm) || !IsKnownNewSituation()) return 1;
    if (mapIdentity == nullptr || (presentedIndex < 0 && (entrypointIdentity == nullptr || *entrypointIdentity == L'\0'))) return 2;

    wchar_t selected[512]{};
    if (NativeAssignMapDetailed(mapIdentity, selected, static_cast<int>(_countof(selected)), nullptr) != 0) return 3;

    void* progMan = nullptr;
    void* aiConfig = nullptr;
    if (NativeResolveWorld(&progMan, &aiConfig, selected, static_cast<int>(_countof(selected)), mapIdentity) != 0) return 4;
    NativeWorldCloseMap(progMan);

    EntrypointDiagnostics diagnostics{};
    gRequestedEntrypointIdentity = entrypointIdentity;
    const int accepted = NativeWorldNewSituationDirect(progMan, presentedIndex, &diagnostics);
    gRequestedEntrypointIdentity = nullptr;
    gLastEntrypointDiagnostics = diagnostics;
    if (accepted == 0) return diagnostics.status == 0 ? 5 : 100 + diagnostics.status;
    NativeWorldCalcAiSum(aiConfig);
    NativeWorldSetAiVehicles(progMan);
    NativeWorldSetTime(progMan);
    NativeWorldInitializeScheduledAi(progMan);
    NativeWorldFinalizeStart();
    return 0;
}

// This reproduces the Start form's own control sequence. The direct internal
// loader call is deliberately not used: it bypasses selection state and was
// observed to fail OMSI's runtime checks.
extern "C" __declspec(dllexport) int __cdecl NativeStartSavedSituation(const wchar_t* situationIdentity) {
    void* form = nullptr;
    int situationIndex = -1;
    if (!ReadStartForm(&form) || !ResolveSituationIndex(form, situationIdentity, &situationIndex)) return 1;

    auto newMapMode = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + StartModeNewMapOffset);
    auto selectedMapMode = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + StartModeSelectedMapOffset);
    auto savedSituationMode = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + StartModeSavedSituationOffset);
    auto selector = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + StartSituationControlOffset);
    if (!SetRadioChecked(newMapMode, false) || !SetRadioChecked(selectedMapMode, false) || !SetRadioChecked(savedSituationMode, true) ||
        !SetSituationItemIndex(selector, situationIndex)) return 2;

    InvokeSituationSelectionChanged(form);
    InvokeStartModeChanged(form);
    InvokeStartButton1Click(form);
    return *reinterpret_cast<int*>(reinterpret_cast<unsigned char*>(form) + StartModalResultOffset) == 1 ? 0 : 3;
}

// Copies a completed selection's semantic diagnostics. It never exposes an
// OMSI object pointer or reads live OMSI memory after the selection returned.
extern "C" __declspec(dllexport) int __cdecl NativeGetLastEntrypointSelection(
    int* presentedIndex, int* rawIndex,
    wchar_t* presentedName, int presentedNameCapacity,
    wchar_t* rawName, int rawNameCapacity) {
    if (presentedIndex == nullptr || rawIndex == nullptr || presentedName == nullptr || rawName == nullptr ||
        presentedNameCapacity < 2 || rawNameCapacity < 2 || gLastEntrypointDiagnostics.status != 0) return 0;
    *presentedIndex = gLastEntrypointDiagnostics.confirmedPresentedIndex;
    *rawIndex = gLastEntrypointDiagnostics.rawIndex;
    wcsncpy_s(presentedName, presentedNameCapacity, gLastEntrypointDiagnostics.presentedName, _TRUNCATE);
    wcsncpy_s(rawName, rawNameCapacity, gLastEntrypointDiagnostics.rawName, _TRUNCATE);
    return 1;
}

namespace {
constexpr uintptr_t D3DDeviceSlot = 0x008627D0;
constexpr unsigned long D3DStateNotReady = 0;
constexpr unsigned long D3DStateReady = 1;
constexpr unsigned long D3DStateLost = 2;
constexpr unsigned long D3DStateResetting = 3;
constexpr unsigned long D3DStateStopping = 4;
constexpr unsigned long D3DStateStopped = 5;
constexpr unsigned long D3DTransitionNone = 0;
constexpr unsigned long D3DTransitionReady = 1;
constexpr unsigned long D3DTransitionLost = 2;
constexpr unsigned long D3DTransitionResetting = 3;
constexpr unsigned long D3DTransitionRestored = 4;
constexpr unsigned long D3DTransitionStopped = 5;
constexpr unsigned long TextureUnused = 0;
constexpr unsigned long TextureLive = 1;
constexpr unsigned long TextureReleased = 2;
constexpr unsigned long TextureStale = 3;
constexpr int D3DTextureCapacity = 128;
constexpr unsigned long D3DTransitionCapacity = 32;

struct D3DTextureEntry {
    unsigned __int64 handle;
    IDirect3DTexture9* texture;
    unsigned long state;
    unsigned long generation;
    unsigned long width;
    unsigned long height;
    unsigned long format;
    unsigned long levels;
};

SRWLOCK gD3DLock = SRWLOCK_INIT;
IDirect3DDevice9* gD3DDevice = nullptr;
IUnknown* gD3DCandidate = nullptr;
unsigned long gD3DState = D3DStateNotReady;
unsigned long gD3DGeneration = 0;
unsigned long gD3DNextHandle = 0;
unsigned long gD3DOwnerThread = 0;
unsigned long gD3DResetThread = 0;
unsigned long gD3DTransitions[D3DTransitionCapacity]{};
unsigned long gD3DTransitionRead = 0;
unsigned long gD3DTransitionWrite = 0;
unsigned long gD3DTransitionCount = 0;
D3DTextureEntry gD3DTextures[D3DTextureCapacity]{};
using D3DResetMethod = HRESULT (STDMETHODCALLTYPE*)(IDirect3DDevice9*, D3DPRESENT_PARAMETERS*);
D3DResetMethod gD3DOriginalReset = nullptr;
void** gD3DResetSlot = nullptr;

void QueueTransition(unsigned long transition) {
    if (transition == D3DTransitionNone || transition > D3DTransitionStopped) return;
    if (gD3DTransitionCount == D3DTransitionCapacity) {
        gD3DTransitionRead = (gD3DTransitionRead + 1) % D3DTransitionCapacity;
        --gD3DTransitionCount;
    }
    gD3DTransitions[gD3DTransitionWrite] = transition;
    gD3DTransitionWrite = (gD3DTransitionWrite + 1) % D3DTransitionCapacity;
    ++gD3DTransitionCount;
}

unsigned long PopTransition() {
    if (gD3DTransitionCount == 0) return D3DTransitionNone;
    const auto transition = gD3DTransitions[gD3DTransitionRead];
    gD3DTransitionRead = (gD3DTransitionRead + 1) % D3DTransitionCapacity;
    --gD3DTransitionCount;
    return transition;
}

unsigned long LiveTextureCount() {
    unsigned long count = 0;
    for (const auto& entry : gD3DTextures) if (entry.state == TextureLive && entry.texture != nullptr) ++count;
    return count;
}

void InvalidateTextures(unsigned long state) {
    for (auto& entry : gD3DTextures) {
        if (entry.state != TextureLive || entry.texture == nullptr) continue;
        entry.texture->Release();
        entry.texture = nullptr;
        entry.state = state;
    }
}

void ReleaseDevice() {
    if (gD3DDevice != nullptr) {
        gD3DDevice->Release();
        gD3DDevice = nullptr;
    }
    gD3DCandidate = nullptr;
}

HRESULT STDMETHODCALLTYPE D3DResetHook(IDirect3DDevice9* self, D3DPRESENT_PARAMETERS* parameters) {
    D3DResetMethod original = nullptr;
    AcquireSRWLockExclusive(&gD3DLock);
    original = gD3DOriginalReset;
    gD3DResetThread = GetCurrentThreadId();
    gD3DState = D3DStateResetting;
    InvalidateTextures(TextureStale);
    ++gD3DGeneration;
    QueueTransition(D3DTransitionResetting);
    ReleaseSRWLockExclusive(&gD3DLock);

    if (original == nullptr) return D3DERR_INVALIDCALL;
    const auto result = original(self, parameters);

    AcquireSRWLockExclusive(&gD3DLock);
    if (SUCCEEDED(result)) {
        gD3DState = D3DStateReady;
        QueueTransition(D3DTransitionRestored);
    } else {
        gD3DState = result == D3DERR_DEVICENOTRESET ? D3DStateResetting : D3DStateLost;
        QueueTransition(gD3DState == D3DStateResetting ? D3DTransitionResetting : D3DTransitionLost);
    }
    ReleaseSRWLockExclusive(&gD3DLock);
    return result;
}

bool InstallD3DResetHook(IDirect3DDevice9* device) {
    if (gD3DOriginalReset != nullptr) return true;
    if (device == nullptr || !Readable(device, sizeof(void*))) return false;
    auto** vtable = *reinterpret_cast<void***>(device);
    if (vtable == nullptr || !Readable(vtable + 16, sizeof(void*))) return false;
    auto** slot = vtable + 16;
    auto* original = reinterpret_cast<D3DResetMethod>(*slot);
    if (original == nullptr || original == &D3DResetHook) return false;
    DWORD oldProtection = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &oldProtection)) return false;
    *slot = reinterpret_cast<void*>(&D3DResetHook);
    DWORD ignored = 0;
    VirtualProtect(slot, sizeof(void*), oldProtection, &ignored);
    gD3DOriginalReset = original;
    gD3DResetSlot = slot;
    return true;
}

void RemoveD3DResetHook() {
    if (gD3DResetSlot != nullptr && gD3DOriginalReset != nullptr && Readable(gD3DResetSlot, sizeof(void*)) &&
        *gD3DResetSlot == reinterpret_cast<void*>(&D3DResetHook)) {
        DWORD oldProtection = 0;
        if (VirtualProtect(gD3DResetSlot, sizeof(void*), PAGE_READWRITE, &oldProtection)) {
            *gD3DResetSlot = reinterpret_cast<void*>(gD3DOriginalReset);
            DWORD ignored = 0;
            VirtualProtect(gD3DResetSlot, sizeof(void*), oldProtection, &ignored);
        }
    }
    gD3DResetSlot = nullptr;
    gD3DOriginalReset = nullptr;
}

unsigned long MapLifecycleState(long cooperative) {
    if (cooperative == D3D_OK) return D3DStateReady;
    if (cooperative == D3DERR_DEVICELOST) return D3DStateLost;
    if (cooperative == D3DERR_DEVICENOTRESET) return D3DStateResetting;
    return D3DStateNotReady;
}

unsigned long ObserveLifecycle(long* queryInterfaceResult, long* cooperativeResult, uintptr_t* candidateValue) {
    if (queryInterfaceResult != nullptr) *queryInterfaceResult = E_PENDING;
    if (cooperativeResult != nullptr) *cooperativeResult = E_PENDING;
    auto* candidate = Readable(reinterpret_cast<void*>(D3DDeviceSlot), sizeof(void*))
        ? *reinterpret_cast<IUnknown**>(D3DDeviceSlot) : nullptr;
    if (candidateValue != nullptr) *candidateValue = reinterpret_cast<uintptr_t>(candidate);

    if (candidate == nullptr || !Readable(candidate, sizeof(void*))) {
        if (gD3DState == D3DStateReady || gD3DState == D3DStateLost || gD3DState == D3DStateResetting) {
            InvalidateTextures(TextureStale);
            RemoveD3DResetHook();
            ReleaseDevice();
            ++gD3DGeneration;
            gD3DState = D3DStateLost;
            QueueTransition(D3DTransitionLost);
            return PopTransition();
        }
        gD3DState = D3DStateNotReady;
        return PopTransition();
    }

    unsigned long transition = D3DTransitionNone;
    if (gD3DDevice == nullptr || candidate != gD3DCandidate) {
        const bool replacing = gD3DDevice != nullptr;
        if (replacing) {
            InvalidateTextures(TextureStale);
            RemoveD3DResetHook();
            ReleaseDevice();
            ++gD3DGeneration;
        }
        IDirect3DDevice9* acquired = nullptr;
        const auto query = candidate->QueryInterface(__uuidof(IDirect3DDevice9), reinterpret_cast<void**>(&acquired));
        if (queryInterfaceResult != nullptr) *queryInterfaceResult = query;
        if (FAILED(query) || acquired == nullptr) {
            gD3DState = D3DStateNotReady;
            return D3DTransitionNone;
        }
        gD3DDevice = acquired;
        gD3DCandidate = candidate;
        ++gD3DGeneration;
        if (!InstallD3DResetHook(gD3DDevice)) {
            ReleaseDevice();
            gD3DState = D3DStateNotReady;
            return PopTransition();
        }
        transition = replacing ? D3DTransitionRestored : D3DTransitionReady;
    } else if (queryInterfaceResult != nullptr) {
        *queryInterfaceResult = S_OK;
    }

    const auto previous = gD3DState;
    const auto cooperative = gD3DDevice->TestCooperativeLevel();
    if (cooperativeResult != nullptr) *cooperativeResult = cooperative;
    const auto observed = MapLifecycleState(cooperative);
    if (observed == D3DStateLost || observed == D3DStateResetting) {
        if (previous != observed) {
            InvalidateTextures(TextureStale);
            ++gD3DGeneration;
            transition = observed == D3DStateLost ? D3DTransitionLost : D3DTransitionResetting;
        }
    } else if (observed == D3DStateReady && (previous == D3DStateLost || previous == D3DStateResetting)) {
        ++gD3DGeneration;
        transition = D3DTransitionRestored;
    }
    gD3DState = observed;
    gD3DOwnerThread = GetCurrentThreadId();
    QueueTransition(transition);
    return PopTransition();
}

D3DTextureEntry* FindTexture(unsigned __int64 handle) {
    for (auto& entry : gD3DTextures) if (entry.handle == handle && entry.state != TextureUnused) return &entry;
    return nullptr;
}

D3DTextureEntry* FindTextureSlot() {
    for (auto& entry : gD3DTextures) if (entry.state != TextureLive) return &entry;
    return nullptr;
}

bool SupportedTextureFormat(unsigned long format, unsigned long* bytesPerPixel) {
    unsigned long bytes = 0;
    switch (static_cast<D3DFORMAT>(format)) {
        case D3DFMT_A8R8G8B8: case D3DFMT_X8R8G8B8: bytes = 4; break;
        case D3DFMT_R5G6B5: case D3DFMT_X1R5G5B5: case D3DFMT_A1R5G5B5:
        case D3DFMT_A4R4G4B4: case D3DFMT_A8L8: bytes = 2; break;
        case D3DFMT_A8: case D3DFMT_L8: bytes = 1; break;
        default: return false;
    }
    if (bytesPerPixel != nullptr) *bytesPerPixel = bytes;
    return true;
}

void FillTextureResult(NativeD3DTextureResult* result, D3DTextureEntry* entry) {
    result->executionThreadId = GetCurrentThreadId();
    result->lifecycleState = gD3DState;
    result->deviceGeneration = gD3DGeneration;
    if (entry == nullptr) return;
    result->handle = entry->handle;
    result->textureState = entry->state;
    result->width = entry->width;
    result->height = entry->height;
    result->format = entry->format;
    result->levels = entry->levels;
    if (entry->texture == nullptr || entry->state != TextureLive) return;
    D3DSURFACE_DESC desc{};
    const auto hr = entry->texture->GetLevelDesc(result->level, &desc);
    result->operationResult = hr;
    if (SUCCEEDED(hr)) {
        result->levelWidth = desc.Width;
        result->levelHeight = desc.Height;
        result->format = desc.Format;
        if (result->level == 0) { result->width = desc.Width; result->height = desc.Height; }
        else {
            D3DSURFACE_DESC root{};
            if (SUCCEEDED(entry->texture->GetLevelDesc(0, &root))) { result->width = root.Width; result->height = root.Height; }
        }
    }
}
}

// Acquires and retains exactly one QI reference while initialized. The raw
// OMSI slot is borrowed and is never AddRef'd or released directly.
extern "C" __declspec(dllexport) int __cdecl NativeD3DProbe(NativeD3DStatus* result) {
    if (result == nullptr) return 1;
    *result = {};
    result->slot = D3DDeviceSlot;
    result->executionThreadId = GetCurrentThreadId();
    if (!NativeValidateBuild()) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    result->lifecycleTransition = ObserveLifecycle(&result->queryInterfaceResult, &result->cooperativeLevelResult, &result->candidate);
    result->ownedDeviceReferences = gD3DDevice == nullptr ? 0 : 1;
    result->lifecycleState = gD3DState;
    result->deviceGeneration = gD3DGeneration;
    result->liveTextureCount = LiveTextureCount();
    result->resetHookInstalled = gD3DOriginalReset != nullptr && gD3DResetSlot != nullptr ? 1 : 0;
    result->lastResetThreadId = gD3DResetThread;
    result->status = gD3DDevice == nullptr ? 5 : 0;
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureCreate(
    unsigned long width, unsigned long height, unsigned long format, unsigned long levels, NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {};
    result->operationResult = E_INVALIDARG;
    if (!NativeValidateBuild()) { result->status = 2; return result->status; }
    if (width == 0 || height == 0 || width > 4096 || height > 4096 || levels > 16 || !SupportedTextureFormat(format, nullptr)) {
        result->status = 6; return result->status;
    }
    AcquireSRWLockExclusive(&gD3DLock);
    ObserveLifecycle(nullptr, &result->operationResult, nullptr);
    if (gD3DState != D3DStateReady || gD3DDevice == nullptr) {
        result->status = gD3DState == D3DStateLost ? 8 : (gD3DState == D3DStateResetting ? 9 : 7);
        FillTextureResult(result, nullptr); ReleaseSRWLockExclusive(&gD3DLock); return result->status;
    }
    auto* entry = FindTextureSlot();
    if (entry == nullptr) { result->status = 10; FillTextureResult(result, nullptr); ReleaseSRWLockExclusive(&gD3DLock); return result->status; }
    IDirect3DTexture9* texture = nullptr;
    const auto hr = gD3DDevice->CreateTexture(width, height, levels, D3DUSAGE_DYNAMIC, static_cast<D3DFORMAT>(format), D3DPOOL_DEFAULT, &texture, nullptr);
    result->operationResult = hr;
    if (FAILED(hr) || texture == nullptr) { result->status = 11; FillTextureResult(result, nullptr); ReleaseSRWLockExclusive(&gD3DLock); return result->status; }
    const auto token = ++gD3DNextHandle == 0 ? ++gD3DNextHandle : gD3DNextHandle;
    entry->handle = (static_cast<unsigned __int64>(gD3DGeneration) << 32) | token;
    entry->texture = texture;
    entry->state = TextureLive;
    entry->generation = gD3DGeneration;
    entry->width = width;
    entry->height = height;
    entry->format = format;
    entry->levels = texture->GetLevelCount();
    result->level = 0;
    result->status = 0;
    FillTextureResult(result, entry);
    ReleaseSRWLockExclusive(&gD3DLock);
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureDescribe(
    unsigned __int64 handle, unsigned long level, NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {}; result->handle = handle; result->level = level;
    if (!NativeValidateBuild()) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    ObserveLifecycle(nullptr, nullptr, nullptr);
    auto* entry = FindTexture(handle);
    if (entry == nullptr || entry->generation != static_cast<unsigned long>(handle >> 32)) result->status = 12;
    else if (entry->state == TextureReleased) result->status = 13;
    else if (entry->state != TextureLive || entry->generation != gD3DGeneration) result->status = 12;
    else if (level >= entry->texture->GetLevelCount()) result->status = 14;
    else { result->status = 0; FillTextureResult(result, entry); }
    if (result->status != 0) FillTextureResult(result, entry);
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureUpdate(
    unsigned __int64 handle, unsigned long level, unsigned long x, unsigned long y,
    unsigned long width, unsigned long height, const unsigned char* pixels, unsigned long pixelBytes,
    NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {}; result->handle = handle; result->level = level;
    if (!NativeValidateBuild()) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    ObserveLifecycle(nullptr, nullptr, nullptr);
    auto* entry = FindTexture(handle);
    if (entry == nullptr || entry->state == TextureStale || entry->generation != static_cast<unsigned long>(handle >> 32) || entry->generation != gD3DGeneration) result->status = 12;
    else if (entry->state == TextureReleased) result->status = 13;
    else if (gD3DState == D3DStateLost) result->status = 8;
    else if (gD3DState == D3DStateResetting) result->status = 9;
    else if (gD3DState != D3DStateReady) result->status = 7;
    else if (level >= entry->texture->GetLevelCount() || width == 0 || height == 0 || pixels == nullptr) result->status = 14;
    else {
        D3DSURFACE_DESC desc{};
        auto hr = entry->texture->GetLevelDesc(level, &desc);
        unsigned long bytesPerPixel = 0;
        if (FAILED(hr) || !SupportedTextureFormat(desc.Format, &bytesPerPixel) || x > desc.Width || y > desc.Height || width > desc.Width - x || height > desc.Height - y || pixelBytes != width * height * bytesPerPixel) {
            result->status = 14; result->operationResult = FAILED(hr) ? hr : E_INVALIDARG;
        } else {
            RECT rect{ static_cast<LONG>(x), static_cast<LONG>(y), static_cast<LONG>(x + width), static_cast<LONG>(y + height) };
            D3DLOCKED_RECT locked{};
            hr = entry->texture->LockRect(level, &locked, &rect, 0);
            result->operationResult = hr;
            if (FAILED(hr)) result->status = 15;
            else {
                const auto rowBytes = width * bytesPerPixel;
                for (unsigned long row = 0; row < height; ++row)
                    memcpy(static_cast<unsigned char*>(locked.pBits) + row * locked.Pitch, pixels + row * rowBytes, rowBytes);
                hr = entry->texture->UnlockRect(level);
                result->operationResult = hr;
                result->status = FAILED(hr) ? 16 : 0;
            }
        }
    }
    const auto operationResult = result->operationResult;
    FillTextureResult(result, entry);
    result->operationResult = operationResult;
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureRelease(unsigned __int64 handle, NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {}; result->handle = handle;
    if (!NativeValidateBuild()) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    auto* entry = FindTexture(handle);
    if (entry == nullptr || entry->generation != static_cast<unsigned long>(handle >> 32)) result->status = 12;
    else if (entry->state == TextureReleased) result->status = 13;
    else if (entry->state == TextureStale) result->status = 12;
    else {
        entry->texture->Release(); entry->texture = nullptr; entry->state = TextureReleased;
        result->operationResult = S_OK; result->status = 0;
    }
    FillTextureResult(result, entry);
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) void __cdecl NativeD3DShutdown() {
    AcquireSRWLockExclusive(&gD3DLock);
    gD3DState = D3DStateStopping;
    InvalidateTextures(TextureStale);
    RemoveD3DResetHook();
    ReleaseDevice();
    ++gD3DGeneration;
    gD3DState = D3DStateStopped;
    QueueTransition(D3DTransitionStopped);
    gD3DOwnerThread = GetCurrentThreadId();
    ReleaseSRWLockExclusive(&gD3DLock);
}
