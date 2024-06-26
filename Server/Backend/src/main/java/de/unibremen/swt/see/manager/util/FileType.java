package de.unibremen.swt.see.manager.util;

/**
 * Represents the different types of files that can be uploaded to the
 * application.
 * <p>
 * This {@code enum} defines the file types that are used by the SEE game server
 * to configure multiplayer scenarios and Code Cities.
 * 
 * @see de.unibremen.swt.see.manager.service.FileService#createFile(Server, FileType, MultipartFile)
 */
public enum FileType {

    /**
     * Represents a SEE Code City configuration.
     * <p>
     * A {@code CFG} file with the configuration of a SEE Code City setup.
     */
    CFG,
    
    /**
     * Represents a Data Provider for a SEE Code City.
     * <p>
     * A {@code CSV} file with the data for a SEE Code City setup.
     */
    CSV,
    
    /**
     * Represents a Data Provider for a SEE Code City.
     * <p>
     * A {@code GXL} file with the data for a SEE Code City setup.
     */
    GXL,
    
    /**
     * Represents the source code for a SEE Code City.
     * <p>
     * A {@code ZIP} file with the source code files that are linked to a
     * Code City displayed in SEE.
     */
    SOURCE,
    
    /**
     * Represents a Visual Studio Solution.
     * <p>
     * Visual Studio can be connected to SEE using a Visual Studio Solution –
     * a container for one or more projects.
     * 
     * @see <a href="https://learn.microsoft.com/en-us/visualstudio/ide/solutions-and-projects-in-visual-studio?">What are Visual Studio solutions &amp; projects?</a>
     */
    SOLUTION,
}
