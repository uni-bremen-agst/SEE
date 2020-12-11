using SEE.Game;
using System;
using UnityEngine;

namespace SEE.GO.Menu
{
    public class MenuFactory
    {
        /// <summary>
        /// The name we use for the game object representing the menu.
        /// </summary>
        private const string MouseMenuName = "0";

        /// <summary>
        /// The color of the menu itself, that is, the outer circle serving as a canvas
        /// for the menu entries.
        /// </summary>
        private static readonly Color colorOfMenu = Color.white;

        /// <summary>
        /// The distance in Unity units between the outer circle representing
        /// the menu and the nested inner circles representing the menu entries.
        /// </summary>
        private const float distanceBetweenOuterAndInnerCircles = 0.05f;

        /// <summary>
        /// Path of the prefix for the sprite to be instantiated for the menu itself.
        /// </summary>
        private const string menuSprite = "Icons/Circle";

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

        /// <summary>
        /// Creates the circular menu. The returned game object represents the menu.
        /// Its name is "0", which will serve as an index. The menu entries will
        /// be children game objects of the menu. The first has name "1", the 
        /// second the name "2", and so on. Again, these names serve as an index.
        /// Every such child will have a <see cref="MenuEntry"/> component attached
        /// to it.
        /// 
        /// A <see cref="CircularMenu"/> component will be attached to the returned menu.
        /// 
        /// If <paramref name="entries"/> has exactly one element only, the returned
        /// game object will have exactly one child. There is not other peculiarity.
        /// 
        /// Precondition: <paramref name="entries"/> must have at least one element;
        /// otherwise an exception will be thrown.
        /// </summary>
        /// <param name="entries">the descriptors of the menu entries</param>
        /// <param name="radius">the radius the circular menu should have</param>
        /// <param name="depth">the depth of the menu and the menu entries (z length)</param>
        /// <returns>a new circular menu with given <paramref name="radius"/></returns>
        public static GameObject CreateMenu(MenuDescriptor[] entries, float radius, float depth)
        {
            int numberOfEntries = entries.Length;
            if (numberOfEntries < 1 || numberOfEntries > circles.Length)
            {
                throw new Exception("Unsupported number of inner circles: " + numberOfEntries);
            }
            else
            {
                GameObject menu = SpriteFactory.NewCircularSprite(menuSprite, radius, depth, colorOfMenu);
                menu.transform.position = Vector3.zero; // the real position will be set when it becomes visible

                menu.name = MouseMenuName;
                AddInnerCircles(menu, radius, depth, entries);
                menu.AddComponent<CircularMenu>();
                menu.SetVisibility(false);
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
                // create the sprite circle
                MenuDescriptor entry = entries[menuEntryIndex - 1];
                GameObject menuEntry = SpriteFactory.NewCircularSprite(entry.SpriteFile, absoluteInnerRadius, depth, entry.InactiveColor);
                // the name of the game object for the menu entry holds the entry's index
                menuEntry.name = menuEntryIndex.ToString();
                {
                    // Set the position of the inner circle
                    Vector3 position = menu.transform.position;
                    position.x += center.x * radius;
                    position.y += center.y * radius;
                    position.z += distanceBetweenOuterAndInnerCircles; // strangely enough, the z axis is inverted for sprites
                    menuEntry.transform.position = position;
                }
                {
                    // set up the MenuEntry component attached to the menuEntry.
                    MenuEntry menuEntryComponent = menuEntry.AddComponent<MenuEntry>();
                    menuEntryComponent.IsTransient = entry.IsTransient;
                    menuEntryComponent.ActiveColor = entry.ActiveColor;
                    menuEntryComponent.InactiveColor = entry.InactiveColor;
                    menuEntryComponent.EntryOn = entry.EntryOn;
                    menuEntryComponent.EntryOff = entry.EntryOff;
                    menuEntryComponent.Active = false;
                }
                menuEntry.transform.SetParent(menu.transform);
                AddEntryLabel(menuEntry, entry.Label, absoluteInnerRadius);
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
            if (!string.IsNullOrEmpty(menuEntryLabel))
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