using System;

namespace NetworkGameEngine.Generator.Debug
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            GameObject gameObject = new GameObject();   
            gameObject.AddComponent(new TestComponent(1234));
            Console.ReadLine();
            Console.ReadLine();
        }
    }
}
