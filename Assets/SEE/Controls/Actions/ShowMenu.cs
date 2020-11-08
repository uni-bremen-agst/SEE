using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using System;
using TMPro;
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
            if (menu == null)
            {
                menu = CreateMenu(entries, Radius, Depth);
                Off();
            }
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
        /// Creates the circular menu.
        /// </summary>
        /// <param name="radius">the radius the circular menu should have</param>
        /// <returns>a new circular menu with given <paramref name="radius"/></returns>
        private static GameObject CreateMenu(int entries, float radius, float depth)
        {
            GameObject menu = NewCircle(radius, depth, Color.white);
            menu.transform.position = Vector3.zero; // the real position will be set when it becomes visible
            menu.name = MouseMenuName;

            AddInnerCircles(menu, radius, depth, entries);
            return menu;
        }

        private static void AddInnerCircles(GameObject menu, float radius, float depth, int entries)
        {
            if (entries < 1 || entries > circles.Length)
            {
                throw new Exception("Unsupported number of inner circles: " + entries);
            } 
            else if (entries > 1)
            {
                
                InnerCircles selectedInnerCircles = circles[entries - 1];
                float relativeInnerRadius = selectedInnerCircles.radius;
                float absoluteInnerRadius = relativeInnerRadius * radius;
                int menuEntryIndex = 1;
                foreach (Vector2 center in selectedInnerCircles.centers)
                {
                    Color color = Color.Lerp(Color.black, Color.grey, (float)(menuEntryIndex-1)/(float)entries);
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
                    {
                        // Adds the text label to the menu entry
                        string menuEntryLabel = "label" + inner.name;
                        GameObject label = TextFactory.GetTextWithWidth
                                                 (text: menuEntryLabel, position: Vector3.zero, 
                                                  width: 1.8f * absoluteInnerRadius, textColor: Color.white, lift: false);
                        Portal.SetInfinitePortal(label);
                        label.transform.SetParent(inner.transform);
                        // Text will be centered within the inner circle.
                        {
                            RectTransform rect = label.GetComponent<RectTransform>();
                            rect.localPosition = Vector3.zero;
                        }
                    }
                    menuEntryIndex++;
                }
            }
        }

        private static GameObject NewCircle(float radius, float depth, Color color)
        {
            //GameObject menu = GameObject.CreatePrimitive(PrimitiveType.Sphere);

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
                    Debug.LogFormat("Hit menu entry: {0}\n", hitEntry);
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
                    GameObject hitObject = hit2D.collider.gameObject;
                    if (hitObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer renderer))
                    {
                        renderer.color = InvertColor(renderer.color);
                    }
                }
            }
            return entry != -1;
        }

        struct InnerCircles
        {
            public float radius;   
            public Vector2[] centers;
            public InnerCircles(float radius, Vector2[] centers)
            {
                this.radius = radius;
                this.centers = centers;
            }
        }

        public int entries = 3;

        /// <summary>
        /// A mapping of the number of equally sized inner circles to be enclosed
        /// in an outer circle with radius 1 onto the radius and co-ordinates of
        /// those inner circles.
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