package de.unibremen.informatik.vcs2see;

import de.unibremen.informatik.st.libvcs4j.RevisionRange;
import de.unibremen.informatik.st.libvcs4j.VCSEngine;
import de.unibremen.informatik.st.libvcs4j.VCSEngineBuilder;

import java.io.IOException;
import java.util.Optional;

/**
 * Component which uses LibVCS4j to traverse the repository and run an analysis on each revision.
 *
 * @author Felix Gaebler
 * @version 1.0.0
 */
public class RepositoryCrawler {

    private VCSEngine engine;

    public void crawl() {
        PropertiesManager propertiesManager = Vcs2See.getPropertiesManager();
        String path = propertiesManager.getProperty("repository.path").orElseThrow();
        Type type = Type.valueOf(propertiesManager.getProperty("repository.type").orElseThrow());

        switch (type) {
            case GIT:
                engine = VCSEngineBuilder.ofGit(path).build();
                break;

            case HG:
                engine = VCSEngineBuilder.ofHG(path).build();
                break;

            case SVN:
                engine = VCSEngineBuilder.ofSVN(path).build();
                break;

            default:
                engine = VCSEngineBuilder.of(path).build();
                break;
        }

        propertiesManager.setProperty("repository.temp", engine.getOutput().toString());
    }

    public Optional<RevisionRange> nextRevision() throws IOException {
        return engine.next();
    }

    /**
     * Type of repository. All types supported by LibVCS4j.
     */
    public enum Type {
        GIT, HG, SVN;
    }

}
