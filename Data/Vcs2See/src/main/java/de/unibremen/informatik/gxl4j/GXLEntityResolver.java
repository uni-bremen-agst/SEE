/*
 * @(#)GXLEntityResolver.java 0.92 2004-04-22
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

import org.xml.sax.InputSource;
import org.xml.sax.EntityResolver;
import org.xml.sax.SAXException;
import java.io.InputStream;
import java.io.IOException;

/** Private class that ensures that a local copy of the GXL DTD is always used.
 *	If another DTD is declared (one not ending in "gxl-1.0.dtd") an exception will
 *	be thrown, since this package only handles GXL version 1.0.
 *	<p>
 *	The reason for this class is to (1) avoid unnecessary network traffic and (2)
 *	to warn users if they use differing GXL versions in conjunction with this package.
 *	@author Alex Ivanov
 */
class GXLEntityResolver implements EntityResolver {
	/** Resolves entities to streams, in our case by always loading a local DTD copy from the jar-file. */
	public InputSource resolveEntity(String publicID, String systemID) throws SAXException, IOException {
		// Throw an exception if SYSTEM doesn't point to gxl-1.0.dtd
		if (systemID != null) {
			int index = systemID.lastIndexOf("gxl-1.0.dtd");
			if (index != (systemID.length() - 11))
				throw new GXLValidationException(GXLValidationException.UNSUPPORTED_VERSION);
		}

		// Load local copy of the DTD
		InputStream input = getClass().getResourceAsStream("/gxl-1.0.dtd");
		if (input == null)
			throw new AssertionError("Error in GXLEntityResolver: Couldn't load resource \"/gxl-1.0.dtd\"");
		InputSource source = new InputSource(input);

		return source;
	}
}