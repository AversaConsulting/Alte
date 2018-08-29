Imports System.Runtime.InteropServices
'

'           Porter stemmer in VB.NET, translation of the CSharp port (csharp2.txt).

'           The original paper is in

'                   Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
'                   no. 3, pp 130-137,

'           See also http://www.tartarus.org/~martin/PorterStemmer

'           History:

'           Release 1

'           Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
'           The words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
'           is then out outside the bounds of b.

'           Release 2

'           Similarly,

'           Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
'           'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
'           b[j] is then outside the bounds of b.

'           Release 3

'           Considerably revised 4/9/00 in the light of many helpful suggestions
'           from Brian Goetz of Quiotix Corporation (brian@quiotix.com).

'           Release 4

'           This revision allows the Porter Stemmer Algorithm to be exported via the
'           .NET Framework. To facilate its use via .NET, the following commands need to be
'           issued to the operating system to register the component so that it can be
'           imported into .Net compatible languages, such as Delphi.NET, Visual C#.NET,
'           Visual C++.NET, etc.

'           1. Create a stong name:
'                        sn -k Keyfile.snk
'           2. Compile the VB.NET class, which creates an assembly PorterStemmerAlgorithm.dll
'                        vbc /t:library PorterStemmerAlgorithm.vb
'           3. Register the dll with the Windows Registry
'                  and so expose the interface to COM Clients via the type library
'                  ( PorterStemmerAlgorithm.tlb will be created)
'                        regasm /tlb PorterStemmerAlgorithm.dll
'           4. Load the component in the Global Assembly Cache
'                        gacutil -i PorterStemmerAlgorithm.dll

'           Note: You must have the .Net Studio installed.

'           Once this process is performed you should be able to import the class
'           via the appropiate mechanism in the language that you are using.

'           i.e in Delphi 7 .NET this is simply a matter of selecting:
'                        Project | Import Type Libary
'           And then selecting Porter stemmer in VB.NET Version 1.4"!




'            Stemmer, implementing the Porter Stemming Algorithm
'
'            The Stemmer class transforms a word into its root form.  The input
'            word can be provided a character at time (by calling add()), or at once
'            by calling one of the various stem(something) methods.
'

