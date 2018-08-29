Imports System.Net
Imports System.Reflection
Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Table
Imports Microsoft.WindowsAzure.Storage.Blob
Imports System.Text.RegularExpressions
Imports Alte

<Serializable>
Public Class AlteObject


    Sub New(AlteSession As AlteSession)
        Me.Session = AlteSession
    End Sub

    Sub New()

    End Sub

    Sub Save()

        If Me.Session Is Nothing Then
            Throw New Exception("No session set")
        End If

        Me.Session.SaveObject(Of AlteObject)(Me)

    End Sub

    Overridable Sub onSaving(ByRef Cancel As Boolean)

    End Sub

    Overridable Sub onSaved(isNew As Boolean)

    End Sub

    Overridable Sub onDeleting(ByRef Cancel As Boolean)

    End Sub


    Overridable Sub onDeleted()

    End Sub


    <Ignore, IgnoreProperty>
    Property Session As AlteSession

    ''' <summary>
    ''' Internal Use Only
    ''' </summary>
    ''' <returns></returns>
    <Ignore>
    Property IndexRowKeys As New List(Of String)

    ' store the etag of the one value we retrieved - then when we save, pass this into
    ' this update or delete and the batch will fail if changed as we write EVERY instance every time
    ''' <summary>
    ''' Internal use only
    ''' </summary>
    ''' <returns></returns>
    <Ignore>
    Property EtagKey As String

    ''' <summary>
    ''' Internal use only
    ''' </summary>
    ''' <returns></returns>
    <Ignore>
    Property EtagValue As String

    Public _isNew As Boolean = True

    Private _id As String
    Property ID As String
        Get
            Return _id
        End Get
        Set(value As String)

            If _isNew Then
                _id = value
            Else
                Throw New Exception("Can't set ID for existing record")
            End If
            'to implement this we need to flatten/restore ID ourselves

        End Set
    End Property

    Sub SetID(Optional ID_Type As ID_Style = ID_Style.EasyBase30_12)

        ID = CreateID(ID_Type)

    End Sub

End Class