package counter.unterordner;

public class UnterOrdnerTest {
    public UnterOrdnerTest(){

    }

    public void printTest(){
        System.out.println("UnterOrdnerTest!");
    }
	
	private class InnereKlasse {
	  public int foobar(int j)
	  {
		  int i = 5;
		  return i * j;
	  }
	}
}

class ZweiteKlasse {
	
	  public ZweiteKlasse() {}
	  
	  public int foo(int j)
	  {
		  int i = 10;
		  return i + j;
	  }
}
