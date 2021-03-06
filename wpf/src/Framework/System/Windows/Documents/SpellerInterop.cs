//---------------------------------------------------------------------------
//
// File: SpellerInterop.cs
//
// Description: Custom COM marshalling code and interfaces for interaction
//              with the Natural Language Group's nl6 proofing engine.
//
//---------------------------------------------------------------------------

namespace System.Windows.Documents
{
    using System.Collections;
    using System.Runtime.InteropServices;
    using MS.Win32;
    using System.Security;
    using System.Security.Permissions;
    using System.IO;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using MS.Internal.PresentationFramework;

    // Custom COM marshalling code and interfaces for interaction
    // with the Natural Language Group's nl6 proofing engine.
    internal class SpellerInterop : IDisposable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Construct an NLG-based speller interop layer
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code calls into NlLoad, which elevates unmanaged code permission.
        ///     TreatAsSafe: This function call takes no input parameters
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal SpellerInterop()
        {
            // Start the lifetime of Natural Language library
            UnsafeNlMethods.NlLoad();

            bool exceptionThrown = true;
            try
            {
                //
                // Allocate the TextChunk.
                //

                _textChunk = CreateTextChunk();

                //
                // Allocate the TextContext.
                //

                ITextContext textContext = CreateTextContext();
                try
                {
                    _textChunk.put_Context(textContext);
                }
                finally
                {
                    Marshal.ReleaseComObject(textContext);
                }

                //
                // Set nl properties.
                //

                _textChunk.put_ReuseObjects(true);

                exceptionThrown = false;
            }
            finally
            {
                if (exceptionThrown)
                {
                    if (_textChunk != null)
                    {
                        Marshal.ReleaseComObject(_textChunk);
                        _textChunk = null;
                    }

                    UnsafeNlMethods.NlUnload();
                }
            }
        }

