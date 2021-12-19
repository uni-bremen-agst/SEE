/*
 * @(#)GXL.java	0.9 2003-12-07
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

/** Provides static GXL constants that can be used if wanted. 
 *	<p>
 *	For information on the semantic meaning of the attributes and elements,
 *	see the corresponding element documentation. 
 */
public class GXL {
	/** The url to the gxl dtd version 1.0 */
	public static final String SYSTEM_ID = "http://www.gupro.de/GXL/gxl-1.0.dtd";
	// Elements
	/** The ATTR element name, "attr". */
	public static final String ATTR = "attr";
	/** The BAG element name, "bag". */
	public static final String BAG = "bag";
	/** The BOOL element name, "bool". */
	public static final String BOOL = "bool";
	/** The EDGE element name, "edge". */
	public static final String EDGE = "edge";
	/** The ENUM element name, "enum". */
	public static final String ENUM = "enum";
	/** The FLOAT element name, "float". */
	public static final String FLOAT = "float";
	/** The GRAPH element name, "graph". */
	public static final String GRAPH = "graph";
	/** The GXL element name, "gxl". */
	public static final String GXL = "gxl";
	/** The INT element name, "int". */
	public static final String INT = "int";
	/** The LOCATOR element name, "locator". */
	public static final String LOCATOR = "locator";
	/** The NODE element name, "node". */
	public static final String NODE = "node";
	/** The REL element name, "rel". */
	public static final String REL = "rel";
	/** The RELEND element name, "relend". */
	public static final String RELEND = "relend";
	/** The SEQ element name, "seq". */
	public static final String SEQ = "seq";
	/** The SET element name, "set". */
	public static final String SET = "set";
	/** The STRING element name, "string". */
	public static final String STRING = "string";
	/** The TUP element name, "tup". */
	public static final String TUP = "tup";
	/** The TYPE element name, "type". */
	public static final String TYPE = "type";
	// Attributes
	/** The DIRECTION attribute name, "direction". */
	public static final String DIRECTION = "direction";
	/** The EDGEIDS attribute name, "edgeids". */
	public static final String EDGEIDS = "edgeids";
	/** The EDGEMODE attribute name, "edgemode". */
	public static final String EDGEMODE = "edgemode";
	/** The ENDORDER attribute name, "endorder". */
	public static final String ENDORDER = "endorder";
	/** The FROM attribute name, "from". */
	public static final String FROM = "from";
	/** The FROMORDER attribute name, "fromorder". */
	public static final String FROMORDER = "fromorder";
	/** The HYPERGRAPH attribute name, "hypergraph". */
	public static final String HYPERGRAPH = "hypergraph";
	/** The ID attribute name, "id". */
	public static final String ID = "id";
	/** The ISDIRECTED attribute name, "isdirected". */
	public static final String ISDIRECTED = "isdirected";
	/** The KIND attribute name, "kind". */
	public static final String KIND = "kind";
	/** The NAME attribute name, "name". */
	public static final String NAME = "name";
	/** The ROLE attribute name, "role". */
	public static final String ROLE = "role";
	/** The STARTORDER attribute name, "startorder". */
	public static final String STARTORDER = "startorder";
	/** The TARGET attribute name, "target". */
	public static final String TARGET = "target";
	/** The TO attribute name, "to". */
	public static final String TO = "to";
	/** The TOORDER attribute name, "toorder". */
	public static final String TOORDER = "toorder";
	/** The XLINK_HREF attribute name, "xlink:href". */
	public static final String XLINK_HREF = "xlink:href";
	/** The XLINK_TYPE attribute name, "xlink:type". */
	public static final String XLINK_TYPE = "xlink:type";
	/** The XMLNS_XLINK attribute name, "xmlns:xlink". */
	public static final String XMLNS_XLINK = "xmlns:xlink";
	// Enumerated Attribute values
	/** The DEFAULTDIRECTED attribute value, "defaultdirected". */
	public static final String DEFAULTDIRECTED = "defaultdirected";
	/** The DEFAULTUNDIRECTED attribute value, "defaultundirected". */
	public static final String DEFAULTUNDIRECTED = "defaultundirected";
	/** The DIRECTED attribute value, "directed". */
	public static final String DIRECTED = "directed";
	/** The FALSE attribute value, "false". */
	public static final String FALSE = "false";
	/** The GXL_XMLNS_XLINK attribute value, "http://www.w3.org/1999/xlink". */
	public static final String GXL_XMLNS_XLINK = "http://www.w3.org/1999/xlink";
	/** The IN attribute value, "in". */
	public static final String IN = "in";
	/** The NONE attribute value, "none". */
	public static final String NONE = "none";
	/** The OUT attribute value, "out". */
	public static final String OUT = "out";
	/** The SIMPLE attribute value, "simple". */
	public static final String SIMPLE = "simple";
	/** The TRUE attribute value, "true". */
	public static final String TRUE = "true";
	/** The UNDIRECTED attribute value, "undirected". */
	public static final String UNDIRECTED = "undirected";
	// Attribute types
	/** The ID attribute type, "ID". */
	public static final String TYPE_ID = "ID";
	/** The IDREF attribute type, "IDREF". */
	public static final String TYPE_IDREF = "IDREF";
	/** The NMTOKEN attribute type, "NMTOKEN". */
	public static final String TYPE_NMTOKEN = "NMTOKEN";
	/** The CDATA attribute type, "CDATA". */
	public static final String TYPE_CDATA = "CDATA";
	/** The ENUMERATION attribute type, "ENUMERATION". */
	public static final String TYPE_ENUMERATION = "ENUMERATION";


