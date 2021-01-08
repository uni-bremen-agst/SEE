PROJECT=TestProjectToDebug
TOOLSJAR=/snap/vlc/1620/usr/lib/jvm/java-8-openjdk-amd64/lib/tools.jar

all: compile $(PROJECT).jlg $(PROJECT).gxl

compile:
	find . -name "*.java"|xargs javac -g -cp $(TOOLSJAR)

$(PROJECT).jlg:
	java -cp $(TOOLSJAR):. ExecutedLoCLogger -output $(PROJECT).jlg Main

$(PROJECT).gxl: $(PROJECT).rfg
	rfgexport -f GXL --view "Code Facts" $< $@

$(PROJECT).rfg:
	find . -name "*.java"|xargs java2rfg -rfg $@ -8

clean:
	rm -f $(PROJECT).rfg $(PROJECT).jlg *.log *~
	find . -name "*.class" | xargs rm -f

