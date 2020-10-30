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






CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Globalization","CA1305:SpecifyIFormatProvider", MessageId="System.String.Format(System.IFormatProvider,System.String,System.Object[])", Scope="member", Target="System.Printing.PrintQueue.#.cctor()");
CA_GLOBAL_SUPPRESS_MESSAGE("Microsoft.Globalization","CA1305:SpecifyIFormatProvider", MessageId="System.String.Format(System.IFormatProvider,System.String,System.Object[])", Scope="member", Target="System.Printing.PrintSystemJobInfo.#.cctor()");

