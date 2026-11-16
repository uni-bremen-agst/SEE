
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SEE.Controls;
using SEE.Controls.Interactables;
using SEE.Game;
using SEE.Game.City;
using SEE.GraphElementRefs;
using SEE.Graphs;
using SEE.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.Gizmos
{
    /// <summary>
    /// PlayMode regression tests for the hover lifecycle of <see cref="CityCursor"/>.
    /// </summary>
    internal class TestCityCursor : TestUI
    {
        /// <summary>
        /// Game objects created by the tests and removed during teardown.
        /// </summary>
        private readonly List<GameObject> createdObjects = new();

        /// <summary>
        /// Releases hover states and destroys objects created by a test.
        /// </summary>
        /// <returns>An enumerator that waits for object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDownCityCursor()
        {
            foreach (GameObject gameObject in createdObjects)
            {
                if (gameObject != null
                    && gameObject.TryGetComponent(out InteractableGraphElement interactable)
                    && interactable.IsHovered)
                {
                    interactable.SetHoverFlags(0, false);
                }
            }

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.Destroy(createdObjects[i]);
                }
            }

            createdObjects.Clear();
            yield return null;
        }

        /// <summary>
        /// Verifies that removing a hovered node clears the cursor and tracking
        /// state without reporting an invalid graph as an error.
        /// Also verifies that the removed node is ignored by subsequent hover events.
        /// </summary>
        /// <returns>An enumerator waiting for initialization and pending hover checks.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator RemovedNodeCleansHoverState()
        {
            EnsureCamera();
            SEECity city = CreateCity("City A");
            yield return null;

            Graph graph = new("TestGraphA");
            AssignGraph(city, graph);

            Node node = new() { ID = "TestNodeA" };
            graph.AddNode(node);

            InteractableGraphElement interactable = CreateNode(city, node);
            CityCursor cursor = city.GetComponent<CityCursor>();

            interactable.SetHoverFlags((uint)HoverFlag.World, false);

            Assert.That(cursor.Cursor.GetFocusses(),
                        Does.Contain(interactable));

            graph.RemoveNode(node);
            Assert.That(node.ItsGraph, Is.Null);

            interactable.SetHoverFlags(0, false);

            Assert.That(cursor.Cursor.GetFocusses(), Is.Empty);
            AssertNoTracking(cursor);

            // Hover events can still arrive before the Unity object is destroyed.
            interactable.SetHoverFlags((uint)HoverFlag.World, false);
            Assert.That(cursor.Cursor.GetFocusses(), Is.Empty);

            interactable.SetHoverFlags(0, false);

            // An earlier asynchronous threshold check must not restore state.
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(cursor.Cursor.GetFocusses(), Is.Empty);
            AssertNoTracking(cursor);
        }

        /// <summary>
        /// Verifies that removing a hovered edge clears the cursor and hover tracking
        /// state without reporting an invalid graph as an error.
        /// Also verifies that the removed edge is ignored by subsequent hover events.
        /// </summary>
        /// <returns>An enumerator waiting for initialization and pending hover checks.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator RemovedEdgeCleansHoverState()
        {
            EnsureCamera();
            SEECity city = CreateCity("City A");
            yield return null;

            Graph graph = new("TestGraphEdge");
            AssignGraph(city, graph);

            Node source = new() { ID = "Source" };
            Node target = new() { ID = "Target" };
            graph.AddNode(source);
            graph.AddNode(target);

            Edge edge = graph.AddEdge(source, target, "Dependency");

            GameObject gameObject = new(edge.ID)
            {
                tag = Tags.Edge
            };

            createdObjects.Add(gameObject);
            gameObject.transform.SetParent(city.transform, false);

            EdgeRef edgeRef = gameObject.AddComponent<EdgeRef>();
            edgeRef.Value = edge;

            InteractableGraphElement interactable =
                gameObject.AddComponent<InteractableGraphElement>();

            CityCursor cursor = city.GetComponent<CityCursor>();

            interactable.SetHoverFlags((uint)HoverFlag.World, false);

            Assert.That(cursor.Cursor.GetFocusses(),
                        Does.Contain(interactable));

            graph.RemoveEdge(edge);
            Assert.That(edge.ItsGraph, Is.Null);

            interactable.SetHoverFlags(0, false);

            Assert.That(cursor.Cursor.GetFocusses(), Is.Empty);
            AssertNoTracking(cursor);

            // Hover events can still arrive after the edge has been removed.
            interactable.SetHoverFlags((uint)HoverFlag.World, false);
            Assert.That(cursor.Cursor.GetFocusses(), Is.Empty);

            interactable.SetHoverFlags(0, false);

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.That(cursor.Cursor.GetFocusses(), Is.Empty);
            AssertNoTracking(cursor);
        }

        /// <summary>
        /// Verifies that cleaning a removed node in one city does not affect
        /// the hover focus of another city.
        /// </summary>
        /// <returns>An enumerator waiting for city initialization.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator RemovedNodeDoesNotAffectOtherCity()
        {
            EnsureCamera();
            SEECity firstCity = CreateCity("City A");
            SEECity secondCity = CreateCity("City B");
            yield return null;

            Graph firstGraph = new("TestGraphA");
            Graph secondGraph = new("TestGraphB");

            AssignGraph(firstCity, firstGraph);
            AssignGraph(secondCity, secondGraph);

            Node firstNode = new() { ID = "TestNodeA" };
            Node secondNode = new() { ID = "TestNodeB" };

            firstGraph.AddNode(firstNode);
            secondGraph.AddNode(secondNode);

            InteractableGraphElement firstInteractable =
                CreateNode(firstCity, firstNode);
            InteractableGraphElement secondInteractable =
                CreateNode(secondCity, secondNode);

            CityCursor firstCursor = firstCity.GetComponent<CityCursor>();
            CityCursor secondCursor = secondCity.GetComponent<CityCursor>();

            firstInteractable.SetHoverFlags((uint)HoverFlag.World, false);
            secondInteractable.SetHoverFlags((uint)HoverFlag.World, false);

            Assert.That(firstCursor.Cursor.GetFocusses(),
                        Does.Contain(firstInteractable));
            Assert.That(secondCursor.Cursor.GetFocusses(),
                        Does.Contain(secondInteractable));

            firstGraph.RemoveNode(firstNode);
            firstInteractable.SetHoverFlags(0, false);

            Assert.That(firstCursor.Cursor.GetFocusses(), Is.Empty);
            Assert.That(secondCursor.Cursor.GetFocusses(),
                        Does.Contain(secondInteractable));

            AssertNoTracking(firstCursor);

            secondInteractable.SetHoverFlags(0, false);

            Assert.That(secondCursor.Cursor.GetFocusses(), Is.Empty);
            AssertNoTracking(secondCursor);
        }

        /// <summary>
        /// Creates a city with a cursor component.
        /// </summary>
        /// <param name="name">The name of the city.</param>
        /// <returns>The new city component.</returns>
        private SEECity CreateCity(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);

            SEECity city = gameObject.AddComponent<SEECity>();
            gameObject.AddComponent<CityCursor>();

            return city;
        }

        /// <summary>
        /// Assigns test graph data directly without invoking graph loading or
        /// rendering workflows.
        /// </summary>
        /// <param name="city">The city receiving the graph.</param>
        /// <param name="graph">The graph to assign.</param>
        private static void AssignGraph(SEECity city, Graph graph)
        {
            FieldInfo field = typeof(SEECity).GetField(
                "loadedGraph",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field.SetValue(city, graph);
        }

        /// <summary>
        /// Creates an interactable representation of an existing graph node.
        /// </summary>
        /// <param name="city">The city containing the node.</param>
        /// <param name="node">The represented graph node.</param>
        /// <returns>The node's interactable component.</returns>
        private InteractableGraphElement CreateNode(SEECity city, Node node)
        {
            GameObject gameObject = new(node.ID)
            {
                tag = Tags.Node
            };

            createdObjects.Add(gameObject);
            gameObject.transform.SetParent(city.transform, false);

            NodeRef nodeRef = gameObject.AddComponent<NodeRef>();
            nodeRef.Value = node;

            return gameObject.AddComponent<InteractableGraphElement>();
        }

        /// <summary>
        /// Ensures that the cursor has a camera when running in an empty test scene.
        /// </summary>
        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new("CityCursor Test Camera")
            {
                tag = Tags.MainCamera
            };

            cameraObject.AddComponent<Camera>();
            createdObjects.Add(cameraObject);
        }

        /// <summary>
        /// Verifies that neither hover tracking collection retains an entry.
        /// </summary>
        /// <param name="cursor">The cursor whose state is inspected.</param>
        private static void AssertNoTracking(CityCursor cursor)
        {
            FieldInfo startTimesField = typeof(CityCursor).GetField(
                "hoverStartTimes",
                BindingFlags.Instance | BindingFlags.NonPublic);

            FieldInfo thresholdField = typeof(CityCursor).GetField(
                "hoverThresholdReached",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(startTimesField, Is.Not.Null);
            Assert.That(thresholdField, Is.Not.Null);

            Dictionary<InteractableObject, float> startTimes =
                (Dictionary<InteractableObject, float>)
                startTimesField.GetValue(cursor);

            HashSet<InteractableObject> thresholdReached =
                (HashSet<InteractableObject>)
                thresholdField.GetValue(cursor);

            Assert.That(startTimes, Is.Empty);
            Assert.That(thresholdReached, Is.Empty);
        }
    }
}
