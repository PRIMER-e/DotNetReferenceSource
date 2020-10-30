//
// FxCop Violation Message Suppressions
//  Approved List
//

#include "win32inc.hpp"

using namespace System::Diagnostics::CodeAnalysis;

//***************************************************************************************************************************
// New Suppressions since 3.0 RTM
//***************************************************************************************************************************

//***************************************************************************************************************************
// Proposed suppressions for Orcas SP1
//***************************************************************************************************************************

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="MS.Internal.PrintWin32Thunk.AttributeNameToInfoLevelMapping.DriverThunk.DriverThunkingProfile.#RegisterAttributeMap()");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.Internal.GDIExporter.GeometryProxy.#ConvertToPathGeometry()");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.Internal.GDIExporter.TrueTypeFont.#write16(System.UInt32,System.UInt16)");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.Internal.GDIExporter.TrueTypeFont.#writeString(System.UInt32,System.UInt32,System.Char[],System.Text.Encoding)");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.Internal.GDIExporter.TrueTypeFont.#writeString(System.UInt32,System.UInt32,System.String,System.Text.Encoding)");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Printing.PrintDocumentImageableArea.#VerifyAccess()");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Printing.PrintJobInfoCollection.#VerifyAccess()");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Printing.PrintJobSettings.#VerifyAccess()");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Printing.PrintQueueCollection.#VerifyAccess()");

//**************************************************************************************************************************************************************
// 




CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Printing.PrintSystemObject.#CreatePropertiesDelegates()");

// New since V4 RTM:

//**************************************************************************************************************************************************************
// Bad FxCop detection:
// Install calls UpdateFromContext
// ProcessNameTable calls CheckSum
// ReplaceAll calls ReplaceFamilyName
//**************************************************************************************************************************************************************
CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.Internal.GDIExporter.FontInstallInfo.#UpdateFromContext({modreq(System.Runtime.CompilerServices.IsImplicitlyDereferenced)}Microsoft.Internal.GDIExporter.FontStreamContext)");
CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.Internal.GDIExporter.TrueTypeFont.#CheckSum(System.UInt32,System.UInt32)");
CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Scope="member", Target="Microsoft.Internal.GDIExporter.TrueTypeFont.#ReplaceFamilyName(System.UInt32,System.UInt32,System.Char[],System.Text.Encoding)");
