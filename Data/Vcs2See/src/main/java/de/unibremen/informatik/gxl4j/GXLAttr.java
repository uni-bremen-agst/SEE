/*
 * @(#)GXLAttr.java	0.9 2003-12-07
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

/** Represents the GXL <code>attr</code> element. 
 *	<p>
 *	The <code>attr</code> element is used to provide attributes to <code>GXLAttributedElement</code>s 
 *	in a name/value fashion.
 *	<p>
 *	Please observe the difference between (1) <code>GXLAttr</code> elements that attribute GXL entities
 *	and (2) XML element attributes that is used to markup the GXL format.
 *	<p>
 *	<b>Children</b><br>
 *	<code>GXLType</code> - see {@link GXLType}.<br>
 *	<code>GXLAttr</code> - <code>attr</code> elements are themselves attributed elements so as to provide for hierachical attributes.<br>
 *	<code>GXLValue</code> - each <code>attr</code> element MUST contain exactly one value.<br>
 *	<p>
 *	<b>Attributes</b><br>
 *	<code>id</code> - Optional id of this <code>attr</code> element.<br>
 *	<code>name</code> - Required name of this <code>attr</code> element. MUST be unique across the <code>GXLAttributedElement</code>
 *	that this <code>attr</code> belongs to.<br>
 *	<code>kind</code> - Optional string describing this <code>attr</code> element.<br>
 *
 *	@see GXLAttributedElement
 *	@see GXLValue
 */
public class GXLAttr extends GXLAttributedElement {
	//
	// CONSTRUCTORS
	//

	/** Creates a new <code>GXLAttr</code> element. 
	 *	@param name The required <code>name</code> attribute.
	 *	@param value The required value of this <code>attr</code> element.
	 */
	public GXLAttr(String name, GXLValue value) {
		super(GXL.ATTR);
		setAttribute(GXL.NAME, name);
		add(value);
	}

	/** Creates a new GXLAttr element (used when reading a document). 
	 *	@param element The DOM element to copy information from. 
	 */
	GXLAttr(Element element) {
		super(GXL.ATTR, element);
		createChildren(element);
	}

	//
	// SPECIAL METHOD
	//

	/** Changes the value contained in this <code>attr</code> element.
	 *	This method is useful when you want to change the type of value that the attribute
	 *	represents, which would otherwise require creating a new <code>attr</code> element
	 *	and inserting that instead. In the case of changing the value to the same type,
	 *	the subclasses of GXLValue provides appropriate methods as well.
	 *	@param value The new value of this <code>attr</code> element.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see GXLAtomicValue#setValue(String)
	 *	@since 0.91
	 */
	public void setValue(GXLValue value) {
		// Validate this modification (throws on error)
		GXLValidator.validateAttrValueModification(gxlDocument, this, value);
		// All checks finished, make the change
		GXLAttrValueModification mod = new GXLAttrValueModification(this, getValue(), value);
		mod.execute();
	}

	//
	// CHILD TYPE ACCESSORS
	//
	
	/** Returns the <code>GXLValue</code> of this <code>attr</code> element. 
	 *	@return The <code>GXLValue</code> of this <code>attr</code> element. 
	 */
	public GXLValue getValue() {
		GXLValue result = null;
		int index = getChildCount()-1;
		if (index >= 0 && (getChildAt(index) instanceof GXLValue))
			result = (GXLValue) getChildAt(index);
		return result;
	}

	//
	// ATTRIBUTE ACCESSORS
	//

	/** Returns the value of the <code>name</code> attribute or <code>null</code> if the attribute isn't set.
	 *	This simply calls <code>getAttribute(GXL.NAME)</code>.
	 *	@return The value of the <code>name</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public String getName() {
		return getAttribute(GXL.NAME);
	}

	/** Sets the value of the <code>name</code> attribute.
	 *	This simply calls <code>setAttribute(GXL.NAME, name)</code>.
	 *	@param name The new value.
	 *	@throws GXLValidationException If this action would result in an invalid GXL document.
	 *	@see #setAttribute(String, String)
	 */
	public void setName(String name) {
		setAttribute(GXL.NAME, name);
	}

	/** Returns the value of the <code>kind</code> attribute or <code>null</code> if the attribute isn't set.
	 *	This simply calls <code>getAttribute(GXL.KIND)</code>.
	 *	@return The value of the <code>kind</code> attribute.
	 *	@see #getAttribute(String)
	 */
	public String getKind() {
		return getAttribute(GXL.KIND);
	}

	/** Sets the value of the <code>kind</code> attribute. 
	 *	This simply calls <code>setAttribute(GXL.KIND, kind)</code>.
	 *	@param name The new value.
	 *	@see #setAttribute(String, String)
	 */
	public void setKind(String kind) {
		setAttribute(GXL.KIND, kind);
	}
}
