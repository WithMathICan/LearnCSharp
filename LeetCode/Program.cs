using LeetCode;
using System.Diagnostics;

internal class Program {
    private static void Main(string[] args) {
        //new LeetCode1().Test();
        //InterestRate.InterestRateUI();
        new AhillTask().Test();
    }
}

internal class LeetCode1 {
    private class Data(int[] nums, int target, int[] result) {
        public int[] Nums = nums;
        public int Target = target;
        public int[] Result = result;
    }

    private readonly List<Data> TestData = [
        new Data([2,7,11,15], 9, [0, 1]),
        new Data([3,2,4], 6, [1, 2]),
        new Data([3,3], 6, [0, 1]),
    ];

    public int[] TwoSum(int[] nums, int target) {
        for (int i = 0; i < nums.Length; i++) {
            for (int j = i + 1; j < nums.Length; j++) {
                if (nums[i] + nums[j] == target) return [i, j];
            }
        }
        return [];
    }

    public int[] TwoSum1(int[] nums, int target) {
        for (int i = 0; i < nums.Length; i++) {
            //nums[i] + nums[j] == target
            int diff = target - nums[i];
            int index = Array.IndexOf(nums, diff);
            if (index != -1) {
                return [i, index];
            }
        }
        return [];
    }

    public int[] TwoSum2(int[] nums, int target) {
        Dictionary<int, int> used = [];
        for (int i = 0; i < nums.Length; i++) {
            used[nums[i]] = i;
        }
        for (int i = 0; i < nums.Length; i++) {
            int diff = target - nums[i];
            if (used.TryGetValue(diff, out int index)) { 
                return [i, index];
            }
        }
        return [];
    }

    public int[] TwoSumOptimized(int[] nums, int target) {
        Dictionary<int, int> used = []; 
        for (int i = 0; i < nums.Length; i++) {
            int diff = target - nums[i];
            if (used.TryGetValue(diff, out int index)) {
                return [index, i];
            } else {
                used.Add(nums[i], i);
            }
        }
        return [];
    }

    public void Test() {
        foreach (var data in TestData) {
            int[] res = TwoSum2(data.Nums, data.Target);
            //Debug.Assert(res.SequenceEqual(data.Result));
            int[] resOptimized = TwoSumOptimized(data.Nums, data.Target);
            Debug.Assert(resOptimized.SequenceEqual(data.Result));
        }
        Console.WriteLine("OK");

    }
}