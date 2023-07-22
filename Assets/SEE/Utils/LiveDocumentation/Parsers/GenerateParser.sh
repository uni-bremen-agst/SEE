for f in .g4; do
java -jar antlr.jar -Dlanguage=CSharp "${f%.}.g4"
if [ $? -ne 0 ]; then
echo "Error occured while generating lexer for $f."
exit 1
else
echo "Lexer generated for $f."
fi
done

exit 0