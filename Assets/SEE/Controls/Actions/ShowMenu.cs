using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides an in-game menu.
    /// 
    /// NOTE: This class is currently under construction and not yet ready.
    /// </summary>
    public class ShowMenu : MonoBehaviour
    {
        /// <summary>
        /// The name we use for the game object representing the menu.
        /// </summary>
        private const string MouseMenuName = "Menu";

        /// <summary>
        /// The distance in Unity units between the outer circle representing
        /// the menu and the nested inner circles representing the menu entries.
        /// </summary>
        private const float distanceBetweenOuterAndInnerCircles = 0.1f;

        /// <summary>
        /// The game object representing the menu.
        /// </summary>
        private GameObject menu;

        /// <summary>
        /// The main camera the menu should be facting to.
        /// </summary>
        private static Camera mainCamera;

        /// <summary>
        /// Radius of the menu.
        /// </summary>
        [Tooltip("The radius of the circular menu.")]
        [Range(0, 2)]
        public float Radius = 0.3f;

        /// <summary>
        /// Radius of the menu.
        /// </summary>
        [Tooltip("The depth of the circular menu (z axis).")]
        [Range(0, 0.1f)]
        public float Depth = 0.01f;

        /// <summary>
        /// The distance between the menu and the camera when the menu is spawned.
        /// </summary>
        [Tooltip("The distance between the menu and the camera when the menu is spawned.")]
        [Range(0, 10)]
        public float CameraDistance = 1.0f;

        /// <summary>
        /// If true (and only if), the menu is visible.
        /// </summary>
        private bool menuIsOn = false;

        private MenuDescriptor[] Entries;

        /// <summary>
        /// The time in seconds between the time in point when any transient
        /// menu entry was activated until all transient menu entries should 
        /// be deactivated again.
        /// </summary>
        private const float activationPeriod = 1.0f;

        /// <summary>
        /// IsActive[i] <=> the menu entry i is activated. Transient menu entries
        /// are activated only for 
        /// </summary>
        private bool[] IsActive;

        /// <summary>
        /// Creates the <see cref="menu"/> if it does not exist yet.
        /// Sets <see cref="mainCamera"/>.
        /// </summary>
        protected virtual void Start()
        {            
            if (mainCamera == null)
            {
                if (Camera.allCameras.Length > 1)
                {
                    Debug.LogFormat("There are {0} cameras in the scene. Expect unexpected visual results.\n", 
                                    Camera.allCameras.Length);
                    foreach (Camera c in Camera.allCameras)
                    {
                        Debug.LogFormat("Camera: {0}\n", c.name);
                    }
                }
                mainCamera = Camera.main;
            }
            Entries = EntriesParameter;
            // For bool, the default value is false, which is what we want:
            // none of the menu entries is activated initially.
            IsActive = new Boolean[Entries.Length]; 
            menu = CreateMenu(Entries, Radius, Depth);
            Off();
        }

        /// <summary>
        /// Shows the menu.
        /// </summary>
        protected virtual void On()
        {
            menu.transform.position = MenuCenterPosition();
            SetVisible(menu, true);
        }

        /// <summary>
        /// Enables/disables the renderers of <paramref name="gameObject"/> and all its
        /// descendants so that they become visible/invisible.
        /// </summary>
        /// <param name="gameObject">objects whose renderer (and those of its children) is to be enabled/disabled</param>
        /// <param name="isVisible">iff true, the renderers will be enabled</param>
        private static void SetVisible(GameObject gameObject, bool isVisible)
        {
            gameObject.GetComponent<Renderer>().enabled = isVisible;
            foreach (Transform child in gameObject.transform)
            {
                SetVisible(child.gameObject, isVisible);
            }
        }

        /// <summary>
        /// Hides the menu.
        /// </summary>
        protected virtual void Off()
        {
            SetVisible(menu, false);
        }

        /// <summary>
        /// The color of the menu itself, that is, the outer circle serving as a canvas
        /// for the menu entries.
        /// </summary>
        private static readonly Color colorOfMenu = Color.white;
        /// <summary>
        /// The inner circles for the menu entries have different colors within
        /// a color range depending upon their index. The first color gets this
        /// color.
        /// </summary>
        private static readonly Color MenuEntryColorStart = Color.black;
        /// <summary>
        /// This is the color for the last menu entry.
        /// </summary>
        private static readonly Color MenuEntryColorEnd = Color.grey;
        /// <summary>
        /// The color of the text within the inner circles, that is, the label of all
        /// menu entries.
        /// </summary>
        private static readonly Color menuEntryTextColor = Color.white;

        public delegate void EntryEvent();

        public struct MenuDescriptor
        {
            public readonly string Label;
            public readonly EntryEvent EntryOn;
            public readonly EntryEvent EntryOff;
            public readonly bool IsTransient;

            public MenuDescriptor(string label, EntryEvent entryOn, EntryEvent entryOff, bool isTransient)
            {
                Label = label;
                EntryOn = entryOn;
                EntryOff = entryOff;
                IsTransient = isTransient;
            }
        }

        private static void EntryAOn()
        {
            Debug.Log("EntryAOn\n");
        }

        private static void EntryAOff()
        {
            Debug.LogError("EntryAOff must never be called: is transient.\n");
        }

        private static void EntryBOn()
        {
            Debug.Log("EntryBOn\n");
        }

        private static void EntryBOff()
        {
            Debug.Log("EntryBOff\n");
        }

        private static void EntryCOn()
        {
            Debug.Log("EntryCOn\n");
        }

        private static void EntryCOff()
        {
            Debug.LogError("EntryCOff must never be called: is transient.\n");
        }

        private static readonly MenuDescriptor[] EntriesParameter =
            {
                new MenuDescriptor(label: "A", entryOn: EntryAOn, entryOff: EntryAOff, true),
                new MenuDescriptor(label: "B", entryOn: EntryBOn, entryOff: EntryBOff, false),
                new MenuDescriptor(label: "C", entryOn: EntryCOn, entryOff: EntryCOff, true),
            };

        /// <summary>
        /// Creates the circular menu.
        /// </summary>
        /// <param name="entries">the labels of the menu entries</param>
        /// <param name="radius">the radius the circular menu should have</param>
        /// <param name="depth">the depth of the menu and the menu entries (z length)</param>
        /// <returns>a new circular menu with given <paramref name="radius"/></returns>
        private static GameObject CreateMenu(MenuDescriptor[] entries, float radius, float depth)
        {
            int numberOfEntries = entries.Length;
            if (numberOfEntries < 1 || numberOfEntries > circles.Length)
            {
                throw new Exception("Unsupported number of inner circles: " + numberOfEntries);
            }
            else
            {
                GameObject menu = NewCircle(radius, depth, colorOfMenu);
                menu.transform.position = Vector3.zero; // the real position will be set when it becomes visible
               
                if (numberOfEntries > 1)
                {
                    menu.name = MouseMenuName;
                    AddInnerCircles(menu, radius, depth, entries);
                }
                else
                {
                    int index = 1;
                    menu.name = index.ToString();
                    AddEntryLabel(menu, entries[0].Label, radius);
                }
                return menu;
            }
        }

        /// <summary>
        /// Creates the menu entries as inner circles nested within given <paramref name="menu"/>.
        /// </summary>
        /// <param name="menu">the menu these menu entries belong to</param>
        /// <param name="radius">the radius of the menu</param>
        /// <param name="depth">the depth of the menu entries (z length); must be greater than 0</param>
        /// <param name="entries">the labels of the menu entries</param>
        private static void AddInnerCircles(GameObject menu, float radius, float depth, MenuDescriptor[] entries)
        {
            int numberOfEntries = entries.Length;
            InnerCircles selectedInnerCircles = circles[numberOfEntries - 1];
            float relativeInnerRadius = selectedInnerCircles.radius;
            float absoluteInnerRadius = relativeInnerRadius * radius;
            int menuEntryIndex = 1;
            foreach (Vector2 center in selectedInnerCircles.centers)
            {
                Color color = Color.Lerp(MenuEntryColorStart, MenuEntryColorEnd, (float)(menuEntryIndex - 1) / (float)numberOfEntries);
                // create the sprite circle
                GameObject inner = NewCircle(absoluteInnerRadius, depth, color);
                // the name of the game object for the menu entry holds the entry's index
                inner.name = menuEntryIndex.ToString();
                {
                    // Set the position of the inner circle
                    Vector3 position = menu.transform.position;
                    position.x += center.x * radius;
                    position.y += center.y * radius;
                    position.z += distanceBetweenOuterAndInnerCircles; // strangely enough, the z axis is inversed for sprites
                    inner.transform.position = position;
                }
                inner.transform.SetParent(menu.transform);
                AddEntryLabel(inner, entries[menuEntryIndex - 1].Label, absoluteInnerRadius);
                menuEntryIndex++;
            }
        }

        /// <summary>
        ///  Adds <paramref name="menuEntryLabel"/> to the <paramref name="gameObject"/> which is assumed by
        ///  circle with given <paramref name="radius"/>.  The ext will be centered within the circle.
        /// </summary>
        /// <param name="gameObject">the game object where to add the text as a child</param>
        /// <param name="menuEntryLabel">the text to be added</param>
        /// <param name="radius">the radius of <paramref name="gameObject"/></param>
        private static void AddEntryLabel(GameObject gameObject, string menuEntryLabel, float radius)
        {                                  
            GameObject label = TextFactory.GetTextWithWidth
                                     (text: menuEntryLabel, position: Vector3.zero,
                                      width: 1.8f * radius, textColor: menuEntryTextColor, lift: false);
            Portal.SetInfinitePortal(label);
            label.transform.SetParent(gameObject.transform);
            // Text will be centered within the inner circle.
            {
                RectTransform rect = label.GetComponent<RectTransform>();
                rect.localPosition = Vector3.zero;
            }
        }

        private static GameObject NewCircle(float radius, float depth, Color color)
        {
            UnityEngine.Object prefab = IconFactory.LoadSprite("Icons/Circle");
            if (prefab == null)
            {
                throw new Exception("Cannot create menu.\n");
            }
            else
            {
                GameObject menu = UnityEngine.Object.Instantiate(prefab) as GameObject;
                menu.tag = Tags.UI;
                // add the circle to the UI layer so that it does not collide with other game objects
                menu.layer = LayerMask.NameToLayer("UI");
                if (menu.TryGetComponent<SpriteRenderer>(out SpriteRenderer renderer))
                {
                    renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.color = color;
                }
                menu.transform.localScale = new Vector3(2 * radius, 2 * radius, depth);
                menu.AddComponent<CircleCollider2D>();
                return menu;
            }
        }

        /// <summary>
        /// Returns the center position in world space where the menu 
        /// should be located when it is spawned.
        /// </summary>
        /// <returns>center position of menu in world space</returns>
        private Vector3 MenuCenterPosition()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = Mathf.Max(mainCamera.nearClipPlane, CameraDistance);
                return mainCamera.ScreenToWorldPoint(mousePosition);
            }
            else
            {
                // FIXME
                throw new NotImplementedException("ShowMenu.MenuCenterPosition() implemented only for desktop environment.");
            }
        }

        private void Update()
        {
            bool oldState = menuIsOn;
            // space bar toggles menu            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                menuIsOn = !menuIsOn;
            }
            if (menuIsOn)
            {
                if (oldState != menuIsOn)
                {
                    On();
                }
                // Menu should be facing the camera
                menu.transform.LookAt(mainCamera.transform);
                if (SelectedMenuEntry(out int hitEntry))
                {
                    // hitEntry == 0 => the menu itself was selected                    
                    Debug.LogFormat("Hit menu entry: {0}\n", hitEntry);
                    if (hitEntry > 0)
                    {
                        // the index of menu entries starts at 1, while the arrays start at 0
                        hitEntry--;
                        MenuDescriptor entry = Entries[hitEntry];
                        IsActive[hitEntry] = !IsActive[hitEntry];
                        if (entry.IsTransient || IsActive[hitEntry])
                        {
                            entry.EntryOn();
                        }
                        else
                        {
                            entry.EntryOff();
                        }
                        ShowActivation();
                    }
                }
            }
            else
            {
                if (oldState != menuIsOn)
                {
                    Off();
                }
            }
        }

        private static readonly Color ActivationColor = Color.blue;

        private void ShowActivation()
        {
            int i = 0;
            foreach(Transform child in menu.transform)
            {
                if (IsActive[i])
                {
                    SetColor(child.gameObject, ActivationColor);
                }                
                i++;
            }
        }

        private static void SetColor(GameObject hitObject, Color color)
        {
            if (hitObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer renderer))
            {
                renderer.color = color;
            }
        }

        /// <summary>
        /// Returns the inverted color of <paramref name="color"/>.
        /// </summary>
        /// <param name="color">color to be inverted</param>
        /// <returns>inverted color</returns>
        private static Color InvertColor(Color color) 
        {
            return new Color(1.0f-color.r, 1.0f-color.g, 1.0f-color.b);
        }

        private bool SelectedMenuEntry(out int entry)
        {
            entry = -1;
            if (Input.GetMouseButtonUp(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
                if (hit2D.collider != null && hit2D.collider.CompareTag(Tags.UI))
                {
                    if (hit2D.collider.name.Equals(MouseMenuName))
                    {
                        entry = 0;
                    }
                    else
                    {
                        entry = int.Parse(hit2D.collider.name);
                    }
                    //GameObject hitObject = hit2D.collider.gameObject;
                    //SetColor(hitObject);
                }
            }
            return entry != -1;
        }

        /// <summary>
        /// Representation of inner circles nested within an outer circle with 
        /// radius 1. The inner circles have all the same radius. They do not
        /// overlap and are as close together as possible. That is, the area
        /// covered by the outer circle minus the sum of all areas of all inner
        /// circles is minimal.
        /// </summary>
        struct InnerCircles
        {
            /// <summary>
            /// The radius of the inner circle.
            /// </summary>
            public float radius;   
            /// <summary>
            /// The co-ordinates of the centers of the inner circles.
            /// </summary>
            public Vector2[] centers;
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="radius">the radius of the inner circle</param>
            /// <param name="centers">the co-ordinates of the centers of the inner circles</param>
            public InnerCircles(float radius, Vector2[] centers)
            {
                this.radius = radius;
                this.centers = centers;
            }
        }

        /// <summary>
        /// A mapping of the number of equally sized inner circles to be enclosed
        /// in an outer circle with radius 1 onto the radius and co-ordinates of
        /// those inner circles.
        /// 
        /// The radius of the inner circles and their co-ordinates set so that
        /// they do not overlap and the area covered by the outer circle minus 
        /// the sum of all areas of all inner circles is minimal.
        /// 
        /// The data were retrieved from http://hydra.nat.uni-magdeburg.de/packing/cci/cci.html.
        /// </summary>
        private static readonly InnerCircles[] circles =
        {
            /* 1 */ new InnerCircles(1.0f, new Vector2[]{ Vector2.zero}),
            /* 2 */ new InnerCircles(0.5f, 
                                     new Vector2[]{ 
                                         new Vector2(0.5f, 0), 
                                         new Vector2(-0.5f, 0)}),
            /* 3 */ new InnerCircles(0.46410161518f, 
                                     new Vector2[]{
                                         new Vector2( 0.000000000000000000000000000000f,  0.535898384862245412945107316988f),
                                         new Vector2(-0.464101615137754587054892683011f, -0.267949192431122706472553658494f), 
                                         new Vector2( 0.464101615137754587054892683011f, -0.267949192431122706472553658494f), 
                                         }),
            /* 4 */ new InnerCircles(0.4142135623f,
                                     new Vector2[]{
                                         new Vector2(0.414213562373095048801688724210f, 0.414213562373095048801688724210f),
                                         new Vector2(-0.414213562373095048801688724210f, 0.414213562373095048801688724210f),                                         
                                         new Vector2(-0.414213562373095048801688724210f, -0.414213562373095048801688724210f),
                                         new Vector2(0.414213562373095048801688724210f, -0.414213562373095048801688724210f),
                                     }),

            /* 5 */ new InnerCircles(0.3701919081f,
                                     new Vector2[]{
                                         new Vector2(0.000000000000000000000000000000f, 0.629808091841249862297762358942f),
                                         new Vector2(-0.598983089761037227177173011864f, 0.194621403573803879364825731779f),
                                         new Vector2(-0.370191908158750137702237641058f, -0.509525449494428810513706911251f),
                                         new Vector2(0.370191908158750137702237641058f, -0.509525449494428810513706911251f),                                         
                                         new Vector2(0.598983089761037227177173011864f, 0.194621403573803879364825731779f),                                         
                                     }),

            /* 6 */ new InnerCircles(0.3333333333f,
                                     new Vector2[]{
                                         new Vector2( 0.333333333333333333333333333333f, 0.577350269189625764509148780502f),
                                         new Vector2(-0.333333333333333333333333333333f, 0.577350269189625764509148780502f),
                                         new Vector2(-0.666666666666666666666666666667f, 0.000000000000000000000000000000f),
                                         new Vector2(-0.333333333333333333333333333333f, -0.577350269189625764509148780502f),
                                         new Vector2( 0.333333333333333333333333333333f, -0.577350269189625764509148780502f),                                         
                                         new Vector2( 0.666666666666666666666666666667f, 0.000000000000000000000000000000f),
                                     }),


            /* 7 */ new InnerCircles(0.3333333333f,
                                     new Vector2[]{
                                         new Vector2( 0.333333333333333333333333333333f, 0.577350269189625764509148780502f),
                                         new Vector2(-0.333333333333333333333333333333f, 0.577350269189625764509148780502f),
                                         new Vector2(-0.666666666666666666666666666667f, 0.000000000000000000000000000000f),
                                         new Vector2(-0.333333333333333333333333333333f, -0.577350269189625764509148780502f),
                                         new Vector2( 0.333333333333333333333333333333f, -0.577350269189625764509148780502f),
                                         new Vector2( 0.666666666666666666666666666667f, 0.000000000000000000000000000000f),
                                         new Vector2( 0.000000000000000000000000000000f,  0.000000000000000000000000000000f),
                                     }),

            /* 8 */ new InnerCircles(0.30259338834f,
                                     new Vector2[]{
                                         new Vector2( 0.000000000000000000000000000000f,  0.697406611651388697090795775067f),
                                         new Vector2(-0.545254445070410775447749861103f,  0.434825910113495061957667559237f),
                                         new Vector2(-0.679921171839088240043878874469f, -0.155187570571975671990838057814f),
                                         new Vector2(-0.302593388348611302909204224933f, -0.628341645367213738512227388956f),
                                         new Vector2( 0.302593388348611302909204224933f, -0.628341645367213738512227388956f),                                         
                                         new Vector2( 0.679921171839088240043878874469f, -0.155187570571975671990838057814f),                                         
                                         new Vector2( 0.545254445070410775447749861103f,  0.434825910113495061957667559237f),
                                         new Vector2( 0.000000000000000000000000000000f,  0.000000000000000000000000000000f)}),

            /* 9 */ new InnerCircles( 0.2767686539f,
                                     new Vector2[]{
                                         new Vector2( 0.276768653914155215717770973808f,  0.668178637919298919997757686523f),
                                         new Vector2(-0.276768653914155215717770973808f,  0.668178637919298919997757686523f),
                                         new Vector2(-0.668178637919298919997757686523f,  0.276768653914155215717770973808f),
                                         new Vector2(-0.668178637919298919997757686523f, -0.276768653914155215717770973808f),                                                                                  
                                         new Vector2(-0.276768653914155215717770973808f, -0.668178637919298919997757686523f),                                         
                                         new Vector2( 0.276768653914155215717770973808f, -0.668178637919298919997757686523f),
                                         new Vector2( 0.668178637919298919997757686523f, -0.276768653914155215717770973808f),
                                         new Vector2( 0.668178637919298919997757686523f,  0.276768653914155215717770973808f),                                         
                                         new Vector2( 0.000000000000000000000000000000f,  0.000000000000000000000000000000f)}),

            /* 10 */ new InnerCircles( 0.26225892419f,
                                     new Vector2[]{
                                         new Vector2( 0.415055617900124834285924684739f,  0.609910427019080420778753583334f),
                                         new Vector2(-0.415055617900124834285924684739f,  0.609910427019080420778753583334f),
                                         new Vector2(-0.715460686241806569843043724712f,  0.179938604472344027862805139398f),
                                         new Vector2(-0.654207495490543857031888931623f, -0.340990392505480262378855842125f),
                                         new Vector2(-0.262258924190165855095630653709f, -0.689552138434555425611558523406f),
                                         new Vector2( 0.262258924190165855095630653709f, -0.689552138434555425611558523406f),
                                         new Vector2( 0.654207495490543857031888931623f, -0.340990392505480262378855842125f),
                                         new Vector2( 0.715460686241806569843043724712f,  0.179938604472344027862805139398f),                                        
                                         new Vector2( 0.000000000000000000000000000000f,  0.289211491381498022358656198900f),
                                         new Vector2( 0.000000000000000000000000000000f, -0.235306356998833687832605108517f),
                                     }),

            /* 11 */ new InnerCircles( 0.2548547017f,
                                     new Vector2[]{
                                         new Vector2(-0.478970165152397209191173115760f,  0.570814415065811443520636508336f),
                                         new Vector2(-0.733824866869546118800008853460f,  0.129393123143898335937886211109f),
                                         new Vector2(-0.645314757823482092972348022140f, -0.372572649141425545195582131150f),
                                         new Vector2(-0.254854701717148909608835737700f, -0.700207538209709779458522719445f),
                                         new Vector2( 0.254854701717148909608835737700f, -0.700207538209709779458522719445f),                                         
                                         new Vector2( 0.645314757823482092972348022140f, -0.372572649141425545195582131150f),
                                         new Vector2( 0.733824866869546118800008853460f,  0.129393123143898335937886211109f),
                                         new Vector2( 0.478970165152397209191173115760f,  0.570814415065811443520636508336f),
                                         new Vector2( 0.000000000000000000000000000000f,  0.396483531848771796650108754372f),
                                         new Vector2(-0.254854701717148909608835737700f, -0.044937760073141310932641542855f),
                                         new Vector2( 0.254854701717148909608835737700f, -0.044937760073141310932641542855f),  
                                     }),

            /* 12 */ new InnerCircles( 0.24816347057f,
                                     new Vector2[]{
                                         new Vector2( 0.274977276528483469583692765135f,  0.699746857353277958904884966346f),
                                         new Vector2(-0.274977276528483469583692765135f,  0.699746857353277958904884966346f),
                                         new Vector2(-0.651109533978045892394293176753f,  0.375918264714156579227972756434f),
                                         new Vector2(-0.743487192950506270382840218584f, -0.111736121739513839036609043190f),
                                         new Vector2(-0.468509916422022800799147453449f, -0.588010735613764119868275923156f),
                                         new Vector2( 0.000000000000000000000000000000f, -0.751836529428313158455945512868f),
                                         new Vector2( 0.468509916422022800799147453449f, -0.588010735613764119868275923156f),
                                         new Vector2( 0.743487192950506270382840218584f, -0.111736121739513839036609043190f),
                                         new Vector2( 0.651109533978045892394293176753f,  0.375918264714156579227972756434f),
                                         new Vector2( 0.000000000000000000000000000000f,  0.286554493075190339159239992000f),
                                         new Vector2(-0.248163470571686841544054487132f, -0.143277246537595169579619996000f),
                                         new Vector2( 0.248163470571686841544054487132f, -0.143277246537595169579619996000f),                                                                  
                                     }),

        };

    }
}