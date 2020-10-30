'******************************************************************************
'* ConsoleApplicationBase.vb
'*
'* Copyright (c) Microsoft Corporation.  All rights reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Strict On
Option Explicit On

Imports System.Reflection
Imports System.ComponentModel
Imports System.Security.Permissions
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.MyServices
Imports Microsoft.VisualBasic.CompilerServices
Imports ExUtils = Microsoft.VisualBasic.CompilerServices.ExceptionUtils


Namespace Microsoft.VisualBasic.ApplicationServices

    '''**************************************************************************
    ''' ;ConsoleApplicationBase
    ''' <summary>
    ''' Abstract class that defines the application Startup/Shutdown model for VB 
    ''' Windows Applications such as console, winforms, dll, service.
    ''' </summary>
    ''' <remarks></remarks>
    <HostProtection(Resources:=HostProtectionResource.ExternalProcessMgmt)> _
    Public Class ConsoleApplicationBase : Inherits ApplicationBase

        '= PUBLIC =============================================================

        '''**************************************************************************
        ''' ;New
        ''' <summary>
        ''' Constructs the application Shutdown/Startup model object
        ''' </summary>
        ''' <remarks>We have to have a parameterless ctor because the platform specific Application 
        ''' object derives from this one and it doesn't define a ctor.  The partial class generated by the
        ''' designer defines the ctor in order to configure the application.</remarks>
        Public Sub New()
            MyBase.New()
        End Sub

        '''**************************************************************************
        ''' ;CommandLineArgs
        ''' <summary>
        '''  Returns the command line arguments for the current application.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>This function differs from System.Environment.GetCommandLineArgs in that the
        ''' path of the executing file (the 0th entry) is omitted from the returned collection</remarks>
        Public ReadOnly Property CommandLineArgs() As System.Collections.ObjectModel.ReadOnlyCollection(Of String)
            Get
                If m_CommandLineArgs Is Nothing Then
                    'Get rid of Arg(0) which is the path of the executing program.  Main(args() as string) doesn't report the name of the app and neither will we
                    Dim EnvArgs As String() = System.Environment.GetCommandLineArgs
                    If EnvArgs.GetLength(0) >= 2 Then '1 element means no args, just the executing program.  >= 2 means executing program + one or more command line arguments
                        Dim NewArgs(EnvArgs.GetLength(0) - 2) As String 'dimming z(0) gives a z() of 1 element.
                        System.Array.Copy(EnvArgs, 1, NewArgs, 0, EnvArgs.GetLength(0) - 1) 'copy everything but the 0th element (the path of the executing program)
                        m_CommandLineArgs = New System.Collections.ObjectModel.ReadOnlyCollection(Of String)(NewArgs)
                    Else
                        m_CommandLineArgs = New System.Collections.ObjectModel.ReadOnlyCollection(Of String)(New String() {})  'provide the empty set
                    End If
                End If
                Return m_CommandLineArgs
            End Get
        End Property

        '''*************************************************************************
        ''';Deployment
        ''' <summary>
        '''  Gives access to the current ApplicationDeployment via My
        ''' </summary>
        ''' <value>The current ApplicationDeployment</value>
        ''' <remarks></remarks>
        Public ReadOnly Property Deployment() As System.Deployment.Application.ApplicationDeployment
            Get
                Return System.Deployment.Application.ApplicationDeployment.CurrentDeployment
            End Get
        End Property

        '''*************************************************************************
        ''';IsNetworkDeployed
        ''' <summary>
        '''  Indicates whether or not the current application was deployed
        ''' </summary>
        ''' <value>True if the current application was deployed, otherwise False</value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsNetworkDeployed() As Boolean
            Get
                Return System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed
            End Get
        End Property

        '= PROTECTED =============================================================

        '''*************************************************************************
        ''';InternalCommandLine
        ''' <summary>
        ''' Allows derived classes to set what the command line should look like.  WindowsFormsApplicationBase calls this
        ''' for instance because we snag the command line from Main().
        ''' </summary>
        ''' <remarks></remarks>
        <EditorBrowsable(EditorBrowsableState.Advanced)> Protected WriteOnly Property InternalCommandLine() As System.Collections.ObjectModel.ReadOnlyCollection(Of String)
            Set(ByVal value As System.Collections.ObjectModel.ReadOnlyCollection(Of String))
                m_CommandLineArgs = value
            End Set
        End Property

        '= FRIEND =============================================================

        '= PRIVATE ==========================================================

        Private m_CommandLineArgs As System.Collections.ObjectModel.ReadOnlyCollection(Of String) ' Lazy-initialized and cached collection of command line arguments.
    End Class 'ApplicationBase
End Namespace
