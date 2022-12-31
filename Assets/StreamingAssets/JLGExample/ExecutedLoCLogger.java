import com.sun.jdi.*;
import com.sun.jdi.connect.Connector;
import com.sun.jdi.connect.LaunchingConnector;
import com.sun.jdi.event.*;
import com.sun.jdi.request.*;

import java.io.*;
import java.net.URL;
import java.util.*;

public class ExecutedLoCLogger {

    private String debugClassNameWithArgs;
    private String debugClassName;
    private List<String> classes = new ArrayList<>();
    private List<String> classesWithPaths = new ArrayList<>();
    private PrintWriter writer = new PrintWriter(System.out);
    Stack<ReferenceType> activeClasses = new Stack<ReferenceType>();
    List<String> methodLookUpTable = new ArrayList<>();
    List<String> fieldLookUpTable = new ArrayList<>();

    /**
     * !Important!: This Logger only tracks code for each .java file in the target Folder. If there are multiple
     * classes within one .java file only the "Main" class of the file will be tracked. If you want to make
     * sure every class of your project is tracked, give each class its own .java file.
     * This main method handles the Input (args) and prepares the debugging process.
     * Then it starts debugging with startLogging().
     * @param args Input for the Logger. Contains Main Class of project to log and other options.
     * @throws Exception
     */
    public static void main(String[] args){
        ExecutedLoCLogger logger = new ExecutedLoCLogger();
        int ix;
        for(ix = 0;ix < args.length; ++ix){
            String arg = args[ix];
            if (arg.charAt(0) != '-'){
                break;
            }else if(arg.equals("-output")){
                try{
                logger.setWriter(new PrintWriter(new FileWriter(args[++ix])));
                }catch (IOException e){
                    System.err.println("Cannot open File "+ args[ix]);
                    System.exit(1);
                }
            }else if (arg.equals("-help")){
                System.out.println("You choose help");
                help();
                System.exit(0);
            }else{
                System.out.println("Wrong entry: " + arg);
                help();
                System.exit(0);
            }
        }
        if (ix >= args.length){
            System.err.println("Target Class missing!");
            help();
            System.exit(1);
        }

        StringBuffer sb = new StringBuffer();
        sb.append(args[ix]);
        for(++ix; ix< args.length; ++ix){
            sb.append(' ');
            sb.append(args[ix]);
        }
        logger.setDebugClassNameWithArgs(sb.toString());
        logger.setDebugClassName(stripClassNameOfArgs(logger.getDebugClassNameWithArgs()));
        getAllClassesOfProject(logger);
        logger.writer.println("$"+logger.getClassesWithPaths());
        startLogging(logger);
    }

    /**
     * Main logging method. Launches and connects to the target VM.
     * Handles the eventqueue and starts methods based on the events in the queue.
     * @param logger
     */
    public static void startLogging(ExecutedLoCLogger logger){
        VirtualMachine vm = null;
        try {
            vm = logger.connectAndLaunchVM();
            logger.displayTargetOutput(vm.process().getInputStream());
            logger.displayTargetOutput(vm.process().getErrorStream());
            logger.forwardInputToTarget(vm.process().getOutputStream());
            logger.enableClassPrepareRequests(vm);
            logger.enableMethodEntryAndExitRequests(vm);
            EventSet es;
            while ((es = vm.eventQueue().remove()) != null){
                for (Event e : es){
                    if(e instanceof ClassPrepareEvent) {
                        logger.handleClassPrepareEvent(vm, (ClassPrepareEvent) e);
                    }
                    if(e instanceof ModificationWatchpointEvent){
                        logger.handleModificationWatchpointEvent((ModificationWatchpointEvent) e);
                    }
                    if(e instanceof MethodEntryEvent){
                        logger.handleMethodEntryEvent(vm, (MethodEntryEvent) e);
                    }
                    if(e instanceof MethodExitEvent){
                        logger.handleMethodExitEvent(vm, (MethodExitEvent) e);
                    }
                    if(e instanceof StepEvent){
                        logger.handleStepEvent((StepEvent) e);
                    }


                     vm.resume();
                }
            }

        }catch (VMDisconnectedException e){
            System.out.println("VM disconnected.");
            logger.printLookUpTable();
            logger.writer.close();
            System.exit(0);

        } catch (Exception e){
            e.printStackTrace();
        }
    }

