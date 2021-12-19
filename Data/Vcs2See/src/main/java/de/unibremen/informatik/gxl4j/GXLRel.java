/*
 * @(#)GXLRel.java	0.9 2003-12-21
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

/** Represents the GXL <code>rel</code> element. 
 *	<p>
 *	The <code>rel</code> element represents a relation between an arbitrary number of <code>GXLGraphElement</code>s.
 *	GXL <code>edge</code> elements can be viewed as a shorthand for a binary relation.
 *	<p>
 *	<b>Ordering of tentacles</b><br>
 *	The tentacles (<code>relend</code>s) of this relation are ordered in two different ways. Using the methods 
 *	<code>getRelendAt(int)</code> and <code>getChildAt(int)</code> will return the tentacles in the order they appeared
 *	in the GXL file, the order they were added. By using the method <code>getTentacleAt(int)</code> inherited from
 *	<code>GXLLocalConnection</code> they will be returned in the order specified by using the <code>startorder</code>
 *	attribute. Any tentacles that doesn't define the startorder will be returned in an arbitrary order <i>after</i>
 *	the tentacles containing a startorder.
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLType</code> - see {@link GXLType}.<br>
 *	<code>GXLAttr</code> - Attributes describing this relation.<br>
 *	<code>GXLGraph</code> - <code>rel</code> elements may contain <code>graph</code> elements, creating a hierarchical graph.<br>
 *	<code>GXLRelend</code> - Relation ends, that is the tentacles of this relation describing which graph elements belong
 *	to this relation.
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>id</code> - Optional id of this <code>rel</code> element.<br>
 *	<code>isdirected</code> - Optional boolean defining if this relations tentacles are directed. Note that this attribute overrides
 *	the <code>edgemode</code> attribute of the wrapping <code>graph</code> element (see {@link GXLGraph}). Also note that
 *	the actual direction of the tentacles are defined by the <code>relend</code> direction attribute.<br>
 *
 *	@see GXLLocalConnection
 *	@see GXLRelend
 *	@see GXLGraphElement
 */
public class GXLRel extends GXLLocalConnection {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLRel</code> element containing no tentacles. */
	public GXLRel() {
		super(GXL.REL);
	}

	/** Creates a new GXLRel element (used when reading a document). */
	GXLRel(Element element) {
		super(GXL.REL, element);
		createChildren(element);
	}

	//
	// CHILD TYPE ACCESSORS
	//

	/** Returns the number of <code>relend</code> children contained in this <code>rel</code> element. 
	 *	@return The number of <code>relend</code> children contained in this <code>rel</code> element. 
	 *	@see #getChildCount()
	 *	@see #getTentacleCount()
	 */
	public int getRelendCount() {
		int size = getChildCount();
		int result = size;
		while (result > 0 && !(getChildAt(size-result) instanceof GXLRelend))
			result--;
		return result;
	}

	/** Returns the specified <code>relend</code> child. 
	 *	@param The index of the specified relend.
	 *	@return The specified <code>relend</code> child. 
	 *	@see #getChildAt(int i)
	 *	@see #getTentacleAt(int i)
	 */
	public GXLRelend getRelendAt(int i) {
		return (GXLRelend) getChildAt(getChildCount()-getRelendCount()+i);
	}
}
