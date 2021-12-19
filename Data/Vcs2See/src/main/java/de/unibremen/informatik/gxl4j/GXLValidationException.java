/*
 * @(#)GXLValidationException.java	0.92 2004-04-22
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

/** Thrown when an operation that would render a GXLDocument invalid has been attempted. 
 *	<p>
 *	For information on the different validation errors that might occur in a GXL document
 *	and when these exceptions are thrown, see the documentation on the field constants of this class.
 *	<p>
 *	Note that the current version of this GXL package DOES NOT validate against GXL schemas.
 *	This is a feature that might be added in a future version.
 */
public class GXLValidationException extends RuntimeException {
	//
	// CONSTANTS
	//

	// DTD
	/** All elements in a gxl file must be of a type described in the DTD. 
	 *	Thrown when a resource is read that contains non-GXL elements.
	 */
	public static final int INVALID_ELEMENT_TYPE = 1;
	/** GXLElements only allow certain types of children. 
	 *	Thrown when a element is added to a parent that doesn't allow that particular type.
	 */
	public static final int INVALID_CHILD_TYPE = 2;
	/** GXLTypedElements may at the most contain one GXLType. 
	 *	Thrown when a second type element is added to a already typed element.
	 */
	public static final int DUPLICATE_TYPE = 3;
	/** GXLAttr may at the most contain one GXLValue. 
	 *	Thrown when a second value element is added to an attribute element already containing a value.
	 */
	public static final int DUPLICATE_VALUE = 4;
	/** GXLAttr must contain one GXLValue. 
	 *	Thrown (1) when a value is removed from an attribute element or (2) when a non-valued attribute
	 *	element is added to another element.
	 */
	public static final int REQUIRED_VALUE_MISSING = 5;
	/** GXLElements must contain their required attributes. 
	 *	Thrown (1) when an element is added that doesn't contain all required attributes or (2)
	 *	when a required attribute is removed.
	 */
	public static final int REQUIRED_ATTRIBUTE_MISSING = 6;
	/** GXLElements must not contain any attributes not described in the DTD. 
	 *	Thrown when an attribute is set that isn't allowed by that particular element type.
	 */
	public static final int INVALID_ATTRIBUTE = 7;
	/** GXLElement attributes must not contain invalid values. 
	 *	Thrown when an attribute with enumerated values is set to a non-allowed value. An example
	 *	is the direction attribute of the relend attribute that can only be set to "in", "out" and "none".
	 */
	public static final int INVALID_ATTRIBUTE_VALUE = 8;
	/** IDs must be unique throughout a GXLDocument. 
	 *	Thrown (1) when an element tree is added to a GXL document resulting in two or more elements
	 *	having the same id, or (2) when the id attribute of an element is set to a value already used
	 *	in the elements document.
	 */
	public static final int DUPLICATE_ID = 9;

