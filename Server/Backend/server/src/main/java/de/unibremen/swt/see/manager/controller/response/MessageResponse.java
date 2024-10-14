package de.unibremen.swt.see.manager.controller.response;

/**
 * Data container for a message response.
 * <p>
 * The message is usually displayed to the user, e.g., after logging in.
 */
public class MessageResponse {

    /**
     * Response message.
     */
    private String message;

    /**
     * Constructs a {@code MessageResponse} with the given {@code message}.
     *
     * @param message message content
     */
    public MessageResponse(String message) {
        this.message = message;
    }

    /**
     * @return message content
     */
    public String getMessage() {
        return message;
    }

    /**
     * @param message message content
     */
    public void setMessage(String message) {
        this.message = message;
    }
}
