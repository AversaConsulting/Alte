Public Class FullTextResultComparer
    Implements IEqualityComparer(Of FullTextResult)

    Public Overloads Function Equals(x As FullTextResult, y As FullTextResult) As Boolean Implements IEqualityComparer(Of FullTextResult).Equals

        If x.ID = y.ID Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Overloads Function GetHashCode(obj As FullTextResult) As Integer Implements IEqualityComparer(Of FullTextResult).GetHashCode

        Return obj.ID.GetHashCode

    End Function
End Class