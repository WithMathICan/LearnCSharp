using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeetCode {
    internal class InterestRate {

        static bool GetNumberFromConsole(string title, out decimal value) {
            value = default!;
            Console.Write(title);
            string? valueStr = Console.ReadLine();
            if (string.IsNullOrEmpty(valueStr)) {
                return false;
            }
            return decimal.TryParse(valueStr, CultureInfo.CurrentCulture, out value);
        }

        static void GetData(out decimal interestRate, out decimal principalAmount, out decimal monthsCount) {
            while (!GetNumberFromConsole("Enter annual interest rate (e.g., 5 for 5%): ", out interestRate)) {
                Console.WriteLine("Error, in getting data. Try again, please.");
            }
            while (!GetNumberFromConsole("Enter principal amount (deposit sum in $): ", out principalAmount)) {
                Console.WriteLine("Error, in getting data. Try again, please.");
            }
            while (!GetNumberFromConsole("Enter time period (number of months): ", out monthsCount)) {
                Console.WriteLine("Error, in getting data. Try again, please.");
            }
        }

        static void ValidateInputParams(decimal interestRate, decimal principalAmount, decimal monthsCount) {
            if (interestRate < 0) {
                throw new ArgumentException("Input param should be greater, than 0", nameof(interestRate));
            }
            if (interestRate > 90) {
                throw new ArgumentException("Interest rate seems unusually high", nameof(interestRate));
            }
            if (principalAmount < 0) {
                throw new ArgumentException("Input param should be greater, than 0", nameof(principalAmount));
            }
            if (monthsCount < 0) {
                throw new ArgumentException("Input param should be greater, than 0", nameof(monthsCount));
            }
        }

        static decimal CalculateSimpleInterest(decimal interestRate, decimal principalAmount, decimal monthsCount) {
            ValidateInputParams(interestRate, principalAmount, monthsCount);
            decimal yearsCount = monthsCount / 12.0M;
            return (interestRate * principalAmount * yearsCount) / 100.0M;
        }

        public static void InterestRateUI() {
            Console.WriteLine("=== Simple Interest Calculator ===");
            Console.WriteLine("This program calculates simple interest on your deposit.");
            Console.WriteLine("Please enter the following information:");
            GetData(out decimal interestRate, out decimal principalAmount, out decimal monthsCount);
            try {
                decimal simpleInterest = CalculateSimpleInterest(interestRate, principalAmount, monthsCount);
                Console.WriteLine($"Simple Interest = {simpleInterest}$");
                Console.WriteLine($"Total Amount = {simpleInterest + principalAmount}$");
            } catch(ArgumentException argEx) {
                Console.WriteLine(argEx.Message);
            } catch(Exception) {
                Console.WriteLine("Unexpected error during calculation. Try again later.");
            }
        }
    }
}
