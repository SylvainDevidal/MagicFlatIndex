using System;

using MagicFlatIndex;

namespace TestFlatFile
{
    class Program
    {
        static void Main()
        {
            const int PERSON_COUNT = 1000000;

            Console.WriteLine("Generate names...");
            Person[] persons = new Person[PERSON_COUNT];
            for (int i = 0; i < PERSON_COUNT; )
            {
                persons[i] = new Person() { Id = ++i, Prenom = $"Firstname {i}", Nom = $"Lastname {i}" };
            }

            Console.WriteLine("Shuffle names..."); 
            Random rnd = new Random();
            rnd.Shuffle(persons);

            Console.WriteLine("Open file...");
            using (FlatFile<Person> personFile = new FlatFile<Person>("person"))
            {
                Console.WriteLine("Clear file...");
                personFile.Truncate();

                Console.WriteLine("Insert names...");
                for (int i = 0; i < persons.Length; i++)
                {
                    personFile.Insert(persons[i]);
                }

                Console.WriteLine("Load all the names...");
                Person[] res = personFile.SelectAll();
                Console.WriteLine($"The data file contains {res.Length} names");

                Console.WriteLine("Search key 100 000 :");
                Person p = personFile.Select(100000);
                if (p == null)
                {
                    Console.WriteLine("Not found...");
                }
                else
                {
                    Console.WriteLine(p);
                    Console.WriteLine("Change it to Alfred E.Neuman...");
                    p.Prenom = "Alfred";
                    p.Nom = "E.Neuman";
                    personFile.Update(p);
                }

                Console.WriteLine("Search key 200 000 :");
                p = personFile.Select(200000);
                if (p == null)
                {
                    Console.WriteLine("Not found...");
                }
                else
                {
                    Console.WriteLine(p);
                }

                Console.WriteLine("Search key 100 000 :");
                p = personFile.Select(100000);
                if (p == null)
                {
                    Console.WriteLine("Not found...");
                }
                else
                {
                    Console.WriteLine(p);
                }

                Console.WriteLine("Delete key 200 000 :");
                Console.WriteLine(personFile.Delete(200000));

                Console.WriteLine("Search key 200 000 :");
                p = personFile.Select(200000);
                if (p == null)
                {
                    Console.WriteLine("Not found...");
                }
                else
                {
                    Console.WriteLine(p);
                }

                Console.WriteLine("Load all persons again...");
                res = personFile.SelectAll();
                Console.WriteLine($"The file contains {res.Length} names");
            }
        }
    }
}