    /**
     * Makes the write print out the LookUp tables.
     */
    private void printLookUpTable() {
        writer.print("*");
        for(int i = 0; i < methodLookUpTable.size(); i++){
            writer.print("-"+i+"="+methodLookUpTable.get(i)+";");
        }
        for(int i =0; i < fieldLookUpTable.size();i++){
            writer.print("#"+i+"="+fieldLookUpTable.get(i)+";");
        }
    }

    /**
     * Launches and connects to the VM of the given Main Class.
     */
    public VirtualMachine connectAndLaunchVM()throws Exception{
        LaunchingConnector lc = Bootstrap.virtualMachineManager().defaultConnector();
        Map<String, Connector.Argument> args = lc.defaultArguments();
        args.get("main").setValue(debugClassNameWithArgs);
        return lc.launch(args);
    }


    /**
     * Forwards the logged programs console output to this console.
     */
    public void displayTargetOutput(InputStream stream){
        Thread thread = new Thread("target Output reader"){
            @Override
            public void run(){
                try{
                    printStream(stream);
                }catch (IOException ex){
                    System.out.println("Failed reading Output");
                }
            }
        };
        thread.setPriority(Thread.MAX_PRIORITY-1);
        thread.start();
    }

    /**
     * Print method used in displayTargetOutput.
     */
    public void printStream(InputStream stream) throws IOException {
        BufferedReader in =
                new BufferedReader(new InputStreamReader(stream));
        int i;
        try {
            while ((i = in.read()) != -1) {
                System.out.print((char)i);
            }
        } catch (IOException ex) {
            String s = ex.getMessage();
            if (!s.startsWith("Bad file number")) {
                throw ex;
            }
        }
    }

