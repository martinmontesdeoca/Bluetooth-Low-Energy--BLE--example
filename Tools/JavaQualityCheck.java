import com.sun.source.doctree.DocCommentTree;
import com.sun.source.tree.ClassTree;
import com.sun.source.tree.CompilationUnitTree;
import com.sun.source.tree.MethodTree;
import com.sun.source.util.DocTrees;
import com.sun.source.util.JavacTask;
import com.sun.source.util.TreePathScanner;
import java.io.File;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import javax.tools.JavaCompiler;
import javax.tools.StandardJavaFileManager;
import javax.tools.ToolProvider;

/** Uses the JDK syntax tree to check Java documentation and type sizes without Android dependencies. */
public final class JavaQualityCheck {
    /** Parses repository Java sources and returns a failing exit code for undocumented or oversized declarations. */
    public static void main(String[] args) throws Exception {
        File root = new File(args.length == 0 ? "." : args[0]).getCanonicalFile();
        List<File> files = new ArrayList<>();
        collect(new File(root, "Native/AndroidBle/src"), files);
        files.add(new File(root, "Tools/JavaQualityCheck.java"));
        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();
        int[] counts = new int[3];
        boolean inventory = Arrays.asList(args).contains("--inventory");
        try (StandardJavaFileManager manager = compiler.getStandardFileManager(null, null, null)) {
            JavacTask task = (JavacTask) compiler.getTask(null, manager, null,
                    Arrays.asList("-proc:none", "-encoding", "UTF-8"), null, manager.getJavaFileObjectsFromFiles(files));
            Iterable<? extends CompilationUnitTree> units = task.parse();
            DocTrees docs = DocTrees.instance(task);
            for (CompilationUnitTree unit : units) {
                String path = root.toPath().relativize(new File(unit.getSourceFile().toUri()).toPath())
                        .toString().replace('\\', '/');
                new TreePathScanner<Void, Void>() {
                    /** Counts every named or anonymous class from declaration to closing brace. */
                    @Override public Void visitClass(ClassTree tree, Void unused) {
                        long start = docs.getSourcePositions().getStartPosition(unit, tree);
                        long end = docs.getSourcePositions().getEndPosition(unit, tree);
                        long lines = unit.getLineMap().getLineNumber(end - 1) - unit.getLineMap().getLineNumber(start) + 1;
                        if (lines > 300) {
                            System.err.println(path + ": " + tree.getSimpleName() + " has " + lines + " lines.");
                            counts[2]++;
                        }
                        counts[0]++;
                        return super.visitClass(tree, unused);
                    }

                    /** Requires a meaningful Javadoc body for constructors, interface methods and overrides. */
                    @Override public Void visitMethod(MethodTree tree, Void unused) {
                        long start = docs.getSourcePositions().getStartPosition(unit, tree);
                        long line = unit.getLineMap().getLineNumber(start);
                        DocCommentTree comment = docs.getDocCommentTree(getCurrentPath());
                        if (inventory) System.out.println(path + "\t" + line + "\t" + tree.getName());
                        else if (comment == null || comment.getFullBody().isEmpty()) {
                            System.err.println(path + ":" + line + ": missing Javadoc: " + tree.getName());
                            counts[2]++;
                        }
                        counts[1]++;
                        return super.visitMethod(tree, unused);
                    }
                }.scan(unit, null);
            }
        }
        if (!inventory) System.out.println("Java: " + counts[0] + " types, " + counts[1] + " methods; " + counts[2] + " issues.");
        if (counts[2] != 0) System.exit(1);
    }

    /** Collects authored Java sources recursively without relying on shell globs. */
    private static void collect(File directory, List<File> files) {
        File[] children = directory.listFiles();
        if (children == null) throw new IllegalArgumentException("Missing source directory: " + directory);
        for (File child : children) {
            if (child.isDirectory()) collect(child, files);
            else if (child.getName().endsWith(".java")) files.add(child);
        }
    }
}
