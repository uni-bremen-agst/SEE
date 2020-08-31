using SEE.DataModel;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Evolution
{
    public class Marker
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graphRenderer">renderer used to retrieve the roof position of the game objects to be marked</param>
        public Marker(GraphRenderer graphRenderer)
        {
            this.graphRenderer = graphRenderer;
            black = new Materials(graphRenderer.Shader, new ColorRange(Color.black, Color.black, 1));
            green = new Materials(graphRenderer.Shader, new ColorRange(Color.green, Color.green, 1));
        }

        /// <summary>
        /// Black material used for the posts on top of nodes to be removed. Will be set
        /// at Start(). Cannot be initialized here because of Unity restrictions.
        /// </summary>
        private readonly Materials black;

        /// <summary>
        /// Green material used for the posts on top of new nodes to be added. Will be set
        /// at Start(). Cannot be initialized here because of Unity restrictions.
        /// </summary>
        private readonly Materials green;

        /// <summary>
        /// The scale of all posts to be put onto game nodes in order to mark them.
        /// </summary>
        private Vector3 postScale = new Vector3(3.0f, 100.0f, 3.0f);

        /// <summary>
        /// The renderer used to retrieve the roof position of the game objects to be marked.
        /// </summary>
        private readonly GraphRenderer graphRenderer;

        /// <summary>
        /// The list of posts added for the new game objects since the last call to Clear().
        /// </summary>
        private List<GameObject> posts = new List<GameObject>();

        /// <summary>
        /// Marks the given <paramref name="block"/> as dying by putting a post on top
        /// of its roof. 
        /// </summary>
        /// <param name="block"></param>
        /// <param name="material">the shared material for the post</param>
        /// <returns>the resulting post</returns>
        private GameObject MarkByPost(GameObject block, Materials material)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.tag = Tags.Decoration;
            Renderer renderer = post.GetComponent<Renderer>();
            // Object should not cast shadows: too expensive and may hide information,
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material.DefaultMaterial(0, 0);
            Vector3 position = graphRenderer.GetRoof(block);
            position.y += postScale.y / 2.0f;
            post.transform.position = position;
            post.transform.localScale = postScale;
            return post;
        }

        /// <summary>
        /// Marks the given <paramref name="block"/> as dying by putting a black post on top
        /// of its roof.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>the resulting post</returns>
        public GameObject MarkDead(GameObject block)
        {
            GameObject post = MarkByPost(block, black);
            post.name = "dead " + block.name;
            // Makes post a child of block so that it moves along with it during the animation.
            // In addition, it will also be destroyed along with its parent block.
            post.transform.SetParent(block.transform, true);
            return post;
        }

        /// <summary>
        /// Marks the given <paramref name="block"/> as coming into existence by putting a green post on top
        /// of its roof. Adds the created marking post to the cache.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>the resulting post</returns>
        public GameObject MarkBorn(GameObject block)
        {
            GameObject post = MarkByPost(block, green);
            post.name = "new " + block.name;
            // We need to add post to posts so that it can be destroyed at the beginning of the
            // next animation cycle.
            posts.Add(post);
            return post;
        }

        /// <summary>
        /// Destroys all marking created since the last call to Clear(). Clears the
        /// cache of markers.
        /// </summary>
        public void Clear()
        {
            foreach (GameObject gameObject in posts)
            {
                Object.Destroy(gameObject);
            }
            posts.Clear();
        }
    }
}