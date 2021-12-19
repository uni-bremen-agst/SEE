/*
 * @(#)GXLLocator.java	0.8 2003-09-25
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

/** Represents the GXL <code>locator</code> element. 
 *	<p>
 *	The <code>locator</code> element encapsulates a URI, that is a reference to some other entity
 *	such as a file or a web resource.
 *	<p>
 *	<b>Children</b><br>
 *	The <code>locator</code> element may not contain any children.
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>xlink:href</code> - Required uri.
 */
public class GXLLocator extends GXLValue {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLLocator</code> element. 
	 *	@param uri The URI encapsulated by the new <code>GXLLocator</code>.
	 */
	public GXLLocator(URI uri) {
		super(GXL.LOCATOR);
		setAttribute(GXL.XLINK_HREF, uri.toString());
	}

	/** Creates a new GXLLocator element (used when reading a document). */
	GXLLocator(Element element) {
		super(GXL.LOCATOR, element);
		createChildren(element);
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Returns the encapsulated URI of this <code>GXLLocator</code>. 
	 *	This method is essentially a shorthand for <code>new URI(getAttribute(GXL.XLINK_HREF))</code>.
	 *	@return The encapsulated URI.
	 */
	public URI getURI() {
		try {
			return new URI(getAttribute(GXL.XLINK_HREF));
		}
		catch (URISyntaxException use) {
		}
		return null;
	}

	/** Sets the encapsulated URI. 
	 *	@param uri The new URI.
	 */
	public void setURI(URI uri) {
		setAttribute(GXL.XLINK_HREF, uri.toString());
	}

	//
	// PUBLIC METHODS
	//
	
	/** Compares this <code>GXLLocator</code> to another object. 
	 *	Equality is compared by using the java.net.URI.equals method.
	 *	@param o The other object to compare this to.
	 *	@return Whether this <code>GXLLocator</code> is considered equal to the compared object.
	 *	@throws ClassCastException If the compared object is not an instance of <code>GXLLocator</code>.
	 */
	public boolean equals(Object o) {
		GXLLocator l = (GXLLocator) o;
		return l.getURI().equals(getURI());
	}
}
