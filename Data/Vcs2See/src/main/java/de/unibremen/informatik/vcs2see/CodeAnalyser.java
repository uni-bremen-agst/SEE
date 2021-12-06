package de.unibremen.informatik.vcs2see;

import lombok.Getter;

import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.Arrays;
import java.util.Optional;
import java.util.stream.Collectors;

/**
 * Component to run baushaus on path.
 *
 * @author Felix Gaebler
 * @version 1.0.0
 */
public class CodeAnalyser {

    /**
     * Helper method to run a ProcessBuilder and output the output to the console.
     * @param command command which should be executed
     * @param directory where the command should be executed
     * @throws IOException exception
     */
    private void run(String command, String directory) throws IOException {
        ProcessBuilder processBuilder = new ProcessBuilder();
        processBuilder.command(command.split("\\s(?=(?:[^\"]*([\"])[^\"]*\\1)*[^\"]*$)"));
        processBuilder.directory(new File(directory));

        System.out.println(String.join(" ", processBuilder.command()));

        if(!processBuilder.directory().exists() && !processBuilder.directory().mkdirs()) {
            throw new IOException("Could not create directory");
        }

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

        String beforeCommand = propertiesManager.getProperty("analyser.before.command").orElseThrow();
        String beforeDirectory = propertiesManager.getProperty("analyser.before.command").orElseThrow();
        run(beforeCommand, beforeDirectory);

        for(int i = 0; true; i++) {
            String key = "analyser." + i + ".";
            Optional<String> optionalCommand = propertiesManager.getProperty(key + "command");
            Optional<String> optionalDirectory = propertiesManager.getProperty(key + "directory");
            if(optionalCommand.isEmpty() || optionalDirectory.isEmpty()) {
                break;
            }

            String command = replacePlaceholders(optionalCommand.get(), revision);
            String directory = replacePlaceholders(optionalDirectory.get(), revision);
            run(command, directory);
        }

        String afterCommand = propertiesManager.getProperty("analyser.after.command").orElseThrow();
        String afterDirectory = propertiesManager.getProperty("analyser.after.command").orElseThrow();
        run(afterCommand, afterDirectory);
    }

    public String replacePlaceholders(String input, int revision) {
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();

        for(String key : propertiesManager.getKeys()) {
            input = input.replaceAll("%" + key + "%", propertiesManager.getProperty(key).orElse(""));
        }

        // Collect language extensions
        String language = propertiesManager.getProperty("repository.language").orElseThrow();
        String extensions = Arrays.stream(Language.valueOf(language).getExtensions())
                .map(extension -> "-i \"*." + extension + "\"")
                .collect(Collectors.joining(" "));

        String repositoryName = propertiesManager.getProperty("repository.name").orElse("");
        input = input.replaceAll("%filename%", repositoryName + "-" + revision);
        input = input.replaceAll("%extensions%", extensions);
        return input;
    }

    /**
     * Enum of supported programming languages of Bauhaus including the file extensions of this programming language.
     */
    public enum Language {
        C("i", "c", "h"),
        CPP("ii", "cpp", "cxx", "c++", "cc", "tcc", "hpp", "hxx", "h++", "hh", "C", "H", "inl", "preinc"),
        CS("cs"),
        ADA("adb", "ads", "ada"),
        JAVA("java");

        @Getter
        private final String[] extensions;

        Language(String... extensions) {
            this.extensions = extensions;
        }

        public String regex() {
            return ".*\\" + Arrays.stream(extensions)
                    .map(extension -> "." + extension)
                    .collect(Collectors.joining("|"));
        }
    }

}
