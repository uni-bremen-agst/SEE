package de.unibremen.informatik.vcs2see;

import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.Optional;

/**
 * Component to run baushaus on path.
 *
 * @author Felix Gaebler
 * @version 1.0.0
 */
public class CodeAnalyser {

    /**
     * Helper method to run a ProcessBuilder and output the output to the console.
     * @param processBuilder ProcessBuilder which should be executed
     * @throws IOException exception
     */
    private void run(ProcessBuilder processBuilder) throws IOException {
        System.out.println(String.join(" ", processBuilder.command()));

        processBuilder.redirectErrorStream(true);
        Process process = processBuilder.start();
        BufferedReader input = new BufferedReader(new InputStreamReader(process.getInputStream()));

        String line;
        while ((line = input.readLine()) != null) {
            System.out.println(line);
        }
    }

    /**
     *
     * @param revision
     * @throws IOException
     */
    public void analyse(int revision) throws IOException {
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();

        for(int i = 0; true; i++) {
            String key = "analyser." + i + ".";
            Optional<String> optionalCommand = propertiesManager.getProperty(key + "command");
            Optional<String> optionalDirectory = propertiesManager.getProperty(key + "directory");
            if(optionalCommand.isEmpty() || optionalDirectory.isEmpty()) {
                break;
            }

            String command = replacePlaceholders(optionalCommand.get(), revision);
            String directory = optionalDirectory.get();

            ProcessBuilder processBuilder = new ProcessBuilder();
            processBuilder.command(command.split("\\s(?=(?:[^\"]*([\"])[^\"]*\\1)*[^\"]*$)"));
            processBuilder.directory(new File(directory));
            run(processBuilder);
        }
    }

    public String replacePlaceholders(String input, int revision) {
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();

        for(String key : propertiesManager.getKeys()) {
            input = input.replaceAll("%" + key + "%", propertiesManager.getProperty(key).orElse(""));
        }

        String repositoryName = propertiesManager.getProperty("repository.name").orElse("");
        input = input.replaceAll("%filename%", repositoryName + "-" + revision);
        return input;
    }

}
