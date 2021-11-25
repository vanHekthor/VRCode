class Main {

    public static void main(String[] args) {

        boolean CALCULATE = true;
        boolean OPTION_A = false;
        boolean OPTION_B = true;
        boolean OPTION_C = false;
        boolean OPTION_D = true;

        Calc c = new Calc(15,5);

        if (CALCULATE) {
            int sum = 0;
            if (OPTION_A) { sum = c.add(); }
            
            int diff = 0;
            if (OPTION_B) { diff = c.sub(); }

            int prod = 0;
            if (OPTION_C) { prod = c.mul(); }

            float quot = 0;
            if (OPTION_D) { quot = c.div(); }

            System.out.println(sum);
            System.out.println(diff);
            System.out.println(prod);
            System.out.println(quot);
        }
    }

}
