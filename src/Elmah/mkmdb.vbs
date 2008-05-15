'
' ELMAH - Error Logging Modules and Handlers for ASP.NET
' Copyright (c) 2007 Atif Aziz. All rights reserved.
'
'  Author(s):
'
'      Atif Aziz, http:'www.raboof.com
'
' This library is free software; you can redistribute it and/or modify it 
' under the terms of the New BSD License, a copy of which should have 
' been delivered along with this distribution.
'
' THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
' "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
' LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
' PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
' OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
' SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
' LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
' DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
' THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
' (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
' OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
'

' enum KeyTypeEnum

Const adKeyPrimary = 1
Const adKeyForeign = 2
Const adKeyUnique = 3

' enum ColumnAttributesEnum

Const adColFixed = 1
Const adColNullable = 2

' enum DataTypeEnum

Const adEmpty = 0
Const adTinyInt = 16
Const adSmallInt = 2
Const adInteger = 3
Const adBigInt = 20
Const adUnsignedTinyInt = 17
Const adUnsignedSmallInt = 18
Const adUnsignedInt = 19
Const adUnsignedBigInt = 21
Const adSingle = 4
Const adDouble = 5
Const adCurrency = 6
Const adDecimal = 14
Const adNumeric = 131
Const adBoolean = 11
Const adError = 10
Const adUserDefined = 132
Const adVariant = 12
Const adIDispatch = 9
Const adIUnknown = 13
Const adGUID = 72
Const adDate = 7
Const adDBDate = 133
Const adDBTime = 134
Const adDBTimeStamp = 135
Const adBSTR = 8
Const adChar = 129
Const adVarChar = 200
Const adLongVarChar = 201
Const adWChar = 130
Const adVarWChar = 202
Const adLongVarWChar = 203
Const adBinary = 128
Const adVarBinary = 204
Const adLongVarBinary = 205
Const adChapter = 136
Const adFileTime = 64
Const adPropVariant = 138
Const adVarNumeric = 139

Function CreateTable(ByVal Catalog, ByVal Name)

    Set Table = CreateObject("ADOX.Table")
    Set Table.ParentCatalog = Catalog
    Table.Name = Name

    ' See http://msdn.microsoft.com/en-us/library/aa164917(office.10).aspx for 
    ' type mappings between MS Access and ADOX

    With Table.Columns
        .Append "ErrorId", adVarWChar, 32
        .Append "Application", adVarWChar, 60
        ' TODO: Check why this is 30 in AccessErrorLog but 50 in original MDB?
        .Append "Host", adVarWChar, 30
        .Append "Type", adVarWChar, 100
        .Append "Source", adVarWChar, 60
        .Append "Message", adLongVarWChar
        .Append "User", adVarWChar, 60
        .Append "StatusCode", adInteger
        .Append "TimeUtc", adDate
        .Append "SequenceNumber", adInteger
        .Item("SequenceNumber").Properties("AutoIncrement") = True
        .Append "AllXml", adLongVarWChar
    End With

    Table.Keys.Append "PrimaryKey", adKeyPrimary, "ErrorId"

    Catalog.Tables.Append Table
    Set CreateTable = Table

End Function

Sub Main()

    ' TODO: See if the MDB file already exists?
    ' Set Connection = CreateObject("ADODB.Connection")
    ' Connection.Open "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=biblio.mdb"
    ' Set Catalog.ActiveConnection = Connection
    
    ' TODO: Allow the file name to be sent in as an argument
    
    Set Catalog = CreateObject("ADOX.Catalog")
    Catalog.Create "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=elmah.mdb"
    CreateTable Catalog, "ELMAH_Error"

End Sub

Main