	/** GXL Elements (alphabetically ordered) */
	static final String[] ELEMENTS = new String[] {	
		ATTR,
		BAG,
		BOOL,
		EDGE,
		ENUM,
		FLOAT,
		GRAPH,
		GXL,
		INT,
		LOCATOR,
		NODE,
		REL,
		RELEND,
		SEQ,
		SET,
		STRING,
		TUP,
		TYPE,
	};

	/** GXL Attributes (alphabetically ordered) */
	static final String[] ATTRIBUTES = new String[] {	
		DIRECTION,
		EDGEIDS,
		EDGEMODE,
		ENDORDER,
		FROM,
		FROMORDER,
		HYPERGRAPH,
		ID,
		ISDIRECTED,
		KIND,
		NAME,
		ROLE,
		STARTORDER,
		TARGET,
		TO,
		TOORDER,
		XLINK_HREF,
		XLINK_TYPE,
		XMLNS_XLINK,
	};

	/** The allowed child elements. Same order as ELEMENTS above. */
	static final String[][] ALLOWED_CHILDREN = new String[][] {
		// ATTR
		{ ATTR, BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP, TYPE },
		// BAG
		{ BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP },
		// BOOL
		{},
		// EDGE
		{ ATTR, GRAPH, TYPE },
		// ENUM
		{},
		// FLOAT
		{},
		// GRAPH
		{ ATTR, EDGE, NODE, REL, TYPE },
		// GXL
		{ GRAPH },
		// INT
		{},
		// LOCATOR
		{},
		// NODE
		{ ATTR, GRAPH, TYPE },
		// REL
		{ ATTR, GRAPH, RELEND, TYPE },
		// RELEND
		{ ATTR },
		// SEQ
		{ BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP },
		// SET
		{ BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP },
		// STRING
		{},
		// TUP
		{ BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP },
		// TYPE
		{}
	};

	/** The required order of child elements. Same order as ELEMENTS above. */
	static final String[][][] CHILD_ORDER = new String[][][] {
		// ATTR
		{ { TYPE }, { ATTR }, { BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP } },
		// BAG
		{ { BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP } },
		// BOOL
		{},
		// EDGE
		{ { TYPE }, { ATTR }, { GRAPH } },
		// ENUM
		{},
		// FLOAT
		{},
		// GRAPH
		{ { TYPE }, { ATTR }, { EDGE, NODE, REL } },
		// GXL
		{ { GRAPH } },
		// INT
		{},
		// LOCATOR
		{},
		// NODE
		{ { TYPE }, { ATTR }, { GRAPH } },
		// REL
		{ { TYPE }, { ATTR }, { GRAPH }, { RELEND } },
		// RELEND
		{ { ATTR } },
		// SEQ
		{ { BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP } },
		// SET
		{ { BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP } },
		// STRING
		{},
		// TUP
		{ { BAG, BOOL, ENUM, FLOAT, INT, LOCATOR, SEQ, SET, STRING, TUP } },
		// TYPE
		{}
	};

	/** The allowed attributes. Same order as ELEMENTS above. */
	static final String[][] ALLOWED_ATTRIBUTES = new String[][] {
		// ATTR
		{ ID, NAME, KIND },
		// BAG
		{},
		// BOOL
		{},
		// EDGE
		{ ID, FROM, TO, FROMORDER, TOORDER, ISDIRECTED },
		// ENUM
		{},
		// FLOAT
		{},
		// GRAPH
		{ ID, ROLE, EDGEIDS, HYPERGRAPH, EDGEMODE },
		// GXL
		{ XMLNS_XLINK },
		// INT
		{},
		// LOCATOR
		{ XLINK_TYPE, XLINK_HREF },
		// NODE
		{ ID },
		// REL
		{ ID, ISDIRECTED },
		// RELEND
		{ TARGET, ROLE, DIRECTION, STARTORDER, ENDORDER },
		// SEQ
		{},
		// SET
		{},
		// STRING
		{},
		// TUP
		{},
		// TYPE
		{ XLINK_TYPE, XLINK_HREF }
	};

