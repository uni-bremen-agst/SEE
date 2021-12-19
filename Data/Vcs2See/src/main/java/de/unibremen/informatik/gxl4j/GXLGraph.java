/*
 * @(#)GXLGraph.java	0.9 2003-12-20
 *
 * Copyright (C) 2003 Erik Larsson
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 *
 */

package net.sourceforge.gxl;

import org.w3c.dom.Element;

/** Represents the GXL <code>graph</code> element. 
 *	<p>
 *	The <code>graph</code> element represents a graph containing graph elements.
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLType</code> - see {@link GXLType}.<br>
 *	<code>GXLAttr</code> - Attributes describing this graph.<br>
 *	<code>GXLNode</code> - Nodes contained in this graph.<br>
 *	<code>GXLEdge</code> - Edges contained in this graph.<br>
 *	<code>GXLRel</code> - Relations contained in this graph.<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>id</code> - Required id of this <code>graph</code> element.<br>
 *	<code>role</code> - Optional string describing the function of this graph.<br>
 *	<code>edgeids</code> - Optional boolean, describes whether this graphs edges and relations contains ids.
 *	Due to ambiguos use by applications, this value is not enforced by this package. Defaults to <code>false</code>.<br>
 *	<code>hypergraph</code> - Optional boolean, describes whether this graph may contain <code>rel</code> elements.
 *	Defaults to <code>false</code>.<br>
 *	<code>edgemode</code> - Optional enumeration defining the direction of edges and relations contained in this graph.
 *	Possible values are <code>directed</code>, <code>undirected</code>, <code>defaultdirected</code> and
 *	<code>defaultundirected</code>. The defaultX values implies that the direction can be overridden by individual
 *	edges and relations using the <code>directed</code> attribute.<br>
 *
 *	@see GXLGraphElement
 */
public class GXLGraph extends GXLTypedElement {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLGraph</code> element with the specified id.
	 *	@param id The id of the new graph element.
	 */
	public GXLGraph(String id) {
		super(GXL.GRAPH);
		setAttribute(GXL.ID, id);
	}

	/** Creates a new GXLGraph element (used when reading a document). */
	GXLGraph(Element element) {
		super(GXL.GRAPH, element);
		createChildren(element);
	}

	//
	// CHILD TYPE ACCESSORS
	//

	/** Returns the number of <code>GXLGraphElement</code>s (nodes, edges and relations) contained in this graph.
	 *	@return The number of <code>GXLGraphElement</code>s (nodes, edges and relations) contained in this graph. 
	 *	@see #getChildCount()
	 */
	public int getGraphElementCount() {
		int size = getChildCount();
		int result = size;
		while (result > 0 && !(getChildAt(size-result) instanceof GXLGraphElement))
			result--;
		return result;
	}

	/** Returns the specified <code>GXLGraphElement</code>.
	 *	@param i The index of the specified <code>GXLGraphElement</code>.
	 *	@return The specified <code>GXLGraphElement</code>.
	 *	@see #getChildAt(int i)
	 */
	public GXLGraphElement getGraphElementAt(int i) {
		return (GXLGraphElement) getChildAt(getChildCount()-getGraphElementCount()+i);
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Returns the value of the <code>role</code> attribute or <code>null</code> if the attribute isn't set.
	 *	This simply calls <code>getAttribute(GXL.ROLE)</code>.
	 *	@return The value of the <code>role</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public String getRole() {
		return getAttribute(GXL.ROLE);
	}

	/** Sets the value of the <code>role</code> attribute. 
	 *	This simply calls <code>setAttribute(GXL.ROLE, role)</code>.
	 *	@param role The new value.
	 *	@see #setAttribute(String, String)
	 */
	public void setRole(String role) {
		setAttribute(GXL.ROLE, role);
	}

	/** Returns the value of the <code>edgeids</code> attribute.
	 *	This simply calls <code>GXL.TRUE.equals(getAttribute(GXL.EDGEIDS))</code>.
	 *	@return The value of the <code>edgeids</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public boolean getEdgeIDs() {
		return GXL.TRUE.equals(getAttribute(GXL.EDGEIDS));
	}

	/** Sets the value of the <code>edgeids</code> attribute. 
	 *	This simply calls <code>setAttribute(GXL.EDGEIDS, edgeids ? GXL.TRUE : GXL.FALSE)</code>.
	 *	@param edgeids The new value.
	 *	@see #setAttribute(String, String)
	 */
	public void setEdgeIDs(boolean edgeids) {
		setAttribute(GXL.EDGEIDS, edgeids ? GXL.TRUE : GXL.FALSE);
	}

	/** Returns the value of the <code>hypergraph</code> attribute.
	 *	This simply calls <code>GXL.TRUE.equals(getAttribute(GXL.HYPERGRAPH))</code>.
	 *	@return The value of the <code>hypergraph</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public boolean getAllowsHyperGraphs() {
		return GXL.TRUE.equals(getAttribute(GXL.HYPERGRAPH));
	}

	/** Sets the value of the <code>hypergraph</code> attribute. 
	 *	This simply calls <code>setAttribute(GXL.HYPERGRAPH, hypergraph ? GXL.TRUE : GXL.FALSE)</code>.
	 *	@param hypergraph The new value.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setAttribute(String, String)
	 */
	public void setAllowsHyperGraphs(boolean hypergraph) {
		setAttribute(GXL.HYPERGRAPH, hypergraph ? GXL.TRUE : GXL.FALSE);
	}

	/** Returns the value of the <code>edgemode</code> attribute.
	 *	This simply calls <code>getAttribute(GXL.EDGEMODE)</code>.
	 *	@return The value of the <code>edgemode</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public String getEdgeMode() {
		return getAttribute(GXL.EDGEMODE);
	}

	/** Sets the value of the <code>edgemode</code> attribute. 
	 *	This simply calls <code>setAttribute(GXL.EDGEMODE, edgemode)</code>.
	 *	@param edgemode The new value.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setAttribute(String, String)
	 */
	public void setEdgeMode(String edgemode) {
		setAttribute(GXL.EDGEMODE, edgemode);
	}
}
