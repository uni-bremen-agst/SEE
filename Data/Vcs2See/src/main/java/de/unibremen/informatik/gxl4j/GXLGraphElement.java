/*
 * @(#)GXLGraphElement.java	0.9 2003-11-16
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

import java.util.Vector;
import org.w3c.dom.Element;

/** Superclass for all <code>GXLElement</code>s that can be connected by <code>GXLLocalConnection</code>s. 
 *	Provides methods to access the edges connecting this graph element and sub-hierarchical graphs,
 *	since all <code>GXLGraphElement</code>s may contain graphs.
 *	<p>
 *	<b>Ordering</b><br>
 *	Edge tentacles incident to a graph element can be ordered using order attributes. For convenience
 *	the tentacles returned by this class is ordered the same way, with unordered tentacles last.
 *	For example, if three edges (edge1, edge2 and edge3) connect to this graph element, edge1 has order 3,
 *	edge2 contains no order attribute and edge3 contains order 1, this class will return them in the following
 *	order: edge3, edge1, edge2.<br>
 *	Unordered edges is returned in an arbitrary order.
 *
 *	@see GXLLocalConnection
 *	@see GXLEdge
 *	@see GXLRel
 *	@see GXLNode
 */
public abstract class GXLGraphElement extends GXLTypedElement {
	/** A list of connected tentacles from various local connections. */
	Vector connections;

	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLGraphElement. */
	GXLGraphElement(String elementName) {
		super(elementName);
		connections = new Vector();
	}

	/** Creates a new GXLGraphElement. */
	GXLGraphElement(String elementName, Element element) {
		super(elementName, element);
		connections = new Vector();
	}

	//
	// CHILD TYPE ACCESSORS
	//

	/** Returns the number of <code>GXLGraph</code>s in this element. 
	 *	@return The number of <code>GXLGraph</code>s in this element. 
	 *	@see #getChildCount()
	 */
	public int getGraphCount() {
		int start = 0;
		while (start < getChildCount() && !(getChildAt(start) instanceof GXLGraph))
			start++;
		int result = 0;
		while (start+result < getChildCount() && (getChildAt(start+result) instanceof GXLGraph))
			result++;
		return result;
	}

	/** Returns the specified <code>GXLGraph</code>.
	 *	@param i The index of the specified <code>GXLGraph</code>.
	 *	@return The specified <code>GXLGraph</code>.
	 *	@see #getChildAt(int i)
	 */
	public GXLGraph getGraphAt(int i) {
		int start = 0;
		while (start < getChildCount() && !(getChildAt(start) instanceof GXLGraph))
			start++;
		return (GXLGraph) getChildAt(start+i);
	}

	//
	// CONNECTION METHODS
	//

	/** Returns the number of <code>GXLLocalConnectionTentacle</code>s connected to this <code>GXLGraphElement</code>. 
	 *	@return The number of <code>GXLLocalConnectionTentacle</code>s connected to this <code>GXLGraphElement</code>. 
	 */
	public int getConnectionCount() {
		return connections.size();
	}

	/** Returns the specified tentacle. 
	 *	Note that the order of the returned tentacle will follow order attributes of the edges, if any.
	 *	@param i The index of the specified tentacle.
	 *	@return The specified tentacle.
	 */
	public GXLLocalConnectionTentacle getConnectionAt(int i) {
		return (GXLLocalConnectionTentacle) connections.elementAt(i);
	}
}