	/** The required attributes. Same order as ALLOWED_ATTRIBUTES above. */
	static final boolean[][] REQUIRED_ATTRIBUTES = new boolean[][] {
		// ATTR
		{ false, true, false },
		// BAG
		{},
		// BOOL
		{},
		// EDGE
		{ false, true, true, false, false, false },
		// ENUM
		{},
		// FLOAT
		{},
		// GRAPH
		{ true, false, false, false, false },
		// GXL
		{ false },
		// INT
		{},
		// LOCATOR
		{ false, true },
		// NODE
		{ true },
		// REL
		{ false, false },
		// RELEND
		{ true, false, false, false, false },
		// SEQ
		{},
		// SET
		{},
		// STRING
		{},
		// TUP
		{},
		// TYPE
		{ false, true }
	};

	/** GXL Attribute Default values (Same order as ATTRIBUTES) */
	static final String[] ATTRIBUTE_DEFAULTS = new String[] {	
		// DIRECTION
		null,
		// EDGEIDS
		FALSE,
		// EDGEMODE
		DIRECTED,
		// ENDORDER
		null,
		// FROM
		null,
		// FROMORDER
		null,
		// HYPERGRAPH
		FALSE,
		// ID
		null,
		// ISDIRECTED
		null,
		// KIND
		null,
		// NAME
		null,
		// ROLE
		null,
		// STARTORDER
		null,
		// TARGET
		null,
		// TO
		null,
		// TOORDER
		null,
		// XLINK_HREF
		null,
		// XLINK_TYPE
		SIMPLE,
		// XMLNS_XLINK
		GXL_XMLNS_XLINK,
	};

	/** GXL Attribute Enumeration values (Same order as ATTRIBUTES) */
	static final String[][] ATTRIBUTE_VALUES = new String[][] {	
		// DIRECTION
		{ IN, OUT, NONE },
		// EDGEIDS
		{ TRUE, FALSE },
		// EDGEMODE
		{ DIRECTED, UNDIRECTED, DEFAULTDIRECTED, DEFAULTUNDIRECTED },
		// ENDORDER
		null,
		// FROM
		null,
		// FROMORDER
		null,
		// HYPERGRAPH
		{ TRUE, FALSE },
		// ID
		null,
		// ISDIRECTED
		{ TRUE, FALSE },
		// KIND
		null,
		// NAME
		null,
		// ROLE
		null,
		// STARTORDER
		null,
		// TARGET
		null,
		// TO
		null,
		// TOORDER
		null,
		// XLINK_HREF
		null,
		// XLINK_TYPE
		{ SIMPLE },
		// XMLNS_XLINK
		{ GXL_XMLNS_XLINK },
	};

	/** GXL Attribute types (Same order as ATTRIBUTES) */
	static final String[] ATTRIBUTE_TYPES = new String[] {	
		// DIRECTION
		TYPE_ENUMERATION,
		// EDGEIDS
		TYPE_ENUMERATION,
		// EDGEMODE
		TYPE_ENUMERATION,
		// ENDORDER
		TYPE_CDATA,
		// FROM
		TYPE_IDREF,
		// FROMORDER
		TYPE_CDATA,
		// HYPERGRAPH
		TYPE_ENUMERATION,
		// ID
		TYPE_ID,
		// ISDIRECTED
		TYPE_ENUMERATION,
		// KIND
		TYPE_NMTOKEN,
		// NAME
		TYPE_NMTOKEN,
		// ROLE
		TYPE_NMTOKEN,
		// STARTORDER
		TYPE_CDATA,
		// TARGET
		TYPE_IDREF,
		// TO
		TYPE_IDREF,
		// TOORDER
		TYPE_CDATA,
		// XLINK_HREF
		TYPE_CDATA,
		// XLINK_TYPE
		TYPE_ENUMERATION,
		// XMLNS_XLINK
		TYPE_CDATA,
	};

	//
	// Static helper methods
	//

	/** Returns the index of element in array or -1 if array doesn't contain element. 
	 *	Uses Object.equals() for comparison. No null elements allowed. */
	static int indexOf(Object[] array, Object element) {
		int result = -1;
		for (int i = 0; result == -1 && i < array.length; i++)
			if (array[i].equals(element))
				result = i;
		return result;
	}

	/** Returns true if array contains element. 
	 *	Uses Object.equals() for comparison. No null elements allowed. */
	static boolean contains(Object[] array, Object element) {
		return indexOf(array, element) >= 0;
	}

	/** Private constructor, this class should never be instantiated. */
	private GXL() {
	}
}
