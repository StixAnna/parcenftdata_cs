using System;
using System.IO;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using DotNetEnv;
using System.Xml.Linq;

class NftParcer
{
    static void Main(string[] args)
    {
        Env.Load();
        string userColls_url = Env.GetString("USER_URL") + "#collections";
        string neededcollection = Env.GetString("NEEDED_COLLECTION");
        string userDataDir = Env.GetString("USERDATADIR_CHROME");
        string saletype = Env.GetString("SALETYPE");
        string ownership = Env.GetString("OWNERSHIP");
        Uri uri;
        string sock_href, soxname, contract_href, owner_href, nftOwner_tonviewer_href, nftOwnerAddr, nftContrAddr, numberstr;
        string sockListWindow, sockWindow, ownerWindow;

        // Указываем путь к файлу CSV
        string filePath = "parce.csv";
        //mainpage_start
        ChromeOptions options = new ChromeOptions();
        options.AddArgument($"user-data-dir={userDataDir}");        // Устанавливаем параметр UserDataDir для использования профиля пользователя Chrome
        IWebDriver driver = new ChromeDriver(options);              // Создаем новый экземпляр драйвера Chrome с опциями
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
        driver.Navigate().GoToUrl(userColls_url);
        Thread.Sleep(1000);
        //Выбираем коллекцию
        IWebElement collections = driver.FindElement(By.ClassName("Grid__grid"));
        IReadOnlyCollection<IWebElement> collectionLinks = collections.FindElements(By.CssSelector("a.CollectionPreview"));
        foreach (IWebElement link in collectionLinks)
        {
            string hrefValue = link.GetAttribute("href");
            // Проверяем, содержит ли атрибут href нужное значение
            if (hrefValue.Contains($"{neededcollection}"))
            {
                link.Click();
                break; 
            }
        }
        Thread.Sleep(3000);
        //listsettings_setup_start
        //Отображение списком (по умелчанию сетка, выбор того что не подсвечено(списком))
        driver.FindElement(By.XPath("//div[@class='SegmentedControlItem' and @style='border-radius: var(--radius_buttons_small);']")).Click();  //setting_list
        IWebElement EntitySideBar = driver.FindElement(By.ClassName("EntitySideBar"));
        //EntitySideBar.FindElement(By.XPath("//div[@class='LibraryCell__main' and text()='For Sale']")).Click();               //setting_saletype__test
        EntitySideBar.FindElement(By.XPath($"//div[@class='LibraryCell__main' and text()='{saletype}']")).Click();              //setting_saletype
        EntitySideBar.FindElement(By.XPath($"//div[@class='LibraryCell__main' and text()='{ownership}']")).Click();             //setting_ownership
        Thread.Sleep(2000);
        long previousHeight = -1; // Изначально предыдущая высота равна -1, чтобы первая проверка сработала всегда
        while (true)        //Цикл полной загрузки списка
        {   
            long currentHeight = (long)js.ExecuteScript("return document.body.scrollHeight;");
            if (currentHeight == previousHeight) { break; }
            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.Sleep(2000);
            previousHeight = currentHeight;
        }
        //listsettings_setup_end

        //Находим tbody cо всеми socks.Находим все дочерние элементы TableRow 
        IReadOnlyCollection<IWebElement> RowElements = driver.FindElement(By.TagName("tbody")).FindElements(By.ClassName("TableRow"));
        // Открываем файл для записи
        if (File.Exists(filePath))
            File.Delete(filePath);
        File.AppendAllText(filePath, "soxname,nftContrAddr,nftOwnerAddr,OwnerTokensCount\n");
        Console.WriteLine("soxname,nftContrAddr,nftOwnerAddr,OwnerTokensCount");
        // Итерируемся по найденным элементам TableRow и выполняем действия с каждым из них
        foreach (IWebElement RowElement in RowElements)
        {
            sockListWindow = driver.CurrentWindowHandle;
            sock_href = RowElement.FindElement(By.TagName("a")).GetAttribute("href");
            // Открываем новую вкладку, переходим на нее и переходим по ссылке
            js.ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles[1]);
            driver.Navigate().GoToUrl(sock_href);
            sockWindow = driver.CurrentWindowHandle;
            Thread.Sleep(1000);
            // Находим soxDetails cо всеми Details
            IWebElement soxDetails = driver.FindElement(By.ClassName("TwoColsPageRight"));
            soxname = soxDetails.FindElement(By.CssSelector(".LibraryTypography.LibraryTypography--w-bold.LibraryDisplay.LibraryDisplay--l-2")).Text;
            IWebElement NftPageMoreInfo = soxDetails.FindElement(By.ClassName("NftPageMoreInfo"));
            NftPageMoreInfo.FindElement(By.CssSelector(".LibraryTabsItem[data-value='details']")).Click();
            IWebElement eother = soxDetails.FindElement(By.ClassName("NftPageDetails__other-card"));
            contract_href = eother.FindElement(By.TagName("a")).GetAttribute("href");                   //kostil'

            uri = new Uri(contract_href);
            nftContrAddr = uri.Segments[1];
            owner_href = soxDetails.FindElement(By.ClassName("NftPageOwner--profile")).GetAttribute("href");

            js.ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles[2]);
            driver.Navigate().GoToUrl(owner_href);
            ownerWindow = driver.CurrentWindowHandle;
            Thread.Sleep(1000);
            IWebElement EntityPageInfoCard = driver.FindElement(By.ClassName("EntityPageInfoCard"));
            nftOwner_tonviewer_href = EntityPageInfoCard.FindElement(By.TagName("a")).GetAttribute("href");
            uri = new Uri(nftOwner_tonviewer_href);
            nftOwnerAddr = uri.Segments[1];

            js.ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles[3]);
            driver.Navigate().GoToUrl(nftOwner_tonviewer_href + "?section=tokens");

            IWebElement tview_ftable = driver.FindElement(By.ClassName("c1tsewex"));////////////////////////////
            IWebElement tview_ptable = tview_ftable.FindElement(By.ClassName("t1ik79b9"));//////////////////////
            IReadOnlyCollection<IWebElement> tokens = tview_ptable.FindElements(By.ClassName("t1ytm1tj"));

            numberstr = "0";
            foreach (IWebElement token in tokens)
            {
                IWebElement token_mainpath = token.FindElement(By.ClassName("t1r1kjal")); // Найти элемент, содержащий основной текст///////////////
                if (token_mainpath.Text.Contains("Nobby Game"))
                {
                    IWebElement token_labelpath = token.FindElement(By.ClassName("tkf4fqk"));///////////////////////////////
                    numberstr = token_labelpath.Text.Replace("\r\nSOX","").Replace(",", "");
                    break;
                }
            }
            File.AppendAllText(filePath, $"{soxname},{nftContrAddr},{nftOwnerAddr},{numberstr}\n");
            Console.WriteLine($"{soxname},{nftContrAddr},{nftOwnerAddr},{numberstr}");
            driver.Close(); // закрываем текущую вкладку
            driver.SwitchTo().Window(ownerWindow); // возвращаемся к предыдущей вкладке
            driver.Close(); // закрываем текущую вкладку
            driver.SwitchTo().Window(sockWindow); // возвращаемся к предыдущей вкладке
            driver.Close(); // закрываем текущую вкладку
            driver.SwitchTo().Window(sockListWindow); // возвращаемся к предыдущей вкладке
        }
        driver.Close(); // закрываем текущую вкладку
        Console.WriteLine("theendddddddddddd");
    }
}
