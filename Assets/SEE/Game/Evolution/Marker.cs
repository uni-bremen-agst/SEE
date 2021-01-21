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
        /// <param name="markerWidth">the width (x and z lengths) of the markers</param>
        /// <param name="markerHeight">the height (y length) of the markers</param>
        public Marker(GraphRenderer graphRenderer, float markerWidth, float markerHeight)
        {
            this.graphRenderer = graphRenderer;
            ColorRange deadColorRange = new ColorRange(Color.black, Color.black, 1);
            ColorRange bornColorRange = new ColorRange(Color.green, Color.green, 1);
            bornPostFactory = new CubeFactory(graphRenderer.ShaderType, bornColorRange);
            deadPostFactory = new CubeFactory(graphRenderer.ShaderType, deadColorRange);
            if (markerHeight < 0)
            {
                this.markerHeight = 0;
                Debug.LogError("SEE.Game.Evolution.Marker received a negative marker height.\n");
            }
            else
            {
                this.markerHeight = markerHeight;
            }
            if (markerWidth < 0)
            {
                this.markerWidth = 0;
                Debug.LogError("SEE.Game.Evolution.Marker received a negative marker width.\n");
            }
            else
            {
                this.markerWidth = markerWidth;
            }
        }

        /// <summary>
        /// The height of the posts used to mark new and deleted objects from one version to the next one.
        /// </summary>
        private readonly float markerHeight;

        /// <summary>
        /// The width (x and z lengths) of the posts used to mark new and deleted objects from one version 
        /// to the next one.
        /// </summary>
        private readonly float markerWidth;

        /// <summary>
        /// The renderer used to retrieve the roof position of the game objects to be marked.
        /// </summary>
        private readonly GraphRenderer graphRenderer;

        /// <summary>
        /// The list of posts added for the new game objects since the last call to Clear().
        /// </summary>
        private readonly List<GameObject> posts = new List<GameObject>();

        /// <summary>
        /// The factory to create posts above existing blocks ceasing to exist.
        /// </summary>
        private readonly CubeFactory deadPostFactory;

        /// <summary>
        /// The factory to create posts above new blocks coming into existence.
        /// </summary>
        private readonly CubeFactory bornPostFactory;

        /// <summary>
        /// Marks the given <paramref name="block"/> as dying/getting alive by putting a post on top
        /// of its roof. 
        /// </summary>
        /// <param name="block"></param>
        /// <param name="postFactory">the factory to create the post</param>
        /// <returns>the resulting post</returns>
        private GameObject MarkByPost(GameObject block, CubeFactory postFactory)
        {
            // The marker should be drawn in front of the block, hence, its render
            // queue offset must be greater than the one of the block.
            GameObject post = postFactory.NewBlock(0, block.GetRenderQueue() + 1);

            // FIXME: These kinds of posts make sense only for leaf nodes.
            // Could we better use some kind of blinking now that the cities
            // are drawn in miniature?

            post.tag = Tags.Decoration;
            Vector3 postScale;           
            Vector3 position = graphRenderer.GetRoof(block);

            postScale.x = markerWidth;
            postScale.z = markerWidth;
            postScale.y = markerHeight;

            position.y += postScale.y / 2.0f;
            post.transform.position = position;
            post.transform.localScale = postScale;

            // Makes post a child of block so that it moves along with it during the animation.
            // In addition, it will also be destroyed along with its parent block.
            post.transform.SetParent(block.transform, true);

            // Render new node power beam
            Vector3 powerBeamDimensions = new Vector3(position.x, block.transform.position.y, position.z);
            MoveScaleShakeAnimator.BeamAnimator.GetInstance().CreatePowerBeam(powerBeamDimensions,
                AdditionalBeamDetails.newBeamColor, AdditionalBeamDetails.powerBeamDimensions, "new", block.name);

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
            GameObject post = MarkByPost(block, deadPostFactory);
            post.name = "dead " + block.name;
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
            GameObject post = MarkByPost(block, bornPostFactory);
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