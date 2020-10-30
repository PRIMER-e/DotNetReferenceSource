//---------------------------------------------------------------------------
//---------------------------------------------------------------------------
//
// <copyright file=TextParentUndoUnit.cs company=Microsoft>
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// Description: 
//
//      See spec at http://avalon/uis/Stock%20Services/Undo%20spec.htm 
// 
// History:  
//  03/23/2004 : eveselov - created
//
//---------------------------------------------------------------------------
using System;
using MS.Internal;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents.Internal;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Markup;
using System.Text;
using MS.Utility;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    /// <summary>
    /// TextParentUndoUnit
    /// </summary>
    internal class TextParentUndoUnit : ParentUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="selection">
        /// TextSelection before executing the operation.
        /// </param>
        internal TextParentUndoUnit(ITextSelection selection)
            : this(selection, selection.AnchorPosition, selection.MovingPosition)
        {
        }

        internal TextParentUndoUnit(ITextSelection selection, ITextPointer anchorPosition, ITextPointer movingPosition)
            : base(String.Empty)
        {
            _selection = selection;

            _undoAnchorPositionOffset = anchorPosition.Offset;
            _undoAnchorPositionDirection = anchorPosition.LogicalDirection;
            _undoMovingPositionOffset = movingPosition.Offset;
            _undoMovingPositionDirection = movingPosition.LogicalDirection;

            // 






#if DEBUG
            _redoAnchorPositionOffset = -1;
            _redoMovingPositionOffset = -1;
#else
            _redoAnchorPositionOffset = 0;
            _redoMovingPositionOffset = 0;
#endif
        }

        /// <summary>
        /// Creates a redo unit from an undo unit.
        /// </summary>
        protected TextParentUndoUnit(TextParentUndoUnit undoUnit)
            : base(String.Empty)
        {
            _selection = undoUnit._selection;

            _undoAnchorPositionOffset = undoUnit._redoAnchorPositionOffset;
            _undoAnchorPositionDirection = undoUnit._redoAnchorPositionDirection;
            _undoMovingPositionOffset = undoUnit._redoMovingPositionOffset;
            _undoMovingPositionDirection = undoUnit._redoMovingPositionDirection;

            // 






#if DEBUG
            _redoAnchorPositionOffset = -1;
            _redoMovingPositionOffset = -1;
#else
            _redoAnchorPositionOffset = 0;
            _redoMovingPositionOffset = 0;
#endif
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Implements IUndoUnit::Do().  For IParentUndoUnit, this means iterating through
        /// all contained units and calling their Do().
        /// </summary>
        public override void Do()
        {
            base.Do(); // Note: TextParentUndoUnit will be created here by our callback CreateParentUndoUnitForSelf.

            ITextContainer textContainer = _selection.Start.TextContainer;
            ITextPointer anchorPosition = textContainer.CreatePointerAtOffset(_undoAnchorPositionOffset, _undoAnchorPositionDirection);
            ITextPointer movingPosition = textContainer.CreatePointerAtOffset(_undoMovingPositionOffset, _undoMovingPositionDirection);

            _selection.Select(anchorPosition, movingPosition);

            _redoUnit.RecordRedoSelectionState();
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implements a callback called from base.Do method for
        /// creating appropriate ParentUndoUnit for redo.
        /// </summary>
        /// <returns></returns>
        protected override IParentUndoUnit CreateParentUndoUnitForSelf()
        {
            _redoUnit = CreateRedoUnit();
            return _redoUnit;
        }

        protected virtual TextParentUndoUnit CreateRedoUnit()
        {
            return new TextParentUndoUnit(this);
        }

        protected void MergeRedoSelectionState(TextParentUndoUnit undoUnit)
        {
            _redoAnchorPositionOffset = undoUnit._redoAnchorPositionOffset;
            _redoAnchorPositionDirection = undoUnit._redoAnchorPositionDirection;
            _redoMovingPositionOffset = undoUnit._redoMovingPositionOffset;
            _redoMovingPositionDirection = undoUnit._redoMovingPositionDirection;
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// This method should be called just before the undo unit is closed.  It will capture
        /// the current selectionStart and selectionEnd offsets for use later when this undo unit
        /// gets Redone.
        /// </summary>
        internal void RecordRedoSelectionState()
        {
            RecordRedoSelectionState(_selection.AnchorPosition, _selection.MovingPosition);
        }

        /// <summary>
        /// This method should be called just before the undo unit is closed.  It will capture
        /// the current selectionStart and selectionEnd offsets for use later when this undo unit
        /// gets Redone.
        /// </summary>
        internal void RecordRedoSelectionState(ITextPointer anchorPosition, ITextPointer movingPosition)
        {
            _redoAnchorPositionOffset = anchorPosition.Offset;
            _redoAnchorPositionDirection = anchorPosition.LogicalDirection;
            _redoMovingPositionOffset = movingPosition.Offset;
            _redoMovingPositionDirection = movingPosition.LogicalDirection;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly ITextSelection _selection;

        private readonly int _undoAnchorPositionOffset;
        private readonly LogicalDirection _undoAnchorPositionDirection;

        private readonly int _undoMovingPositionOffset;
        private readonly LogicalDirection _undoMovingPositionDirection;

        private int _redoAnchorPositionOffset;
        private LogicalDirection _redoAnchorPositionDirection;

        private int _redoMovingPositionOffset;
        private LogicalDirection _redoMovingPositionDirection;

        private TextParentUndoUnit _redoUnit;

#if DEBUG
        // Debug-only unique identifier for this instance.
        private readonly int _debugId = _debugIdCounter++;

        // Debug-only id counter.
        private static int _debugIdCounter;
#endif
        #endregion Private Fields
    }
}
