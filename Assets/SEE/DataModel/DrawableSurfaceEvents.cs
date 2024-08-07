using SEE.DataModel.Drawable;
using SEE.Tools.ReflexionAnalysis;
using System;

namespace SEE.DataModel
{
    /// <summary>
    /// An event representing a change to a drawable surface component.
    /// </summary>
    public class DrawableSurfaceEvent : ChangeEvent
    {
        /// <summary>
        /// The affected component
        /// </summary>
        public readonly DrawableSurface Surface;

        public DrawableSurfaceEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, affectedGraph, change)
        {
            Surface = surface;
        }

        protected override string Description() =>
            $"{Surface} has been changed";
    }

    /// <summary>
    /// Event indicating the addition of a new drawable surface.
    /// </summary>
    public class AddSurfaceEvent : DrawableSurfaceEvent
    {
        public AddSurfaceEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, surface, affectedGraph, change)
        {
        }

        protected override string Description() =>
            $"{Surface} has been created";
    }

    /// <summary>
    /// Event indicating the removal of a drawable surface.
    /// </summary>
    public class RemoveSurfaceEvent : DrawableSurfaceEvent
    {
        public RemoveSurfaceEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, surface, affectedGraph, change)
        {
        }

        protected override string Description() =>
            $"{Surface} has been deleted";
    }

    /// <summary>
    /// Event indicating the color change of a drawable surface.
    /// </summary>
    public class ColorChangeEvent : DrawableSurfaceEvent
    {
        public ColorChangeEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, surface, affectedGraph, change)
        {
        }

        protected override string Description() =>
            $"{Surface.Color} has been changed.";
    }

    /// <summary>
    /// Event indicating the description change of a drawable surface.
    /// </summary>
    public class DescriptionChangeEvent : DrawableSurfaceEvent
    {
        public DescriptionChangeEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, surface, affectedGraph, change)
        {
        }

        protected override string Description() =>
            $"{Surface.Description} has been changed.";
    }

    /// <summary>
    /// Event indicating the lighting change of a drawable surface.
    /// </summary>
    public class LightingChangeEvent : DrawableSurfaceEvent
    {
        public LightingChangeEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, surface, affectedGraph, change)
        {
        }

        protected override string Description() =>
            $"{Surface.Lighting} has been changed.";
    }

    /// <summary>
    /// Event indicating the visibility change of a drawable surface.
    /// </summary>
    public class VisibilityChangeEvent : DrawableSurfaceEvent
    {
        public VisibilityChangeEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, surface, affectedGraph, change)
        {
        }
        protected override string Description() =>
            $"{Surface.Visibility} has been changed.";
    }

    /// <summary>
    /// Event indicating the page of a drawable surface changes.
    /// </summary>
    public class PageChangeEvent : DrawableSurfaceEvent
    {
        public PageChangeEvent(Guid versionId, DrawableSurface surface,
            ReflexionSubgraphs? affectedGraph = null, ChangeType? change = null)
            : base(versionId, surface, affectedGraph, change)
        {
        }

        protected override string Description() =>
            $"{Surface.CurrentPage} has been changed.";
    }
}
