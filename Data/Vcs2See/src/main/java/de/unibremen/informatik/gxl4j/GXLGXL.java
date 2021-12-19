/*
 * @(#)GXLGXL.java	0.9 2003-12-20
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

/** Represents the GXL <code>gxl</code> element. 
 *	<p>
 *	The <code>gxl</code> element is the top container of any GXL document. This element
 *	is automatically created when a <code>GXLDocument</code> is created and can be accessed
 *	via the <code>GXLDocument.getDocumentElement()</code> method.
 *	<p>
 *	Note that although a <code>gxl</code> element may contain several graphs, none of these graphs
 *	may be connected to each other using edges or relations.
 *	<b>Children</b><br>
 *	<code>GXLGraph</code> - The graphs contained in this document.<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>xmlns:xlink</code> - Fixed reference to introduce the xlink namespace, cannot be modified.<br>
 *
 *	@see GXLDocument
 *	@see GXLGraph
 */
public class GXLGXL extends GXLElement {
	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLNode element. */
	GXLGXL(GXLDocument gxlDocument) {
		super(GXL.GXL);
		this.gxlDocument = gxlDocument;
	}

	/** Creates a new GXLNode element (used when reading a document). */
	GXLGXL(GXLDocument gxlDocument, Element element) {
		super(GXL.GXL, element);
		this.gxlDocument = gxlDocument;
		createChildren(element);
	}

	//
	// CHILD TYPE ACCESSORS
	//

	/** Returns the number of graphs contained in this gxl element. 
	 *	@return The number of graphs contained in this gxl element. 
	 *	@see #getChildCount()
	 */
	public int getGraphCount() {
		return getChildCount();
	}

	/** Returns the specified graph. 
	 *	@param i The index of the specified graph.
	 *	@return The specified graph.
	 *	@see #getChildAt(int i)
	 */
	public GXLGraph getGraphAt(int i) {
		return (GXLGraph) getChildAt(i);
	}
}
