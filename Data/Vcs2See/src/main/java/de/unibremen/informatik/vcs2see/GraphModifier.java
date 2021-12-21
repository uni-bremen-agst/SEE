package de.unibremen.informatik.vcs2see;

import de.unibremen.informatik.st.libvcs4j.Commit;
import de.unibremen.informatik.st.libvcs4j.FileChange;
import de.unibremen.informatik.st.libvcs4j.LineChange;
import de.unibremen.informatik.st.libvcs4j.VCSFile;
import net.sourceforge.gxl.*;
import org.xml.sax.SAXException;

import java.io.File;
import java.io.IOException;
import java.time.format.DateTimeFormatter;
import java.util.*;
import java.util.stream.Collectors;

/**
 * Component which can modify the graph of the GXL file.
 *
 * @author Felix Gaebler
 * @version 1.0.0
 */
public class GraphModifier {

    private GXLDocument document;

    private final Map<String, GXLNode> nodes;

    private final Deque<String> mostRecent;

    private final Map<String, Integer> mostFrequent;

    public GraphModifier() {
        this.nodes = new HashMap<>();
        this.mostFrequent = new LinkedHashMap<>();
        this.mostRecent = new LinkedList<>();
    }

    /**
     * Processes single commit into graph.
     * @param commit commit to extract information from
     * @param index ordinal of commit
     * @throws IOException exception
     * @throws SAXException exception
     */
    public void process(Commit commit, int index) throws IOException, SAXException {
        // Get GXL file of revision
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();
        String path = propertiesManager.getProperty("modifier.path").orElseThrow();
        path = Vcs2See.getCodeAnalyser().replacePlaceholders(path, index);

        File file = new File(path);
        document = new GXLDocument(file);

        loadNodes();
        loadCommit(commit);
        populateNodes();
        addCommitGraph(commit);

        document.write(file);
        System.gc();
    }

    /**
     * Loads Nodes from GXL file.
     */
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

    /**
     * Loads commit from VCSLib4j.
     * @param commit commit
     * @throws IOException exception
     */
    private void loadCommit(Commit commit) throws IOException {
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();
        String basePath = propertiesManager.getProperty("project.base").orElseThrow();
        CodeAnalyser.Language language = CodeAnalyser.Language.valueOf(propertiesManager.getProperty("repository.language").orElseThrow());

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
            if(!path.matches(language.regex())) {
                continue;
            }

            // Calculate and add line changes.
            if(nodes.containsKey(path)) {
                List<LineChange> lineChanges = fileChange.computeDiff();

                List<Integer> insertedLines = lineChanges.stream()
                        .filter(lineChange -> lineChange.getType() == LineChange.Type.INSERT)
                        .map(LineChange::getLine)
                        .collect(Collectors.toList());
                List<Integer> deletedLines = lineChanges.stream()
                        .filter(lineChange -> lineChange.getType() == LineChange.Type.DELETE)
                        .map(LineChange::getLine)
                        .collect(Collectors.toList());
                List<Integer> editedLines = insertedLines.stream()
                        .filter(deletedLines::contains)
                        .collect(Collectors.toList());
                insertedLines.removeAll(editedLines);
                deletedLines.removeAll(editedLines);

                GXLNode node = nodes.get(path);
                node.setAttr("Metric.Vcs2See.Commit.Line_Changes", new GXLInt(lineChanges.size()));
                node.setAttr("Metric.Vcs2See.Commit.Lines_Added", new GXLInt(insertedLines.size()));
                node.setAttr("Metric.Vcs2See.Commit.Lines_Edited", new GXLInt(editedLines.size()));
                node.setAttr("Metric.Vcs2See.Commit.Lines_Deleted", new GXLInt(deletedLines.size()));

                consoleManager.print("- " + path);
            } else {
                System.err.println("- " + path);
                System.exit(0);
            }

            // Calculate most recent changes
            addMostRecent(path);

            // Calculate most frequent changes
            addMostFrequent(path);
        }
    }

    /**
     * Populates GXL Nodes with queried information.
     */
    private void populateNodes() {
        List<String> list = (LinkedList<String>) mostRecent;
        for(int i = 0; i < list.size(); i++) {
            String path = list.get(i);
            if(nodes.containsKey(path)) {
                int frequent = Math.round(calculateMostFrequent(mostFrequent.getOrDefault(path, 0)));

                GXLNode node = nodes.get(path);
                node.setAttr("Metric.Vcs2See.Most_Recent_Edit", new GXLInt(calculateMostRecent(list.size(), i)));
                node.setAttr("Metric.Vcs2See.Most_Frequent_Edit", new GXLInt(frequent));
            }
        }
    }

    /**
     * Adds commit attributes to current graph.
     * @param commit commit
     */
    private void addCommitGraph(Commit commit) {
        GXLGraph codeGraph = document.getDocumentElement().getGraphAt(0);
        codeGraph.setAttr("CommitId", new GXLString(commit.getId()));
        codeGraph.setAttr("CommitAuthor", new GXLString(commit.getAuthor()));
        codeGraph.setAttr("CommitMessage", new GXLString(commit.getMessage()));
        codeGraph.setAttr("CommitTimestamp", new GXLString(commit.getDateTime().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME)));
    }

    private void addMostRecent(String path) {
        mostRecent.remove(path);
        mostRecent.addFirst(path);

        if(mostRecent.size() > 255) {
            mostRecent.removeLast();
        }
    }

    private void addMostFrequent(String path) {
        if(mostFrequent.computeIfPresent(path, (k, v) -> v + 1) == null) {
            mostFrequent.put(path, 1);
        }
    }

    /**
     * Calculates most recent value from collected information.
     * @param size size of the list
     * @param index index of the calculated element
     * @return most recent value
     */
    private int calculateMostRecent(int size, int index) {
        int step = 255 / Math.max(size - 1, 1);
        return 255 - (step  * index);
    }

    /**
     * Calculates most frequent value from collected information.
     * @param value value of current node
     * @return most frequent value
     */
    private int calculateMostFrequent(int value) {
        float min = 0; //mostFrequent.values().stream().min(Integer::compareTo).orElse(0);
        float max = mostFrequent.values().stream().max(Integer::compareTo).orElse(255);
        float output = (255 / (max - min)) * (value - min);
        return (int) output;
    }

}
