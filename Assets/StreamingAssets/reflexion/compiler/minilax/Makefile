CC = cafeCC -B.

CFLAGS = -I. 
OBJECTS = macros.o error.o xmem.o argument.o symtable.o constab.o \
	  scanner.o parser.o prntree.o semantic.o typechk.o \
	  codegen.o threeadr.o codelist.o minilax.o

.PHONY: ALL targets clones

ALL: targets minilax.rfg clones

minilax: $(OBJECTS)
	$(CC)  $(CFLAGS) $(OBJECTS) -o minilax

minilax_for_processline: minilax
	-ir2rfg minilax minilax_initial.rfg	

minilax.rfg:	minilax
	-ir2rfg minilax minilax.rfg
	gxl_import --gxl_file Architecture.gxl --view=Architecture --graph minilax.rfg minilax.rfg 
	gxl_import --gxl_file Mapping.gxl --view=Mapping --graph minilax.rfg minilax.rfg
	@echo " ************************************************** "
	@echo " You can now start 'gravis minilax.rfg'. "
	@echo " ************************************************** "

clones: minilax.irclones.gxl minilax.clones.csv

minilax.irclones.gxl: minilax
	@echo "Clone detection using irclones."
	-irclones --output_gxl minilax.irclones.gxl minilax

minilax.clones.csv:
	@echo "Clone detection using cpf."
	cpf --min_tokens 180 --output_csv minilax.clones.csv --output_gxl minilax.clones.gxl --include "*.[ch]" .

depend:
	gcc -MM *.c > Makefile.depend

%.o: %.c
	$(CC) -c $(CFLAGS) -o $@ $<

clean: cleancompile
	-$(RM) *~
	-$(RM) *.rfg
	-$(RM) *.cfg
	-$(RM) *.gml
	-$(RM) *.saved
	-$(RM) *.rsf
	-$(RM) *.csv
	-$(RM) *.preprocessed
	-$(RM) minilax.irclones.gxl

cleancompile:
	-$(RM) *.o
	-$(RM) minilax*.log
	-$(RM) minilax

backup:
	zip minilax_$('date +%y%m%d') * -x *.o *~ *.zip CBAM *.cbam minilax
