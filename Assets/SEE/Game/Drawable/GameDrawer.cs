using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.GO.Factories;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides the creation of <see cref="LineConf"/>.
    /// </summary>
    public static class GameDrawer
    {
        #region Types
        /// <summary>
        /// The different color kinds.
        /// </summary>
        [Serializable]
        public enum ColorKind
        {
            Monochrome,
            Gradient,
            TwoDashed
        }

        /// <summary>
        /// Gets a list with the color kinds.
        /// If <paramref name="isDashedLineKind"/> is true, the list contains <see cref="ColorKind.TwoDashed"/>.
        /// Otherwise, the list only contains <see cref="ColorKind.Monochrome"/> and <see cref="ColorKind.Gradient"/>.
        /// </summary>
        /// <param name="isDashedLineKind">Whether the line has a dashed line child.</param>
        /// <returns>The color kind list depending on <paramref name="isDashedLineKind"/>.</returns>
        public static IList<ColorKind> GetColorKinds(bool isDashedLineKind)
        {
            if (isDashedLineKind)
            {
                return Enum.GetValues(typeof(ColorKind)).Cast<ColorKind>().ToList();
            }
            else
            {
                return new List<ColorKind>() { ColorKind.Monochrome, ColorKind.Gradient };
            }
        }

        /// <summary>
        /// The different line kinds.
        /// </summary>
        [Serializable]
        public enum LineKind
        {
            Solid,
            Dashed,
            Dashed25,
            Dashed50,
            Dashed75,
            Dashed100
        }

        /// <summary>
        /// Gets a list with all the different line kinds.
        /// </summary>
        /// <returns>A list with all the line kinds.</returns>
        public static IList<LineKind> GetLineKinds()
        {
            return Enum.GetValues(typeof(LineKind)).Cast<LineKind>().ToList();
        }
        #endregion

        #region Core Line Creation
        /// <summary>
        /// Sets up a line object based on the parameters.
        /// It creates the initial line.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should displayed.</param>
        /// <param name="name">The line name, can be empty.</param>
        /// <param name="positions">The positions of the line.</param>
        /// <param name="colorKind">The color kind of the line.</param>
        /// <param name="primaryColor">The primary color of the line.</param>
        /// <param name="secondaryColor">The secondary color of the line.</param>
        /// <param name="thickness">The line thickness.</param>
        /// <param name="order">The order in layer for the line.</param>
        /// <param name="lineKind">The line kind of the line.</param>
        /// <param name="tiling">The tiling for a dashed line kind.</param>
        /// <param name="associatedPage">The assoiated surface page for this object.</param>
        /// <param name="line">The created line object.</param>
        /// <param name="renderer">The line renderer of the line.</param>
        /// <param name="meshCollider">The mesh collider of the line.</param>
        /// <param name="addLineCapValueHolder">
        /// Whether a LineCapValueHolder should be added. True for normal lines, false for line caps.
        /// </param>
        private static void Setup(GameObject surface, string name, Vector3[] positions,
            ColorKind colorKind, Color primaryColor, Color secondaryColor, float thickness,
            int order, LineKind lineKind, float tiling, int associatedPage,
            out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider, bool addLineCapValueHolder = true)
        {
            /// If the object has been created earlier, it already has a name,
            /// and this name is taken from the parameters <paramref name="name"/>.
            if (name.Length > Tags.Line.Length)
            {
                line = new(name);
            }
            else
            {
                /// Otherwise, a name for the line will be generated.
                /// For this, the <see cref="ValueHolder.LinePrefix"/> is concatenated with
                /// the object ID along with a random string consisting of four characters.
                line = new("");

                name = ValueHolder.LinePrefix + line.GetInstanceID() + RandomStrings.GetRandomString(4);
                /// Check if the name is already in use. If so, generate a new name.
                while (GameFinder.FindAttachedOrLocalDescendant(surface, name) != null)
                {
                    name = ValueHolder.LinePrefix + line.GetInstanceID() + RandomStrings.GetRandomString(4);
                }
                line.name = name;
            }
            /// Sets up the drawable holder <see cref="DrawableSetupManager"/>.
            DrawableSetupManager.Setup(surface, out GameObject _, out GameObject attachedObjects);

            /// Assign the line tag to the line object.
            line.tag = Tags.Line;

            /// Add the line object to the hierarchy below the attached objects - object of the drawable.
            line.transform.SetParent(attachedObjects.transform);

            /// Adds the line renderer to the line object.
            renderer = line.AddComponent<LineRenderer>();
            /// Adds the mesh collider to the line object.
            meshCollider = line.AddComponent<MeshCollider>();
            /// Ensure that the line is represented in a flat (2D) manner.
            renderer.alignment = LineAlignment.TransformZ;
            /// Sets the correct material for the chosen line kind.
            renderer.sharedMaterial = GetMaterial(primaryColor, lineKind);
            /// Adds the line value holder to the object and assign the color kind to it.
            line.AddComponent<LineValueHolder>().ColorKind = colorKind;
            /// Set the color(s) of the line depending on the chosen color kind.
            switch (colorKind)
            {
                case ColorKind.Monochrome:
                    break;
                case ColorKind.Gradient:
                    renderer.material.color = Color.white;
                    renderer.startColor = primaryColor;
                    renderer.endColor = secondaryColor;
                    break;
                case ColorKind.TwoDashed:
                    Material[] materials = new Material[2];
                    materials[0] = renderer.materials[0];
                    materials[1] = GetMaterial(Color.white, LineKind.Solid);
                    GetRenderer(line).materials = materials;
                    renderer.materials[1].color = secondaryColor;
                    break;
            }
            /// Adds the line cap value holder and the line anchro value holder to the object.
            if (addLineCapValueHolder)
            {
                line.AddComponent<LineCapValueHolder>();
                line.AddComponent<LineAnchorValueHolder>();
            }
            /// Sets the texture mode of the renderer depending on the chosen line kind.
            SetTextureMode(renderer, lineKind);
            /// Sets the texture scale of the renderer depending on the chosen line kind.
            SetRendererTextrueScale(renderer, lineKind, tiling);
            /// Sets the line thickness.
            renderer.startWidth = thickness;
            renderer.endWidth = renderer.startWidth;
            /// Use world space must be false, as it allows the line to be moved and rotated.
            renderer.useWorldSpace = false;
            /// Ensure that the renderer have enough positions for the <paramref name="positions">.
            renderer.positionCount = positions.Length;
            /// Make the line ends round.
            renderer.numCapVertices = 90;

            /// Set the position of the line and ensure the correct order in the layer.
            /// Additionally, adopt the rotation of the attached object.
            line.transform.SetPositionAndRotation(attachedObjects.transform.position, attachedObjects.transform.rotation);
            line.transform.position -= order * ValueHolder.DistanceToDrawable.z * line.transform.forward;

            /// Adds the order in layer value holder component to the line object and sets the order.
            line.AddComponent<OrderInLayerValueHolder>().OrderInLayer = order;
            /// Sets the line kind in the line value holder.
            line.GetComponent<LineValueHolder>().LineKind = lineKind;
            /// Adds a <see cref="AssociatedPageHolder"/> component.
            /// And sets the associated page to the used page.
            line.AddComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
            if (associatedPage != surface.GetComponent<DrawableHolder>().CurrentPage)
            {
                line.SetActive(false);
            }
        }

        /// <summary>
        /// Gets the <see cref="LineRenderer"/> of the line.
        /// </summary>
        /// <param name="line">The line whose Line Renderer is to be returned.</param>
        /// <returns>The <see cref="LineRenderer"/>.</returns>
        private static LineRenderer GetRenderer(GameObject line)
        {
            return line.GetComponent<LineRenderer>();
        }

        /// <summary>
        /// Gets the <see cref="MeshCollider"/> of the line.
        /// </summary>
        /// <param name="line">The line whose Mesh Collider is to be returned.</param>
        /// <returns>The <see cref="MeshCollider"/>.</returns>
        private static MeshCollider GetMeshCollider(GameObject line)
        {
            return line.GetComponent<MeshCollider>();
        }

        /// <summary>
        /// Initiate the drawing of a line.
        /// This call creates the line and adds the initial position.
        /// Additionally, it increases the current order in the layer.
        ///
        /// To add further points to the created line, the <see cref="Drawing"/> method must be
        /// subsequently called with the new points.
        /// To complete the drawing, <see cref="FinishDrawing"/> should be executed at the end.
        /// If desired, <see cref="SetPivot"/> can then be called to set the correct pivot.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be displayed.</param>
        /// <param name="positions">The start positions for the line.</param>
        /// <param name="colorKind">The chosen color kind for the line.</param>
        /// <param name="primaryColor">The chosen primary color for the line.</param>
        /// <param name="secondaryColor">The chosen secondary color for the line.</param>
        /// <param name="thickness">The line thickness.</param>
        /// <param name="lineKind">The chosen line kind.</param>
        /// <param name="tiling">The tiling for a dashed line kind.</param>
        /// <returns>The created line.</returns>
        public static GameObject StartDrawing(GameObject surface, Vector3[] positions, ColorKind colorKind,
            Color primaryColor, Color secondaryColor, float thickness, LineKind lineKind, float tiling)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            Setup(surface, "", positions, colorKind, primaryColor, secondaryColor, thickness,
                holder.OrderInLayer, lineKind, tiling, holder.CurrentPage,
                out GameObject line, out LineRenderer _, out MeshCollider _);
            holder.Inc();
            ValueHolder.MaxOrderInLayer++;

            return line;
        }

        /// <summary>
        /// Updates the positions of an existing line.
        /// </summary>
        /// <param name="line">The line to be updated.</param>
        /// <param name="positions">The new positions for the line.</param>
        public static void Drawing(GameObject line, Vector3[] positions, Color? fillOutColor = null)
        {
            LineRenderer renderer = GetRenderer(line);
            renderer.positionCount = positions.Length;
            /// Ensure that all points of the line have a z-axis value of 0.
            UpdateZPositions(ref positions);
            renderer.SetPositions(positions);

            if (fillOutColor != null && FillOut(line, fillOutColor))
            {
                line.FindDescendant(ValueHolder.FillOut).GetComponent<MeshCollider>().enabled = false;
            }
        }

        /// <summary>
        /// Adds a position to the <see cref="LineRenderer"/> of the <paramref name="line"/> on the given
        /// <paramref name="index"/>.
        /// </summary>
        /// <param name="line">The line on which the position should be added.</param>
        /// <param name="position">The position to be added.</param>
        /// <param name="index">The index on which it should be added.</param>
        public static void DrawPoint(GameObject line, Vector3 position, int index)
        {
            LineRenderer renderer = GetRenderer(line);
            position.z = 0;
            if (renderer.positionCount <= index)
            {
                renderer.positionCount = index + 1;
            }
            renderer.SetPosition(index, position);
        }

        /// <summary>
        /// Finishes drawing a line.
        /// Ensures that the mesh collider aligns with the renderer line points.
        /// However, the generated mesh must have at least three different points for this to work
        /// (otherwise, the mesh won't function).
        ///
        /// Additionally, it can be specified whether the line should form a loop,
        /// meaning that the endpoint is connected to the starting point.
        /// </summary>
        /// <param name="line">The line for which drawing is to be finished.</param>
        /// <param name="loop">Option to connect the line endpoint with the starting point.</param>
        /// <param name="fillOutColor">The color to fill out the line; null if the line should not filled out.</param>
        /// <param name="showInfo">Whether the information of the fill out should be shown.</param>
        public static void FinishDrawing(GameObject line, bool loop, Color? fillOutColor = null, bool showInfo = true)
        {
            LineRenderer renderer = GetRenderer(line);
            MeshCollider meshCollider = GetMeshCollider(line);
            renderer.loop = loop;
            Mesh mesh = new();
            renderer.BakeMesh(mesh);
            if (mesh.vertices.Distinct().Count() >= 3)
            {
                meshCollider.sharedMesh = mesh;
            }
            if (fillOutColor != null && FillOut(line, fillOutColor.Value, showInfo))
            {
                line.FindDescendant(ValueHolder.FillOut).GetComponent<MeshCollider>().enabled = true;
            }
        }

        /// <summary>
        /// Draws or updates an entire line.
        /// It combines the functionality of the methods <see cref="StartDrawing"/>, <see cref="Drawing"/>,
        /// and <see cref="FinishDrawing"/>.
        ///
        /// First, it ensures that the z-axis of the positions is set to 0.
        /// Then, it checks if the line is already present on the drawable.
        /// If so, the line is only refreshed.
        /// Otherwise, it is newly created.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be displayed.</param>
        /// <param name="name">The name of the line, can be empty.</param>
        /// <param name="positions">The positions for the line.</param>
        /// <param name="colorKind">The chosen color kind.</param>
        /// <param name="primaryColor">The chosen primary color for the line.</param>
        /// <param name="secondaryColor">The chosen secondary color for the line.</param>
        /// <param name="thickness">The line thickness.</param>
        /// <param name="loop">Option to connect the line endpoint with the starting point.</param>
        /// <param name="lineKind">The line kind for the line.</param>
        /// <param name="tiling">The tiling for a dashed line kind.</param>
        /// <param name="increaseCurrentOrder">Option to increase the current order in the layer value.
        /// By default, it is set to true.</param>
        /// <param name="fillOutColor">The color for fill out the line; null if the line should not filled out.</param>
        /// <param name="addLineCapValueHolder">
        /// Whether a LineCapValueHolder should be added. True for normal lines, false for line caps.
        /// </param>
        /// <param name="showFillOutInfo">Whether the information of the fill out should be shown.</param>
        /// <returns>The created or updated line.</returns>
        public static GameObject DrawLine(GameObject surface, string name, Vector3[] positions, ColorKind colorKind,
            Color primaryColor, Color secondaryColor, float thickness, bool loop, LineKind lineKind,
            float tiling, bool increaseCurrentOrder = true, Color? fillOutColor = null,
            bool addLineCapValueHolder = true, bool showFillOutInfo = true)
        {
            GameObject lineObject;
            /// Updates the z axis values of the positions to 0.
            UpdateZPositions(ref positions);
            /// If the drawable already has a child with this name, update it.
            if (GameFinder.FindAttachedOrLocalDescendant(surface, name) != null)
            {
                lineObject = GameFinder.FindAttachedOrLocalDescendant(surface, name);
                Drawing(lineObject, positions);
                FinishDrawing(lineObject, loop, fillOutColor, showFillOutInfo);
            }
            else
            {
                /// Block for creating a new line.
                DrawableHolder holder = surface.GetComponent<DrawableHolder>();
                Setup(surface, name, positions, colorKind, primaryColor, secondaryColor, thickness,
                    holder.OrderInLayer, lineKind, tiling, holder.CurrentPage,
                    out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider, addLineCapValueHolder);
                lineObject = line;
                renderer.SetPositions(positions);
                if (increaseCurrentOrder)
                {
                    holder.Inc();
                    ValueHolder.MaxOrderInLayer++;
                }
                FinishDrawing(line, loop, fillOutColor, showFillOutInfo);
            }
            return lineObject;
        }

        /// <summary>
        /// Redraws a line that has been drawn before.
        /// The difference from <see cref="DrawLine"/> is that the object's position,
        /// rotation, scale and order in layer is also restored in this case.
        /// Otherwise, it works almost the same.
        /// The difference is that in the <see cref="ReDrawLine"/>, you cannot specify
        /// that the order in the layer should not be increased.
        /// This only happens if the order in layer of the line to be created is greater
        /// than or equal to the current maximum order.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be displayed.</param>
        /// <param name="name">The name of the line, can be empty.</param>
        /// <param name="positions">The positions for the line.</param>
        /// <param name="colorKind">The chosen color kind.</param>
        /// <param name="primaryColor">The chosen primary color for the line.</param>
        /// <param name="secondaryColor">The chosen secondary color for the line.</param>
        /// <param name="thickness">The line thickness.</param>
        /// <param name="orderInLayer">The order in layer for the line object.</param>
        /// <param name="position">The position for the line object.</param>
        /// <param name="eulerAngles">The euler angles for the line object.</param>
        /// <param name="scale">The scale for the line object.</param>
        /// <param name="loop">Option to connect the line endpoint with the starting point.</param>
        /// <param name="lineKind">The line kind for the line.</param>
        /// <param name="tiling">The tiling for a dashed line kind.</param>
        /// <param name="associatedPage">The associated page of the line.</param>
        /// <param name="fillOutColor">The color for fill out the line; null if the line should not filled out.</param>
        /// <returns>The recreated or updated line.</returns>
        private static GameObject ReDrawLine(GameObject surface, string name, Vector3[] positions,
            ColorKind colorKind, Color primaryColor, Color secondaryColor, float thickness,
            int orderInLayer, Vector3 position, Vector3 eulerAngles, Vector3 scale, bool loop,
            LineKind lineKind, float tiling, int associatedPage, Color? fillOutColor)
        {
            /// Updates the z axis values of the positions to 0.
            UpdateZPositions(ref positions);

            /// Adjusts the current order in the layer if the
            /// order in layer for the line is greater than or equal to it.
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            if (orderInLayer >= holder.OrderInLayer && associatedPage == holder.CurrentPage)
            {
                holder.OrderInLayer = orderInLayer + 1;
            }
            if (associatedPage >= holder.MaxPageSize)
            {
                holder.MaxPageSize = associatedPage + 1;
            }
            if (orderInLayer >= ValueHolder.MaxOrderInLayer)
            {
                ValueHolder.MaxOrderInLayer = orderInLayer + 1;
            }

            /// Block for update an existing line with the given name.
            if (GameFinder.FindAttachedOrLocalDescendant(surface, name) != null)
            {
                GameObject line = GameFinder.FindAttachedOrLocalDescendant(surface, name);
                line.transform.localScale = scale;
                line.transform.localEulerAngles = eulerAngles;
                line.transform.localPosition = position;
                line.GetComponent<OrderInLayerValueHolder>().OrderInLayer = orderInLayer;
                line.GetComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
                Drawing(line, positions);
                FinishDrawing(line, loop, fillOutColor, false);

                return line;
            }
            else
            {
                /// Block for creating of a new line.
                Setup(surface, name, positions, colorKind, primaryColor, secondaryColor, thickness,
                    orderInLayer, lineKind, tiling, associatedPage,
                    out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
                line.transform.localScale = scale;
                line.transform.localEulerAngles = eulerAngles;
                line.transform.localPosition = position;

                renderer.SetPositions(positions);
                FinishDrawing(line, loop, fillOutColor, false);

                return line;
            }
        }

        /// <summary>
        /// Redraws or updates the line of the given <see cref="LineConf"/> to the <paramref name="surface"/>.
        /// It calls <see cref="ReDrawLine(GameObject, string, Vector3[], ColorKind, Color, Color,
        /// float, int, Vector3, Vector3, Vector3, bool, LineKind, float)"/>.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be displayed.</param>
        /// <param name="lineToRedraw">The configuration of the line to be restore.</param>
        /// <returns>The created line.</returns>
        public static GameObject ReDrawLine(GameObject surface, LineConf lineToRedraw)
        {
            GameObject line = ReDrawLine(surface,
                 lineToRedraw.ID,
                 lineToRedraw.RendererPositions,
                 lineToRedraw.ColorKind,
                 lineToRedraw.PrimaryColor,
                 lineToRedraw.SecondaryColor,
                 lineToRedraw.Thickness,
                 lineToRedraw.OrderInLayer,
                 lineToRedraw.Position,
                 lineToRedraw.EulerAngles,
                 lineToRedraw.Scale,
                 lineToRedraw.Loop,
                 lineToRedraw.LineKind,
                 lineToRedraw.Tiling,
                 lineToRedraw.AssociatedPage,
                 LineConf.GetFillOutColor(lineToRedraw));

            ApplyStoredOriginalAnchors(line, lineToRedraw);

            if (lineToRedraw.LineCapStart.CapKind != LineCap.None
                || lineToRedraw.LineCapEnd.CapKind != LineCap.None)
            {
                ApplyLineCaps(
                    line,
                    lineToRedraw.LineCapStart,
                    lineToRedraw.LineCapEnd,
                    LineConf.GetFillOutColor(lineToRedraw),
                    true);
            }

            return line;
        }
        #endregion

        #region Pivot and Collider
        /// <summary>
        /// Sets the pivot of the line to the center of the line.
        /// For an odd number of positions, the pivot is placed precisely at the midpoint.
        /// For an even number, the midpoint is calculated by adding the two middle points and
        /// dividing by two, obtaining the center of the two middle points.
        ///
        /// After determining the midpoint, the line positions are converted to world space,
        /// and the line is shifted to the midpoint.
        /// Subsequently, the world space coordinates are converted back to local,
        /// ensuring that the visual representation of the line remains unchanged while
        /// the pivot is shifted.
        /// </summary>
        /// <param name="line">The line in which the pivot should be set.</param>
        /// <param name="fillOutColor">The color for fill out the line; null if the line should not filled out.</param>
        /// <returns>The line with the pivot in the middle.</returns>
        public static GameObject SetPivot(GameObject line, Color? fillOutColor = null)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = GetRenderer(line);
                Vector3[] positions = new Vector3[renderer.positionCount];
                renderer.GetPositions(positions);
                Vector3 middlePos;
                /// Calculate the middle point.
                if (positions.Length % 2 == 1)
                {
                    /// Block for odd number of positions.
                    middlePos = positions[(int)Mathf.Round(positions.Length / 2)];
                }
                else
                {
                    /// Block for even number of positions.
                    Vector3 left = positions[positions.Length / 2 - 1];
                    Vector3 right = positions[positions.Length / 2];
                    middlePos = (left + right) / 2;
                }

                /// Restoration of the line's appearance.
                middlePos.z = line.transform.localPosition.z;
                Vector3[] convertedPositions = new Vector3[positions.Length];
                Array.Copy(sourceArray: positions, destinationArray: convertedPositions, length: positions.Length);
                /// Transform the line positions to world space.
                line.transform.TransformPoints(convertedPositions);
                /// Move the line to the middle pos.
                line.transform.localPosition = middlePos;
                /// Transform the line positions back to local space.
                line.transform.InverseTransformPoints(convertedPositions);
                /// Update the new line positions.
                Drawing(line, convertedPositions);
                /// Update the mesh collider.
                FinishDrawing(line, renderer.loop, fillOutColor);

                UpdateOriginalAnchors(line, convertedPositions);
            }
            return line;
        }

        /// <summary>
        /// Changes the pivot point of a line.
        /// Will be needed for <see cref="GameLineSplit"/>
        /// </summary>
        /// <param name="line">The line whose pivot point should be changed.</param>
        /// <returns><paramref name="line"/> with the new pivot point.</returns>
        public static GameObject ChangePivot(GameObject line)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = GetRenderer(line);
                Vector3[] positions = new Vector3[renderer.positionCount];
                renderer.GetPositions(positions);
                Vector3 middlePos;
                Vector3[] convertedPositions = new Vector3[positions.Length];
                Array.Copy(sourceArray: positions, destinationArray: convertedPositions, length: positions.Length);
                /// Transform the line positions to world space.
                line.transform.TransformPoints(convertedPositions);
                /// Calculate the middle point.
                if (convertedPositions.Length % 2 == 1)
                {
                    /// Block for odd number of positions.
                    middlePos = convertedPositions[(int)Mathf.Round(convertedPositions.Length / 2)];
                }
                else
                {
                    /// Block for even number of positions.
                    Vector3 left = convertedPositions[convertedPositions.Length / 2 - 1];
                    Vector3 right = convertedPositions[convertedPositions.Length / 2];
                    middlePos = (left + right) / 2;
                }
                /// Move the line to the middle pos.
                line.transform.position = middlePos;
                /// Transform the line positions back to local space.
                line.transform.InverseTransformPoints(convertedPositions);
                /// Update the new line positions.
                Drawing(line, convertedPositions);
                /// Update the mesh collider.
                FinishDrawing(line, renderer.loop);
            }
            return line;
        }

        /// <summary>
        /// Sets the pivot point for shapes.
        /// In this case, the pivot point is placed at the original hit point of creation,
        /// corresponding to the center of the shape.
        /// </summary>
        /// <param name="line">The shape for which the pivot point should be set.</param>
        /// <param name="middlePos">The center position for the shape.</param>
        /// <param name="fillOutColor">The color for fill out the line; null if the line should not filled out.</param>
        /// <param name="updateOriginalAnchors">
        /// Whether the original anchors of the main line should be updated after the pivot change.
        /// This must be false for generated line-cap objects.
        /// </param>
        /// <returns>The modified <paramref name="line"/> GameObject with the new pivot applied.</returns>
        public static GameObject SetPivotShape(GameObject line, Vector3 middlePos, Color? fillOutColor = null,
            bool updateOriginalAnchors = false)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = GetRenderer(line);
                /// Gets the positions of the shape.
                Vector3[] positions = new Vector3[renderer.positionCount];
                renderer.GetPositions(positions);
                middlePos.z = line.transform.localPosition.z;
                /// Gets a copy of the positions.
                Vector3[] convertedPositions = new Vector3[positions.Length];
                Array.Copy(sourceArray: positions, destinationArray: convertedPositions, length: positions.Length);
                /// Transforms the shape positions to world space.
                line.transform.TransformPoints(convertedPositions);
                /// Moves the shape to the middle position.
                line.transform.localPosition = middlePos;
                /// Transforms the shape positions back to local space to obtain the visual representation of the line.
                line.transform.InverseTransformPoints(convertedPositions);
                /// Updates the new line positions.
                Drawing(line, convertedPositions);
                /// Update the mesh collider.
                FinishDrawing(line, renderer.loop, fillOutColor);

                if (updateOriginalAnchors)
                {
                    UpdateOriginalAnchors(line, convertedPositions);
                }
            }
            return line;
        }

        /// <summary>
        /// Refreshes the mesh collider of the line.
        /// The mesh for the Mesh Collider is recalculated.
        /// </summary>
        /// <param name="line">The line whose mesh collider should be refreshed.</param>
        public static void RefreshCollider(GameObject line)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                MeshCollider collider = line.GetComponent<MeshCollider>();
                Mesh mesh = new();
                lineRenderer.BakeMesh(mesh);
                if (mesh.vertices.Distinct().Count() >= 3)
                {
                    collider.sharedMesh = mesh;
                }
            }
        }
        #endregion

        #region Line Appearance
        /// <summary>
        /// Changes the line kind of the given shape.
        /// </summary>
        /// <param name="shape">The shape whose line kind should be changed.</param>
        /// <param name="lineKind">The new line kind.</param>
        /// <param name="tiling">The tiling, if the new line kind is a dashed line kind.</param>
        public static void ChangeLineKind(GameObject shape, LineKind lineKind, float tiling)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                LineValueHolder holder = shape.GetComponent<LineValueHolder>();
                LineRenderer renderer = GetRenderer(shape);
                /// Changes the renderer material.
                renderer.sharedMaterial = GetMaterial(renderer.material.color, lineKind);
                /// Sets the correct texture mode for the renderer depending on the chosen line kind.
                SetTextureMode(renderer, lineKind);
                /// Sets the correct texture scale for the renderer depending on the chosen line kind.
                SetRendererTextrueScale(renderer, lineKind, tiling);
                /// Sets the new line kind to the line value holder.
                holder.LineKind = lineKind;
            }
        }

        /// <summary>
        /// Changes the color kind of the given shape.
        /// </summary>
        /// <param name="shape">The shape whose color kind should be changed.</param>
        /// <param name="colorKind">The new color kind for the shape.</param>
        /// <param name="conf">The old shape configuration; will be needed for restore the colors.</param>
        public static void ChangeColorKind(GameObject shape, ColorKind colorKind, ILineVisualConf conf)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                LineValueHolder holder = shape.GetComponent<LineValueHolder>();
                /// The initial color for deactivated color kinds is white.
                /// If the new ColorKind is TwoDashed, another material must be
                /// added to the line renderer for the second color.
                if (colorKind == ColorKind.TwoDashed)
                {
                    GetRenderer(shape).startColor = Color.white;
                    GetRenderer(shape).endColor = Color.white;
                    if (GetRenderer(shape).materials.Length == 1)
                    {
                        Material[] materials = new Material[2];
                        materials[0] = GetRenderer(shape).materials[0];
                        materials[1] = GetMaterial(Color.white, LineKind.Solid);
                        GetRenderer(shape).materials = materials;
                    }
                }
                else
                {
                    /// Block for the case that the previous color kind was <see cref="ColorKind.TwoDashed"/>,
                    /// then the additional material must be removed.
                    if (GetRenderer(shape).materials.Length > 1)
                    {
                        Material[] materials = new Material[1];
                        materials[0] = GetRenderer(shape).materials[0];
                        GetRenderer(shape).materials = materials;
                    }

                    /// Block for initialing the initial color for the remaining <see cref="ColorKind"/>.
                    if (colorKind == ColorKind.Gradient)
                    {
                        GetRenderer(shape).material.color = Color.white;
                    }
                    else
                    {
                        GetRenderer(shape).startColor = Color.white;
                        GetRenderer(shape).endColor = Color.white;
                    }
                }
                /// Updates the <see cref="LineValueHolder"/>.
                holder.ColorKind = colorKind;

                /// Restores the primary and secondary color of the line.
                GameEdit.ChangePrimaryColor(shape, conf.PrimaryColor);
                GameEdit.ChangeSecondaryColor(shape, conf.SecondaryColor);

                /// If the secondary color is clear, use the primary.
                /// It prevents it from looking like a part of the line has disappeared.
                if (conf.SecondaryColor == Color.clear)
                {
                    GameEdit.ChangeSecondaryColor(shape, conf.PrimaryColor);
                }
            }
        }

        /// <summary>
        /// Sets the texture scale for the line renderer depending on the chosen <paramref name="kind"/>.
        /// Required only for dashed line kinds.
        /// The X-Scale value varies for different LineKinds.
        /// It is multiplied by the material's tiling (15) to achieve the
        /// correct tiling for the dashed line.
        /// Tiling describes the spacing between the dashed lines.
        /// </summary>
        /// <param name="renderer">The line renderer that should be updated by his texture scale.</param>
        /// <param name="kind">The chosen color kind.</param>
        /// <param name="tiling">The tiling for a <see cref="LineKind.Dashed"/>.</param>
        private static void SetRendererTextrueScale(LineRenderer renderer, LineKind kind, float tiling)
        {
            switch (kind)
            {
                case LineKind.Dashed:
                    if (tiling == 0)
                    {
                        tiling = 0.05f;
                    }
                    renderer.textureScale = new Vector2(tiling, 0f);
                    break;
                case LineKind.Dashed25:
                    renderer.textureScale = new Vector2(5f / 3f, 0f);
                    break;
                case LineKind.Dashed50:
                    renderer.textureScale = new Vector2(10f / 3f, 0f);
                    break;
                case LineKind.Dashed75:
                    renderer.textureScale = new Vector2(5f, 0f);
                    break;
                case LineKind.Dashed100:
                    renderer.textureScale = new Vector2(20f / 3f, 0f);
                    break;
            }
        }

        /// <summary>
        /// Sets the appropriate texture mode for the given <see cref="LineRenderer"/>
        /// depending on the selected <see cref="LineKind"/>.
        /// </summary>
        /// <param name="renderer">
        /// The line renderer whose texture mode should be updated.
        /// </param>
        /// <param name="kind">
        /// The visual style of the line.
        /// Dashed line kinds use <see cref="LineTextureMode.Tile"/> so that the
        /// dash pattern keeps a constant size independent of the line length.
        /// All other line kinds use <see cref="LineTextureMode.Stretch"/>.
        /// </param>
        private static void SetTextureMode(LineRenderer renderer, LineKind kind)
        {
            switch (kind)
            {
                case LineKind.Dashed:
                case LineKind.Dashed25:
                case LineKind.Dashed50:
                case LineKind.Dashed75:
                case LineKind.Dashed100:
                    renderer.textureMode = LineTextureMode.Tile;
                    break;

                default:
                    renderer.textureMode = LineTextureMode.Stretch;
                    break;
            }
        }

        /// <summary>
        /// Creates the material associated with the <paramref name="kind"/>.
        /// </summary>
        /// <param name="color">The color for the material.</param>
        /// <param name="kind">The chosen line kind.</param>
        /// <returns>The created material.</returns>
        private static Material GetMaterial(Color color, LineKind kind)
        {
            /// Define the color range.
            ColorRange colorRange = new(color, color, 1);
            MaterialsFactory.ShaderType shaderType;
            /// Select the correct shader type.
            if (kind.Equals(LineKind.Solid))
            {
                /// Material for the <see cref="LineKind.Solid"/>
                shaderType = MaterialsFactory.ShaderType.PortalFreeLine;
            }
            else
            {
                /// Material for the dashed kinds.
                shaderType = MaterialsFactory.ShaderType.DrawableDashedLine;
            }
            /// Gets the material of the shader type.
            MaterialsFactory materials = new(shaderType, colorRange);
            Material material = materials.Get(0);
            return material;
        }
        #endregion

        #region Geometry Helpers

        /// <summary>
        /// Gets the original unshortened line positions for the given line.
        /// The first and last positions are restored from the stored original anchors
        /// if available.
        /// </summary>
        /// <param name="shape">The line GameObject.</param>
        /// <returns>
        /// A copy of the original line positions if available; otherwise a copy of the
        /// current renderer positions.
        /// </returns>
        private static Vector3[] GetOriginalLinePositions(GameObject shape)
        {
            LineConf line = LineConf.GetLine(shape);
            if (line == null || line.RendererPositions == null || line.RendererPositions.Length < 2)
            {
                return null;
            }

            Vector3[] originalPositions = new Vector3[line.RendererPositions.Length];
            Array.Copy(line.RendererPositions, originalPositions, line.RendererPositions.Length);

            originalPositions[0] = line.OriginalStartAnchor;
            originalPositions[originalPositions.Length - 1] = line.OriginalEndAnchor;

            return originalPositions;
        }

        /// <summary>
        /// Updates the stored original anchors of the given line.
        /// Existing values are overwritten.
        /// </summary>
        /// <param name="line">The line whose anchors should be updated.</param>
        /// <param name="positions">The new original positions.</param>
        public static void UpdateOriginalAnchors(GameObject line, Vector3[] positions)
        {
            if (line == null || positions == null || positions.Length < 2)
            {
                return;
            }

            LineAnchorValueHolder anchorHolder = line.GetComponent<LineAnchorValueHolder>();
            if (anchorHolder == null)
            {
                anchorHolder = line.AddComponent<LineAnchorValueHolder>();
            }

            anchorHolder.OriginalStartAnchor =
                new Vector3(positions[0].x, positions[0].y, 0.0f);

            anchorHolder.OriginalEndAnchor =
                new Vector3(
                    positions[positions.Length - 1].x,
                    positions[positions.Length - 1].y,
                    0.0f);

            anchorHolder.HasOriginalAnchors = true;
        }

        /// <summary>
        /// Applies the stored original anchors from the given line configuration to the line object.
        /// </summary>
        /// <param name="line">The line object whose anchor holder should be updated.</param>
        /// <param name="lineConf">The line configuration containing the stored original anchors.</param>
        private static void ApplyStoredOriginalAnchors(GameObject line, LineConf lineConf)
        {
            if (line == null || lineConf == null)
            {
                return;
            }

            LineAnchorValueHolder anchorHolder = line.GetComponent<LineAnchorValueHolder>();
            if (anchorHolder == null)
            {
                anchorHolder = line.AddComponent<LineAnchorValueHolder>();
            }

            anchorHolder.OriginalStartAnchor = lineConf.OriginalStartAnchor;
            anchorHolder.OriginalEndAnchor = lineConf.OriginalEndAnchor;
            anchorHolder.HasOriginalAnchors = true;
        }

        /// <summary>
        /// Sets the z positions of the given <paramref name="positions"/> to zero.
        /// It is needed because a Line Renderer by itself
        /// changes the z values in case of an overlap.
        /// However, this is problematic for the change order in layer variant.
        /// </summary>
        /// <param name="positions">The positions of the line from the line renderer.</param>
        private static void UpdateZPositions(ref Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i].z = 0;
            }
        }

        /// <summary>
        /// Counts the different positions of an <see cref="Vector3"/> array.
        /// </summary>
        /// <param name="positions">The positions to be examined.</param>
        /// <returns>The count of different positions.</returns>
        public static int DifferentPositionCounter(Vector3[] positions)
        {
            return new List<Vector3>(positions).Distinct().ToList().Count;
        }

        /// <summary>
        /// Counts the different positions of a shape game object.
        /// </summary>
        /// <param name="shape">The shape game object.</param>
        /// <returns>The count of different positions.</returns>
        public static int DifferentPositionCounter(GameObject shape)
        {
            if (shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
            {
                Vector3[] positions = new Vector3[GetRenderer(shape).positionCount];
                shape.GetComponent<LineRenderer>().GetPositions(positions);
                return DifferentPositionCounter(positions);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculates the various vertices of a mesh that has been computed
        /// from the line points of the line renderer.
        /// </summary>
        /// <param name="line">The line which holds the line renderer.</param>
        /// <returns>The number of different vertices.</returns>
        public static int DifferentMeshVerticesCounter(GameObject line)
        {
            if (line.CompareTag(Tags.Line))
            {
                LineRenderer renderer = GetRenderer(line);
                Mesh mesh = new();
                renderer.BakeMesh(mesh);
                return mesh.vertices.Distinct().ToList().Count;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Converts a world space coordinate to a local space coordinate
        /// as if it were a line originating from that point.
        /// A line is created for the calculation and then deleted afterward.
        /// </summary>
        /// <param name="surface">The targeted drawable surface.</param>
        /// <param name="position">The position to be transformed.</param>
        /// <returns>The converted position.</returns>
        public static Vector3 GetConvertedPosition(GameObject surface, Vector3 position)
        {
            Vector3 convertedPosition;
            Setup(surface, "", new Vector3[] { position }, ColorKind.Monochrome, ValueHolder.CurrentPrimaryColor,
                Color.clear, ValueHolder.CurrentThickness, 0, ValueHolder.CurrentLineKind, 1, 0,
                out GameObject line, out LineRenderer renderer, out MeshCollider meshCollider);
            convertedPosition = line.transform.InverseTransformPoint(position) - ValueHolder.DistanceToDrawable;
            Destroyer.Destroy(line);
            return convertedPosition;
        }
        #endregion

        #region Fill Out
        /// <summary>
        /// Creates or updates the fill-out object for a line-based shape.
        /// The fill-out is only possible for objects tagged as <see cref="Tags.Line"/>
        /// or <see cref="Tags.LineCap"/> that contain more than two different positions.
        /// If a fill-out object already exists, its color and mesh are updated.
        /// Otherwise, a new fill-out object is created as a child of the given shape.
        /// </summary>
        /// <param name="shape">
        /// The line-based shape whose interior should be filled.
        /// Must be tagged as <see cref="Tags.Line"/> or <see cref="Tags.LineCap"/>.
        /// </param>
        /// <param name="color">
        /// The fill color to use.
        /// If null, the current shape color is used.
        /// </param>
        /// <param name="showInfo">
        /// Whether an info notification should be shown if the fill-out cannot be created.
        /// </param>
        /// <returns>
        /// True if the fill-out mesh was successfully created or updated;
        /// otherwise, false.
        /// </returns>
        public static bool FillOut(GameObject shape, Color? color = null, bool showInfo = false)
        {
            if ((shape.CompareTag(Tags.Line) || shape.CompareTag(Tags.LineCap))
                && DifferentPositionCounter(shape) > 2)
            {
                GameObject fillOut;
                MeshFilter meshFilter;
                MeshCollider collider;
                GameObject ownFillOut = GetOwnFillOutObject(shape);

                if (ownFillOut == null)
                {
                    fillOut = new(ValueHolder.FillOut);
                    fillOut.transform.SetParent(shape.transform);
                    fillOut.transform.rotation = shape.transform.rotation;
                    Vector3 pos = shape.transform.position;
                    /// To avoid an overlapping issue, position the fill slightly behind the line.
                    fillOut.transform.position = new Vector3(pos.x, pos.y, pos.z + 0.00001f);
                    meshFilter = fillOut.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = fillOut.AddComponent<MeshRenderer>();
                    collider = fillOut.AddComponent<MeshCollider>();
                    Color fillColor = color ?? shape.GetColor();
                    /// Set a default material if none is assigned
                    if (meshRenderer.sharedMaterial == null)
                    {
                        meshRenderer.sharedMaterial = GetMaterial(fillColor, LineKind.Solid);
                    }
                }
                else
                {
                    fillOut = ownFillOut;
                    meshFilter = fillOut.GetComponent<MeshFilter>();
                    collider = fillOut.GetComponent <MeshCollider>();
                    GameEdit.ChangeFillOutColor(shape, color ?? shape.GetColor());
                }

                Vector3[] worldPos = new Vector3[shape.GetComponent<LineRenderer>().positionCount];
                shape.GetComponent<LineRenderer>().GetPositions(worldPos);
                /// Creates the mesh for the fill out area.
                int numPos = shape.GetComponent<LineRenderer>().positionCount;
                Vector3[] vertices = new Vector3[numPos];
                int[] triangles = new int[(numPos - 2) * 3];
                for (int i = 0; i < numPos; i++)
                {
                    vertices[i] = worldPos[i];
                }
                int t = 0;
                for (int i = 1; i < numPos - 1; i++)
                {
                    triangles[t] = 0;
                    triangles[t + 1] = i;
                    triangles[t + 2] = i + 1;
                    t += 3;
                }
                Mesh mesh = new() { vertices = vertices, triangles = triangles };

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                meshFilter.mesh = mesh;
                if (mesh.vertices.Distinct().ToList().Count > 2)
                {
                    collider.sharedMesh = mesh;
                    GameScaler.SetScale(fillOut, Vector3.one);
                    return true;
                }
                else
                {
                    GameObject.DestroyImmediate(fillOut);
                    return false;
                }
            } else
            {
                if (showInfo)
                {
                    ShowNotification.Info("Fill out cannot be applied.",
                        "The fill out cannot be applied because the selected object either is no line or has too few points.");
                }
                if (shape.FindDescendant(ValueHolder.FillOut))
                {
                    Destroyer.Destroy(shape.FindDescendant(ValueHolder.FillOut));
                }
                return false;
            }
        }

        /// <summary>
        /// Returns the direct fill-out child object of the given shape.
        /// Only direct children are considered.
        /// </summary>
        /// <param name="shape">The shape whose own fill-out child should be returned.</param>
        /// <returns>The direct fill-out child or null if none exists.</returns>
        internal static GameObject GetOwnFillOutObject(GameObject shape)
        {
            if (shape == null)
            {
                return null;
            }

            Transform child = shape.transform.Find(ValueHolder.FillOut);
            return child != null ? child.gameObject : null;
        }
        #endregion

        #region Line Caps
        /// <summary>
        /// Removes all generated line cap child objects from the given shape.
        /// </summary>
        /// <param name="shape">The shape whose generated line caps should be removed.</param>
        public static void RemoveLineCaps(GameObject shape)
        {
            if (shape == null)
            {
                return;
            }

            List<GameObject> capsToRemove = new();

            foreach (Transform child in shape.transform)
            {
                if (child.gameObject.name.StartsWith(ValueHolder.LineStartCapPrefix, StringComparison.Ordinal)
                    || child.gameObject.name.StartsWith(ValueHolder.LineEndCapPrefix, StringComparison.Ordinal))
                {
                    capsToRemove.Add(child.gameObject);
                }
            }

            foreach (GameObject cap in capsToRemove)
            {
                // Line caps are removed immediately because they may be recreated in the same frame.
                // Delayed destruction would keep the old object alive until frame end, causing
                // name collisions and preventing correct cap recreation.
                // A synchronous immediate rebuild is preferred over an async delay because
                // cap changes should be completed deterministically in one operation without
                // temporary intermediate states or additional task coordination.
                GameObject.DestroyImmediate(cap);
            }
        }

        /// <summary>
        /// Draws a line cap as a child object of the given line shape using the visual settings
        /// of the parent line.
        /// </summary>
        /// <param name="shape">The parent line shape.</param>
        /// <param name="prefix">The prefix describing the cap position.</param>
        /// <param name="points">The local points of the line cap.</param>
        /// <param name="anchor">The anchor point of the cap in the local space of the parent line.</param>
        /// <param name="angleInDegrees">The rotation angle of the cap in degrees.</param>
        /// <param name="line">The parent line configuration whose visual settings are used.</param>
        /// <param name="capKind">The cap kind.</param>
        /// <returns>The created or updated line cap object.</returns>
        public static GameObject DrawLineCap(GameObject shape, string prefix, Vector3[] points,
            Vector3 anchor, float angleInDegrees, LineConf line, LineCap capKind)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            LineCapConf capConf = CreateLineCapConf(line, null, capKind);
            return DrawLineCap(shape, prefix, points, anchor, angleInDegrees, capConf);
        }

        /// <summary>
        /// Draws a line cap as a child object of the given line shape using the visual settings
        /// stored in the given <see cref="LineCapConf"/>.
        /// </summary>
        /// <param name="shape">The parent line shape.</param>
        /// <param name="name">The name of the line cap.</param>
        /// <param name="points">The local points of the line cap.</param>
        /// <param name="anchor">The anchor point of the cap in the local space of the parent line.</param>
        /// <param name="angleInDegrees">The rotation angle of the cap in degrees.</param>
        /// <param name="capConf">The configuration of the line cap.</param>
        /// <returns>The created or updated line cap object.</returns>
        public static GameObject DrawLineCap(GameObject shape, string name, Vector3[] points,
            Vector3 anchor, float angleInDegrees, LineCapConf capConf)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            if (capConf == null)
            {
                throw new ArgumentNullException(nameof(capConf));
            }

            GameObject drawableSurface = GameFinder.GetDrawableSurface(shape);

            Color? fillOutColor = capConf.FillOutStatus && CanApplyFillOut(points)
                ? capConf.FillOutColor
                : null;

            GameObject capObject = DrawLine(drawableSurface, name, points, capConf.ColorKind,
                capConf.PrimaryColor, capConf.SecondaryColor, capConf.Thickness, false, capConf.LineKind,
                capConf.Tiling, false, fillOutColor, false, false);

            SetPivotShape(capObject, Vector3.zero);
            capObject.transform.SetParent(shape.transform, false);
            capObject.transform.localEulerAngles = new Vector3(0.0f, 0.0f, angleInDegrees);
            capObject.transform.localPosition = new Vector3(anchor.x, anchor.y, shape.transform.localPosition.z);
            capObject.tag = Tags.LineCap;

            LineCapValueHolder capValueHolder = shape.GetComponent<LineCapValueHolder>();
            if (name.StartsWith(ValueHolder.LineStartCapPrefix))
            {
                capValueHolder.StartCap = capConf.CapKind;
            }
            else
            {
                capValueHolder.EndCap = capConf.CapKind;
            }

            return capObject;
        }

        /// <summary>
        /// Determines whether a fill-out can be applied to a shape defined by the given points.
        /// A fill-out is only possible if the shape consists of more than two distinct points,
        /// that is, if it describes a closed or fillable area instead of a simple line segment.
        /// </summary>
        /// <param name="points">The points describing the shape.</param>
        /// <returns>
        /// True if the shape can be filled; otherwise, false.
        /// </returns>
        private static bool CanApplyFillOut(Vector3[] points)
        {
            return points != null && points.Distinct().Count() > 2;
        }

        /// <summary>
        /// Builds a unique name for a line cap object based on the given shape.
        /// The shape prefix "Line" is removed from the shape name if present.
        /// </summary>
        /// <param name="shape">The parent shape of the line cap.</param>
        /// <param name="prefix">The prefix of the line cap object.</param>
        /// <returns>A unique and readable line cap object name.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="shape"/> is null.
        /// </exception>
        private static string GetLineCapName(GameObject shape, string prefix)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            string shapeName = shape.name;

            // Remove "Line" prefix if present
            if (shapeName.StartsWith(ValueHolder.LinePrefix, StringComparison.Ordinal))
            {
                shapeName = shapeName.Substring(ValueHolder.LinePrefix.Length);
            }

            return prefix + shapeName;
        }

        /// <summary>
        /// Returns all line cap objects belonging to the given line.
        /// The start or end cap objects are selected depending on <paramref name="isStartCap"/>.
        /// </summary>
        /// <param name="shape">The line whose cap objects should be returned.</param>
        /// <param name="isStartCap">True to return the start cap objects; false to return the end cap objects.</param>
        /// <returns>A list containing all matching line cap objects.
        /// Returns an empty list if no matching objects exist.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="shape"/> is null.
        /// </exception>
        internal static List<GameObject> GetLineCapObjects(GameObject shape, bool isStartCap)
        {
            string capName = GetLineCapName(shape,
                isStartCap ? ValueHolder.LineStartCapPrefix : ValueHolder.LineEndCapPrefix);

            return shape.FindAllDescendantWithStartingName(capName);
        }

        /// <summary>
        /// Applies the given start and end line caps to the specified line.
        /// Existing line cap objects are removed and recreated based on the provided configurations.
        /// The line itself is shortened so that it visually connects correctly to the caps.
        ///
        /// Depending on <paramref name="useCapConfVisuals"/>, the visual appearance of the caps
        /// is determined differently:
        /// - If false, the caps inherit their visual properties (color, line kind, thickness, etc.)
        ///   from the parent line.
        /// - If true, the caps use the visual properties stored in the given <see cref="LineCapConf"/>.
        /// </summary>
        /// <param name="shape">The line GameObject to which the caps should be applied.</param>
        /// <param name="startConf">The configuration of the start cap.</param>
        /// <param name="endConf">The configuration of the end cap.</param>
        /// <param name="fillOutColor">The fill-out color of the line, if any.</param>
        /// <param name="useCapConfVisuals">
        /// Whether the caps should use their own stored visual configuration (true)
        /// or inherit the visual properties from the parent line (false).
        /// </param>
        public static void ApplyLineCaps(GameObject shape, LineCapConf startConf, LineCapConf endConf,
            Color? fillOutColor = null, bool useCapConfVisuals = false)
        {
            if (shape == null)
            {
                return;
            }

            RemoveLineCaps(shape);

            LineConf line = LineConf.GetLine(shape);
            if (line == null)
            {
                return;
            }

            Vector3[] originalPositions = GetOriginalLinePositions(shape);
            if (originalPositions == null || originalPositions.Length < 2)
            {
                return;
            }

            Vector3[] shortenedPositions = new Vector3[originalPositions.Length];
            Array.Copy(originalPositions, shortenedPositions, originalPositions.Length);

            line.RendererPositions = originalPositions;

            ApplyLineCapToPositions(shape, line, shortenedPositions, startConf, LineCapPosition.Start);
            ApplyLineCapToPositions(shape, line, shortenedPositions, endConf, LineCapPosition.End);

            Drawing(shape, shortenedPositions, fillOutColor);

            DrawLineCapObject(shape, line, startConf, LineCapPosition.Start, useCapConfVisuals);
            DrawLineCapObject(shape, line, endConf, LineCapPosition.End, useCapConfVisuals);
        }

        /// <summary>
        /// Applies the shortening required for the given line cap to the supplied line positions.
        /// </summary>
        /// <param name="shape">The line GameObject.</param>
        /// <param name="line">The full line configuration.</param>
        /// <param name="positions">The positions to shorten.</param>
        /// <param name="conf">The line-cap configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        private static void ApplyLineCapToPositions(GameObject shape, LineConf line, Vector3[] positions,
            LineCapConf conf, LineCapPosition position)
        {
            if (shape == null || line == null || positions == null
                || conf == null || conf.CapKind == LineCap.None
                || !CanCalculate(line, position))
            {
                return;
            }

            List<LineCapShape> capShapes = GetShapes(conf.CapKind, line, position);
            LineCapShape capShape = capShapes[0];

            Vector3 anchor;
            Vector3 direction;

            if (position == LineCapPosition.Start)
            {
                anchor = line.RendererPositions[0];
                direction = line.RendererPositions[0] - line.RendererPositions[1];
            }
            else
            {
                anchor = line.RendererPositions[line.RendererPositions.Length - 1];
                direction = line.RendererPositions[line.RendererPositions.Length - 1]
                            - line.RendererPositions[line.RendererPositions.Length - 2];
            }

            if (direction == Vector3.zero)
            {
                return;
            }

            float angleInDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector3 rotatedConnectionPoint = RotatePoint(capShape.ConnectionPoint, angleInDegrees);

            if (position == LineCapPosition.Start)
            {
                positions[0] = anchor + rotatedConnectionPoint;
            }
            else
            {
                positions[positions.Length - 1] = anchor + rotatedConnectionPoint;
            }
        }

        /// <summary>
        /// Draws the line cap object for the given configuration.
        /// </summary>
        /// <param name="shape">The parent line GameObject.</param>
        /// <param name="line">The full line configuration.</param>
        /// <param name="conf">The line-cap configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <param name="useCapConfVisuals">
        /// Whether the visual settings of <paramref name="conf"/> should be used.
        /// If false, the visual settings of <paramref name="line"/> are used instead.
        /// </param>
        private static void DrawLineCapObject(GameObject shape, LineConf line, LineCapConf conf,
            LineCapPosition position, bool useCapConfVisuals)
        {
            if (shape == null || line == null
                || conf == null || conf.CapKind == LineCap.None
                || !CanCalculate(line, position))
            {
                return;
            }

            List<LineCapShape> capShapes = GetShapes(conf.CapKind, line, position);

            Vector3 anchor;
            Vector3 direction;
            string prefix;

            if (position == LineCapPosition.Start)
            {
                anchor = line.RendererPositions[0];
                direction = line.RendererPositions[0] - line.RendererPositions[1];
                prefix = ValueHolder.LineStartCapPrefix;
            }
            else
            {
                anchor = line.RendererPositions[line.RendererPositions.Length - 1];
                direction = line.RendererPositions[line.RendererPositions.Length - 1]
                            - line.RendererPositions[line.RendererPositions.Length - 2];
                prefix = ValueHolder.LineEndCapPrefix;
            }

            if (direction == Vector3.zero)
            {
                return;
            }

            float angleInDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            for (int i = 0; i < capShapes.Count; i++)
            {
                LineCapShape capShape = capShapes[i];

                string name = GetLineCapName(shape, prefix) + "_" + i;

                if (useCapConfVisuals)
                {
                    DrawLineCap(shape, name, capShape.Points, anchor, angleInDegrees, conf);
                }
                else
                {
                    DrawLineCap(shape, name, capShape.Points, anchor, angleInDegrees, line, conf.CapKind);
                }
            }
        }

        /// <summary>
        /// Rotates a local point around the origin by the given angle in degrees.
        /// </summary>
        /// <param name="point">The point to rotate.</param>
        /// <param name="angleInDegrees">The rotation angle in degrees.</param>
        /// <returns>The rotated point.</returns>
        private static Vector3 RotatePoint(Vector3 point, float angleInDegrees)
        {
            float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleInRadians);
            float sin = Mathf.Sin(angleInRadians);

            float x = (point.x * cos) - (point.y * sin);
            float y = (point.x * sin) + (point.y * cos);

            return new Vector3(x, y, point.z);
        }

        /// <summary>
        /// Creates a normalized line-cap configuration for the given cap kind based on
        /// an existing line-cap configuration.
        /// Existing visual settings are preserved as far as possible, while cap-specific
        /// defaults required by the new cap kind are applied.
        /// </summary>
        /// <param name="line">The parent line configuration.</param>
        /// <param name="existingConf">The existing line-cap configuration to preserve.</param>
        /// <param name="newCapKind">The newly selected line cap kind.</param>
        /// <returns>The updated line-cap configuration.</returns>
        internal static LineCapConf CreateLineCapConf(LineConf line, LineCapConf existingConf, LineCap newCapKind)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            LineCapConf capConf = new();

            bool reuseExistingVisuals = existingConf != null && existingConf.CapKind != LineCap.None;

            if (reuseExistingVisuals)
            {
                capConf.ColorKind = existingConf.ColorKind;
                capConf.PrimaryColor = existingConf.PrimaryColor;
                capConf.SecondaryColor = existingConf.SecondaryColor;
                capConf.Thickness = existingConf.Thickness;
                capConf.LineKind = existingConf.LineKind;
                capConf.Tiling = existingConf.Tiling;
                capConf.FillOutStatus = existingConf.FillOutStatus;
                capConf.FillOutColor = existingConf.FillOutColor;
            }
            else
            {
                capConf.ColorKind = line.ColorKind;
                capConf.PrimaryColor = line.PrimaryColor;
                capConf.SecondaryColor = line.SecondaryColor;
                capConf.Thickness = line.Thickness;
                capConf.LineKind = LineKind.Solid;
                capConf.Tiling = ValueHolder.StandardLineTiling;
                capConf.FillOutStatus = false;
                capConf.FillOutColor = Color.clear;
            }

            capConf.CapKind = newCapKind;

            ApplyCapKindDefaults(line, capConf);

            return capConf;
        }

        /// <summary>
        /// Applies cap-kind-specific defaults to the given line-cap configuration.
        /// Existing values are preserved unless the selected cap kind requires a
        /// specific override.
        /// </summary>
        /// <param name="line">The parent line configuration.</param>
        /// <param name="capConf">The line-cap configuration to normalize.</param>
        /// <remarks>
        /// If a line cap defines its own fill-out defaults here,
        /// it should also be added to <see cref="ActionHelpers.LineCapPointsCalculator.HasOwnFillOutDefault"/>
        /// so the edit-mode restoration logic behaves correctly.
        /// </remarks>
        private static void ApplyCapKindDefaults(LineConf line, LineCapConf capConf)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (capConf == null)
            {
                throw new ArgumentNullException(nameof(capConf));
            }

            switch (capConf.CapKind)
            {
                case LineCap.Composition:
                    capConf.FillOutStatus = true;

                    if (capConf.FillOutColor == Color.clear)
                    {
                        capConf.FillOutColor = capConf.PrimaryColor;
                    }
                    break;

                case LineCap.None:
                    capConf.FillOutStatus = false;
                    capConf.FillOutColor = Color.clear;
                    break;
            }
        }
        #endregion
    }
}
