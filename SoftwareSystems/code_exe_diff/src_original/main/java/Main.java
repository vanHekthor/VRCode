import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

class Main {

    public static void main(String[] args) {

        boolean CALCULATE = true;
        boolean GENERATE_NUM_LIST = true;

        boolean BINARY_LIST = false;
        boolean INT_LIST = true;

        boolean SORT_LIST = true;
        boolean MERGE_SORT = false;
        boolean QUICK_SORT = false;
        boolean BUBBLE_SORT = true;
        
        boolean PRINT_NUM_LIST = true;

        boolean OPTION_A = false;
        boolean OPTION_B = true;
        boolean OPTION_C = false;
        boolean OPTION_D = true;

        Calc c = new Calc(15,5);

        if (CALCULATE) {
            int sum = 0;
            if (OPTION_A) {
                 sum = c.add(); 
            }
            
            int diff = 0;
            if (OPTION_B) {
                 diff = c.sub(); 
            }

            int prod = 0;
            if (OPTION_C) {
                 prod = c.mul();
            }

            float quot = 0;
            if (OPTION_D) {
                quot = c.div();
            }

            System.out.println(sum);
            System.out.println(diff);
            System.out.println(prod);
            System.out.println(quot);
        }

        if (GENERATE_NUM_LIST) {
            RandomNumberGenerator gen = new RandomNumberGenerator();

            List<Integer> numList = new ArrayList<Integer>();
            
            if (BINARY_LIST)
                numList = gen.generate01List(52);
            else if (INT_LIST)
                numList = gen.generateIntegerList(52, 5, 15);

            if (SORT_LIST) {
                ListSorter listSorter = new ListSorter();
                
                if (MERGE_SORT)
                    numList = listSorter.mergeSort(numList);
                if (QUICK_SORT)
                    numList = listSorter.quickSort(numList);
                if (BUBBLE_SORT)
                    numList = listSorter.bubbleSort(numList);
            }    
        
            if (PRINT_NUM_LIST) {
                System.out.println(Arrays.toString(numList.toArray()));
            }
        }
    }

}
