package de.unibremen.swt.see.manager.controller;

/**
 * Common helper functions used in controllers.
 */
public class ControllerUtils {

    /**
     * Wraps a message into a rudimentary JSON structure for server responses.
     *
     * @param message the message
     * @return wrapped message
     */
    public static String wrapMessage(String message) {
        return "{\"message\":\"" + message + "\"}";
    }
}
