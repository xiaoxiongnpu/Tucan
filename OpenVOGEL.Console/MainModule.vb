﻿'Open VOGEL (https://en.wikibooks.org/wiki/Open_VOGEL)
'Open source software for aerodynamics
'Copyright (C) 2018 Guillermo Hazebrouck (gahazebrouck@gmail.com)

'This program Is free software: you can redistribute it And/Or modify
'it under the terms Of the GNU General Public License As published by
'the Free Software Foundation, either version 3 Of the License, Or
'(at your option) any later version.

'This program Is distributed In the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty Of
'MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License For more details.

'You should have received a copy Of the GNU General Public License
'along with this program.  If Not, see < http:  //www.gnu.org/licenses/>.

Imports OpenVOGEL.AeroTools.CalculationModel.Models.Aero.Components
Imports OpenVOGEL.MathTools.Algebra.CustomMatrices
Imports OpenVOGEL.MathTools.Algebra.EuclideanSpace

Module MainModule

    Sub Main()

        System.Console.WriteLine("** OpenVOGEL console **")

        Dim Quit As Boolean = False

        While Not Quit

            Dim Line As String = System.Console.ReadLine()

            Select Case Line
                Case "quit"
                    Quit = True
                Case "test"
                    TestAerodynamicSolver()
                Case "steady"
                    System.Console.WriteLine("enter file name:")
                    RunSteady(System.Console.ReadLine)
            End Select

        End While


    End Sub

    Private Sub RunSteady(Path As String)

        Dim Core As New AeroTools.CalculationModel.Solver.Solver
        AddHandler Core.PushMessage, AddressOf OutputMessage
        AddHandler Core.PushProgress, AddressOf OutputProgress
        AddHandler Core.PushResultLine, AddressOf OutputMessage
        Core.ReadFromXML(Path)
        If Core.Lattices.Count > 0 Then
            Core.SteadyState(Path)
            Core.ReportResutls()
        End If

    End Sub

    Private Sub OutputMessage(Message As String)
        System.Console.WriteLine(Message)
    End Sub

    Private Sub OutputProgress(Message As String, Progress As Integer)
        System.Console.WriteLine(String.Format("{0}: {1}", Progress, Message))
    End Sub

End Module