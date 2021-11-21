class Calc {

    private int a = 0;
    private int b = 0;

    public Calc(int a, int b) {
        this.a = a;
        this.b = b;
    }

    public int add() {
        return a+b;
    }

    public int sub() {
        return a-b;
    }

    public int mul() {
        return a*b;
    }

    public float div() {
        return (float) a / (float) b;
    }

}