        ~SpellerInterop()
        {
            Dispose(false);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <SecurityNote>
        ///     Critical: This code calls into NlLoad, which elevates unmanaged code permission.
        ///     TreatAsSafe: This function call sets the current speller locale, which is harmless.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal void SetLocale(Int32 lcid)
        {
            _textChunk.put_Locale(lcid);
        }

        // Sets an indexed option on the speller's TextContext.
        /// <SecurityNote>
        /// Critical - This code extracts the TextContext which is a COM pointer with elevation code. it also
        /// sets TextContext's options based on untrusted input.
        /// </SecurityNote>
        [SecurityCritical]
        internal void SetContextOption(string option, object value)
        {
            ITextContext textContext;

            _textChunk.get_Context(out textContext);

            if (textContext != null)
            {
                try
                {
                    IProcessingOptions options;

                    textContext.get_Options(out options);
                    if (options != null)
                    {
                        try
                        {
                            options.put_Item(option, value);
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(options);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }
        }

        // Helper for methods that need to iterate over segments within a text run.
        // Returns the total number of segments encountered.
        /// <SecurityNote>
        /// Critical - access ITextChunk and calls Critical SetInputArray() with untrusted params.
        /// </SecurityNote>
        [SecurityCritical]
        internal int EnumTextSegments(char[] text, int count,
            EnumSentencesCallback sentenceCallback, EnumTextSegmentsCallback segmentCallback, object data)
        {
            int segmentCount = 0;

            // Unintuively, the speller engine will grab and store the pointer
            // we pass into ITextChunk.SetInputArray.  So it's not safe to merely
            // pinvoke text directly.  We need to allocate a chunk of memory
            // and keep it fixed for the duration of this method call.
            IntPtr inputArray = Marshal.AllocHGlobal(count * 2);

            try
            {
                // Give the TextChunk its next block of text.
                Marshal.Copy(text, 0, inputArray, count);
                _textChunk.SetInputArray(inputArray, count);

                //
                // Iterate over sentences.
                //

                UnsafeNativeMethods.IEnumVariant sentenceEnumerator;

                // Note because we're in the engine's ReuseObjects mode, we may
                // not use ITextChunk.get_Sentences.  We must use the enumerator.
                _textChunk.GetEnumerator(out sentenceEnumerator);
                try
                {
                    NativeMethods.VARIANT variant = new NativeMethods.VARIANT();
                    int[] fetched = new int[1];
                    bool continueIteration = true;

                    sentenceEnumerator.Reset();

                    do
                    {
                        int result;

                        variant.Clear();

                        result = EnumVariantNext(sentenceEnumerator, variant, fetched);

                        if (result != NativeMethods.S_OK)
                            break;
                        if (fetched[0] == 0)
                            break;

                        SpellerInterop.ISentence sentence = (SpellerInterop.ISentence)variant.ToObject();
                        try
                        {
                            int sentenceSegmentCount;
                            sentence.get_Count(out sentenceSegmentCount);

                            segmentCount += sentenceSegmentCount;

                            if (segmentCallback != null)
                            {
                                //
                                // Iterate over segments.
                                //

                                for (int i = 0; continueIteration && i < sentenceSegmentCount; i++)
                                {
                                    SpellerInterop.ITextSegment textSegment;

                                    // Do a callback for this ITextSegment.
                                    sentence.get_Item(i, out textSegment);
                                    try
                                    {
                                        if (!segmentCallback(textSegment, data))
                                        {
                                            continueIteration = false;
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.ReleaseComObject(textSegment);
                                    }
                                }
                            }

                            // Make another callback when we're done with the entire sentence.
                            if (sentenceCallback != null)
                            {
                                continueIteration = sentenceCallback(sentence, data);
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(sentence);
                        }
                    }
                    while (continueIteration);

                    variant.Clear();
                }
                finally
                {
                    Marshal.ReleaseComObject(sentenceEnumerator);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(inputArray);
            }

            return segmentCount;
        }

        // Enumerates a segment's subsegments, making a callback on each iteration.
        /// <SecurityNote>
        /// Critical - accesses ITextChunk.
        /// TreatAsSafe: Does not set any state.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void EnumSubsegments(object textSegmentHandle, EnumTextSegmentsCallback segmentCallback, object data)
        {
            ITextSegment textSegment = (ITextSegment)textSegmentHandle;
            int subSegmentCount;
            bool result = true;

            textSegment.get_Count(out subSegmentCount);

            // Walk the subsegments, the error's in there somewhere.
            for (int i = 0; result && i < subSegmentCount; i++)
            {
                ITextSegment subSegment;

                textSegment.get_Item(i, out subSegment);
                try
                {
                    result = segmentCallback(subSegment, data);
                }
                finally
                {
                    Marshal.ReleaseComObject(subSegment);
                }
            }
        }

        // Returns the final symbol offset of a sentence.
        /// <SecurityNote>
        ///     Critical: This code calls into NlLoad, which elevates unmanaged code permission.
        ///     TreatAsSafe: This function is readonly, and returns safe data.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static int GetSentenceEndOffset(object sentenceHandle)
        {
            ISentence sentence = (ISentence)sentenceHandle;

            int endOffset = -1;

            int sentenceSegmentCount;
            sentence.get_Count(out sentenceSegmentCount);

            if (sentenceSegmentCount > 0)
            {
                ITextSegment textSegment;
                STextRange sTextRange;

                sentence.get_Item(sentenceSegmentCount - 1, out textSegment);
                try
                {
                    textSegment.get_Range(out sTextRange);
                }
                finally
                {
                    Marshal.ReleaseComObject(textSegment);
                }

                endOffset = sTextRange.Start + sTextRange.Length;
            }

            return endOffset;
        }

        // Returns the subsegment count of a segment.
        /// <SecurityNote>
        ///     Critical: This code calls into NlLoad, which elevates unmanaged code permission.
        ///     TreatAsSafe: This function is readonly, and returns safe data.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static int GetSegmentCount(object textSegmentHandle)
        {
            ITextSegment textSegment = (ITextSegment)textSegmentHandle;
            int subSegmentCount;

            textSegment.get_Count(out subSegmentCount);

            return subSegmentCount;
        }

        // Returns the symbol offset and length of a segment.
        /// <SecurityNote>
        ///     Critical: This code calls into NlLoad, which elevates unmanaged code permission.
        ///     TreatAsSafe: This function is readonly, and returns safe data.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static STextRange GetSegmentRange(object textSegmentHandle)
        {
            ITextSegment textSegment = (ITextSegment)textSegmentHandle;
            STextRange sTextRange;

            textSegment.get_Range(out sTextRange);

            return sTextRange;
        }

        // Returns the role (error, etc.) of a segment.
        /// <SecurityNote>
        ///     Critical: This code calls into NlLoad, which elevates unmanaged code permission.
        ///     TreatAsSafe: This function is readonly, and returns safe data.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static RangeRole GetSegmentRole(object textSegmentHandle)
        {
            ITextSegment textSegment = (ITextSegment)textSegmentHandle;
            RangeRole role;

            textSegment.get_Role(out role);

            return role;
        }

        // Fills an array with suggestions matching a segment.
        // suggestions may be null.
        // If the segment has no suggestions (usually because it is not misspelled,
        // but also possible for errors the engine cannot make sense of, or that are
        // contained in sub-segments), this method returns false.
        /// <SecurityNote>
        /// Critical - it calls get_Suggestions(), which is Critical. it calls Marshal.PtrToStringUni, which LinkDemand's,
        /// with trusted params.
        /// TreatAsSafe - it calls it with a trusted variant out param.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static bool GetSuggestions(object textSegmentHandle, ArrayList suggestions)
        {
            ITextSegment textSegment = (ITextSegment)textSegmentHandle;
            UnsafeNativeMethods.IEnumVariant variantEnumerator;

            textSegment.get_Suggestions(out variantEnumerator);

            if (variantEnumerator == null)
            {
                // nl6 will return null enum instead of an empty enum.
                return false;
            }

            bool hasSuggestions = false;

            try
            {
                NativeMethods.VARIANT variant = new NativeMethods.VARIANT();
                int[] fetched = new int[1];

                while (true)
                {
                    int result;

                    variant.Clear();

                    result = EnumVariantNext(variantEnumerator, variant, fetched);

                    if (result != NativeMethods.S_OK)
                        break;
                    if (fetched[0] == 0)
                        break;

                    hasSuggestions = true;

                    if (suggestions != null)
                    {
                        // Convert the VARIANT to string, and add it to our list.
                        // There's some special magic here.  The VARIANT is VT_UI2/ByRef.
                        // But under the hood it's really a raw WCHAR *.
                        suggestions.Add(Marshal.PtrToStringUni(variant.data1.Value));
                    }
                    else
                    {
                        // Caller just wants to know if any suggestions are
                        // available, no need to iterate further.
                        break;
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(variantEnumerator);
            }

            return hasSuggestions;
        }

        /// <summary>
        /// Unloads given custom dictionary
        /// </summary>
        /// <param name="lexicon"></param>
        /// <SecurityNote>
        /// critical - works with critical _textChunk member
        /// </SecurityNote>
        [SecurityCritical]
        internal void UnloadDictionary(ILexicon lexicon)
        {
            ITextContext textContext = null;
            try
            {
                _textChunk.get_Context(out textContext);
                textContext.RemoveLexicon(lexicon);
            }
            finally
            {
                Marshal.ReleaseComObject(lexicon);

                if (textContext != null)
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }
        }

        /// <summary>
        /// Loads custom dictionary
        /// </summary>
        /// <param name="lexiconFilePath"></param>
        /// <returns></returns>
        /// <SecurityNote>
        /// critical - returns reference to internal wrapper to COM interface.
        /// </SecurityNote>
        [SecurityCritical]
        internal ILexicon LoadDictionary(string lexiconFilePath)
        {
            return AddLexicon(lexiconFilePath);
        }


        /// <summary>
        /// Loads custom dictionary.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="trustedFolder"></param>
        /// <returns></returns>
        /// <remarks>
        /// There are 2 kinds of files we're trying to load here: Files specified by user directly, and files
        /// which we created and filled with data from pack Uri locations specified by user.
        /// These 'trusted' files are placed under <paramref name="trustedFolder"/>.
        ///
        /// Explicitely specified file locations we will passed to ILexicon APIs without asserting
        /// Security permissions, so it would pass in FullTrust and fail in PartialTrust.
        ///
        /// Files specified in <paramref name="trustedFolder"/> are wrapped in FileIOPermission.Assert(),
        /// providing read access to trusted files under <paramref name="trustedFolder"/>, i.e. additionally
        /// we're making sure that specified trusted locations are under the trusted Folder.
        ///
        /// This is needed to differentiate a case when user passes in a local path location which just happens to be under
        /// trusted folder. We still want to fail in this case, since we want to trust only files that we've created.
        /// </remarks>
        /// <SecurityNote>
        /// Critical -
        /// 1. Works with paths, loads files. See also remarks section for more detail.
        /// 2. Asserts FileIOPermission to load file from specified locations.
        /// </SecurityNote>
        [SecurityCritical]
        internal ILexicon LoadDictionary(Uri item, string trustedFolder)
        {
            // Assert neccessary security to load trusted files.
            new FileIOPermission(FileIOPermissionAccess.Read, trustedFolder).Assert();
            try
            {
                return LoadDictionary(item.LocalPath);
            }
            finally
            {
                FileIOPermission.RevertAssert();
            }
        }

        /// <summary>
        /// Releases all currently loaded lexicons.
        /// </summary>
        /// <SecurityNote>
        /// Critical - uses security critical _textChunk
        /// </SecurityNote>
        [SecurityCritical]
        internal void ReleaseAllLexicons()
        {
            ITextContext textContext = null;
            try
            {
                _textChunk.get_Context(out textContext);
                Int32 lexiconCount = 0;
                textContext.get_LexiconCount(out lexiconCount);
                while (lexiconCount > 0)
                {
                    ILexicon lexicon = null;
                    textContext.get_Lexicon(0, out lexicon);
                    textContext.RemoveLexicon(lexicon);
                    Marshal.ReleaseComObject(lexicon);
                    lexiconCount--;
                }

            }
            finally
            {
                if (textContext != null)
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }

        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Types
        //
        //------------------------------------------------------

        #region Internal Types

        // Callback delegate for EnumTextSegments method.
        internal delegate bool EnumSentencesCallback(object sentence, object data);

        // Callback delegate for EnumTextSegments method.
        internal delegate bool EnumTextSegmentsCallback(object textSegment, object data);

        // typedef struct STextRange
        //     {
        //     long Start;
        //     long Length;
        //     } 	STextRange;
        [StructLayout(LayoutKind.Sequential)]
        internal struct STextRange
        {
            internal Int32 Start
            {
                get { return _start; }
            }

            internal Int32 Length
            {
                get { return _length; }
            }

            private readonly Int32 _start;
            private readonly Int32 _length;
        }

        internal enum SpellingReform
        {
            BothPreAndPost = 0,
            Prereform = 1,
            Postreform = 2,
            NotSet = 3,
        };

        internal enum RangeRole
        {
            ecrrSimpleSegment = 0,
            ecrrAlternativeForm = 1,
            ecrrIncorrect = 2,
            ecrrAutoReplaceForm = 3,
            ecrrCorrectForm = 4,
            ecrrPreferredForm = 5,
            ecrrNormalizedForm = 6,
            ecrrCompoundSegment = 7,
            ecrrPhraseSegment = 8,
            ecrrNamedEntity = 9,
            ecrrCompoundWord = 10,
            ecrrPhrase = 11,
            ecrrUnknownWord = 12,
            ecrrContraction = 13,
            ecrrHyphenatedWord = 14,
            ecrrContractionSegment = 15,
            ecrrHyphenatedSegment = 16,
            ecrrCapitalization = 17,
            ecrrAccent = 18,
            ecrrRepeated = 19,
            ecrrDefinition = 20,
            ecrrOutOfContext = 21,
        };

        #endregion Internal Types

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods



        //------------------------------------------------------
        //
        //  ILexicon management methods
        //
        //------------------------------------------------------
        #region ILexicon management methods

        /// <summary>
        /// Adds new custom dictionary to the spell engine.
        /// </summary>
        /// <param name="lexiconFilePath"></param>
        /// <returns>Reference to new ILexicon</returns>
        ///
        /// <SecurityNote>
        /// Critical - accesses files, which are critical resources. uses critical member _textChunk.
        /// Note that this method is part of logic for loading custom dicitonaries and it provides part of neccessary security
        /// related functionality to make <see cref="Speller.OnDictionaryUriAdded"/> TAS, and any changes
        /// need to be coordinated with that method.
        /// In particular this method
        /// - demands access to the file specified by a path before doing any work wtih it.
        /// - makes sure no file information is disclosed in PartialTrust if there was an exception.
        /// </SecurityNote>
        [SecurityCritical]
        private ILexicon AddLexicon(string lexiconFilePath)
        {
            ITextContext textContext = null;
            ILexicon lexicon = null;
            bool exception = true;
            bool hasDemand = false;

            try
            {
                FileIOPermission fileIOPermission = new FileIOPermission(FileIOPermissionAccess.Read, lexiconFilePath);
                fileIOPermission.Demand();
                hasDemand = true;

                lexicon = SpellerInterop.CreateLexicon();
                lexicon.ReadFrom(lexiconFilePath);
                _textChunk.get_Context(out textContext);
                textContext.AddLexicon(lexicon);
                exception = false;
            }
            catch (Exception e)
            {
                // We'll provide details of exception only if Demand to access lexiconFilePath was satisfied.
                // Otherwise it's a security concern to disclose this data.
                if (hasDemand)
                {
                    throw new ArgumentException(SR.Get(SRID.CustomDictionaryFailedToLoadDictionaryUri, lexiconFilePath), e);
                }
                else
                {
                    throw;// Demand has failed so we're rethrowing security exception.
                }
            }
            finally
            {
                if ((exception) &&(lexicon != null))
                {
                    Marshal.ReleaseComObject(lexicon);
                }
                if (null != textContext)
                {
                    Marshal.ReleaseComObject(textContext);
                }
            }
            return lexicon;
        }

        #endregion ILexicon management methods

        /// <summary>
        /// Internal interop resource cleanup
        /// </summary>
        /// <SecurityNote>
        ///     Critical: This code calls into NlUnload, which elevates unmanaged code permission.
        ///     TreatAsSafe: This function call takes no input memory block
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(SR.Get(SRID.TextEditorSpellerInteropHasBeenDisposed));

            if (_textChunk != null)
            {
                Marshal.ReleaseComObject(_textChunk);
                _textChunk = null;
            }

            // Stop the lifetime of Natural Language library
            UnsafeNlMethods.NlUnload();

            _isDisposed = true;
        }


        // Returns an object exported from NaturalLanguage6.dll's class factory.
        /// <SecurityNote>
        ///     Critical: This takes an arbitrary clsid and iid and calls NlGetClassObject.
        ///     It return a pointer to the COM object instantiated.
        /// </SecurityNote>
        [SecurityCritical]
        private static object CreateInstance(Guid clsid, Guid iid)
        {
            object classObject;
            UnsafeNlMethods.NlGetClassObject(ref clsid, ref iid, out classObject);
            return classObject;
        }

        // Creates a new ITextContext instance.
        /// <SecurityNote>
        /// Critical - Calls CreateInstance, which is Critical.
        /// </SecurityNote>
        [SecurityCritical]
        private static ITextContext CreateTextContext()
        {
            return (ITextContext)CreateInstance(CLSID_ITextContext, IID_ITextContext);
        }

        // Creates a new ITextChunk instance.
        /// <SecurityNote>
        /// Critical - Calls CreateInstance, which is Critical.
        /// </SecurityNote>
        [SecurityCritical]
        private static ITextChunk CreateTextChunk()
        {
            return (ITextChunk)CreateInstance(CLSID_ITextChunk, IID_ITextChunk);
        }

        // Creates a new ILexicon instance.
        /// <SecurityNote>
        /// Critical - Calls CreateInstance, which is Critical.
        /// </SecurityNote>
        [SecurityCritical]
        private static ILexicon CreateLexicon()
        {
            return (ILexicon)CreateInstance(CLSID_Lexicon, IID_ILexicon);
        }



        // Helper for IEnumVariant.Next call -- the debugger isn't displaying
        // variables in any method with the call.
        /// <SecurityNote>
        ///     Critical: This code has an unsafe code block where it dereferences an object
        ///      and calls a method with an elevation
        /// </SecurityNote>
        [SecurityCritical]
        private static int EnumVariantNext(UnsafeNativeMethods.IEnumVariant variantEnumerator, NativeMethods.VARIANT variant, int[] fetched)
        {
            int result;

            unsafe
            {
                fixed (void* pVariant = &variant.vt)
                {
                    result = variantEnumerator.Next(1, (IntPtr)pVariant, fetched);
                }
            }

            return result;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Interfaces
        //
        //------------------------------------------------------

        #region Private Interfaces

        private static class UnsafeNlMethods
        {
            /// <SecurityNote>
            ///     Critical: This elevates to unmanaged code permission
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            [DllImport(DllImport.PresentationNative, PreserveSig = false)]
            internal static extern void NlLoad();

            /// <SecurityNote>
            ///     Critical: This elevates to unmanaged code permission
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            [DllImport(DllImport.PresentationNative, PreserveSig = true)]
            internal static extern void NlUnload();

            /// <SecurityNote>
            ///     Critical: This elevates to unmanaged code permission
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            [DllImport(DllImport.PresentationNative, PreserveSig = false)]
            internal static extern void NlGetClassObject(ref Guid clsid, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object classObject);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("004CD7E2-8B63-4ef9-8D46-080CDBBE47AF")]
        internal interface ILexicon
        {
            //[
            //]
            //HRESULT ReadFrom ([in] BSTR filename);
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void ReadFrom ([MarshalAs( UnmanagedType.BStr )]string fileName);

            //[
            //]
            //HRESULT WriteTo ([in] BSTR filename);
            void stub_WriteTo ();

            //[
            //]
            //HRESULT GetEnumerator ([retval,out] ILexiconEntryEnumerator **enumerator);
            void stub_GetEnumerator ();

            //[
            //]
            //HRESULT IndexOf (
            //             [in] BSTR word,
            //             [out,retval] long *index);
            void stub_IndexOf();

            //[
            //]
            //HRESULT TagFor (
            //             [in] BSTR word,
            //             [in] long tagIndex,
            //             [out,retval] long *index);
            void stub_TagFor ();

            //[
            //]
            //HRESULT ContainsPrefix (
            //             [in] BSTR prefix,
            //             [out,retval] VARIANT_BOOL *containsPrefix);
            void stub_ContainsPrefix();

            //[
            //]
            //HRESULT Add ([in] BSTR entry);
            void stub_Add();

            //[
            //]
            //HRESULT Remove ([in] BSTR entry);
        	void stub_Remove();
            //[
            //    propget
            //]
            //HRESULT Version ([out, retval, ref] BSTR *pval);
            void stub_Version();


            //[
            //    helpstring("The number of elements in this collection."),
            //    propget
            //]
            //HRESULT Count ([out, retval, ref] long *pval);
            void stub_Count();


            //[
            //    helpstring("Get an enumerator of elements in this collection."),
            //    restricted,
            //    propget
            //]
            //HRESULT _NewEnum ([out, retval, ref] IEnumVARIANT **pval);
            void stub__NewEnum();


            //[
            //    propget
            //]
            //HRESULT Item (
            //             [in] long key,
            //    [out, retval, ref] ILexiconEntry **pval);
            void stub_get_Item();

            //[
            //    propput
            //]
            //HRESULT Item (
            //             [in] long key,
            //    [in] ILexiconEntry *val);
            void stub_set_Item();

            //[
            //    propget
            //]
            //HRESULT ItemByName (
            //             [in] BSTR key,
            //    [out, retval, ref] ILexiconEntry **pval);
            void stub_get_ItemByName();

            //[
            //    propput
            //]
            //HRESULT ItemByName (
            //             [in] BSTR key,
            //    [in] ILexiconEntry *val);
            void stub_set_ItemByName();

            //[
            //    propget
            //]
            //HRESULT PropertyCount ([out, retval, ref] long *pval);
            void stub_get0_PropertyCount();


            //[
            //    helpstring("The keys for this dictionary are the names of the properties, the value are VARIANTS."),
            //    propget
            //]
            //HRESULT Property (
            //             [in] VARIANT index,
            //    [out, retval, ref] VARIANT *pval);
            void stub_get1_Property();

            //[
            //    helpstring("The keys for this dictionary are the names of the properties, the value are VARIANTS."),
            //    propput
            //]
            //HRESULT Property (
            //             [in] VARIANT index,
            //    [in] VARIANT val);
            void stub_set_Property();

            //[
            //    propget
            //]
            //HRESULT IsSealed ([out, retval, ref] VARIANT_BOOL *pval);
            void stub_get_IsSealed();


            //[
            //    propget
            //]
            //HRESULT IsReadOnly ([out, retval, ref] VARIANT_BOOL *pval);
            void stub_get_IsReadOnly();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("B6797CC0-11AE-4047-A438-26C0C916EB8D")]
        private interface ITextContext
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultDialectCount )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_DefaultDialectCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultDialect )(
            //     ITextContext * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_DefaultDialect();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *AddDefaultDialect )(
            //     ITextContext * This,
            //     /* [in] */ LCID dicalect);
            void stub_AddDefaultDialect();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *RemoveDefaultDialect )(
            //     ITextContext * This,
            //     /* [in] */ LCID dicalect);
            void stub_RemoveDefaultDialect();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_LexiconCount )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_LexiconCount([MarshalAs(UnmanagedType.I4)] out Int32 lexiconCount);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Lexicon )(
            //     ITextContext * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ILexicon **pval);
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Lexicon(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ILexicon lexicon);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *AddLexicon )(
            //     ITextContext * This,
            //     /* [in] */ ILexicon *pLexicon);
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void AddLexicon([In, MarshalAs(UnmanagedType.Interface)] ILexicon lexicon);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *RemoveLexicon )(
            //     ITextContext * This,
            //     /* [in] */ ILexicon *pLexicon);
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void RemoveLexicon([In, MarshalAs(UnmanagedType.Interface)] ILexicon lexicon);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Version )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ BSTR *pval);
            void stub_get_Version();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_ResourceLoader )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ ILoadResources **pval);
            void stub_get_ResourceLoader();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_ResourceLoader )(
            //     ITextContext * This,
            //     /* [in] */ ILoadResources *val);
            void stub_put_ResourceLoader();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Options )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ IProcessingOptions **pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and returns a COM pointer
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Options([MarshalAs(UnmanagedType.Interface)] out IProcessingOptions val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Capabilities )(
            //     ITextContext * This,
            //     /* [in] */ LCID locale,
            //     /* [ref][retval][out] */ IProcessingOptions **pval);
            void get_Capabilities(Int32 locale, [MarshalAs(UnmanagedType.Interface)] out IProcessingOptions val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Lexicons )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Lexicons();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Lexicons )(
            //     ITextContext * This,
            //     /* [in] */ IEnumVARIANT *val);
            void stub_put_Lexicons();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_MaxSentences )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_MaxSentences();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_MaxSentences )(
            //     ITextContext * This,
            //     /* [in] */ long val);
            void stub_put_MaxSentences();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSingleLanguage )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSingleLanguage();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsSingleLanguage )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsSingleLanguage();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSimpleWordBreaking )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSimpleWordBreaking();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsSimpleWordBreaking )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsSimpleWordBreaking();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_UseRelativeTimes )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_UseRelativeTimes();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_UseRelativeTimes )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_UseRelativeTimes();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IgnorePunctuation )(
            // ITextContext * This,
            // /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IgnorePunctuation();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IgnorePunctuation )(
            // ITextContext * This,
            // /* [in] */ VARIANT_BOOL val);
            void stub_put_IgnorePunctuation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsCaching )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsCaching();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsCaching )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsCaching();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsShowingGaps )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsShowingGaps();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsShowingGaps )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsShowingGaps();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsShowingCharacterNormalizations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsShowingCharacterNormalizations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsShowingCharacterNormalizations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsShowingCharacterNormalizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsShowingWordNormalizations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsShowingWordNormalizations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsShowingWordNormalizations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsShowingWordNormalizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingCompounds )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingCompounds();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingCompounds )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingCompounds();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingInflections )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingInflections();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingInflections )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingInflections();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingLemmas )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingLemmas();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingLemmas )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingLemmas();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingExpansions )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingExpansions();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingExpansions )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingExpansions();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsComputingBases )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsComputingBases();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsComputingBases )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsComputingBases();

            // /* [propget][helpstring] */ HRESULT STDMETHODCALLTYPE get_IsComputingPartOfSpeechTags(
            // /* [ref][retval][out] */ VARIANT_BOOL *pval) = 0;
            void stub_get_IsComputingPartOfSpeechTags();

            // /* [propput][helpstring] */ HRESULT STDMETHODCALLTYPE put_IsComputingPartOfSpeechTags(
            // /* [in] */ VARIANT_BOOL val) = 0;
            void stub_put_IsComputingPartOfSpeechTags();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingDefinitions )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingDefinitions();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingDefinitions )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingDefinitions();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingDateTimeMeasures )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingDateTimeMeasures();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingDateTimeMeasures )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingDateTimeMeasures();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingPersons )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingPersons();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingPersons )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingPersons();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingLocations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingLocations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingLocations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingLocations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingOrganizations )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingOrganizations();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingOrganizations )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingOrganizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsFindingPhrases )(
            //     ITextContext * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsFindingPhrases();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsFindingPhrases )(
            //     ITextContext * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsFindingPhrases();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("549F997E-0EC3-43d4-B443-2BF8021010CF")]
        private interface ITextChunk
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_InputText )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ BSTR *pval);
            void stub_get_InputText();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_InputText )(
            //     ITextChunk * This,
            //     /* [in] */ BSTR val);
            void stub_put_InputText();

            // /* [restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetInputArray )(
            //     ITextChunk * This,
            //     /* [string][in] */ LPCWSTR str,
            //     /* [in] */ long size);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void SetInputArray([In] IntPtr inputArray, Int32 size);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *RegisterEngine )(
            //     ITextChunk * This,
            //     /* [in] */ GUID *guid,
            //     /* [in] */ BSTR dllName);
            void stub_RegisterEngine();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *UnregisterEngine )(
            //     ITextChunk * This,
            //     /* [in] */ GUID *guid);
            void stub_UnregisterEngine();

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_InputArray )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ LPCWSTR *pval);
            void stub_get_InputArray();

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_InputArrayRange )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ STextRange *pval);
            void stub_get_InputArrayRange();

            // /* [propput][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_InputArrayRange )(
            //     ITextChunk * This,
            //     /* [in] */ STextRange val);
            void stub_put_InputArrayRange();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ long *pval);
            void get_Count(out Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     ITextChunk * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ISentence **pval);
            void get_Item(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ISentence val);

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get__NewEnum();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Sentences )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            /// <SecurityNote>
            ///   Critical : Returns critical argument of type IEnumVariant
            /// </SecurityNote>
            [SecurityCritical]
            void get_Sentences([MarshalAs(UnmanagedType.Interface)] out MS.Win32.UnsafeNativeMethods.IEnumVariant val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Context )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ ITextContext **pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Context([MarshalAs(UnmanagedType.Interface)] out ITextContext val);

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Context )(
            //     ITextChunk * This,
            //     /* [in] */ ITextContext *val);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and take a COM pointer
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void put_Context([MarshalAs(UnmanagedType.Interface)] ITextContext val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_Locale();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Locale )(
            //     ITextChunk * This,
            //     /* [in] */ LCID val);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void put_Locale(Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsLocaleReliable )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsLocaleReliable();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsLocaleReliable )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsLocaleReliable();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsEndOfDocument )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsEndOfDocument();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_IsEndOfDocument )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT_BOOL val);
            void stub_put_IsEndOfDocument();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetEnumerator )(
            //     ITextChunk * This,
            //     /* [retval][out] */ IEnumVARIANT **ppSent);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and returns a COM pointer
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out MS.Win32.UnsafeNativeMethods.IEnumVariant val);

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ToString )(
            //     ITextChunk * This,
            //     /* [retval][out] */ BSTR *pstr);
            void stub_ToString();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ProcessStream )(
            //     ITextChunk * This,
            //     /* [in] */ IRangedTextSource *input,
            //     /* [out][in] */ IRangedTextSink *output);
            void stub_ProcessStream();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_ReuseObjects )(
            //     ITextChunk * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void get_ReuseObjects(out bool val);

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_ReuseObjects )(
            //     ITextChunk * This,
            //     /* [in] */ VARIANT_BOOL val);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void put_ReuseObjects(bool val);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("F0C13A7A-199B-44be-8492-F91EAA50F943")]
        private interface ISentence
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ISentence * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ISentence * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ long *pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Count(out Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Parent )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ ITextChunk **pval);
            void stub_get_Parent();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     ISentence * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ITextSegment **pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and retrieves a pointer
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Item(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ITextSegment val);

            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get__NewEnum();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Segments )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Segments();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetEnumerator )(
            //     ISentence * This,
            //     /* [retval][out] */ IEnumVARIANT **string);
            void stub_GetEnumerator();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsEndOfParagraph )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsEndOfParagraph();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsUnfinished )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsUnfinished();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsUnfinishedAtEnd )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsUnfinishedAtEnd();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_Locale();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsLocaleReliable )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsLocaleReliable();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Range )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ STextRange *pval);
            void stub_get_Range();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_RequiresNormalization )(
            //     ISentence * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_RequiresNormalization();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ToString )(
            //     ISentence * This,
            //     /* [retval][out] */ BSTR *string);
            void stub_ToString();

            // /* [helpstring][restricted] */ HRESULT ( STDMETHODCALLTYPE *CopyToString )(
            //     ISentence * This,
            //     /* [in][string] */ LPWSTR pStr,
            //     /* [in][out] */ long* pcch,
            //     /* [in] */ VARIANT_BOOL fAlwaysCopy);
            void stub_CopyToString();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("AF4656B8-5E5E-4fb2-A2D8-1E977E549A56")]
        private interface ITextSegment
        {
            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSurfaceString )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSurfaceString();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Range )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ STextRange *pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and retrieves a range
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Range([MarshalAs(UnmanagedType.Struct)] out STextRange val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Identifier )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_Identifier();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Unit )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ MeasureUnit *pval);
            void stub_get_Unit();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Count(out Int32 val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     ITextSegment * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ ITextSegment **pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and retrieves a pi
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Item(Int32 index, [MarshalAs(UnmanagedType.Interface)] out ITextSegment val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Expansions )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Expansions();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Bases )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Bases();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SuggestionScores )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_SuggestionScores();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PropertyCount )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_PropertyCount();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Property )(
            //     ITextSegment * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Property();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Property )(
            //     ITextSegment * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            void stub_put_Property();

            // /* [helpstring][restricted] */ HRESULT ( STDMETHODCALLTYPE *CopyToString )(
            //     ISentence * This,
            //     /* [in][string] */ LPWSTR pStr,
            //     /* [in][out] */ long* pcch,
            //     /* [in] */ VARIANT_BOOL fAlwaysCopy);
            void stub_CopyToString();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Role )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ RangeRole *pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and retrieves a range
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Role(out RangeRole val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_PrimaryType )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ PrimaryRangeType *pval);
            void stub_get_PrimaryType();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SecondaryType )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ SecondaryRangeType *pval);
            void stub_get_SecondaryType();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SpellingVariations )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_SpellingVariations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_CharacterNormalizations )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_CharacterNormalizations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Representations )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Representations();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Inflections )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Inflections();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Suggestions )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code and retrieves a range
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void get_Suggestions([MarshalAs(UnmanagedType.Interface)] out MS.Win32.UnsafeNativeMethods.IEnumVariant val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Lemmas )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Lemmas();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SubSegments )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_SubSegments();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Alternatives )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get_Alternatives();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ToString )(
            //     ITextSegment * This,
            //     /* [retval][out] */ BSTR *string);
            void stub_ToString();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsPossiblePhraseStart )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsPossiblePhraseStart();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_SpellingScore )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_SpellingScore();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsPunctuation )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsPunctuation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsEndPunctuation )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsEndPunctuation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSpace )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSpace();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsAbbreviation )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsAbbreviation();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsSmiley )(
            //     ITextSegment * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsSmiley();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("C090356B-A6A5-442a-A204-CFD5415B5902")]
        private interface IProcessingOptions
        {
            // /* [propget][restricted][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ IEnumVARIANT **pval);
            void stub_get__NewEnum();

            // /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetEnumerator )(
            //     IProcessingOptions * This,
            //     /* [retval][out] */ IEnumVARIANT **ppSent);
            void stub_GetEnumerator();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Locale )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ LCID *pval);
            void stub_get_Locale();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Count )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ long *pval);
            void stub_get_Count();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
            //     IProcessingOptions * This,
            //     /* [in] */ long index,
            //     /* [ref][retval][out] */ BSTR *pval);
            void stub_get_Name();

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_Item )(
            //     IProcessingOptions * This,
            //     /* [in] */ VARIANT index,
            //     /* [ref][retval][out] */ VARIANT *pval);
            void stub_get_Item();

            // /* [propput][helpstring] */ HRESULT ( STDMETHODCALLTYPE *put_Item )(
            //     IProcessingOptions * This,
            //     /* [in] */ VARIANT index,
            //     /* [in] */ VARIANT val);
            /// <SecurityNote>
            ///     Critical: Elevates to call unmanaged code
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void put_Item(object index, object val);

            // /* [propget][helpstring] */ HRESULT ( STDMETHODCALLTYPE *get_IsReadOnly )(
            //     IProcessingOptions * This,
            //     /* [ref][retval][out] */ VARIANT_BOOL *pval);
            void stub_get_IsReadOnly();
        }

        #endregion Private Interfaces

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <SecurityNote>
        ///     Critical: This code holds reference to a COM object which can run code under elevation
        /// </SecurityNote>
        [SecurityCritical]
        private ITextChunk _textChunk;

        // True after this object has been disposed.
        private bool _isDisposed;

        // 333E6924-4353-4934-A7BE-5FB5BDDDB2D6
        private static readonly Guid CLSID_ITextContext = new Guid(0x333E6924, 0x4353, 0x4934, 0xA7, 0xBE, 0x5F, 0xB5, 0xBD, 0xDD, 0xB2, 0xD6);

        // B6797CC0-11AE-4047-A438-26C0C916EB8D
        private static readonly Guid IID_ITextContext = new Guid(0xB6797CC0, 0x11AE, 0x4047, 0xA4, 0x38, 0x26, 0xC0, 0xC9, 0x16, 0xEB, 0x8D);

        // 89EA5B5A-D01C-4560-A874-9FC92AFB0EFA
        private static readonly Guid CLSID_ITextChunk = new Guid(0x89EA5B5A, 0xD01C, 0x4560, 0xA8, 0x74, 0x9F, 0xC9, 0x2A, 0xFB, 0x0E, 0xFA);

        // 549F997E-0EC3-43d4-B443-2BF8021010CF
        private static readonly Guid IID_ITextChunk = new Guid(0x549F997E, 0x0EC3, 0x43d4, 0xB4, 0x43, 0x2B, 0xF8, 0x02, 0x10, 0x10, 0xCF);

        private static readonly Guid CLSID_Lexicon = new Guid("D385FDAD-D394-4812-9CEC-C6575C0B2B38");
        private static readonly Guid IID_ILexicon = new Guid("004CD7E2-8B63-4ef9-8D46-080CDBBE47AF");

        #endregion Private Fields
    }
}

