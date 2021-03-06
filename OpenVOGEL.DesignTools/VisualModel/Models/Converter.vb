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

Imports OpenVOGEL.AeroTools.CalculationModel
Imports OpenVOGEL.AeroTools.CalculationModel.Solver
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Aero
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Aero.Components
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Structural
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Structural.Library
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Structural.Library.Nodes
Imports OpenVOGEL.AeroTools.CalculationModel.Models.Structural.Library.Elements
Imports OpenVOGEL.AeroTools.CalculationModel.Settings
Imports OpenVOGEL.MathTools.Algebra.EuclideanSpace
Imports OpenVOGEL.DesignTools.VisualModel.Models
Imports OpenVOGEL.DesignTools.VisualModel.Models.Components
Imports System.Runtime.CompilerServices

Namespace VisualModel.Models

    ''' <summary>
    ''' The converter module provides a group of extesions to the Solver to automatically
    ''' load the different kinds of standard surfaces from the design model. 
    ''' </summary>
    Public Module Converter

        ''' <summary>
        ''' Transfers a geometric model to the calculation cell
        ''' </summary>
        ''' <param name="Model">Model to be transferred</param>
        ''' <param name="GenerateStructure">Indicates if a structural link that should be created</param>
        ''' <remarks></remarks>
        <Extension()>
        Public Sub GenerateFromExistingModel(This As Solver,
                                             ByVal Model As DesignModel,
                                             Settings As SimulationSettings,
                                             Optional ByVal GenerateStructure As Boolean = False)

            This.Settings = New SimulationSettings
            This.Settings.Assign(Settings)

            ' Import polar database
            '---------------------------------------------------

            If Not IsNothing(Model.PolarDataBase) Then

                This.PolarDataBase = Model.PolarDataBase.Clone()

            End If

            ' Add lifting surfaces
            '---------------------------------------------------

            If GenerateStructure Then This.StructuralLinks = New List(Of StructuralLink)

            Dim count As Integer = 0

            For ObjectIndex = 0 To Model.Objects.Count - 1

                If TypeOf Model.Objects(ObjectIndex) Is LiftingSurface AndAlso Model.Objects(ObjectIndex).IncludeInCalculation Then

                    Dim Wing As LiftingSurface = Model.Objects(ObjectIndex)

                    count += 1

                    This.AddLiftingSurface(Wing, False, GenerateStructure, Wing.Symmetric)

                    If Wing.Symmetric Then
                        This.AddLiftingSurface(Wing, True, GenerateStructure, Wing.Symmetric)
                    End If

                End If

            Next

            ' Add fuselages
            '---------------------------------------------------

            For ObjectIndex = 0 To Model.Objects.Count - 1

                If TypeOf Model.Objects(ObjectIndex) Is Fuselage AndAlso Model.Objects(ObjectIndex).IncludeInCalculation Then

                    Dim Body As Fuselage = Model.Objects(ObjectIndex)

                    Dim Lattice As New BoundedLattice

                    This.Lattices.Add(Lattice)

                    For NodeIndex = 0 To Body.NumberOfNodes - 1

                        Lattice.AddNode(Body.Mesh.Nodes(NodeIndex).Position)

                    Next

                    For PanelIndex = 0 To Body.NumberOfPanels - 1

                        Dim Node1 As Integer = Body.Mesh.Panels(PanelIndex).N1
                        Dim Node2 As Integer = Body.Mesh.Panels(PanelIndex).N2
                        Dim Node3 As Integer = Body.Mesh.Panels(PanelIndex).N3
                        Dim Node4 As Integer = Body.Mesh.Panels(PanelIndex).N4
                        Dim Reversed As Boolean = Body.Mesh.Panels(PanelIndex).IsReversed
                        Dim Slender As Boolean = Body.Mesh.Panels(PanelIndex).IsSlender

                        If Body.Mesh.Panels(PanelIndex).IsTriangular Then

                            Lattice.AddVortexRing3(Node1, Node2, Node3, Reversed, Slender)

                        Else

                            Lattice.AddVortexRing4(Node1, Node2, Node3, Node4, Reversed, Slender)

                        End If

                        Lattice.VortexRings(PanelIndex).IsPrimitive = Body.Mesh.Panels(PanelIndex).IsPrimitive

                        ' Add the Kutta vortex at the trailing edge of the anchor
                        ' NOTE: no convection recommended from here due to high chance of spurious velocities

                        If Lattice.VortexRings(PanelIndex).IsPrimitive Then
                            Dim KuttaVortex As New Wake
                            KuttaVortex.Primitive.Nodes.Add(Node2)
                            KuttaVortex.Primitive.Nodes.Add(Node3)
                            KuttaVortex.Primitive.Rings.Add(Lattice.VortexRings.Count - 1)
                            KuttaVortex.CuttingStep = 0
                            Lattice.Wakes.Add(KuttaVortex)
                        End If

                    Next

                End If

            Next

            ' Add jet engine nacelles
            '---------------------------------------------------

            For ObjectIndex = 0 To Model.Objects.Count - 1

                If TypeOf Model.Objects(ObjectIndex) Is JetEngine AndAlso Model.Objects(ObjectIndex).IncludeInCalculation Then

                    Dim Nacelle As JetEngine = Model.Objects(ObjectIndex)

                    This.AddJetEngine(Nacelle)

                End If

            Next

            ' Add imported surfaces
            '---------------------------------------------------

            For ObjectIndex = 0 To Model.Objects.Count - 1

                If TypeOf Model.Objects(ObjectIndex) Is ImportedSurface AndAlso Model.Objects(ObjectIndex).IncludeInCalculation Then

                    Dim Body As ImportedSurface = Model.Objects(ObjectIndex)

                    Dim Lattice As New BoundedLattice

                    This.Lattices.Add(Lattice)

                    For NodeIndex = 0 To Body.NumberOfNodes - 1

                        Lattice.AddNode(Body.Mesh.Nodes(NodeIndex).Position)

                    Next

                    For PanelIndex = 0 To Body.NumberOfPanels - 1

                        Dim Node1 As Integer = Body.Mesh.Panels(PanelIndex).N1
                        Dim Node2 As Integer = Body.Mesh.Panels(PanelIndex).N2
                        Dim Node3 As Integer = Body.Mesh.Panels(PanelIndex).N3
                        Dim Node4 As Integer = Body.Mesh.Panels(PanelIndex).N4
                        Dim Reversed As Boolean = Body.Mesh.Panels(PanelIndex).IsReversed
                        Dim Slender As Boolean = Body.Mesh.Panels(PanelIndex).IsSlender

                        If Body.Mesh.Panels(PanelIndex).IsTriangular Then

                            Lattice.AddVortexRing3(Node1, Node2, Node3, Reversed, Slender)

                        Else

                            Lattice.AddVortexRing4(Node1, Node2, Node3, Node4, Reversed, Slender)

                        End If

                        Lattice.VortexRings(PanelIndex).IsPrimitive = Body.Mesh.Panels(PanelIndex).IsPrimitive

                    Next

                End If

            Next

            If This.Lattices.Count = 0 Then

                Throw New Exception("There are no lattices in the calculation model")

            End If

            ' Set global indices in the elements (to access circulation from matrices)
            '-------------------------------------------------------------------------

            This.IndexateLattices()

            ' Find surrounding rings
            '---------------------------------------------------

            This.FindSurroundingRingsGlobally()

            ' Populate wakes with vortices
            '---------------------------------------------------

            For Each Lattice In This.Lattices

                Lattice.PopulateVortices()

            Next

            ' Global inertial properties
            '---------------------------------------------------

            Dim Inertia As InertialProperties = Model.GetGlobalInertia

            Settings.Mass = Inertia.Mass

            Settings.CenterOfGravity.X = Inertia.Xcg
            Settings.CenterOfGravity.Y = Inertia.Ycg
            Settings.CenterOfGravity.Z = Inertia.Zcg

            Inertia.ToMainInertia(This.Settings.MainInertialAxes,
                                  This.Settings.Ixx,
                                  This.Settings.Iyy,
                                  This.Settings.Izz)

        End Sub

        ''' <summary>
        ''' Adds a bounded lattice with wakes from a lifting surface.
        ''' </summary>
        ''' <param name="Surface"></param>
        ''' <param name="Mirror"></param>
        ''' <param name="GenerateStructure"></param>
        ''' <remarks></remarks>
        <Extension()>
        Private Sub AddLiftingSurface(This As Solver,
                                      ByRef Surface As LiftingSurface,
                                      Optional ByVal Mirror As Boolean = False,
                                      Optional ByVal GenerateStructure As Boolean = False,
                                      Optional IsSymetric As Boolean = True)

            ' Add nodal points
            '-----------------------------------------

            Dim Lattice As New BoundedLattice

            This.Lattices.Add(Lattice)

            For NodeIndex = 0 To Surface.NumberOfNodes - 1

                Lattice.AddNode(Surface.Mesh.Nodes(NodeIndex).Position)

                If Mirror Then Lattice.Nodes(Lattice.Nodes.Count - 1).Position.Y *= -1

            Next

            ' Add rings
            '-----------------------------------------

            For PanelIndex = 0 To Surface.NumberOfPanels - 1

                Dim Node1 As Integer = Surface.Mesh.Panels(PanelIndex).N1
                Dim Node2 As Integer = Surface.Mesh.Panels(PanelIndex).N2
                Dim Node3 As Integer = Surface.Mesh.Panels(PanelIndex).N3
                Dim Node4 As Integer = Surface.Mesh.Panels(PanelIndex).N4

                Lattice.AddVortexRing4(Node1, Node2, Node3, Node4, False, True)

            Next

            ' Add wakes
            '-----------------------------------------

            If Surface.ConvectWake Then

                Dim Wake As New AeroTools.CalculationModel.Models.Aero.Wake

                For PrimitiveIndex = Surface.FirstPrimitiveNode To Surface.LastPrimitiveNode
                    Wake.Primitive.Nodes.Add(Surface.GetPrimitiveNodeIndex(PrimitiveIndex) - 1)
                Next

                For PrimitiveIndex = Surface.FirstPrimitiveSegment To Surface.LastPrimitiveSegment
                    Wake.Primitive.Rings.Add(Surface.GetPrimitivePanelIndex(PrimitiveIndex) - 1)
                Next

                Wake.CuttingStep = Surface.CuttingStep

                Wake.SupressInnerCircuation = IsSymetric

                Lattice.Wakes.Add(Wake)

            End If

            ' Generate structural link
            '-----------------------------------------

            Dim KineLink As KinematicLink
            Dim MechaLink As MechanicLink

            Dim StrNodeCount As Integer = -1
            Dim StrElementCount As Integer = -1

            If Surface.IncludeStructure And GenerateStructure Then

                ' Add root node (this node is being clamped, and it is the only one with contrains at the moment):

                Dim StructuralLink As New StructuralLink

                StrNodeCount = 0
                StrElementCount = -1
                StructuralLink.StructuralCore.StructuralSettings.Assign(This.Settings.StructuralSettings)
                StructuralLink.StructuralCore.Nodes.Add(New StructuralNode(StrNodeCount))
                StructuralLink.StructuralCore.Nodes(StrNodeCount).Position.Assign(Surface.StructuralPartition(0).P)
                If (Mirror) Then StructuralLink.StructuralCore.Nodes(StrNodeCount).Position.Y *= -1
                StructuralLink.StructuralCore.Nodes(StrNodeCount).Contrains.Clamped()

                ' Add kinematic link

                Dim LinkedVortexIndex As Integer = -1 ' > linked vortex ring
                Dim LinkedNodeIndex As Integer = -1 ' > linked node

                KineLink = New KinematicLink(StructuralLink.StructuralCore.Nodes(StrNodeCount))
                For n = 0 To Surface.NumberOfChordPanels
                    LinkedNodeIndex += 1
                    KineLink.Link(Lattice.Nodes(LinkedNodeIndex))
                Next
                StructuralLink.KinematicLinks.Add(KineLink)

                ' Add rest of the nodes and elements:

                For PartitionNodeIndex = 1 To Surface.StructuralPartition.Count - 1

                    ' Add nodes:

                    StrNodeCount += 1

                    StructuralLink.StructuralCore.Nodes.Add(New StructuralNode(StrNodeCount))
                    StructuralLink.StructuralCore.Nodes(StrNodeCount).Position.Assign(Surface.StructuralPartition(PartitionNodeIndex).P)
                    If (Mirror) Then StructuralLink.StructuralCore.Nodes(StrNodeCount).Position.Y *= -1

                    ' Add element:

                    StrElementCount += 1

                    Dim StrElement As New ConstantBeamElement(StrElementCount)
                    StrElement.NodeA = StructuralLink.StructuralCore.Nodes(StrNodeCount - 1)
                    StrElement.NodeB = StructuralLink.StructuralCore.Nodes(StrNodeCount)
                    StrElement.Section.Assign(Surface.StructuralPartition(StrElementCount).LocalSection)
                    StructuralLink.StructuralCore.Elements.Add(StrElement)

                    ' Add kinematic link:

                    Dim LeadingEdgeNodeIndex As Integer = LinkedNodeIndex + 1 '(leading edge lattice node index)

                    KineLink = New KinematicLink(StructuralLink.StructuralCore.Nodes(StrNodeCount))

                    For NodeCounter = 0 To Surface.NumberOfChordPanels
                        LinkedNodeIndex += 1
                        KineLink.Link(Lattice.Nodes(LinkedNodeIndex))
                    Next

                    StructuralLink.KinematicLinks.Add(KineLink)

                    Dim TrailingEdgeNodeIndex As Integer = LinkedNodeIndex '(trailing edge lattice node index)

                    ' Add mechanic link:

                    MechaLink = New MechanicLink(StrElement)

                    For PanelCounter = 0 To Surface.NumberOfChordPanels - 1
                        LinkedVortexIndex += 1
                        MechaLink.Link(Lattice.VortexRings(LinkedVortexIndex))
                    Next

                    StructuralLink.MechanicLinks.Add(MechaLink)

                    ' Find chordwise vector

                    Dim ChordVector As New Vector3
                    ChordVector.X = Lattice.Nodes(TrailingEdgeNodeIndex).Position.X - Lattice.Nodes(LeadingEdgeNodeIndex).Position.X
                    ChordVector.Y = Lattice.Nodes(TrailingEdgeNodeIndex).Position.Y - Lattice.Nodes(LeadingEdgeNodeIndex).Position.Y
                    ChordVector.Z = Lattice.Nodes(TrailingEdgeNodeIndex).Position.Z - Lattice.Nodes(LeadingEdgeNodeIndex).Position.Z

                    ' Build the element orthonormal basis:

                    ' NOTE: U has the direction of the element

                    StrElement.Basis.U.X = StrElement.NodeB.Position.X - StrElement.NodeA.Position.X
                    StrElement.Basis.U.Y = StrElement.NodeB.Position.Y - StrElement.NodeA.Position.Y
                    StrElement.Basis.U.Z = StrElement.NodeB.Position.Z - StrElement.NodeA.Position.Z
                    StrElement.Basis.U.Normalize()

                    ' NOTE: W is normal to the surface

                    StrElement.Basis.W.FromVectorProduct(ChordVector, StrElement.Basis.U)
                    StrElement.Basis.W.Normalize()

                    ' NOTE: V is normal to W and U, and points to the trailing edge

                    StrElement.Basis.V.FromVectorProduct(StrElement.Basis.W, StrElement.Basis.U)

                Next

                ' Add this structural link to the list:

                This.StructuralLinks.Add(StructuralLink)

            End If

            ' Load chordwise stripes (for skin drag computation)

            Dim VortexRingIndex As Integer = 0

            For RegionIndex = 0 To Surface.WingRegions.Count - 1

                Dim Region As WingRegion = Surface.WingRegions(RegionIndex)

                'If (Not IsNothing(PolarDataBase)) And PolarDataBase.Polars.Count > 0 Then

                For SpanPanelIndex = 1 To Region.SpanPanelsCount

                    Dim Stripe As New ChorwiseStripe()

                    Stripe.Polars = Region.PolarFamiliy

                    For PanelCount = 1 To Surface.NumberOfChordPanels
                        Stripe.Rings.Add(Lattice.VortexRings(VortexRingIndex))
                        VortexRingIndex += 1
                    Next

                    Lattice.ChordWiseStripes.Add(Stripe)

                Next

                'End If

            Next

        End Sub

        ''' <summary>
        ''' Includes the model of a jet engine in the calculation core
        ''' </summary>
        <Extension()>
        Private Sub AddJetEngine(This As Solver, ByRef Nacelle As JetEngine)

            Dim Lattice As New BoundedLattice

            This.Lattices.Add(Lattice)

            For j = 0 To Nacelle.NumberOfNodes - 1

                Lattice.AddNode(Nacelle.Mesh.Nodes(j).Position)

            Next

            For j = 0 To Nacelle.NumberOfPanels - 1

                Dim Node1 As Integer = Nacelle.Mesh.Panels(j).N1
                Dim Node2 As Integer = Nacelle.Mesh.Panels(j).N2
                Dim Node3 As Integer = Nacelle.Mesh.Panels(j).N3
                Dim Node4 As Integer = Nacelle.Mesh.Panels(j).N4

                Lattice.AddVortexRing4(Node1, Node2, Node3, Node4, False, True)

            Next

            ' Add wakes:

            If Nacelle.ConvectWake And Nacelle.CuttingStep > 0 Then

                Dim Wake As New AeroTools.CalculationModel.Models.Aero.Wake

                Wake.SupressInnerCircuation = False

                For k = 0 To Nacelle.Resolution
                    Wake.Primitive.Nodes.Add(Nacelle.NumberOfNodes + k - Nacelle.Resolution - 1)
                Next

                Wake.Primitive.Nodes.Add(Nacelle.NumberOfNodes - Nacelle.Resolution - 1)

                For k = 0 To Nacelle.Resolution
                    Wake.Primitive.Rings.Add(Nacelle.NumberOfPanels + k - Nacelle.Resolution - 1)
                Next

                Wake.CuttingStep = Nacelle.CuttingStep

                Lattice.Wakes.Add(Wake)

            End If

        End Sub

        ''' <summary>
        ''' Sets the lattices on the result object
        ''' </summary>
        ''' <param name="Results"></param>
        ''' <remarks></remarks>
        <Extension()>
        Public Sub SetCompleteModelOnResults(This As Solver, ByRef Results As ResultModel)

            Dim Frame As New ResultFrame(ResultFrameKinds.EndState)
            Results.Frames.Add(Frame)
            Results.ActiveFrame = Frame
            Frame.Model.Name = "Results"
            Frame.Model.Clear()
            Frame.Model.MaximumLift = 0.0#
            Frame.Model.LiftVectors.Clear()
            Frame.Model.MaximumInducedDrag = 0.0#
            Frame.Model.InducedDragVectors.Clear()
            Frame.Model.MaximumSkinDrag = 0.0#
            Frame.Model.SkinDragVectors.Clear()

            Results.SimulationSettings.Assign(This.Settings)

            Dim CantidadDeSuperficies As Integer = 0

            Dim GlobalIndexNodes As Integer = -1
            Dim GlobalIndexRings As Integer = -1

            For Each Lattice In This.Lattices

                '-----------------------'
                ' Load the nodal points '
                '-----------------------'

                For Each NodalPoint In Lattice.Nodes

                    GlobalIndexNodes += 1
                    NodalPoint.IndexG = GlobalIndexNodes
                    Frame.Model.AddNodalPoint(NodalPoint.Position)

                Next

                '-----------------------'
                ' Load the vortex rings '
                '-----------------------'

                For Each VortexRing In Lattice.VortexRings

                    GlobalIndexRings += 1

                    If VortexRing.Type = VortexRingType.VR4 Then

                        Frame.Model.AddPanel(VortexRing.Node(1).IndexG,
                                               VortexRing.Node(2).IndexG,
                                               VortexRing.Node(3).IndexG,
                                               VortexRing.Node(4).IndexG)

                    Else

                        Frame.Model.AddPanel(VortexRing.Node(1).IndexG,
                                               VortexRing.Node(2).IndexG,
                                               VortexRing.Node(3).IndexG,
                                               VortexRing.Node(1).IndexG)

                    End If

                    Frame.Model.Mesh.Panels(GlobalIndexRings).Circulation = VortexRing.G
                    Frame.Model.Mesh.Panels(GlobalIndexRings).SourceStrength = VortexRing.S
                    Frame.Model.Mesh.Panels(GlobalIndexRings).Cp = VortexRing.Cp
                    Frame.Model.Mesh.Panels(GlobalIndexRings).Area = VortexRing.Area
                    Frame.Model.Mesh.Panels(GlobalIndexRings).NormalVector.Assign(VortexRing.Normal)
                    Frame.Model.Mesh.Panels(GlobalIndexRings).LocalVelocity.Assign(VortexRing.VelocityT)
                    Frame.Model.Mesh.Panels(GlobalIndexRings).ControlPoint.Assign(VortexRing.ControlPoint)
                    Frame.Model.Mesh.Panels(GlobalIndexRings).IsSlender = VortexRing.IsSlender

                Next

                '------------------------'
                ' Load the fixed vectors '
                '------------------------'

                For Each Stripe In Lattice.ChordWiseStripes

                    ' Lift vectors
                    '-----------------------------------
                    If Stripe.Lift.EuclideanNorm > 0.0 Then
                        Dim LiftVector As New FixedVector
                        LiftVector.Vector.Assign(Stripe.Lift)
                        LiftVector.Point.Assign(Stripe.CenterPoint)
                        Frame.Model.LiftVectors.Add(LiftVector)
                        Frame.Model.MaximumLift = Math.Max(Frame.Model.MaximumLift, Stripe.Lift.EuclideanNorm)
                    End If

                    ' Induced drag vectors
                    '-----------------------------------
                    If Stripe.InducedDrag.EuclideanNorm > 0.0 Then
                        Dim DragVector As New FixedVector
                        DragVector.Vector.Assign(Stripe.InducedDrag)
                        DragVector.Point.Assign(Stripe.CenterPoint)
                        Frame.Model.InducedDragVectors.Add(DragVector)
                        Frame.Model.MaximumInducedDrag = Math.Max(Frame.Model.MaximumInducedDrag, Stripe.InducedDrag.EuclideanNorm)
                    End If

                    ' Skin drag vectors
                    '-----------------------------------
                    If Stripe.SkinDrag.EuclideanNorm > 0.0 Then
                        Dim DragVector As New FixedVector
                        DragVector.Vector.Assign(Stripe.SkinDrag)
                        DragVector.Point.Assign(Stripe.CenterPoint)
                        Frame.Model.SkinDragVectors.Add(DragVector)
                        Frame.Model.MaximumSkinDrag = Math.Max(Frame.Model.MaximumSkinDrag, Stripe.SkinDrag.EuclideanNorm)
                    End If

                Next
            Next

            Frame.Model.FindPressureRange()
            Frame.Model.UpdatePressureColormap()
            Frame.Model.VisualProperties.ShowColormap = True
            Frame.Model.VisualProperties.ShowVelocityVectors = True
            Frame.Model.VisualProperties.ShowMesh = True
            Frame.Model.FindBestVelocityScale()

            Frame.Model.Mesh.GenerateLattice()

            '------------------------'
            ' Load the wakes         '
            '------------------------'

            GlobalIndexNodes = -1
            GlobalIndexRings = -1
            Frame.Wakes.Clear()

            For Each Lattice In This.Lattices

                For Each Wake In Lattice.Wakes

                    For Each NodalPoint In Wake.Nodes

                        GlobalIndexNodes += 1
                        NodalPoint.IndexG = GlobalIndexNodes
                        Frame.Wakes.AddNodalPoint(NodalPoint.Position)

                    Next

                    For Each VortexRing In Wake.VortexRings

                        GlobalIndexRings += 1
                        Frame.Wakes.AddPanel(VortexRing.Node(1).IndexG,
                                               VortexRing.Node(2).IndexG,
                                               VortexRing.Node(3).IndexG,
                                               VortexRing.Node(4).IndexG)
                        Frame.Wakes.Mesh.Panels(GlobalIndexRings).Circulation = VortexRing.G
                        Frame.Wakes.Mesh.Panels(GlobalIndexRings).SourceStrength = VortexRing.S
                        Frame.Wakes.Mesh.Panels(GlobalIndexRings).IsSlender = VortexRing.IsSlender

                    Next

                    For Each Vortex In Wake.Vortices
                        Dim Segment As New Basics.LatticeSegment
                        Segment.N1 = Vortex.Node1.IndexG
                        Segment.N2 = Vortex.Node2.IndexG
                        Frame.Wakes.Mesh.Lattice.Add(Segment)
                    Next

                Next

            Next

            Frame.Wakes.VisualProperties.ShowMesh = False
            Frame.Wakes.VisualProperties.ShowNodes = True

            ' Load dynamic modes
            '----------------------------------------------

            If This.StructuralLinks IsNot Nothing Then

                Dim Modes As New List(Of ResultContainer)

                For Each Link As StructuralLink In This.StructuralLinks

                    If Link.StructuralCore.Modes IsNot Nothing Then

                        For Each Mode As Mode In Link.StructuralCore.Modes

                            Dim ModelShapeFrame As New ResultFrame(ResultFrameKinds.DynamicMode)
                            Results.Frames.Add(ModelShapeFrame)

                            Dim ModalShapeModel As ResultContainer = ModelShapeFrame.Model
                            Modes.Add(ModalShapeModel)

                            ModalShapeModel.Name = String.Format("Mode {0} - {1:F3}Hz", Mode.Index, Mode.W / (2 * Math.PI))
                            ModalShapeModel.VisualProperties.ColorMesh = Drawing.Color.Maroon
                            ModalShapeModel.VisualProperties.ColorSurface = Drawing.Color.Orange
                            ModalShapeModel.VisualProperties.Transparency = 1.0
                            ModalShapeModel.VisualProperties.ShowSurface = True
                            ModalShapeModel.VisualProperties.ShowMesh = True
                            ModalShapeModel.VisualProperties.ShowNodes = False
                            ModalShapeModel.VisualProperties.ThicknessMesh = 0.8
                            ModalShapeModel.VisualProperties.ShowNodes = False
                            ModalShapeModel.VisualProperties.ShowLoadVectors = False
                            ModalShapeModel.VisualProperties.ShowVelocityVectors = False
                            ModalShapeModel.VisualProperties.ShowColormap = True

                            ' Reset all displacements:

                            For Each OtherLink As StructuralLink In This.StructuralLinks
                                OtherLink.StructuralCore.ResetDisplacements()
                                For Each kl As KinematicLink In OtherLink.KinematicLinks
                                    kl.TransferMotion()
                                Next
                            Next

                            ' Load the displacement associated with the current mode:

                            Link.StructuralCore.TransferModeShapeToNodes(Mode.Index, 1.0)

                            For Each kl As KinematicLink In Link.KinematicLinks

                                kl.TransferMotion()

                            Next

                            ' Make a lattice based on the current modal displacement:

                            GlobalIndexNodes = -1
                            GlobalIndexRings = -1

                            For Each Lattice In This.Lattices

                                For Each NodalPoint In Lattice.Nodes

                                    NodalPoint.IndexG = GlobalIndexNodes
                                    GlobalIndexNodes += 1
                                    ModalShapeModel.AddNodalPoint(NodalPoint.OriginalPosition, NodalPoint.Displacement)

                                Next

                                ModalShapeModel.UpdateDisplacement()

                                For Each VortexRing In Lattice.VortexRings

                                    GlobalIndexRings += 1

                                    ModalShapeModel.AddPanel(VortexRing.Node(1).IndexG + 1,
                                                             VortexRing.Node(2).IndexG + 1,
                                                             VortexRing.Node(3).IndexG + 1,
                                                             VortexRing.Node(4).IndexG + 1)

                                    ModalShapeModel.Mesh.Panels(GlobalIndexRings).Circulation = 0.0
                                    ModalShapeModel.Mesh.Panels(GlobalIndexRings).Cp = 0.0
                                    ModalShapeModel.Mesh.Panels(GlobalIndexRings).IsSlender = True

                                Next

                            Next

                            ModalShapeModel.ActiveResult = ResultContainer.ResultKinds.NodalDisplacement
                            ModalShapeModel.Mesh.GenerateLattice()
                            ModalShapeModel.FindDisplacementsRange()
                            ModalShapeModel.UpdateColormapWithDisplacements()

                        Next

                    End If

                Next

                ' Load transit
                '----------------------------------------------

                Dim NodalDisplacement As New Vector3
                Dim nTimeSteps = 0

                If This.Settings.StructuralSettings.SubSteps < 1 Then This.Settings.StructuralSettings.SubSteps = 1

                If This.StructuralLinks.Count > 0 Then
                    nTimeSteps = This.StructuralLinks(0).ModalResponse.Count '/ Settings.StructuralSettings.SubSteps
                End If

                Dim TimeStep As Integer = 0

                While TimeStep < nTimeSteps - 1

                    ' Make a lattice based on the current modal displacement:

                    Dim Transit As New ResultFrame(ResultFrameKinds.Transit)
                    Transit.Model.Name = String.Format("Step {0}", TimeStep)

                    GlobalIndexNodes = -1
                    GlobalIndexRings = -1

                    For Each Lattice In This.Lattices

                        For Each NodalPoint In Lattice.Nodes

                            GlobalIndexNodes += 1

                            NodalDisplacement.SetToCero()

                            Dim FirstModeIndex As Integer = 0

                            For Each Link In This.StructuralLinks

                                ' Make a lattice based on the current modal displacement:

                                Dim ModalResponse As ModalCoordinates = Link.ModalResponse(TimeStep)

                                For ModeIndex = 0 To Link.StructuralCore.Modes.Count - 1

                                    NodalDisplacement.X += Modes(ModeIndex + FirstModeIndex).Mesh.Nodes(GlobalIndexNodes).Displacement.X * ModalResponse(ModeIndex).P
                                    NodalDisplacement.Y += Modes(ModeIndex + FirstModeIndex).Mesh.Nodes(GlobalIndexNodes).Displacement.Y * ModalResponse(ModeIndex).P
                                    NodalDisplacement.Z += Modes(ModeIndex + FirstModeIndex).Mesh.Nodes(GlobalIndexNodes).Displacement.Z * ModalResponse(ModeIndex).P

                                Next

                                FirstModeIndex += Link.StructuralCore.Modes.Count

                            Next

                            Transit.Model.AddNodalPoint(NodalPoint.OriginalPosition, NodalDisplacement)

                        Next

                        For Each VortexRing In Lattice.VortexRings

                            GlobalIndexRings += 1

                            Transit.Model.AddPanel(VortexRing.Node(1).IndexG + 1,
                                                     VortexRing.Node(2).IndexG + 1,
                                                     VortexRing.Node(3).IndexG + 1,
                                                     VortexRing.Node(4).IndexG + 1)
                            Transit.Model.Mesh.Panels(GlobalIndexRings).Circulation = 0.0
                            Transit.Model.Mesh.Panels(GlobalIndexRings).Cp = 0.0
                            Transit.Model.Mesh.Panels(GlobalIndexRings).IsSlender = True

                        Next

                    Next

                    Transit.Model.Mesh.GenerateLattice()
                    Results.Frames.Add(Transit)

                    TimeStep += 1

                End While

            End If

        End Sub

    End Module

End Namespace

