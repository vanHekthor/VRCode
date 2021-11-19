import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class ListSorter {
    public List<Integer> mergeSort(List<Integer> list) {
        int[] intArray = listToArray(list);
        mSort(intArray, 0, intArray.length - 1);
        return arrayToList(intArray);
    }

    // Merges two subarrays of arr[].
    // First subarray is arr[l..m]
    // Second subarray is arr[m+1..r]
    private void merge(int arr[], int l, int m, int r)
    {
        // Find sizes of two subarrays to be merged
        int n1 = m - l + 1;
        int n2 = r - m;
  
        /* Create temp arrays */
        int L[] = new int[n1];
        int R[] = new int[n2];
  
        /*Copy data to temp arrays*/
        for (int i = 0; i < n1; ++i)
            L[i] = arr[l + i];
        for (int j = 0; j < n2; ++j)
            R[j] = arr[m + 1 + j];
  
        /* Merge the temp arrays */
  
        // Initial indexes of first and second subarrays
        int i = 0, j = 0;
  
        // Initial index of merged subarray array
        int k = l;
        while (i < n1 && j < n2) {
            if (L[i] <= R[j]) {
                arr[k] = L[i];
                i++;
            }
            else {
                arr[k] = R[j];
                j++;
            }
            k++;
        }
  
        /* Copy remaining elements of L[] if any */
        while (i < n1) {
            arr[k] = L[i];
            i++;
            k++;
        }
  
        /* Copy remaining elements of R[] if any */
        while (j < n2) {
            arr[k] = R[j];
            j++;
            k++;
        }
    }
  
    // Main function that sorts arr[l..r] using
    // merge()
    private void mSort(int arr[], int l, int r)
    {
        if (l < r) {
            // Find the middle point
            int m =l+ (r-l)/2;
  
            // Sort first and second halves
            mSort(arr, l, m);
            mSort(arr, m + 1, r);
  
            // Merge the sorted halves
            merge(arr, l, m, r);
        }
    }

    public List<Integer> quickSort(List<Integer> list) {
        int[] intArray = listToArray(list);
        qSort(intArray, 0, intArray.length - 1);
        return arrayToList(intArray);
    }

    // A utility function to swap two elements
    private void swap(int[] arr, int i, int j)
    {
        int temp = arr[i];
        arr[i] = arr[j];
        arr[j] = temp;
    }
    
    /* This function takes last element as pivot, places
    the pivot element at its correct position in sorted
    array, and places all smaller (smaller than pivot)
    to left of pivot and all greater elements to right
    of pivot */
    private int partition(int[] arr, int low, int high)
    {
        
        // pivot
        int pivot = arr[high]; 
        
        // Index of smaller element and
        // indicates the right position
        // of pivot found so far
        int i = (low - 1); 
    
        for(int j = low; j <= high - 1; j++)
        {
            
            // If current element is smaller 
            // than the pivot
            if (arr[j] < pivot) 
            {
                
                // Increment index of 
                // smaller element
                i++; 
                swap(arr, i, j);
            }
        }
        swap(arr, i + 1, high);
        return (i + 1);
    }
    
    /* The main function that implements QuickSort
            arr[] --> Array to be sorted,
            low --> Starting index,
            high --> Ending index
    */
    private void qSort(int[] arr, int low, int high)
    {
        if (low < high) 
        {
            
            // pi is partitioning index, arr[p]
            // is now at right place 
            int pi = partition(arr, low, high);
    
            // Separately sort elements before
            // partition and after partition
            qSort(arr, low, pi - 1);
            qSort(arr, pi + 1, high);
        }
    }

    public List<Integer> bubbleSort(List<Integer> list) {
        int[] intArray = listToArray(list);
        bubbleSort(intArray);
        return arrayToList(intArray);
    }

    private void bubbleSort(int arr[])
    {
        int n = arr.length;
        for (int i = 0; i < n-1; i++)
            for (int j = 0; j < n-i-1; j++)
                if (arr[j] > arr[j+1])
                {
                    // swap arr[j+1] and arr[j]
                    int temp = arr[j];
                    arr[j] = arr[j+1];
                    arr[j+1] = temp;
                }
    }

    private int[] listToArray(List<Integer> list) {
        return list.stream()
                .mapToInt(Integer::intValue)
                .toArray();
    }

    private List<Integer> arrayToList(int[] array) {
        return new ArrayList<>(Arrays.asList(Arrays.stream(array).boxed().toArray(Integer[]::new)));
    }
}
