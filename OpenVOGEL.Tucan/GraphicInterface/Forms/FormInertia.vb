﻿'Open VOGEL (openvogel.org)
'Open source software for aerodynamics
'Copyright (C) 2020 Guillermo Hazebrouck (guillermo.hazebrouck@openvogel.org)

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

Imports OpenVOGEL.AeroTools.CalculationModel.Settings

Public Class FormInertia

    Public Sub SetInertia(ByRef Inertia As InertialProperties)

        nudMass.Value = Inertia.Mass

        nudIxx.Value = Inertia.Ixx
        nudIyy.Value = Inertia.Iyy
        nudIzz.Value = Inertia.Izz

        nudIxy.Value = Inertia.Ixy
        nudIxz.Value = Inertia.Ixz
        nudIyz.Value = Inertia.Iyz

        nudXcg.Value = Inertia.Xcg
        nudYcg.Value = Inertia.Ycg
        nudZcg.Value = Inertia.Zcg

    End Sub

    Public Function GetInertia() As InertialProperties

        Dim Inertia As InertialProperties

        Inertia.Mass = nudMass.Value

        Inertia.Ixx = nudIxx.Value
        Inertia.Iyy = nudIyy.Value
        Inertia.Izz = nudIzz.Value

        Inertia.Ixy = nudIxy.Value
        Inertia.Ixz = nudIxz.Value
        Inertia.Iyz = nudIyz.Value

        Inertia.Xcg = nudXcg.Value
        Inertia.Ycg = nudYcg.Value
        Inertia.Zcg = nudZcg.Value

        Return Inertia

    End Function

End Class