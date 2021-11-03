public class CountConsonants {

    private char[] consonants = {'q','w','r','t','z','p','s','d','f','g','h','j','k','l','y','x','c','v','b','n','m'};

    public CountConsonants(){

    }

    public int countConsonants(String string){
        String s = string.toLowerCase();
        int c = 0;
        for (int i = 0; i < s.length();i++)
        {
            for (char v: consonants) {
                if(s.charAt(i) == v){
                    c++;
                    break;
                }
            }
        }
        return c;
    }

    public char[] getConsonants() {
        return consonants;
    }

    public void setConsonants(char[] consonants) {
        this.consonants = consonants;
    }
}