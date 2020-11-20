import java.util.ArrayList;
import java.util.List;

public class CountToAThousand {

    int first = 0;
    int second = 1;
    String abc = "stringfieldtest";
    List<String> stringListTest = new ArrayList<>();
    int [] intArrayTest = {1,2,3};

    public CountToAThousand(){

    }

    public void countWithFibbonaci(int n){
        stringListTest.add("abc");
        intArrayTest[1] = 2;
        abc = "test";
        char [] arraytest = {'a','b','c','d'};
        if(n>1000){
            System.out.println("The given number is bigger than 1000 and will not be handled.");
        }
        while(n < 1000){
            first = 0;
        second = 1;
        while (n+first+second <= 1000){
        int temp = first;
        first = second;
        second = temp+second;
        }
        System.out.println(n+"+"+ second +"="+(n+second));
        n = n+second;
        }
    }

    public int getFirst() {
        return first;
    }

    public void setFirst(int first) {
        this.first = first;
    }

    public int getSecond() {
        return second;
    }

    public void setSecond(int second) {
        this.second = second;
    }
}
