public class CountVowels {

    private char[] vowels = {'a', 'e', 'i', 'o', 'u'};

    public CountVowels(){
    }

    public int countVowels(String string){
        String s = string.toLowerCase();
        int v = 0;
        for (int i = 0; i < s.length();i++)
        {
            for (char c: vowels) {
               if(s.charAt(i) == c){
                   v++;
                   break;
               }
            }
        }
        return v;
    }

    public char[] getVowels() {
        return vowels;
    }

    public void setVowels(char[] vowels) {
        this.vowels = vowels;
    }
}
