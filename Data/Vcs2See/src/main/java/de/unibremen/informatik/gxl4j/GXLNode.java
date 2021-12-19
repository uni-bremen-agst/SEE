/*
 * @(#)GXLNode.java	0.9 2003-12-21
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

/** Represents the GXL <code>node</code> element. 
 *	<p>
 *	The <code>node</code> element is what it sounds like, a node that can be connected with edges.
 *	Note however that <code>edge</code>s and <code>rel</code>s also are eligible for connecting,
 *	being a subclass of <code>GXLGraphElement</code>.
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLType</code> - see {@link GXLType}.<br>
 *	<code>GXLAttr</code> - Attributes describing this node.<br>
 *	<code>GXLGraph</code> - <code>node</code> elements may contain <code>graph</code> elements, creating a hierarchical graph.<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>id</code> - Required id of this <code>node</code> element.<br>
 *
 *	@see GXLGraphElement
 */
public class GXLNode extends GXLGraphElement {
	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLNode element with the specified id. 
	 *	@param id The id of the new node.
	 */
	public GXLNode(String id) {
		super(GXL.NODE);
		setAttribute(GXL.ID, id);
	}

	/** Creates a new GXLNode element (used when reading a document). */
	GXLNode(Element element) {
		super(GXL.NODE, element);
		createChildren(element);
	}
}
