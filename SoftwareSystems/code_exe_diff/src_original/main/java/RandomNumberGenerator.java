import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Random;
import java.util.stream.Collectors;

public class RandomNumberGenerator {

    public List<Integer> generate01List(int length) {
        List<Integer> numbers = new ArrayList<>();

        for (int i = 0; i < 26; i++) {
            numbers.add(0);
            numbers.add(1);
        }

        Collections.shuffle(numbers);

        return numbers;
    }
    
    public List<Integer> generateIntegerList(int length, int min, int max) {
        Random random = new Random();
        return random.ints(length, min, max + 1)
            .boxed()
            .collect(Collectors.toList());
    }
}
