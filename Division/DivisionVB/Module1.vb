Option Explicit On
Option Infer On
Option Strict On

Module Module1

    Sub Main()
        Dim f As Integer = 10
        Dim res1 As Double = f / 3
        Dim res2 = f / 3
        Dim res3 As Double = f / 3.0
        Dim res4 = f / 3.0
        Console.WriteLine(res1)
        Console.WriteLine(res2)
        Console.WriteLine(res3)
        Console.WriteLine(res4)
        Console.ReadKey()
    End Sub

End Module
