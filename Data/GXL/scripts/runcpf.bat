@echo off

IF "%~1"=="" ( 
  echo language parameter is missing
  echo "usage: %0 (c|cpp|cs|java)"
  goto :EXIT
) 

set LANG=%1

if "%LANG%"=="c" (
    SET "INC=-i *.c -i *.h"
) else (
    if "%LANG%"=="cpp" (
        SET "INC=-i *.ii -i *.cpp -i *.cxx -i *.c++ -i *.cc -i *.tcc -i *.hpp -i *.hxx -i *.h++ -i *.hh -i *.C -i *.H -i *.inl -i *.preinc"
    ) else (
        if "%LANG%"=="cs" (
            SET "INC=-i *.cs"
        ) else (
		    if "%LANG%"=="java" (
              SET "INC=-i *.java"
			) else (
			    echo unsupported language %LANG%
                echo "usage: %0 (c|cpp|cs|java)"
                goto :EXIT
			)			 
        )
    )
)

@echo on
cpf.exe -m 100 -c clones.cpf -s clones.csv -a %INC% .
cpfcsv2rfg clones.cpf clones.csv clones.rfg
rfgexport -o Clones -f GXL clones.rfg clones.gxl

@del clones.cpf clones.csv clones.rfg tokens.files tokens.tok

:EXIT
