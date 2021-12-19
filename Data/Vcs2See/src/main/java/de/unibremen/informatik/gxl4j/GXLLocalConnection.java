/*
 * @(#)GXLLocalConnection.java	0.9 2003-11-16
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

/** Superclass for <code>GXLEdge</code> and <code>GXLRel</code> providing basic edge functionality. 
 *
 *	@see GXLEdge
 *	@see GXLRel
 */
public abstract class GXLLocalConnection extends GXLGraphElement {
	/** List of tentacles of this local connection */
	Vector tentacles = new Vector();

	//
	// CONSTRUCTORS
	//

	/** Creates a new LocalConnection. */
	GXLLocalConnection(String elementName) {
		super(elementName);
	}

	/** Creates a new LocalConnection. */
	GXLLocalConnection(String elementName, Element element) {
		super(elementName, element);
	}

	//
	// CONNECTION METHODS
	//

	/** Returns the number of tentacles that this <code>GXLLocalConnection</code> has. 
	 *	@return The number of tentacles that this <code>GXLLocalConnection</code> has. 
	 */
	public int getTentacleCount() {
		return tentacles.size();
	}

	/** Returns the specified tentacle. 
	 *	@param i The index of the specified tentacle.
	 *	@return The specified tentacle.
	 */
	public GXLLocalConnectionTentacle getTentacleAt(int i) {
		return (GXLLocalConnectionTentacle) tentacles.elementAt(i);
	}

	/** Whether this <code>GXLLocalConnection</code> contains dangling tentacles. 
	 *	@return True iff this <code>GXLLocalConnection</code> contains dangling tentacles. 
	 */
	public boolean containsDanglingTentacles() {
		boolean result = false;
		for (int i = 0; !result && i < getTentacleCount(); i++)
			result = getTentacleAt(i).isDangling();
		return result;
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Whether this <code>GXLLocalConnection</code> is directed. 
	 *	The direction of a <code>GXLLocalConnection</code> is set either by the directed attribute,
	 *	or by the surrounding graphs edgemode attribute.
	 *	@return True iff this <code>GXLLocalConnection</code> is directed. 
	 *	@see GXLGraph#getEdgeMode()
	 */
	public boolean isDirected() {
		String direction = getAttribute(GXL.ISDIRECTED);
		if (direction != null)
			return direction.equals(GXL.TRUE);
		else if (parent != null) {
			GXLGraph graph = (GXLGraph) parent;
			String edgemode = graph.getAttribute(GXL.EDGEMODE);
			return edgemode.equals(GXL.DIRECTED) || edgemode.equals(GXL.DEFAULTDIRECTED);
		}
		else
			return true;
	}

	/** Sets the value of the <code>directed</code> attribute.
	 *	This simply calls <code>setAttribute(GXL.ISDIRECTED, directed ? GXL.TRUE : GXL.FALSE)</code>.
	 *	Note that whether a <code>GXLLocalConnection</code> is directed or not is also dependant on
	 *	the edgemode attribute of the surrounding graph and that in some cases is is illegal to override
	 *	the graphs attribute using the directed attribute.
	 *	@param directed The new value.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see GXLGraph
	 *	@see GXLGraph#setEdgeMode(String edgemode)
	 *	@see #setAttribute(String name, String value)
	 */
	public void setDirected(boolean directed) {
		setAttribute(GXL.ISDIRECTED, directed ? GXL.TRUE : GXL.FALSE);
	}

	//
	// PACKAGE METHODS
	//

	boolean hasKnownDirection() {
		return getAttribute(GXL.ISDIRECTED) != null || parent != null;
	}
}