	// Other
	/** GXLLocalConnections must refer to GXLGraphElements. 
	 *	Thrown (1) when an element tree is added to a gxl document resulting in the gxl document
	 *	containing edges referencing non-<code>GXLGraphElement</code>s, or (2) an attribute is changed,
	 *	causing an illegal edge as described above.
	 */
	public static final int INVALID_CONNECTION = 10;
	/** GXLAttr name attribute must be unique to it's GXLAttributedElement parent. 
	 *	Thrown (1) when an <code>attr</code> element is added to a parent resulting in that parent
	 *	containing two <code>attr</code> elements with the same name attribute, or (2) the name
	 *	attribute of an <code>attr</code> element is changed so as to cause the above clash.
	 */
	public static final int DUPLICATE_NAME = 11;
	/** If GXLGraph hypergraph attribute is set to false, no relations are allowed. 
	 *	Thrown (1) when a relation is added to a graph not permitting it, or (2) when the hypergraph
	 *	attribute is set to false on a graph containing relations.
	 */
	public static final int PROHIBITED_HYPERGRAPH = 12;
	/** If GXLGraph edgemode attribute is set to (un-)directed it cannot be overriden by isdirected attributes. 
	 *	If both directed and undirected edges is desired within the same graph, it's edgemode attribute must
	 *	be set to defaultdirected or defaultundirected.
	 *	Thrown (1) when an edge, with the isdirected attribute set, is added to a graph that doesn't allow override, or
	 *	(2) when the edgemode or isdirected attribute is set, causing the above illegal override.
	 */
	public static final int PROHIBITED_DIRECTION_OVERRIDE = 13;
	/** GXLRelend direction attribute must be set to GXL.IN/GXL.OUT if its GXLRel parent is directed. 
	 *	Thrown (1) when an element tree is added resulting in a directed relation containing a relend
	 *	without the direction being set to GXL.IN or GXL.OUT, or (2) an attribute is changed, causing the
	 *	above result.
	 */
	public static final int REQUIRED_RELEND_DIRECTION_MISSING = 15;
	/** Attributes from-/to-/start-/endorder must be integers. 
	 *	Thrown when one of the order attributes are set to a non-integer.
	 */
	public static final int INVALID_ORDER = 16;
	/** Attributes from-/to-/start-/endorder must be unique to their incident element. 
	 *	Thrown (1) when an element tree is added resulting in the ordering of incidences of an
	 *	element containing duplicates, or (2) when a order attribute is changed, causing the above
	 *	condition.
	 */
	public static final int DUPLICATE_ORDER = 17;
	/** xlink in type/locator must be a valid uri. 
	 *	Thrown when the URI of a type or locator element is set to an invalid URI.
	 */
	public static final int INVALID_URI = 18;
	/** GXLGraphElements in differing top GXLGraphs cannot be connected. 
	 *	Thrown (1) when an element tree is added, resulting in an edge connecting
	 *	elements from differing top graphs, or (2) when an attribute is modified to
	 *	cause the above condition.
	 */
	public static final int INVALID_INTERGRAPH_CONNECTION = 19;
	/** GXLTypes within a GXLGraph must all point to a single schema graph. 
	 *	This is currently not validated.
	 */
	public static final int INVALID_MULTIGRAPH_TYPED_GRAPH = 20; // Not Validated
	/** GXLLocalConnections must not reference themselves. 
	 *	Thrown when an attribute is set so as an edge will reference itself.
	 *	Note that this is actually not prohibited in GXL, but this package sees no point
	 *	in not doing it, and a lot of trouble in allowing it.
	 */
	public static final int INVALID_SELF_REFERENCE = 21;
	/** GXLAtomicValues must conform to their type (int, float and bool). 
	 *	Thrown when an atomic value is set to an invalid value.
	 */
	public static final int INVALID_VALUE = 22;
	/** GXLSet, GXLBag and GXLSeq must have elements of the same value type. 
	 *	Thrown when a value element is added to a bag, set or seq, causing it
	 *	to contain more than one type of elements.
	 */
	public static final int DUPLICATE_VALUE_TYPE = 23;
	/** Only GXL 1.0 is currently supported.
	 *	Thrown when reading a GXL document that contains a DTD declaration not
	 *	pointing to the gxl-1.0.dtd.
	 */
	public static final int UNSUPPORTED_VERSION = 24;

	/** The above types as Strings. */
	private static final String typeToString[] = {
		"", // We start the type count on 1, whereas arrays start on 0
		"INVALID_ELEMENT_TYPE",
		"INVALID_CHILD_TYPE",
		"DUPLICATE_TYPE",
		"DUPLICATE_VALUE",
		"REQUIRED_VALUE_MISSING",
		"REQUIRED_ATTRIBUTE_MISSING",
		"INVALID_ATTRIBUTE",
		"INVALID_ATTRIBUTE_VALUE",
		"DUPLICATE_ID",
		"INVALID_CONNECTION",
		"DUPLICATE_NAME",
		"PROHIBITED_HYPERGRAPH",
		"PROHIBITED_DIRECTION_OVERRIDE",
		"", // 14 Removed, previously REQUIRED_EDGEID_MISSING
		"REQUIRED_RELEND_DIRECTION_MISSING",
		"INVALID_ORDER",
		"DUPLICATE_ORDER",
		"INVALID_URI",
		"INVALID_INTERGRAPH_CONNECTION",
		"INVALID_MULTIGRAPH_TYPED_GRAPH",
		"INVALID_SELF_REFERENCE",
		"INVALID_VALUE",
		"DUPLICATE_VALUE_TYPE",
		"UNSUPPORTED_VERSION",
	};

	//
	// MEMBER VARIABLES
	//

	/** The type of exception. */
	int type;

	//
	// CONSTRUCTORS
	//

	/** Creates a new GXLValidationException. */
	GXLValidationException(int type) {
		super(typeToString[type]);
		this.type = type;
	}

	//
	// PUBLIC METHODS
	//

	/** Returns the type of validation error that caused this exception.
	 *	This value should be compared to the constants found in this class if
	 *	some particular action is to be taken.
	 *	@return The type of validation error that caused this exception. 
	 */
	public int getType() {
		return type;
	}
}
