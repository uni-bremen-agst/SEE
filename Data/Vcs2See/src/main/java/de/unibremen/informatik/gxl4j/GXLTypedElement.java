/*
 * @(#)GXLTypedElement.java	0.9 2003-11-16
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
import org.w3c.dom.Element;

/** Superclass for all <code>GXLElement</code>s that can be typed using the <code>type</code> element. 
 *	<p>
 *	<code>GXLTypedElement</code>s can be typed by adding a <code>type</code> element containing a link
 *	to a GXLSchema element that describes the type. 
 *
 *	@see GXLType
 */
public abstract class GXLTypedElement extends GXLAttributedElement {
	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLTypedElement. */
	GXLTypedElement(String elementName) {
		super(elementName);
	}

	/** Creates a new GXLTypedElement. */
	GXLTypedElement(String elementName, Element element) {
		super(elementName, element);
	}

	//
	// CHILD TYPE ACCESSORS
	//

	/** Returns the type of this element, or null if no type is specified. 
	 *	This method simply checks if a <code>GXLType</code> child exists,
	 *	returning it if so.
	 *	@return The type of this element, or null if no type is specified. 
	 *	@see #getChildAt(int i)
	 */
	public GXLType getType() {
		if (getChildCount() > 0 && (getChildAt(0) instanceof GXLType))
			return (GXLType) getChildAt(0);
		else
			return null;
	}

	/** Sets the type of this element. 
	 *	This method checks if a <code>GXLType</code> child already exists,
	 *	changing it's uri if so. Otherwise a new <code>GXLType</code> is created
	 *	and added to this element.
	 *	@param uri The URI specifying the GXLSchema element that types this element.
	 */
	public void setType(URI uri) {
		if (getChildCount() > 0 && (getChildAt(0) instanceof GXLType))
			((GXLType) getChildAt(0)).setURI(uri);
		else
			add(new GXLType(uri));
	}
}
