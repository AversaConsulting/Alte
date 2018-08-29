
Imports System.Collections.Specialized
Imports System.IO
Imports System.Web
Imports System.Web.Configuration
Imports System.Web.SessionState

Public Class AlteSessionProviderItem
    Inherits Alte.AlteObject

    Property SessionData As Byte()

End Class

Public Class AlteSessionProvider

    Inherits SessionStateStoreProviderBase

    ' using https://github.com/WindowsAzure-Toolkits/wa-toolkit-wp-nugets/blob/master/Storage.Providers/TableStorageSessionStateProvider.cs
    ' as a reference but writing from scratch
    ' http://www.yaldex.com/asp_net_tutorial/html/5c03c544-3239-430b-a813-161946753a1d.htm


    Private Azure As String

    Public Overrides Sub Initialize(name As String, config As NameValueCollection)

        MyBase.Initialize(name, config)
        'Dim ApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath
        'Dim cfg As System.Configuration.Configuration = WebConfigurationManager.OpenWebConfiguration(ApplicationName)
        'Dim pConfig = CType(cfg.GetSection("system.web/sessionState"), SessionStateSection)

        'Azure = pConfig.Providers(pConfig.CustomProvider).Parameters("AzurePassword")

    End Sub

    Public Overrides Function CreateNewStoreData(context As System.Web.HttpContext, timeout As Integer) As System.Web.SessionState.SessionStateStoreData

        Return New SessionStateStoreData(New SessionStateItemCollection(), SessionStateUtility.GetSessionStaticObjects(context), timeout)

    End Function

    Public Overrides Sub CreateUninitializedItem(context As System.Web.HttpContext, id As String, timeout As Integer)

        ' create a blank row? if we dont do this then just need to be able to cope with no record later on
        Stop

    End Sub

    Public Overrides Sub Dispose()
        'nothing to do
    End Sub

    Public Overrides Sub EndRequest(context As System.Web.HttpContext)
        'nothing to do

    End Sub

    Function Database() As Alte.AlteSession

        Dim configkey = "system.web/sessionState"
        Dim Section As SessionStateSection = WebConfigurationManager.GetSection(configkey)
        Dim StorageAccount = Section.StateConnectionString
        Dim DB = New Alte.AlteSession(Split(StorageAccount, "@")(0), Split(StorageAccount, "@")(1))

        Return DB

    End Function

    Public Overrides Function GetItem(context As System.Web.HttpContext, id As String, ByRef locked As Boolean, ByRef lockAge As System.TimeSpan, ByRef lockId As Object, ByRef actions As System.Web.SessionState.SessionStateActions) As System.Web.SessionState.SessionStateStoreData

        Try

            Dim DB = Database()

            'Return Deserialize(context, DB.ReadBlob("sessions", id), 0)
            Return Deserialize(context, DB.GetObjectByID(Of AlteSessionProviderItem)(id).SessionData, 0)

        Catch ex As Exception
            Return New SessionStateStoreData(New SessionStateItemCollection, SessionStateUtility.GetSessionStaticObjects(context), 0)
        End Try


    End Function

    Public Overrides Function GetItemExclusive(context As System.Web.HttpContext, id As String, ByRef locked As Boolean, ByRef lockAge As System.TimeSpan, ByRef lockId As Object, ByRef actions As System.Web.SessionState.SessionStateActions) As System.Web.SessionState.SessionStateStoreData

        Return GetItem(context, id, locked, lockAge, lockId, actions)

    End Function

    Public Overrides Sub InitializeRequest(context As System.Web.HttpContext)

        ' nothing to do

    End Sub

    Public Overrides Sub ReleaseItemExclusive(context As System.Web.HttpContext, id As String, lockId As Object)

        ' not supported
        ' Stop
    End Sub

    Public Overrides Sub RemoveItem(context As System.Web.HttpContext, id As String, lockId As Object, item As System.Web.SessionState.SessionStateStoreData)

        ' not supported
        Stop

    End Sub

    Public Overrides Sub ResetItemTimeout(context As System.Web.HttpContext, id As String)

        ' to do
        'Stop

    End Sub


    Function GetTimeStamp() As String

        Dim centuryBegin As Date = #1/1/2001 0:0:0#
        Dim currentDate As Date = Date.Now
        Dim elapsedTicks As Long = currentDate.Ticks - centuryBegin.Ticks
        Dim elapsedSpan As New TimeSpan(elapsedTicks)

        Return Format(elapsedSpan.TotalMinutes, "00000000")


    End Function


    Public Overrides Sub SetAndReleaseItemExclusive(context As System.Web.HttpContext, id As String, item As System.Web.SessionState.SessionStateStoreData, lockId As Object, newItem As Boolean)

        Dim DB = Database()
        'DB.WriteBlob("sessions", id, Serialize(CType(item.Items, SessionStateItemCollection)))
        Dim ASI As New AlteSessionProviderItem()
        ASI.ID = id
        ASI.SessionData = Serialize(CType(item.Items, SessionStateItemCollection))
        DB.SaveObject(Of AlteSessionProviderItem)(ASI)

    End Sub

    Public Overrides Function SetItemExpireCallback(expireCallback As System.Web.SessionState.SessionStateItemExpireCallback) As Boolean

        Return False
        'dont support

    End Function

    Private Function Serialize(items As SessionStateItemCollection) As Byte()

        Dim ms As MemoryStream = New MemoryStream()
        Dim writer As BinaryWriter = New BinaryWriter(ms)

        If Not items Is Nothing Then
            items.Serialize(writer)
        End If

        writer.Close()

        Return ms.ToArray()
    End Function


    Private Function Deserialize(context As HttpContext, serializedItems As Byte(), timeout As Integer) As SessionStateStoreData

        Dim ms As MemoryStream = New MemoryStream(serializedItems)
        Dim reader As BinaryReader = New BinaryReader(ms)
        Dim sessionItems As SessionStateItemCollection = SessionStateItemCollection.Deserialize(reader)

        Return New SessionStateStoreData(sessionItems, SessionStateUtility.GetSessionStaticObjects(context), timeout)

    End Function



End Class

