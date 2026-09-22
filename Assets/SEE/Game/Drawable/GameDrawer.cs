using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Utils;
using System.Linq;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Provides creation, drawing, updating, and restoration of drawable lines.
    /// </summary>
    public static class GameDrawer
    {
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
            renderer.sharedMaterial = GameLineAppearance.GetMaterial(primaryColor, lineKind);
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
                    materials[1] = GameLineAppearance.GetMaterial(Color.white, LineKind.Solid);
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
            GameLineAppearance.SetTextureMode(renderer, lineKind);
            /// Sets the texture scale of the renderer depending on the chosen line kind.
            GameLineAppearance.SetRendererTextureScale(renderer, lineKind, tiling);
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
        /// If desired, <see cref="GameLineGeometry.SetPivot"/> can then be called to set the correct pivot.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line should be displayed.</param>
        /// <param name="positions">The start positions for the line.</param>
        /// <param name="colorKind">The chosen color kind for the line.</param>
        /// <param name="primaryColor">The chosen primary color for the line.</param>
        /// <param name="secondaryColor">The chosen secondary color for the line.</param>
        /// <param name="thickness">The line thickness.</param>
        /// <param name="lineKind">The chosen line kind.</param>
        /// <param name="tiling">The tiling for a dashed line kind.</param>
        /// <param name="freehandLine">
        /// Whether the created line is a freehand line.
        /// Freehand lines do not support line caps because their first and last
        /// segments can be too short or unstable for reliable cap calculation.
        /// </param>
        /// <returns>The created line.</returns>
        public static GameObject StartDrawing(GameObject surface, Vector3[] positions, ColorKind colorKind,
            Color primaryColor, Color secondaryColor, float thickness, LineKind lineKind, float tiling,
            bool freehandLine = false)
        {
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            Setup(surface, "", positions, colorKind, primaryColor, secondaryColor, thickness,
                holder.OrderInLayer, lineKind, tiling, holder.CurrentPage,
                out GameObject line, out LineRenderer _, out MeshCollider _);
            line.GetComponent<LineValueHolder>().Initialize(freehandLine);
            holder.Inc();
            ValueHolder.MaxOrderInLayer++;

            return line;
        }

        /// <summary>
        /// Updates the positions of an existing line.
        /// </summary>
        /// <param name="line">The line to be updated.</param>
        /// <param name="positions">The new positions for the line.</param>
        /// <param name="fillOutColor">
        /// The color of the fill-out, or null if no fill-out should be updated.
        /// </param>
        /// <param name="preserveFillOutColliderState">
        /// Whether the current enabled state of an existing fill-out collider should
        /// be preserved while updating the fill-out.
        /// </param>
        public static void Drawing(
            GameObject line,
            Vector3[] positions,
            Color? fillOutColor = null,
            bool preserveFillOutColliderState = false)
        {
            bool? fillOutColliderEnabled = null;

            if (preserveFillOutColliderState)
            {
                GameObject existingFillOut = GameLineFillOut.GetOwnFillOutObject(line);

                if (existingFillOut != null)
                {
                    MeshCollider existingCollider = existingFillOut.GetComponent<MeshCollider>();

                    if (existingCollider != null)
                    {
                        fillOutColliderEnabled = existingCollider.enabled;
                    }
                }
            }

            LineRenderer renderer = GetRenderer(line);
            renderer.positionCount = positions.Length;

            /// Ensure that all points of the line have a z-axis value of 0.
            GameLineGeometry.UpdateZPositions(ref positions);
            renderer.SetPositions(positions);

            if (fillOutColor != null && GameLineFillOut.FillOut(line, fillOutColor))
            {
                GameObject fillOut = GameLineFillOut.GetOwnFillOutObject(line);

                if (fillOut != null)
                {
                    MeshCollider collider = fillOut.GetComponent<MeshCollider>();

                    if (collider != null)
                    {
                        collider.enabled = fillOutColliderEnabled ?? false;
                    }
                }
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
            if (fillOutColor != null && GameLineFillOut.FillOut(line, fillOutColor.Value, showInfo))
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
            GameLineGeometry.UpdateZPositions(ref positions);
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
            GameLineGeometry.UpdateZPositions(ref positions);

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

            line.GetComponent<LineValueHolder>().Initialize(lineToRedraw.FreehandLine);

            GameLineGeometry.ApplyStoredOriginalAnchors(line, lineToRedraw);

            if (lineToRedraw.LineCapStart.CapKind != LineCap.None
                || lineToRedraw.LineCapEnd.CapKind != LineCap.None)
            {
                GameLineCapApplicator.ApplyLineCaps(
                    line,
                    lineToRedraw.LineCapStart,
                    lineToRedraw.LineCapEnd,
                    LineConf.GetFillOutColor(lineToRedraw),
                    true);
            }

            return line;
        }
    }
}
