package de.unibremen.informatik.vcs2see;

import de.unibremen.informatik.st.libvcs4j.RevisionRange;
import lombok.Getter;

import java.io.IOException;
import java.util.HashSet;
import java.util.Optional;
import java.util.Set;

/**
 * Vsc2See.
 * Prepares a folder with GLX files for SEE to visualize a version control history.
 *
 * @author Felix Gaebler
 * @version 1.0.0
 */
public class Vcs2See {

    @Getter
    private static ConsoleManager consoleManager;

    @Getter
    private static PropertiesManager propertiesManager;

    @Getter
    private static CodeAnalyser codeAnalyser;

    @Getter
    private static RepositoryCrawler repositoryCrawler;

    @Getter
    private static GraphModifier graphModifier;

    /**
     * Constructor of the main class. Initializes all components.
     * @throws IOException exception
     */
    public Vcs2See() throws IOException {
        propertiesManager = new PropertiesManager();
        propertiesManager.loadProperties();

        consoleManager = new ConsoleManager();
        consoleManager.printWelcome();
        consoleManager.printSeparator();

        codeAnalyser = new CodeAnalyser();

        repositoryCrawler = new RepositoryCrawler();

        graphModifier = new GraphModifier();
    }

    /**
     * Runs the setup in the console. All settings are queried.
     * @throws IOException exception
     */
    private void setup() throws IOException {
        consoleManager.print("SETUP");
        consoleManager.printSeparator();
        consoleManager.print("You can set a new value or accept the current one by \npressing <Enter>. To skip the setup you can set the start \nargument \"-Dci=true\". In CI mode no manual interactions \nare necessary.");
        consoleManager.printSeparator();

        setupEnvironment();
        setupRepository();
        setupBase();
        setupAnalysis();

        propertiesManager.saveProperties();
    }

    /**
     * Runs environment specific setup.
     * @throws IOException exception
     */
    private void setupEnvironment() throws IOException {
        consoleManager.print("SETUP - ENVIRONMENT");
        consoleManager.printSeparator();
        read("environment.bauhaus");
        read("environment.cpfcsv2rfg");
        consoleManager.printSeparator();
    }

    /**
     * Runs repository specific setup.
     * @throws IOException exception
     */
    private void setupRepository() throws IOException {
        consoleManager.print("SETUP - REPOSITORY");
        consoleManager.printSeparator();
        read("repository.name");
        read("repository.path");
        read("repository.language");
        read("repository.type");
        consoleManager.printSeparator();
    }

    /**
     * Runs project specific setup.
     * @throws IOException exception
     */
    private void setupBase() throws IOException {
        consoleManager.print("SETUP - BASE");
        consoleManager.printSeparator();
        read("project.base");//TODO: CHECK PATHS
        consoleManager.printSeparator();
    }

    /**
     * Runs analysis specific setup.
     * @throws IOException exception
     */
    private void setupAnalysis() throws IOException {
        consoleManager.print("SETUP - ANALYSIS");
        consoleManager.printSeparator();
        consoleManager.print("Current value: ");

        // Find existing commands
        Set<String> keys = new HashSet<>();
        for(int i = 0; true; i++) {
            String key = "analyser." + i + ".";
            Optional<String> optionalCommand = propertiesManager.getProperty(key + "command");
            Optional<String> optionalDirectory = propertiesManager.getProperty(key + "directory");
            if(optionalCommand.isEmpty() || optionalDirectory.isEmpty()) {
                break;
            }

            consoleManager.print(key + "command=" + codeAnalyser.replacePlaceholders(optionalCommand.get(), 1));
            consoleManager.print(key + "directory=" + codeAnalyser.replacePlaceholders(optionalDirectory.get(), 1));

            keys.add(key + "command");
            keys.add(key + "directory");
        }

        consoleManager.printSeparator();

        // Amount of analysis commands
        consoleManager.print("Number of commands needed for the analysis. The commands are \nqueried afterwards. Press <Enter> to accept the existing \ncommands. The following placeholders can be used: \n%filename% - resolves the file name (e.g. example-1)\n%extensions% - resolves file extensions -i parameter\n%repository.temp% - resolves the temporary repository path\nAll values from the configuration file can also be used as \nplaceholders (e.g. %environment.bauhaus%)");
        Integer commands = null;
        do {
            String line = consoleManager.readLine("commands=");
            if(line.isBlank()) {
                break;
            }

            try {
                commands = Integer.parseInt(consoleManager.readLine("commands="));
            } catch (NumberFormatException ignored) {
                consoleManager.print("The value must be a number.");
            }
        } while (commands == null);
        consoleManager.printSeparator();

        // Apply old commands
        if(commands == null) {
            return;
        }

        // Remove existing analyser commands
        for(String key : keys) {
            propertiesManager.removeProperty(key);
        }

        // Query analysis commands one by one
        for(int i = 0; i < commands; i++) {
            read("analyser." + i + ".directory");
            read("analyser." + i + ".command");
            consoleManager.printSeparator();
        }
    }

    /**
     * Helper method to read from command line or apply default value.
     * @param key properties file key
     * @throws IOException exception
     */
    private void read(String key) throws IOException {
        Optional<String> value = propertiesManager.getProperty(key);
        consoleManager.print("Current value: " + value.orElse("<empty>"));
        String newValue = consoleManager.readLine(key + "=");
        if(!newValue.isBlank()) {
            propertiesManager.setProperty(key, newValue);
        }
    }

    /**
     * The entry point of the application.
     * Perform steps of the sequence one by one.
     *
     * @param args ignored
     * @throws IOException exception
     */
    public static void main(String[] args) throws IOException {
        Vcs2See vcs2See = new Vcs2See();

        // Check if CI-Mode argument is set
        if(!Boolean.parseBoolean(System.getProperty("ci", "false"))) {
            vcs2See.setup();
        } else {
            consoleManager.print("SETUP\nProgram was started in CI mode. The setup is skipped and \nsettings are read from the file. No manual intervention \nis necessary.");
            consoleManager.printSeparator();
        }

        // Initialize the repository crawler
        repositoryCrawler.crawl();

        // Load first revision before preparing, else clone fails because of non empty directory
        Optional<RevisionRange> optional = repositoryCrawler.nextRevision();

        // Prepare analysis
        codeAnalyser.prepare();
        consoleManager.printSeparator();

        // Go through all revisions
        int i = 1;
        do {
            consoleManager.print("CRAWLING - " + i);
            consoleManager.printSeparator();
            codeAnalyser.analyse(i++);

            RevisionRange revisionRange = optional.get();
            graphModifier.process(revisionRange);
            consoleManager.printSeparator();
        } while ((optional = repositoryCrawler.nextRevision()).isPresent());

        // Postprocess analysis
        codeAnalyser.postprocess();
        consoleManager.printSeparator();

        consoleManager.print("Program finished.");
        consoleManager.printSeparator();
    }

}
