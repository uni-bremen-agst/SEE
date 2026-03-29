using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using System;
using UnityEngine;

namespace SEE.DataModel.Drawable
{
    /// <summary>
    /// Class used by the <see cref="DrawableManagerWindow"> to detect changes to a drawable surface.
    /// </summary>
    public class DrawableSurface : Observable<ChangeEvent>
    {
        public DrawableSurface(GameObject surface)
        {
            if (surface != null && surface.CompareTag(Tags.Drawable))
            {
                initFinishIndicator = false;
                InitAsync(surface).Forget();
            }
        }

        /// <summary>
        /// Instantiates the surface asynchronously.
        /// This is necessary because for objects added during runtime, some properties,
        /// such as the name, are adjusted later.
        /// </summary>
        /// <param name="surface">The depending surface for the instantiation.</param>
        /// <returns>Nothing, it waits until the created <paramref name="surface"/> is properly instantiated.</returns>
        private async UniTaskVoid InitAsync(GameObject surface)
        {
            while (GameFinder.GetUniqueID(surface).Contains("Clone"))
            {
                await UniTask.Yield();
            }
            DrawableConfig config = DrawableConfigManager.GetDrawableConfig(surface);
            description = config.Description;
            lighting = config.Lighting;
            visibility = config.Visibility;
            color = config.Color;
            id = Guid.NewGuid();
            this.surface = surface;
            initFinishIndicator = true;
        }

        /// <summary>
        /// Indicator of whether the instantiation is complete.
        /// </summary>
        private bool initFinishIndicator;

        /// <summary>
        /// Property for the indicator of the instantiation.
        /// </summary>
        public bool InitFinishIndicator
        {
            get { return initFinishIndicator; }
        }

        /// <summary>
        /// The guid.
        /// </summary>
        private Guid id;

        /// <summary>
        /// The <see cref="Guid"/> of the <see cref="DrawableSurface"/> component.
        /// </summary>
        public Guid ID { get { return id; } }

        /// <summary>
        /// Configuration file of the surface on start.
        /// The values can be wrong.
        /// It's only necessary for getting the depending game object.
        /// </summary>
        private GameObject surface;

        /// <summary>
        /// The current depending object of the <see cref="DrawableConfig"/>.
        /// </summary>
        public GameObject CurrentObject { get => surface; }

        /// <summary>
        /// Attribute for the description.
        /// </summary>
        private string description;

        /// <summary>
        /// Description property.
        /// </summary>
        public string Description
        {
            get => description;
            set
            {
                description = value;
                Notify(new DescriptionChangeEvent(id, this));
            }
        }

        /// <summary>
        /// Attribute for the color.
        /// </summary>
        private Color color;

        /// <summary>
        /// Color property.
        /// </summary>
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                Notify(new ColorChangeEvent(id, this));
            }
        }

        /// <summary>
        /// Attribute for the lighting.
        /// </summary>
        private bool lighting;

        /// <summary>
        /// Lighting property.
        /// </summary>
        public bool Lighting
        {
            get => lighting;
            set
            {
                lighting = value;
                Notify(new LightingChangeEvent(id, this));
            }
        }

        /// <summary>
        /// Attribute for the visibility.
        /// </summary>
        private bool visibility;

        /// <summary>
        /// Visibility property.
        /// </summary>
        public bool Visibility
        {
            get => visibility;
            set
            {
                visibility = value;
                Notify(new VisibilityChangeEvent(id, this));
            }
        }

        /// <summary>
        /// Attribute for the current selected page.
        /// </summary>
        private int currentPage;

        /// <summary>
        /// Current selected page property.
        /// </summary>
        public int CurrentPage
        {
            get => currentPage;
            set
            {
                currentPage = value;
                Notify(new PageChangeEvent(id, this));
            }
        }
    }
}
