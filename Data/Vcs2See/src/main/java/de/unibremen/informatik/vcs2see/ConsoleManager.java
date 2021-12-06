package de.unibremen.informatik.vcs2see;

import lombok.Getter;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;

/**
 * Component which processes output and input on the console.
 *
 * @author Felix Gaebler
 * @version 1.0.0
 */
public class ConsoleManager {

    private final BufferedReader reader;

    public ConsoleManager() {
        this.reader = new BufferedReader(new InputStreamReader(System.in));
    }

    /**
     * Reads a line on the input and returns it.
     * @return line read
     * @throws IOException exception
     */
    public String readLine() throws IOException {
        return reader.readLine();
    }

    /**
     * Reads a line on the input with prompt and returns it.
     * @param prompt message which is to be output before the input
     * @return line read
     * @throws IOException exception
     */
    public String readLine(String prompt) throws IOException {
        System.out.print(prompt);
        return reader.readLine();
    }

    /**
     * Prints a line to the console.
     * @param text message to be output
     */
    public void print(String text) {
        System.out.println(text);
    }

    /**
     * Outputs a separator line in the console.
     */
    public void printSeparator() {
        System.out.println("------------------------------------------------------------");
    }

    /**
     * Outputs the logo of the application in the console.
     */
    public void printWelcome() {
        System.out.println("              _____         ___ _____         ");
        System.out.println("             |  |  |___ ___|_  |   __|___ ___ ");
        System.out.println("             |  |  |_ -|  _|  _|__   | -_| -_|");
        System.out.println("              \\___/|___|___|___|_____|___|___|");
    }

}
