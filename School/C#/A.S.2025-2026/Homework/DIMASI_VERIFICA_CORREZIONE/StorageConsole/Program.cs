using DIMASI_VERIFICA;

Storage storage = new Storage();

Product p1 = new Product("00001", "PC", 150.0);
Product p2 = new Product("00002", "Printer", 70.0);

storage.AddNewProduct(p1, 30);
storage.AddNewProduct(p2, 30);

Dictionary<Product, int> dic1 = new Dictionary<Product, int>();

Dictionary<Product, int> dic2 = new Dictionary<Product, int>();

dic1.Add(p1, 15);
dic1.Add(p2, 15);

dic2.Add(p1, 5);
dic2.Add(p2, 5);

Order o1 = new Order("o1", "Mario Rossi", DateOnly.FromDateTime(DateTime.Now.AddDays(5)), dic1, false);
Order o2 = new Order("o2", "Luigi Bianchi", DateOnly.FromDateTime(DateTime.Now.AddDays(2)), dic2, true);

storage.AddNewOrder(o1);
storage.AddNewOrder(o2);
Console.WriteLine(storage.StorageInventory[p1]);
storage.ElaborateOrder();
Console.WriteLine(storage.StorageInventory[p1]);
storage.ElaborateOrder();
Console.WriteLine(storage.StorageInventory[p1]);