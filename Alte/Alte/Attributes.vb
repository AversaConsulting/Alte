Public Class IndexAttribute
    Inherits Attribute
End Class

Public Class IgnoreAttribute
    Inherits Attribute
End Class

Public Class FTSindexAttribute
    Inherits Attribute
End Class

Public Class FTSstoreAttribute
    Inherits Attribute
End Class

Public Class DefaultValueAttribute
    Inherits Attribute
    Sub New(Value As Object)
        Me.Value = Value
    End Sub
    Property Value As Object
End Class