    /**
     * Forwards this programs consoleinput to the targets console.
     */
    public void forwardInputToTarget(OutputStream stream){
        boolean running = true;
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(stream));
        Thread thread = new Thread("target input writer"){
            @Override
            public void run(){
                String input;
                while(running){
                        try{
                            input = reader.readLine();
                            writer.write(input);
                            writer.write(System.lineSeparator()); //this call terminates the current line & lets the reader of the target process know its over.
                            writer.flush();
                        }catch (IOException e){
                            System.out.println("Error reading input.");
                            e.printStackTrace();
                        }
                }
            }
        };
        thread.setPriority(Thread.MAX_PRIORITY-1);
        thread.start();
    }

    /**
     * Creates and enables one ClassPrepareRequest for each class of the target Java project.
     */
    public void enableClassPrepareRequests(VirtualMachine vm){
        EventRequestManager evm = vm.eventRequestManager();
        for(String name: classes){
            ClassPrepareRequest cpr = evm.createClassPrepareRequest();
            cpr.addClassFilter(name);
            cpr.enable();
        }
    }

    /**
     * Creates and enables MethodEntry- and MethodExitRequests for all classes of the target Java project.
     */
    public void enableMethodEntryAndExitRequests(VirtualMachine vm){
        EventRequestManager evm = vm.eventRequestManager();
        for(String name: classes){
            MethodEntryRequest mer = evm.createMethodEntryRequest();
            MethodExitRequest mxr = evm.createMethodExitRequest();
            mer.addClassFilter(name);
            mxr.addClassFilter(name);
            mxr.enable();
            mer.enable();
        }
    }

    /**
     * Creates and enables a ModificationWatchpointRequest for a given Field.
     * No classfilter, because modifications from all classes of the field are important.
     * @param field The field the Request is created for.
     */
    public void enableModificationWatchpointRequests(VirtualMachine vm, Field field){
        EventRequestManager evm = vm.eventRequestManager();
        ModificationWatchpointRequest mwr = evm.createModificationWatchpointRequest(field);
        AccessWatchpointRequest awr = evm.createAccessWatchpointRequest(field);
        awr.enable();
        mwr.enable();
    }

    /**
     * Creates and enables a StepRequest for a Class of a MethodEntryEvent.
     */
    public void enableStepRequest(VirtualMachine vm, MethodEntryEvent event){
        StepRequest sq = vm.eventRequestManager()
                .createStepRequest(event.thread(), StepRequest.STEP_LINE,StepRequest.STEP_INTO);
        sq.addClassFilter(event.method().declaringType().name());
        sq.enable();
    }

    /**
     * Creates and enables a StepRequest based on a MethodExitEvent and a class to filter for.
     */
    public void enableStepRequest(VirtualMachine vm, MethodExitEvent event, ReferenceType classType){
        StepRequest sq = vm.eventRequestManager()
                .createStepRequest(event.thread(), StepRequest.STEP_LINE,StepRequest.STEP_INTO);
        sq.addClassFilter(classType);
        sq.enable();
    }

    /**
     * If a ClassPrepareEvent is thrown, this method calls enableModificationWatchpointRequests for all Fields
     * of the class in the event.
     */
    private void handleClassPrepareEvent(VirtualMachine vm, ClassPrepareEvent e){
        List<Field> fields = e.referenceType().fields();
        if(!fields.isEmpty()){
            for(Field field : fields){
                enableModificationWatchpointRequests(vm, field);
            }
        }
    }

    /**
     * This Method handles Method Entry Events by deleting the previous step request and adding a new one for the class
     * of the method of the event. Also it adds the class to the active classes stack and prints the local variables.
     * Also it adds the methods location to the methodLookUpTable, if it is not in there already.
     */
    private void handleMethodEntryEvent(VirtualMachine vm, MethodEntryEvent e)
            throws AbsentInformationException{
        vm.eventRequestManager().deleteEventRequests(vm.eventRequestManager().stepRequests());
        if(!methodLookUpTable.contains(e.method().toString())){
            methodLookUpTable.add(e.method().toString());
        }
        writer.println("-/"+methodLookUpTable.indexOf(e.method().toString())+">"+e.location().lineNumber());
        try{
        StackFrame sf = e.thread().frame(0);
        printLocalVariables(sf);
        }catch (IncompatibleThreadStateException ex){
            //ignore this exception and let it happen. Comes up every time because MethodEntryEvent always happens at
            //the same time as a StepEvent and they both try to access the same StackFrame.
        }
        activeClasses.push(e.method().declaringType());
        enableStepRequest(vm, e);
    }

    /**
     * This method executes when a MethodExitEvent happens. It removes the previous step request and the corresponding
     * element on the active classes stack. Then it prints the return value of the method and creates a new step
     * request for the new top element of the active classes stack.
     */
    private void handleMethodExitEvent(VirtualMachine vm, MethodExitEvent e){
        vm.eventRequestManager().deleteEventRequests(vm.eventRequestManager().stepRequests());
        activeClasses.pop();
        writer.println("/-"+methodLookUpTable.indexOf(e.method().toString())+">"+e.location().lineNumber());
        writer.println("=>"+e.returnValue());
        if(!activeClasses.empty()){
        enableStepRequest(vm, e, activeClasses.peek());
        }
    }

    /**
     * This Methode handles Step Events. It prints their Location and local Variables. Further it adds the location
     * of the method to the methodLookUpTable, if it is not in there already.
     */
    public void handleStepEvent(StepEvent event) throws AbsentInformationException {
        if (!methodLookUpTable.contains(event.location().method().toString())) {
            methodLookUpTable.add(event.location().method().toString());
        }
            writer.println("-"+
                    methodLookUpTable.indexOf(event.location().method().toString()) + ">" + event.location().lineNumber());
        try{
            StackFrame sf = event.thread().frame(0);
                printLocalVariables(sf);
        } catch (IncompatibleThreadStateException e) {
            //ignore this Exception since it always occurs when a new method is entered, because MethodEntryEvent and
            //StepEvent share a frame and happen at the same time, so only one can access the frame.
        }
    }

    /**
     * This Method handles ModificationWatchpointEvents. When such an Event happens it prints out the Fields name and
     * the value its changing to with the writer.
     * @param e
     * @throws ClassNotLoadedException
     */
    private void handleModificationWatchpointEvent(ModificationWatchpointEvent e) throws ClassNotLoadedException{
        if(!fieldLookUpTable.contains(e.field().name())){
            fieldLookUpTable.add(e.field().name());
        }
        int fieldInt = fieldLookUpTable.indexOf(e.field().name());
        if(e.field().type() instanceof PrimitiveType || e.valueToBe() instanceof StringReference){
            writer.println("#"+fieldInt+"="+e.valueToBe().toString());
        }else if(e.valueToBe() instanceof ArrayReference){
            writer.println("#"+ fieldInt +"="+((ArrayReference) e.valueToBe()).getValues());
        }else if(e.field().type() instanceof ReferenceType) {
            writer.println("#"+fieldInt+"="+ e.valueToBe().type());
        }
    }

    /**
     * This method gets All Classes of a Project this ExecutedLoCLogger is used in. Therefore it uses the recursive
     * scanDirectoryForFiles method.
     */
    private static void getAllClassesOfProject(ExecutedLoCLogger logger){
        ClassLoader loader = ExecutedLoCLogger.class.getClassLoader();
        URL root = loader.getResource("");
        File srcfolder = new File(root.getPath());
        scanDirectoryForFiles(srcfolder, logger);
    }

    /**
     * Prints Local Variables with this classes writer, that are visible in a given StackFrame.
     */
    private void printLocalVariables(StackFrame sf) throws AbsentInformationException {
        try{
        Map<LocalVariable, Value> localVariables = sf.getValues(sf.visibleVariables());
        if (!localVariables.entrySet().isEmpty()) {
            for (Map.Entry<LocalVariable, Value> entry : localVariables.entrySet()) {
                if (entry.getValue() instanceof ArrayReference) {
                    writer.println(entry.getKey().name() + "=" + ((ArrayReference) entry.getValue()).getValues());
                } else if (entry.getValue() instanceof StringReference) {
                    writer.println(entry.getKey().name() + "=" + entry.getValue());
                } else if (entry.getValue() instanceof ObjectReference) {
                    writer.println(entry.getKey().name() + "=" + (((ObjectReference) entry.getValue()).referenceType().name()));
                } else {
                    writer.println(entry.getKey().name() + "=" + entry.getValue());
                }
            }
        }
        }catch (InvalidStackFrameException e){
            //let this error happen.
        }
    }

    public static void scanDirectoryForFiles(File file, ExecutedLoCLogger logger, String directory){
        if(file.isDirectory()){
            String dir = directory+file.getName()+".";
            for (File f: file.listFiles()) {
                scanDirectoryForFiles(f, logger, dir);
            }
        }else if(file.getName().contains(".java")&&!file.getName().contains("ExecutedLoCLogger")){
            logger.classes.add(directory+file.getName().replaceFirst(".java",""));
            logger.classesWithPaths.add(file.getPath());
        }
    }

    /**
     * First scan Directory call. This ignores the top source Folder, in which the project ist located. If File is
     * directory it calls the other scanDirectoryForFiles method, that adds the folder name to the file name,
     * where the file is located. Method Recursively finds all Files that end with .java.
     * @param file This is the Folder the loggerfile is located in.
     */
    public static void scanDirectoryForFiles(File file, ExecutedLoCLogger logger){
        if(file.isDirectory()){
            for (File f: file.listFiles()) {
                scanDirectoryForFiles(f, logger, "");
            }
        }else if(file.getName().endsWith(".java")&&!file.getName().contains("ExecutedLoCLogger")){
            logger.classes.add(file.getName().replaceFirst(".java",""));
            logger.classesWithPaths.add(file.getPath());
        }
    }

    public static String stripClassNameOfArgs(String nameWithArgs){
        return nameWithArgs.split(" ")[0];
    }

    public static void help(){
        System.err.println("Usage: java ExecutedLoCLogger <classname> <args>");
    }

    public String getDebugClassName() {
        return debugClassName;
    }

    public void setDebugClassName(String debugClassName) {
        this.debugClassName = debugClassName;
    }

    public String getDebugClassNameWithArgs() {
        return debugClassNameWithArgs;
    }

    public void setDebugClassNameWithArgs(String debugClassNameWithArgs) {
        this.debugClassNameWithArgs = debugClassNameWithArgs;
    }

    public List<String> getClasses() {
        return classes;
    }

    public void setClasses(List<String> classes) {
        this.classes = classes;
    }

    public PrintWriter getWriter() {
        return writer;
    }

    public void setWriter(PrintWriter writer) {
        this.writer = writer;
    }

    public List<String> getClassesWithPaths() {
        return classesWithPaths;
    }

    public void setClassesWithPaths(List<String> classesWithPaths) {
        this.classesWithPaths = classesWithPaths;
    }

}
