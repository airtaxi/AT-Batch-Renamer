#include "pch.h"
#include "resource.h"

using namespace Microsoft::WRL;

static constexpr wchar_t exe_filename[] = L"BatchRenamerAT.exe";
static constexpr wchar_t default_menu_title[] = L"Rename with AT Batch Renamer";
static constexpr wchar_t simplified_chinese_menu_title[] = L"\x4F7F\x7528" L"AT Batch Renamer" L"\x91CD\x547D\x540D";
static constexpr wchar_t traditional_chinese_menu_title[] = L"\x4F7F\x7528" L"AT Batch Renamer" L"\x91CD\x65B0\x547D\x540D";

// {A7E94C1E-B69E-44F4-B4BF-F26DC33B845C}
static constexpr CLSID CLSID_BatchRenamerATContextMenu =
{ 0xA7E94C1E, 0xB69E, 0x44F4, { 0xB4, 0xBF, 0xF2, 0x6D, 0xC3, 0x3B, 0x84, 0x5C } };

std::wstring GetModuleDirectoryPath()
{
    std::wstring path(MAX_PATH, L'\0');
    DWORD length = 0;
    for (;;)
    {
        length = GetModuleFileNameW(wil::GetModuleInstanceHandle(), path.data(), static_cast<DWORD>(path.size()));
        if (length == 0)
            return {};
        if (length < static_cast<DWORD>(path.size()))
            break;
        path.resize(path.size() * 2);
    }
    path.resize(length);
    auto lastSlash = path.find_last_of(L'\\');
    if (lastSlash != std::wstring::npos)
        path.resize(lastSlash + 1);
    return path;
}

std::wstring QuoteForCommandLineArgument(const std::wstring& argument)
{
    // An empty argument is passed as a pair of quotes
    if (argument.empty())
        return L"\"\"";

    bool needsQuoting = false;
    for (auto character : argument)
    {
        if (character == L' ' || character == L'\t' || character == L'"' || character == L'\\')
        {
            needsQuoting = true;
            break;
        }
    }

    if (!needsQuoting)
        return argument;

    std::wstring quoted = L"\"";
    for (auto iterator = argument.begin(); ; ++iterator)
    {
        size_t backslashCount = 0;
        while (iterator != argument.end() && *iterator == L'\\')
        {
            ++iterator;
            ++backslashCount;
        }

        if (iterator == argument.end())
        {
            // Escape all backslashes before the closing quote
            quoted.append(backslashCount * 2, L'\\');
            break;
        }
        else if (*iterator == L'"')
        {
            // Escape backslashes and the quote
            quoted.append(backslashCount * 2 + 1, L'\\');
            quoted.push_back(*iterator);
        }
        else
        {
            // Backslashes are literal here
            quoted.append(backslashCount, L'\\');
            quoted.push_back(*iterator);
        }
    }
    quoted.push_back(L'"');
    return quoted;
}

bool StartsWithCaseInsensitive(const std::wstring& text, const wchar_t* prefix)
{
    return StrCmpNIW(text.c_str(), prefix, static_cast<int>(wcslen(prefix))) == 0;
}

bool ContainsCaseInsensitive(const std::wstring& text, const wchar_t* value)
{
    return StrStrIW(text.c_str(), value) != nullptr;
}

