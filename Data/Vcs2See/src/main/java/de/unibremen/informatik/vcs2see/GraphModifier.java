package de.unibremen.informatik.vcs2see;

import de.unibremen.informatik.st.libvcs4j.Commit;
import de.unibremen.informatik.st.libvcs4j.FileChange;
import de.unibremen.informatik.st.libvcs4j.RevisionRange;
import de.unibremen.informatik.st.libvcs4j.VCSFile;
import net.sourceforge.gxl.GXLDocument;
import net.sourceforge.gxl.GXLElement;
import net.sourceforge.gxl.GXLGraph;
import net.sourceforge.gxl.GXLNode;
import net.sourceforge.gxl.GXLString;
import org.xml.sax.SAXException;

import java.io.File;
import java.io.IOException;
import java.util.Deque;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.Map;

/**
 * Component which can modify the graph of the GXL file.
 *
 * @author Felix Gaebler
 * @version 1.0.0
 */
public class GraphModifier {

    private File file;

    private GXLDocument document;

    private Map<String, GXLNode> nodes;

    private final Deque<String> mostRecent;

    private final Map<String, Integer> mostFrequent;

    public GraphModifier() {
        this.mostFrequent = new HashMap<>();
        this.mostRecent = new LinkedList<>();
    }

    public void process(RevisionRange revisionRange) throws IOException, SAXException {
        // Get GXL file of revision
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();
        String path = propertiesManager.getProperty("modifier.path").orElseThrow();
        path = Vcs2See.getCodeAnalyser().replacePlaceholders(path, revisionRange.getOrdinal());

        this.file = new File(path);
        this.document = new GXLDocument(file);

        loadNodes();
        for(Commit commit : revisionRange.getCommits()) {
            loadCommit(commit);
        }
    }

    private void loadNodes() {
        ConsoleManager consoleManager = Vcs2See.getConsoleManager();
        consoleManager.print("Nodes:");

        GXLGraph graph = document.getDocumentElement().getGraphAt(0);
        for(int i = 0; i < graph.getGraphElementCount(); i++) {
            GXLElement element = graph.getGraphElementAt(i);

            if(element instanceof GXLNode) {
                GXLNode node = (GXLNode) element;
                if(!node.getType().getURI().toString().equals("File")) {
                    continue;
                }

                GXLString reference = (GXLString) node.getAttr("Linkage.Name").getValue();
                consoleManager.print("- " + reference.getValue());
                nodes.put(reference.getValue(), node);
            }
        }
    }

    private void loadCommit(Commit commit) {
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();
        String basePath = propertiesManager.getProperty("project.base").orElseThrow();

        ConsoleManager consoleManager = Vcs2See.getConsoleManager();
        consoleManager.print("Changes:");

        for (FileChange fileChange : commit.getFileChanges()) {
            // Get the newest file, fallback to old file when type is ADD, null should never occur
            VCSFile file = fileChange.getNewFile().orElse(fileChange.getOldFile().orElse(null));
            if (file == null) {
                System.err.println("No file");
                continue;
            }

            String path = file.getRelativePath()
                    .replace('\\', '/')
                    .replaceAll(basePath, "");
            consoleManager.print("- " + path);
        }
    }

}
