import counter.unterordner.UnterOrdnerTest;
import counter.CountConsonants;
import counter.CountToAThousand;
import counter.CountVowels;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;

public class Main {

    public static void main(String[] args) throws IOException {
		if (args.length == 0) {
	       System.out.println("Hello World!");
	       System.out.println("This program's only purpose is to be debugged.");
	       System.out.println("It can count from a given number to 1000 with fibonacci numbers or");
	       System.out.println("give the count of vowels or consonants in a given String.");
           interactive();
		} else {
			noninteractive(args);
		}
    }

    private static void noninteractive(String[] args) throws IOException {
		int index = 0;
		while (index < args.length) {
			String entry = args[index];
			index++;
			if (entry.toLowerCase().equals("count")){
				System.out.println("You entered count. Enter a number below 1000 to start counting.");
				CountToAThousand ct = new CountToAThousand();
				String s = args[index];
				try{
				int n = Integer.parseInt(s);
				ct.countWithFibbonaci(n);
				}catch (NumberFormatException e){
					System.out.println("You did not enter any number.");
					System.exit(1);
				}
			}else if (entry.toLowerCase().equals("vowels")){
				System.out.println("You entered Vowels. Now enter the string, where you want to count vowels.");
				String string = args[index];
				CountVowels cv = new CountVowels();
				int count = cv.countVowels(string);
				System.out.println("The String you entered contains "+count+" vowels.");
			}else if (entry.toLowerCase().equals("consonants")){
				System.out.println("You entered consonants. Now enter the string, where you want to count consonants.");
				String string = args[index];
				CountConsonants cc = new CountConsonants();
				int count = cc.countConsonants(string);
				System.out.println("The String you entered contains "+count+" Consonants.");
			}else if (entry.toLowerCase().equals("both")){
				System.out.println("You entered both. Now enter your string.");
				String string = args[index];
				CountVowels cv = new CountVowels();
				CountConsonants cc = new CountConsonants();
				int consonants = cc.countConsonants(string);
				int vowels = cv.countVowels(string);
				System.out.println("The entered strings contains "+ vowels +" vowels and "+ consonants + " consonants.");
			}else{
				System.out.println("You did not enter a valid argument.");
				System.exit(1);
			}
			index++;
		}
	}
	
    private static void interactive() throws IOException {
        UnterOrdnerTest uot = new UnterOrdnerTest();
        uot.printTest();
        System.out.println("Choose an Option:");
        System.out.println("Type \"count\" to start the counting function.");
        System.out.println("Type \"vowels\" to count the vowels of a String.");
        System.out.println("Type \"consonants\" to count the consonants of a String.");
        System.out.println("Type \"both\" to count vowels and consonants.");
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        String entry = reader.readLine();
        if (entry.toLowerCase().equals("count")){
            System.out.println("You entered count. Enter a number below 1000 to start counting.");
            CountToAThousand ct = new CountToAThousand();
            String s = reader.readLine();
            try{
            int n = Integer.parseInt(s);
            ct.countWithFibbonaci(n);
            }catch (NumberFormatException e){
                System.out.println("You did not enter any number.");
                afterfunction();
            }
        }else if (entry.toLowerCase().equals("vowels")){
            System.out.println("You entered Vowels. Now enter the string, where you want to count vowels.");
            String string = reader.readLine();
            CountVowels cv = new CountVowels();
            int count = cv.countVowels(string);
            System.out.println("The String you entered contains "+count+" vowels.");
        }else if (entry.toLowerCase().equals("consonants")){
            System.out.println("You entered consonants. Now enter the string, where you want to count consonants.");
            String string = reader.readLine();
            CountConsonants cc = new CountConsonants();
            int count = cc.countConsonants(string);
            System.out.println("The String you entered contains "+count+" Consonants.");
        }else if (entry.toLowerCase().equals("both")){
            System.out.println("You entered both. Now enter your string.");
            String string = reader.readLine();
            CountVowels cv = new CountVowels();
            CountConsonants cc = new CountConsonants();
            int consonants = cc.countConsonants(string);
            int vowels = cv.countVowels(string);
            System.out.println("The entered strings contains "+ vowels +" vowels and "+ consonants + " consonants.");
        }else{
            System.out.println("You did not enter a valid argument.");
        }
        afterfunction();
    }

    private static void afterfunction() throws IOException {
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        System.out.println("You can now exit the programm by typing \"exit\" or go back to the Options menu by typing " +
                "\"menu\"");
        String s = reader.readLine();
        if (s.toLowerCase().equals("menu")) {
            interactive();
        }else if(s.toLowerCase().equals("exit")){
            System.exit(0);
        }else{
        System.out.println("You did not enter a valid argument, try again.");
        afterfunction();
        }
    }
}
