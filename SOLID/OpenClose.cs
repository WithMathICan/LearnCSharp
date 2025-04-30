using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOLID {
    public enum Color {
        Red, Green, Yellow
    }

    public enum Size {
        Large, Medium, Small
    }

    public class Product {
        public readonly string Name;
        public readonly Color Color;
        public readonly Size Size;

        public Product(string name, Color color, Size size) {
            Name = name;
            Color = color;
            Size = size;
        }
    }

    // If we want later add filtering by Size, then we should modify class
    // (I do not agree, because we do not modify, we just add new method).
    public class ProductFilter {
        public IEnumerable<Product> FilterByColor(IEnumerable<Product> products, Color color) {
            return products.Where(x => x.Color == color);
        }

        public IEnumerable<Product> FilterByColorWithYeld(IEnumerable<Product> products, Color color) {
            foreach(var p in products) {
                if (p.Color == color) yield return p;
            }
        }
    }

    //Using pattern Specification
    public abstract class ISpecification<T> {
        public abstract bool IsSatisfied(T item);
    }

    public interface IFilter<T> {
        IEnumerable<T> Filter(IEnumerable<T> item, ISpecification<T> spec);
    }


    internal class OpenClose {
        internal static void TestProducts() {
            List<Product> products = [
                new Product("Product 1", Color.Yellow, Size.Large),
                new Product("Product 2", Color.Green, Size.Small),
                new Product("Product 3", Color.Red, Size.Medium),
            ];

            ProductFilter pf = new();
            var filtered = pf.FilterByColor(products, Color.Red);
            filtered = pf.FilterByColorWithYeld(products, Color.Green);
        }
    }
}
