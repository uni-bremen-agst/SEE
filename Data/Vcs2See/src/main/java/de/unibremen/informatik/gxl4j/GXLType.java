/*
 * @(#)GXLType.java	0.9 2003-12-27
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

import java.net.URI;
import java.net.URISyntaxException;
import org.w3c.dom.Element;

/** Represents the GXL <code>type</code> element. 
 *	<p>
 *	The <code>type</code> element is used in conjuction with a GXL schema to define types of graphs, nodes etc.
 *	The URI of a <code>type</code> element must always point to a GXL schema entity. If types are used, then
 *	all elements in the GXL graph MUST be typed and point to the SAME GXL schema.
 *	<p>
 *	<b>Children</b><br>
 *	The <code>type</code> element may not contain any children.
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>xlink:type</code> - Fixed string = "simple"
 *	<code>xlink:href</code> - Required uri pointing to a GXL schema.
 *
 *	@see GXLTypedElement
 */
public class GXLType extends GXLElement {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLType</code> element. 
	 *	@param uri A URI pointing to a GXL schema.
	 */
	public GXLType(URI uri) {
		super(GXL.TYPE);
		setAttribute(GXL.XLINK_HREF, uri.toString());
	}

	/** Creates a new GXLType element (used when reading a document). */
	GXLType(Element element) {
		super(GXL.TYPE, element);
		createChildren(element);
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Returns the URI of this type. 
	 *	@return The URI of this type. 
	 */
	public URI getURI() {
		try {
			return new URI(getAttribute(GXL.XLINK_HREF));
		}
		catch (URISyntaxException use) {
		}
		return null;
	}

	/** Sets the URI of this type. 
	 *	@param uri The new URI.
	 */
	public void setURI(URI uri) {
		setAttribute(GXL.XLINK_HREF, uri.toString());
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this type to another object.
	 *	Equality is defined as using the URI class' equals method.
	 *	@param o The object to compare this type to.
	 *	@return Whether the specified object is considered equal to this one.
	 *	@throws ClassCastException If the specified object is not an instance of <code>GXLType</code>.
	 */
	public boolean equals(Object o) {
		GXLType t = (GXLType) o;
		return t.getURI().equals(getURI());
	}
}
