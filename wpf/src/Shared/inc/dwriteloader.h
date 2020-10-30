#pragma once

#include <windows.h>

namespace WPFUtils
{
    #if defined(__cplusplus_cli)
    /// <SecurityNote>
    /// Critical - Receives a native pointer as parameter.
    ///            Loads a dll from an input path.
    /// </SecurityNote>
    [System::Security::SecurityCritical]
    #endif
    HMODULE LoadDWriteLibraryAndGetProcAddress(void **pfncptrDWriteCreateFactory);
}
