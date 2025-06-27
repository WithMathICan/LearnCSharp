using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeetCode {
    //После ухаживания в течение длительного времени, Ахилл наконец собирается сделать предложение своей подруге.
    //Она, будучи очень сильной в математике, примет его предложение, если и только если Ахилл решит задачу, данную ему.
    //Задача приводится ниже. Помогите Ахиллу в её решении.
    //Ахиллу предоставляются два числа N и M, а он должен сказать ей остаток от деления 111... (N раз)  на M.
    internal class AhillTask {

        class RemindersData(long n, int m, int res) {
            public long n = n;
            public int m = m;
            public int res = res;
        }

        string inputData = "431435644303281 9655980\r\n195341858741005 2988538\r\n97195453853104 429406\r\n297002544187647 5725383\r\n397037170416690 9714651\r\n517246465951584 725477\r\n49175558926637 4367412\r\n65505987924559 1598832\r\n490777395242445 5120642\r\n77887565852868 3735988\r\n403007812045441 5731538\r\n169943011368999 7206398\r\n416676581927300 3095692\r\n155244946382244 4735366\r\n346017020810865 9095534\r\n163967400387216 6556113\r\n98236084076611 4975528\r\n559258630016835 3684192\r\n197721673437212 2838584\r\n136280305475808 9405912\r\n48321139563978 6887566\r\n184432980640650 3618300\r\n139605926419872 7276157\r\n59703385225868 6233818\r\n188594959488896 5133357\r\n312570891398258 2803655\r\n3766222018656 8610323\r\n204211978026546 2413013\r\n656457690101184 5502724\r\n116689548018570 2935928\r\n433850553699227 2979194\r\n197426335934648 255796\r\n89027993008786 270624\r\n490496621349846 5969432\r\n125102760052150 3077603\r\n172168890375345 1309784\r\n64992281990160 1391452\r\n467475489987066 206368\r\n51586335105355 9353362\r\n197543723518647 8985780\r\n21477306070737 2839606\r\n229032519246805 6646378\r\n380661781012678 7612491\r\n650389213916968 6382691\r\n91862260877296 2659639\r\n157111764247116 571571\r\n528607477068444 4158247\r\n365295144598368 3427896\r\n287535548744860 2064708\r\n34522130024382 2907267\r\n59754537083540 394185\r\n633135006521340 8896676\r\n115122997286037 4549388\r\n210924537996216 7144830\r\n124702926761427 5433169\r\n271732182226408 2031408\r\n316596761190090 7292257\r\n2619676446036 9826037\r\n342777951037557 2045758\r\n35499093228000 9454144\r\n93328549585110 7503473\r\n4415724725488 4205640\r\n120692666014684 4783736\r\n86979473286615 2024735\r\n197315723483505 4414419\r\n78426585432080 5030390\r\n360537141760778 5182430\r\n428344461720198 6261989\r\n8730926594262 8475433\r\n271866733863725 6208827\r\n16333337360220 5931596\r\n123032526317376 5516049\r\n230609416123382 9092210\r\n84489019108250 9305272\r\n12458440318568 2537924\r\n70942009006737 1449805\r\n298122080473084 9128835\r\n647222611831850 9733259\r\n27974690345406 6690972\r\n411158950157124 4583032\r\n209435303419326 8833056\r\n524312290724740 4802524\r\n99208206081831 3904461\r\n225118736381261 3204606\r\n164158687424622 2364139\r\n150699606455560 4299875\r\n503257999342725 8592276\r\n237879738756200 17247\r\n80357092250202 917173\r\n176012599501000 821460\r\n14959191966040 4216954\r\n105827299253995 1123478\r\n224131838483550 713966\r\n132487959815376 2804856\r\n334126343526138 6904529\r\n564947904604236 2460107\r\n763785172985992 4501977\r\n68934620733550 5696217\r\n400796471928375 9853870\r\n410413896383204 5450368";
        string resulst = "3641031\r\n489413\r\n253555\r\n140808\r\n9062661\r\n479883\r\n497723\r\n496087\r\n3934985\r\n3027451\r\n3149915\r\n5324957\r\n215955\r\n1590247\r\n3594961\r\n2988225\r\n4881735\r\n2864007\r\n2169087\r\n1124799\r\n3831555\r\n3164511\r\n6621825\r\n2384861\r\n949727\r\n1111116\r\n512479\r\n1874444\r\n1185587\r\n2133927\r\n1983015\r\n20831\r\n243847\r\n823879\r\n1670108\r\n540175\r\n572079\r\n185415\r\n175901\r\n6490251\r\n825243\r\n1700179\r\n4114105\r\n2444489\r\n2519650\r\n408408\r\n2538633\r\n1403919\r\n494095\r\n0\r\n257906\r\n4095203\r\n454643\r\n2095401\r\n4502000\r\n2028007\r\n1788584\r\n1981252\r\n150833\r\n2215815\r\n1420930\r\n1576711\r\n1205215\r\n1100161\r\n2904183\r\n2842361\r\n25311\r\n5023813\r\n5145074\r\n4696196\r\n552739\r\n2613996\r\n6720041\r\n2392559\r\n1477903\r\n88836\r\n7757671\r\n3547957\r\n2471511\r\n3759263\r\n1633383\r\n3070323\r\n116733\r\n1526951\r\n913271\r\n2881111\r\n6474579\r\n13601\r\n843317\r\n398011\r\n4108361\r\n134841\r\n214401\r\n2661063\r\n609347\r\n2283047\r\n2797219\r\n558160\r\n3635821\r\n910151";

        List<RemindersData> PrepareData() {
            List<RemindersData> result = [];
            var input = inputData.Split("\r\n").Select(x => {
                var arr = x.Split(" ");
                return (long.Parse(arr[0]), int.Parse(arr[1]));
            }).ToList();
            var output = resulst.Split("\r\n").Select(int.Parse).ToList();
            for (int i = 0; i < 100; i++) {
                result.Add(new RemindersData(input[i].Item1, input[i].Item2, output[i]));
            }
            return result;
        }

        public void Test() {
            var data = PrepareData();
            //int m = 1598832;
            //long n = 65505987924559;
            //int res = 496087;
            //int newRes = solve(n, m);
            //Console.WriteLine(newRes);
            var sw = Stopwatch.StartNew();
            int c = 0;
            //data = [data[0], data[1], data[2]];
            foreach (var item in data) {
                var sw1 = Stopwatch.StartNew();
                int res = SolvePower2(item.n, item.m);
                if (item.res != res) {
                    throw new Exception();
                }
                sw1.Stop();
                Console.WriteLine($"{++c}: Elapsed: {sw1.Elapsed.TotalMilliseconds} ms");
            }
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.Elapsed.TotalMilliseconds} ms");
        }

        List<int> FindReminders(int m, out int initialValue) {
            int number = 1;
            List<int> reminders = [1];
            while (true) {
                number = (number * 10) % m;
                initialValue = reminders.IndexOf(number);
                if (initialValue >= 0) {
                    return reminders;
                } else {
                    reminders.Add(number);
                }
            } 
        }
        public int solve(long n, int m) {
            List<int> reminders = FindReminders(m, out int initialValue);
            int count = reminders.Count;
            int result = 0;
            for (int i = 0; i < initialValue; ++i) {
                result = (result + reminders[i]) % m;
            }
            result = result % m;

            int sumOfReminders = 0;
            for (int i = initialValue; i < count; ++i) {
                sumOfReminders = (sumOfReminders + reminders[i]) % m;
            }
            int periodLength = count - initialValue;
            n -= initialValue;
            long division = n / periodLength;
            long rem = n - division * periodLength;
            long divisionByMod = division % m;
            long multiple = sumOfReminders * divisionByMod;
            multiple = multiple % m;
            result = (result + (int)multiple) % m;
            for (int i = 0; i < rem; i++) {
                result += reminders[initialValue + i];
                result = result % m;
            }
            return result;
        }

        public int SolvePower2(long n, int m) {
            if (n == 1) return 1;
            if (n == 2) return 11 % m;
            long rem = 0;
            long unitNumber = 1;
            long tenPower = 10;
            while (n > 0) {
                if (n % 2 == 1) rem = (rem*tenPower + unitNumber) % m;
                n = n / 2;
                unitNumber = (unitNumber + tenPower * unitNumber) % m;
                tenPower = (tenPower * tenPower) % m;
            }
            return (int)rem;
        }
    }
}
