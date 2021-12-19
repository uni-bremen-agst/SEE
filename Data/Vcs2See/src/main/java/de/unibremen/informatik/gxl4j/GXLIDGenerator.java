/*
 * @(#)GXLIDGenerator.java	0.9 2003-11-16
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

import java.util.Map;
import java.util.Hashtable;

/** A simple id generator. 
 *	Since the ids of <code>GXLAttributedElement</code>s must be unique throughout the <code>GXLDocument</code>
 *	this class could be used to ensure that no duplicates are introduced.
 *	<p>
 *	The ids created by this class are of the form <i>prefix</i> + <i>integer</i> where the integer
 *	part are incremented until the id isn't contained in the wrapped GXL document.
 *
 *	@see GXLAttributedElement
 *	@see GXLDocument
 */
public class GXLIDGenerator {
	GXLDocument gxlDocument;
	Map counters;

	//
	// CONSTRUCTORS
	//

	/** Creates a new id generator. 
	 *	@param gxlDocument The GXL document to create unique ids for.
	 */
	public GXLIDGenerator(GXLDocument gxlDocument) {
		this.gxlDocument = gxlDocument;
		counters = new Hashtable();
	}

	//
	// PUBLIC METHODS
	//

	/** Generates a unique ID of the form prefix + number. 
	 *	@param prefix The prefix to use.
	 *	@return The new unique id.
	 */
	public String generateID(String prefix) {
		Integer i = (Integer) counters.get(prefix);
		int cnt = i != null ? i.intValue() : 1;
		while (gxlDocument.containsID(prefix+cnt))
			cnt++;
		counters.put(prefix, new Integer(cnt+1));
		return prefix+cnt;
	}

	/** Generates a unique ID of the form "id" + number. 
	 *	@return The new unique id.
	 */
	public String generateID() { return generateID("id"); }

	/** Generates a unique ID of the form "graph" + number. 
	 *	@return The new unique id.
	 */
	public String generateGraphID() { return generateID("graph"); }

	/** Generates a unique ID of the form "node" + number. 
	 *	@return The new unique id.
	 */
	public String generateNodeID() { return generateID("node"); }

	/** Generates a unique ID of the form "edge" + number. 
	 *	@return The new unique id.
	 */
	public String generateEdgeID() { return generateID("edge"); }

	/** Generates a unique ID of the form "rel" + number. 
	 *	@return The new unique id.
	 */
	public String generateRelID() { return generateID("rel"); }

	/** Generates a unique ID of the form "attr" + number. 
	 *	@return The new unique id.
	 */
	public String generateAttrID() { return generateID("attr"); }
}