std::wstring GetThreadPreferredLanguageNameBuffer()
{
    ULONG preferredLanguageCount = 0;
    ULONG preferredLanguageNameBufferLength = 0;
    if (!GetThreadPreferredUILanguages(MUI_LANGUAGE_NAME, &preferredLanguageCount, nullptr, &preferredLanguageNameBufferLength)
        && GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        return {};

    std::wstring preferredLanguageNameBuffer(preferredLanguageNameBufferLength, L'\0');
    if (!GetThreadPreferredUILanguages(MUI_LANGUAGE_NAME, &preferredLanguageCount, preferredLanguageNameBuffer.data(), &preferredLanguageNameBufferLength))
        return {};
    return preferredLanguageNameBuffer;
}

bool IsTraditionalChineseLanguageTag(const std::wstring& languageTag)
{
    if (!StartsWithCaseInsensitive(languageTag, L"zh")) return false;
    return ContainsCaseInsensitive(languageTag, L"zh-Hant")
        || ContainsCaseInsensitive(languageTag, L"zh-TW")
        || ContainsCaseInsensitive(languageTag, L"zh-HK")
        || ContainsCaseInsensitive(languageTag, L"zh-MO");
}

bool IsSimplifiedChineseLanguageTag(const std::wstring& languageTag)
{
    if (!StartsWithCaseInsensitive(languageTag, L"zh")) return false;
    return ContainsCaseInsensitive(languageTag, L"zh-Hans")
        || ContainsCaseInsensitive(languageTag, L"zh-CN")
        || ContainsCaseInsensitive(languageTag, L"zh-SG");
}

// The string table language fallback does not reliably pick the matching Chinese table, so the
// preferred UI languages are checked explicitly first.
const wchar_t* GetChineseMenuTitleForPreferredLanguage()
{
    auto preferredLanguageNameBuffer = GetThreadPreferredLanguageNameBuffer();
    if (preferredLanguageNameBuffer.empty()) return nullptr;

    for (const wchar_t* preferredLanguageName = preferredLanguageNameBuffer.c_str(); *preferredLanguageName != L'\0'; preferredLanguageName += wcslen(preferredLanguageName) + 1)
    {
        std::wstring languageTag(preferredLanguageName);
        if (IsTraditionalChineseLanguageTag(languageTag)) return traditional_chinese_menu_title;
        if (IsSimplifiedChineseLanguageTag(languageTag)) return simplified_chinese_menu_title;
    }

    return nullptr;
}

bool WriteUtf8Line(HANDLE fileHandle, const wchar_t* text)
{
    int utf8Length = WideCharToMultiByte(CP_UTF8, 0, text, -1, nullptr, 0, nullptr, nullptr);
    if (utf8Length <= 0)
        return false;

    std::string utf8Text(utf8Length - 1, '\0');
    WideCharToMultiByte(CP_UTF8, 0, text, -1, utf8Text.data(), utf8Length, nullptr, nullptr);
    utf8Text += '\n';

    DWORD bytesWritten = 0;
    return WriteFile(fileHandle, utf8Text.c_str(), static_cast<DWORD>(utf8Text.size()), &bytesWritten, nullptr) && bytesWritten == utf8Text.size();
}

class __declspec(uuid("A7E94C1E-B69E-44F4-B4BF-F26DC33B845C"))
BatchRenamerATContextMenuCommand : public RuntimeClass<
    RuntimeClassFlags<ClassicCom>,
    IExplorerCommand>
{
public:
    // IExplorerCommand
    IFACEMETHODIMP GetTitle(_In_opt_ IShellItemArray* /*items*/, _Outptr_result_nullonfailure_ PWSTR* name)
    {
        *name = nullptr;
        if (const auto chineseMenuTitle = GetChineseMenuTitleForPreferredLanguage(); chineseMenuTitle != nullptr)
            return SHStrDupW(chineseMenuTitle, name);

        wchar_t buffer[256] = {};
        if (LoadStringW(wil::GetModuleInstanceHandle(), IDS_MENU_TITLE, buffer, ARRAYSIZE(buffer)) > 0)
            return SHStrDupW(buffer, name);
        return SHStrDupW(default_menu_title, name);
    }

    IFACEMETHODIMP GetIcon(_In_opt_ IShellItemArray* /*items*/, _Outptr_result_nullonfailure_ PWSTR* icon)
    {
        *icon = nullptr;
        std::wstring iconPath = GetModuleDirectoryPath() + exe_filename;
        return SHStrDupW(iconPath.c_str(), icon);
    }

    IFACEMETHODIMP GetToolTip(_In_opt_ IShellItemArray* /*items*/, _Outptr_result_nullonfailure_ PWSTR* infoTip)
    {
        *infoTip = nullptr;
        return E_NOTIMPL;
    }

    IFACEMETHODIMP GetCanonicalName(_Out_ GUID* guidCommandName)
    {
        *guidCommandName = CLSID_BatchRenamerATContextMenu;
        return S_OK;
    }

    IFACEMETHODIMP GetState(_In_opt_ IShellItemArray* /*items*/, _In_ BOOL /*okToBeSlow*/, _Out_ EXPCMDSTATE* commandState)
    {
        *commandState = ECS_ENABLED;
        return S_OK;
    }

    IFACEMETHODIMP GetFlags(_Out_ EXPCMDFLAGS* flags)
    {
        *flags = ECF_DEFAULT;
        return S_OK;
    }

    IFACEMETHODIMP EnumSubCommands(_COM_Outptr_ IEnumExplorerCommand** enumCommands)
    {
        *enumCommands = nullptr;
        return E_NOTIMPL;
    }

    IFACEMETHODIMP Invoke(_In_opt_ IShellItemArray* items, _In_opt_ IBindCtx* /*bindCtx*/) noexcept
    try
    {
        if (!items)
            return S_OK;

        DWORD itemCount = 0;
        RETURN_IF_FAILED(items->GetCount(&itemCount));
        if (itemCount == 0)
            return S_OK;

        std::wstring moduleDirectory = GetModuleDirectoryPath();
        std::wstring executablePath = moduleDirectory + exe_filename;

        // The selected paths go to a temporary file to avoid the command line length limit
        wchar_t tempDirectory[MAX_PATH] = {};
        if (GetTempPathW(MAX_PATH, tempDirectory) == 0)
            return HRESULT_FROM_WIN32(GetLastError());

        wchar_t tempFilePath[MAX_PATH] = {};
        if (GetTempFileNameW(tempDirectory, L"BRAT", 0, tempFilePath) == 0)
            return HRESULT_FROM_WIN32(GetLastError());

        size_t writtenPathCount = 0;
        {
            wil::unique_hfile fileHandle(CreateFileW(tempFilePath, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr));
            if (!fileHandle)
            {
                DeleteFileW(tempFilePath);
                return HRESULT_FROM_WIN32(GetLastError());
            }

            // UTF-8 byte order mark
            const unsigned char byteOrderMark[] = { 0xEF, 0xBB, 0xBF };
            DWORD bytesWritten = 0;
            WriteFile(fileHandle.get(), byteOrderMark, sizeof(byteOrderMark), &bytesWritten, nullptr);

            for (DWORD itemIndex = 0; itemIndex < itemCount; ++itemIndex)
            {
                // Items without a file system path (libraries, virtual folders) are skipped
                ComPtr<IShellItem> shellItem;
                if (FAILED(items->GetItemAt(itemIndex, &shellItem)))
                    continue;

                wil::unique_cotaskmem_string filePath;
                if (FAILED(shellItem->GetDisplayName(SIGDN_FILESYSPATH, &filePath)))
                    continue;

                if (WriteUtf8Line(fileHandle.get(), filePath.get()))
                    ++writtenPathCount;
            }
        }

        if (writtenPathCount == 0)
        {
            DeleteFileW(tempFilePath);
            return S_OK;
        }

        // Command line: "exe_path" --file-list "tempfile"
        std::wstring commandLine = QuoteForCommandLineArgument(executablePath);
        commandLine += L" --file-list ";
        commandLine += QuoteForCommandLineArgument(tempFilePath);

        STARTUPINFOW startupInfo = { sizeof(startupInfo) };
        PROCESS_INFORMATION processInformation = {};

        if (CreateProcessW(
            executablePath.c_str(),
            commandLine.data(),
            nullptr,
            nullptr,
            FALSE,
            0,
            nullptr,
            moduleDirectory.c_str(),
            &startupInfo,
            &processInformation))
        {
            CloseHandle(processInformation.hThread);
            CloseHandle(processInformation.hProcess);
        }
        else
        {
            // The app never reads the list, so it is removed here
            DeleteFileW(tempFilePath);
        }

        return S_OK;
    }
    CATCH_RETURN()
};

CoCreatableClass(BatchRenamerATContextMenuCommand)
CoCreatableClassWrlCreatorMapInclude(BatchRenamerATContextMenuCommand)

STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _COM_Outptr_ void** instance)
{
    return Module<InProc>::GetModule().GetClassObject(rclsid, riid, instance);
}

STDAPI DllCanUnloadNow()
{
    return Module<InProc>::GetModule().GetObjectCount() == 0 ? S_OK : S_FALSE;
}

BOOL APIENTRY DllMain(HMODULE /*hModule*/, DWORD /*reason*/, LPVOID /*reserved*/)
{
    return TRUE;
}
