using System;
using System.Diagnostics;
using MagicFlatIndex;

namespace TestFlatFile
{
    class Program
    {
        static void Main()
        {
            const int PERSONS_COUNT = 5000000;
            const int PERSONS_TO_SEARCH = 100000;

            Console.WriteLine("Open file...");
            using (FlatFile<Person> personFile = new FlatFile<Person>("person"))
            {
                Console.WriteLine("Clear file...");
                personFile.Truncate();

                Console.WriteLine("Generate names... (1/2)");
                Person[] persons = new Person[PERSONS_COUNT];
                for (int i = 0; i < PERSONS_COUNT;)
                {
                    persons[i] = new Person() { Id = ++i, Prenom = $"Firstname {i}", Nom = $"Lastname {i}" };
                }

                Console.WriteLine("Shuffle names... (1/2)");
                Random rnd = new Random();
                rnd.Shuffle(persons);

                Stopwatch sw = Stopwatch.StartNew();

                Console.WriteLine("Insert names... (1/2)");
                Console.WriteLine($"{personFile.InsertMany(persons)} person(s) added");

                sw.Stop();
                Console.WriteLine($"Time to add {PERSONS_COUNT} persons : {sw.ElapsedMilliseconds} ms"); 
                Console.WriteLine($"Average time per record : {sw.ElapsedTicks / PERSONS_COUNT} ticks");

                Console.WriteLine("Generate names... (2/2)");
                persons = new Person[PERSONS_COUNT];
                for (int i = 0; i < PERSONS_COUNT;)
                {
                    persons[i] = new Person() { Id = ++i + PERSONS_COUNT, Prenom = $"Firstname {i + PERSONS_COUNT}", Nom = $"Lastname {i + PERSONS_COUNT}" };
                }

                Console.WriteLine("Shuffle names... (2/2)");
                rnd.Shuffle(persons);

                sw = Stopwatch.StartNew();

                Console.WriteLine("Insert names... (2/2)");
                Console.WriteLine($"{personFile.InsertMany(persons)} person(s) added");

                sw.Stop();
                Console.WriteLine($"Time to add {PERSONS_COUNT} persons : {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"Average time per record : {sw.ElapsedTicks / PERSONS_COUNT} ticks");

                Console.WriteLine("Insert on person...");
                sw = Stopwatch.StartNew();
                personFile.Insert(new Person { Id = PERSONS_COUNT * 2 + 1, Prenom = "Roger", Nom = "Rabbit" });
                sw.Stop();
                Console.WriteLine($"Time to add 1 persons : {sw.ElapsedTicks} ticks");

                Console.WriteLine($"There are {personFile.CountRecords()} persons in the file");


                Console.WriteLine("Rebuild index...");

                sw = Stopwatch.StartNew();

                personFile.RebuildIndex();
                Console.WriteLine($"Time to rebuild index : {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"There are {personFile.CountRecords()} persons in the file");

                sw.Stop();

                Console.WriteLine("Generate a list of persons to find...");
                int[] indices = new int[PERSONS_TO_SEARCH];
                for (int i = 0; i < PERSONS_TO_SEARCH; i++)
                {
                    indices[i] = rnd.Next(1, PERSONS_COUNT * 2 + 1);
                }

                Console.WriteLine("Search those persons and change even to Alfred E. Neuman or delete odd...");
                int found = 0;
                sw = Stopwatch.StartNew();
                for (int i = 0; i < PERSONS_TO_SEARCH; i++)
                {
                    Person person = personFile.Select(indices[i]);
                    if (person != null)
                    {
                        if (i % 2 == 0)
                        {
                            person.Prenom = "Alfred";
                            person.Nom = "E. Neuman";
                            personFile.Update(person);
                        }
                        else
                        {
                            personFile.Delete(person.Id);
                        }
                        found++;
                    }
                }
                sw.Stop();
                Console.WriteLine($"{found} person(s) found in {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"Average time per record : {sw.ElapsedTicks / found} ticks");

                Console.WriteLine($"The file contains {personFile.CountRecords()} names");
                /*
                persons = personFile.SelectAll();
                Console.WriteLine("Persons in the file :");
                foreach (Person p in persons)
                {
                    Console.WriteLine($"   {p}");
                }
                personFile.Shrink(false);
                persons = personFile.SelectAll();
                Console.WriteLine("Persons in the file after shrink :");
                foreach (Person p in persons)
                {
                    Console.WriteLine($"   {p}");
                }
                personFile.Shrink(true);
                persons = personFile.SelectAll();
                Console.WriteLine("Persons in the file after reorder :");
                foreach (Person p in persons)
                {
                    Console.WriteLine($"   {p}");
                }
                */
            }
        }
    }
}