Public Interface StemmerInterface
        Function stemTerm(ByVal s As String) As String
    End Interface

    <ClassInterface(ClassInterfaceType.None)> Public Class PorterStemmer
        Implements StemmerInterface

        Public b As Char()

        Private i As Integer                  ' offset into b
        Private i_end As Integer                  ' offset to end of stemmed word
        Private j, k As Integer
        Private Shared INC As Integer = 200                  ' unit of size whereby b is increased

        Public Sub New()
            b = New Char(INC) {}
            i = 0
            i_end = 0
        End Sub

    ' Implementation of the .NET interface - added as part of release 4 (Leif)
    Public Function stemTerm(ByVal s As String) As String Implements StemmerInterface.stemTerm

        If Left(s, 6).ToLower = "nostem" Then
            Return s
        End If

        setTerm(s)
        stem()
        Return getTerm()
    End Function


    '        SetTerm and GetTerm have been simply added to ease the
    '        interface with other lanaguages. They replace the add functions
    '        and toString function. This was done because the original functions stored
    '        all stemmed words (and each time a new woprd was added, the buffer would be
    '        re-copied each time, making it quite slow). Now, The class interface
    '        that is provided simply accepts a term and returns its stem,
    '        instead of storing all stemmed words.
    '        (Leif)



    Private Sub setTerm(ByVal s As String)
            i = s.Length
            Dim new_b As Char() = New Char(i) {}
            Dim c As Integer
            For c = 0 To (i - 1)
                new_b(c) = s.Chars(c)
            Next
            b = new_b
        End Sub

        Private Function getTerm() As String
            Return New String(b, 0, i_end)
        End Function


        ' Old interface to the class - left for posterity. However, it is not
        ' used when accessing the class via .NET (Leif)*/

        '
        ' Add a character to the word being stemmed.  When you are finished
        ' adding characters, you can call stem(void) to stem the word.
        '
        Public Sub add(ByVal ch As Char)
            Dim c As Integer
            If (i = b.Length) Then
                Dim new_b As Char() = New Char(i + INC) {}
                For c = 0 To (i - 1) Step 1
                    new_b(c) = b(c)
                Next
                b = new_b
            End If
            b(i) = ch
            i = i + 1
        End Sub


        '  Adds wLen characters to the word being stemmed contained in a portion
        '  of a char[] array. This is like repeated calls of add(char ch), but
        '  faster.
        Public Sub add(ByVal w As Char(), ByVal wLen As Integer)
            Dim c As Integer
            If i + wLen >= b.Length Then
                Dim new_b As Char() = New Char(i + wLen + INC) {}
                For c = 0 To (i - 1) Step 1
                    new_b(c) = b(c)
                Next
                b = new_b
            End If
            For c = 0 To (wLen - 1) Step 1
                b(i) = w(c)
                i = i + 1
            Next
        End Sub

        '  After a word has been stemmed, it can be retrieved by toString(),
        '  or a reference to the internal buffer can be retrieved by getResultBuffer
        '  and getResultLength (which is generally more efficient.)
        Public Overrides Function ToString() As String
            Return New String(b, 0, i_end)
        End Function


        '  Returns the length of the word resulting from the stemming process.
        Public Function getResultLength() As Integer
            Return i_end
        End Function


        '  Returns a reference to a character buffer containing the results of
        '  the stemming process.  You also need to consult getResultLength()
        '  to determine the length of the result.
        Public Function getResultBuffer() As Char()
            Return b
        End Function


        '  cons(i) is true <=> b[i] is a consonant.
        Public Function cons(ByVal i As Integer) As Boolean
            Select Case b(i)
                Case "a"c                                  ' Cast string to char. Option Strict On.
                Case "e"c
                Case "i"c
                Case "o"c
                Case "u"c
                    Return False
                Case "y"c
                    If i = 0 Then
                        Return True
                    Else
                        Return Not (cons(i - 1))
                    End If
                Case Else
                    Return True
            End Select
        End Function


        '  m() measures the number of consonant sequences between 0 and j. if c is
        '  a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
        '  presence,
        '          <c><v>       gives 0
        '          <c>vc<v>     gives 1
        '          <c>vcvc<v>   gives 2
        '          <c>vcvcvc<v> gives 3
        '          ....
        '
        Private Function m() As Integer
            Dim n As Integer = 0
            Dim i As Integer = 0

            While True
                If (i > j) Then Return n
                If (Not cons(i)) Then Exit While
                i = i + 1
            End While
            i = i + 1
            While (True)
                While (True)
                    If (i > j) Then Return n
                    If (cons(i)) Then Exit While
                    i = i + 1
                End While
                i = i + 1
                n = n + 1
                While (True)
                    If (i > j) Then Return n
                    If (Not cons(i)) Then Exit While
                    i = i + 1
                End While
                i = i + 1
            End While
        End Function


        '  vowelinstem() is true <=> 0,...j contains a vowel
        Private Function vowelinstem() As Boolean
            Dim i As Integer
            For i = 0 To j Step 1                         '  i <= j
                If (Not cons(i)) Then Return True
            Next
            Return False
        End Function


        '  doublec(j) is true <=> j,(j-1) contain a double consonant.
        Private Function doublec(ByVal j As Integer) As Boolean
            If (j < 1) Then Return False
            If (b(j) <> b(j - 1)) Then Return False
            Return cons(j)
        End Function


        '  cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
        '  and also if the second c is not w,x or y. this is used when trying to
        '  restore an e at the end of a short word. e.g.
        '
        '          cav(e), lov(e), hop(e), crim(e), but
        '          snow, box, tray.
        '
        Private Function cvc(ByVal i As Integer) As Boolean
            If ((i < 2) OrElse (Not cons(i)) OrElse cons(i - 1) OrElse (Not cons(i - 2))) Then
                Return False
            End If
            Dim ch As Char = b(i)
            If (ch = "w"c OrElse ch = "x"c OrElse ch = "y"c) Then Return False
            Return True
        End Function


        Private Function ends(ByVal s As String) As Boolean
            Dim l As Integer = s.Length
            Dim o As Integer = k - l + 1

            If (o < 0) Then Return False

            Dim sc As Char() = s.ToCharArray
            Dim i As Integer

            For i = 0 To (l - 1) Step 1
                If (b(o + i) <> sc(i)) Then Return False
            Next
            j = k - l

            Return True
        End Function


        '  setto(s) sets (j+1),...k to the characters in the string s, readjusting
        '  k.
        Private Sub setto(ByVal s As String)
            Dim l As Integer = s.Length
            Dim o As Integer = j + 1

            Dim sc As Char() = s.ToCharArray
            For i = 0 To (l - 1) Step 1
                b(o + i) = sc(i)
            Next
            k = j + l
        End Sub


        '  r(s) is used further down.
        Private Sub r(ByVal s As String)
            If (m() > 0) Then setto(s)
        End Sub


        '  step1() gets rid of plurals and -ed or -ing. e.g.
        '           caresses  ->  caress
        '           ponies    ->  poni
        '           ties      ->  ti
        '           caress    ->  caress
        '           cats      ->  cat
        '
        '           feed      ->  feed
        '           agreed    ->  agree
        '           disabled  ->  disable
        '
        '           matting   ->  mat
        '           mating    ->  mate
        '           meeting   ->  meet
        '           milling   ->  mill
        '           messing   ->  mess
        '
        '           meetings  ->  meet
        '
        Private Sub step1()
            If (b(k) = "s"c) Then
                If (ends("sses")) Then
                    k = k - 2
                ElseIf (ends("ies")) Then
                    setto("i")
                ElseIf (b(k - 1) <> "s"c) Then
                    k = k - 1
                End If
            End If
            If (ends("eed")) Then
                If (m() > 0) Then
                    k = k - 1
                End If
            ElseIf ((ends("ed") OrElse ends("ing")) AndAlso vowelinstem()) Then
                k = j
                If (ends("at")) Then
                    setto("ate")
                ElseIf (ends("bl")) Then
                    setto("ble")
                ElseIf (ends("iz")) Then
                    setto("ize")
                ElseIf (doublec(k)) Then
                    k = k - 1
                    Dim ch As Char = b(k)
                    If ((ch = "l"c) OrElse (ch = "s"c) OrElse (ch = "z"c)) Then
                        k = k + 1
                    End If
                ElseIf ((m() = 1) AndAlso cvc(k)) Then
                    setto("e")
                End If
            End If
        End Sub


        '  step2() turns terminal y to i when there is another vowel in the stem.
        Private Sub step2()
            If (ends("y") AndAlso vowelinstem()) Then
                b(k) = "i"c
            End If

        End Sub


        '  step3() maps double suffices to single ones. so -ization ( = -ize plus
        '  -ation) maps to -ize etc. note that the string before the suffix must give
        '  m() > 0.
        Private Sub step3()
            If (k = 0) Then Return

            'For Bug 1
            Select Case (b(k - 1))
                Case "a"c
                    If ends("ational") Then
                        r("ate")
                        Exit Select
                    End If
                    If ends("tional") Then
                        r("tion")
                        Exit Select
                    End If
                    Exit Select

                Case "c"c
                    If ends("enci") Then
                        r("ence")
                        Exit Select
                    End If
                    If ends("anci") Then
                        r("ance")
                        Exit Select
                    End If
                    Exit Select

                Case "e"c
                    If ends("izer") Then
                        r("ize")
                        Exit Select
                    End If
                    Exit Select

                Case "l"c
                    If ends("bli") Then
                        r("ble")
                        Exit Select
                    End If
                    If ends("alli") Then
                        r("al")
                        Exit Select
                    End If
                    If ends("entli") Then
                        r("ent")
                        Exit Select
                    End If
                    If ends("eli") Then
                        r("e")
                        Exit Select
                    End If
                    If ends("ousli") Then
                        r("ous")
                        Exit Select
                    End If
                    Exit Select

                Case "o"c
                    If ends("ization") Then
                        r("ize")
                        Exit Select
                    End If
                    If ends("ation") Then
                        r("ate")
                        Exit Select
                    End If
                    If ends("ator") Then
                        r("ate")
                        Exit Select
                    End If
                    Exit Select

                Case "s"c
                    If ends("alism") Then
                        r("al")
                        Exit Select
                    End If
                    If ends("iveness") Then
                        r("ive")
                        Exit Select
                    End If
                    If ends("fulness") Then
                        r("ful")
                        Exit Select
                    End If
                    If ends("ousness") Then
                        r("ous")
                        Exit Select
                    End If
                    Exit Select

                Case "t"c
                    If ends("aliti") Then
                        r("al")
                        Exit Select
                    End If
                    If ends("iviti") Then
                        r("ive")
                        Exit Select
                    End If
                    If ends("biliti") Then
                        r("ble")
                        Exit Select
                    End If
                    Exit Select

                Case "g"c
                    If ends("logi") Then
                        r("log")
                        Exit Select
                    End If
                    Exit Select

                Case Else
                    Exit Select
            End Select
        End Sub


        '  step4() deals with -ic-, -full, -ness etc. similar strategy to step3.
        Private Sub step4()
            Select Case (b(k))
                Case "e"c
                    If ends("icate") Then
                        r("ic")
                        Exit Select
                    End If
                    If ends("ative") Then
                        r("")
                        Exit Select
                    End If
                    If ends("alize") Then
                        r("al")
                        Exit Select
                    End If
                    Exit Select

                Case "i"c
                    If ends("iciti") Then
                        r("ic")
                        Exit Select
                    End If
                    Exit Select

                Case "l"c
                    If ends("ical") Then
                        r("ic")
                        Exit Select
                    End If
                    If ends("ful") Then
                        r("")
                        Exit Select
                    End If
                    Exit Select

                Case "s"c
                    If ends("ness") Then
                        r("")
                        Exit Select
                    End If
                    Exit Select
            End Select
        End Sub


        '  step5() takes off -ant, -ence etc., in context <c>vcvc<v>.
        Private Sub step5()
            If (k = 0) Then Return

            '  for Bug 1
            Select Case (b(k - 1))
                Case "a"c
                    If ends("al") Then
                        Exit Select
                    End If
                    Return

                Case "c"c
                    If ends("ance") Then
                        Exit Select
                    End If
                    If ends("ence") Then
                        Exit Select
                    End If
                    Return

                Case "e"c
                    If ends("er") Then
                        Exit Select
                    End If
                    Return

                Case "i"c
                    If ends("ic") Then
                        Exit Select
                    End If
                    Return

                Case "l"c
                    If ends("able") Then
                        Exit Select
                    End If
                    If ends("ible") Then
                        Exit Select
                    End If
                    Return

                Case "n"c
                    If ends("ant") Then
                        Exit Select
                    End If
                    If ends("ement") Then
                        Exit Select
                    End If
                    If ends("ment") Then
                        Exit Select
                    End If
                    '  element etc. not stripped before the m
                    If ends("ent") Then
                        Exit Select
                    End If
                    Return

                Case "o"c
                    If ends("ion") AndAlso (j >= 0) AndAlso (b(j) = "s"c OrElse b(j) = "t"c) Then
                        '  j >= 0 fixes Bug 2
                        Exit Select
                    End If
                    If ends("ou") Then
                        Exit Select
                    End If
                    Return
                                        'takes care of -ous

                Case "s"c
                    If ends("ism") Then
                        Exit Select
                    End If
                    Return

                Case "t"c
                    If ends("ate") Then
                        Exit Select
                    End If
                    If ends("iti") Then
                        Exit Select
                    End If
                    Return

                Case "u"c
                    If ends("ous") Then
                        Exit Select
                    End If
                    Return

                Case "v"c
                    If ends("ive") Then
                        Exit Select
                    End If
                    Return

                Case "z"c
                    If ends("ize") Then
                        Exit Select
                    End If
                    Return

                Case Else
                    Return
            End Select
            If (m() > 1) Then k = j
        End Sub


        '  step6() removes a final -e if m() > 1.
        Private Sub step6()
            j = k

            If (b(k) = "e"c) Then
                Dim a As Integer = m()
                If (a > 1) OrElse ((a = 1) AndAlso (Not cvc(k - 1))) Then k = k - 1
            End If
            If (b(k) = "l"c) AndAlso doublec(k) AndAlso (m() > 1) Then k = k - 1

        End Sub


        '  Stem the word placed into the Stemmer buffer through calls to add().
        '  Returns true if the stemming process resulted in a word different
        '  from the input.  You can retrieve the result with
        '  getResultLength()/getResultBuffer() or toString().
        '
        Public Sub stem()
            k = i - 1
            If (k > 1) Then
                step1()
                step2()
                step3()
                step4()
                step5()
                step6()
            End If
            i_end = k + 1
            i = 0
        End Sub


    End Class
