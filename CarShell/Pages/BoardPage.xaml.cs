using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CarShell.Pages
{
    public partial class BoardPage : UserControl
    {
        private readonly MainWindow mainWindow;
        private List<DataItem> currentItems = new();

        public BoardPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            ShowEcu();
        }

        private void SetItems(List<DataItem> items)
        {
            currentItems = items;
            RefreshList();
        }

        private void RefreshList()
        {
            string q = SearchBox.Text?.ToLower() ?? "";

            DataList.ItemsSource = currentItems
                .Where(x => x.Name.ToLower().Contains(q))
                .ToList();
        }

        private void ShowEcu()
        {
            SetItems(new List<DataItem>
    {
        new("★", "Обороты двигателя", "856", "об/мин", "OK"),
        new("★", "Скорость автомобиля", "0", "км/ч", "OK"),
        new("★", "Нагрузка двигателя", "18", "%", "OK"),
        new("★", "Температура ОЖ", "84", "°C", "OK"),
        new("", "Температура воздуха", "32", "°C", "OK"),
        new("", "Температура топлива", "38", "°C", "OK"),
        new("", "Температура масла", "79", "°C", "OK"),
        new("★", "Напряжение АКБ", "14.2", "В", "OK"),

        new("★", "MAF расход воздуха", "14.2", "кг/ч", "OK"),
        new("", "MAF g/s", "3.9", "г/с", "OK"),
        new("★", "MAP давление", "101", "кПа", "OK"),
        new("★", "Boost фактический", "101", "кПа", "OK"),
        new("", "Boost заданный", "103", "кПа", "OK"),
        new("", "Ошибка наддува", "-2", "кПа", "OK"),
        new("", "Атмосферное давление", "99", "кПа", "OK"),
        new("", "Скважность N75", "63", "%", "OK"),
        new("", "Положение геометрии турбины", "42", "%", "OK"),

        new("", "Положение EGR", "0", "%", "OK"),
        new("", "Команда EGR", "0", "%", "OK"),
        new("", "Положение дросселя", "4", "%", "OK"),
        new("", "Команда дросселя", "4", "%", "OK"),

        new("", "Давление топлива фактическое", "298", "бар", "OK"),
        new("", "Давление топлива заданное", "300", "бар", "OK"),
        new("", "Ошибка давления топлива", "-2", "бар", "OK"),
        new("", "Количество впрыска", "6.4", "мг/такт", "OK"),
        new("", "Время впрыска", "0.82", "мс", "OK"),

        new("", "Коррекция цилиндра 1", "+0.12", "мг", "OK"),
        new("", "Коррекция цилиндра 2", "-0.08", "мг", "OK"),
        new("", "Коррекция цилиндра 3", "+0.04", "мг", "OK"),
        new("", "Коррекция цилиндра 4", "-0.10", "мг", "OK"),

        new("", "DPF дифф. давление", "0.0", "кПа", "OK"),
        new("", "DPF заполнение", "34", "%", "OK"),
        new("", "DPF сажа", "12", "г", "OK"),
        new("", "DPF зола", "42", "г", "OK"),
        new("", "Температура до DPF", "245", "°C", "OK"),
        new("", "Температура после DPF", "198", "°C", "OK"),
        new("", "Регенерация DPF", "Нет", "", "OK"),
        new("", "Пробег после регенерации", "7200", "км", "OK"),

        new("", "Свечи накала", "OFF", "", "OK"),
        new("", "Реле свечей", "OFF", "", "OK"),
        new("", "Время накала", "0", "с", "OK"),

        new("", "Расход топлива мгновенный", "0.7", "л/ч", "OK"),
        new("", "Средний расход", "6.2", "л/100км", "OK"),
        new("", "Пробег после запуска", "0.0", "км", "OK"),
        new("", "Время работы двигателя", "00:04:12", "", "OK")
    });
        }

        private void ShowAbs()
        {
            SetItems(new List<DataItem>
    {
        new("★", "Скорость колеса FL", "0", "км/ч", "OK"),
        new("★", "Скорость колеса FR", "0", "км/ч", "OK"),
        new("★", "Скорость колеса RL", "0", "км/ч", "OK"),
        new("★", "Скорость колеса RR", "0", "км/ч", "OK"),
        new("", "Педаль тормоза", "OFF", "", "OK"),
        new("", "Давление тормозов", "0", "бар", "OK"),
        new("", "ABS Active", "Нет", "", "OK"),
        new("", "ESP Active", "Нет", "", "OK"),
        new("", "Traction Control", "Нет", "", "OK"),
        new("", "Угол руля", "0", "°", "OK"),
        new("", "Yaw Rate", "0.0", "°/с", "OK"),
        new("", "Поперечное ускорение", "0.00", "g", "OK"),
        new("", "Продольное ускорение", "0.00", "g", "OK")
    });
        }

        private void ShowBody()
        {
            SetItems(new List<DataItem>
            {
                new("★", "Двери", "Закрыты", "", "OK"),
                new("★", "Свет", "AUTO", "", "OK"),
                new("", "Поворотники", "OFF", "", "OK"),
                new("", "Ручник", "OFF", "", "OK"),
                new("", "Яркость салона", "72", "%", "OK"),
                new("★", "Напряжение", "14.2", "В", "OK"),
            });
        }

        private void ShowFav()
        {
            SetItems(new List<DataItem>
            {
                new("★", "Обороты двигателя", "856", "об/мин", "OK"),
                new("★", "Скорость", "0", "км/ч", "OK"),
                new("★", "Температура ОЖ", "84", "°C", "OK"),
                new("★", "MAF", "14.2", "кг/ч", "OK"),
                new("★", "MAP / Boost", "101", "кПа", "OK"),
                new("★", "Напряжение АКБ", "14.2", "В", "OK"),
            });
        }

        private void Ecu_Click(object sender, RoutedEventArgs e) => ShowEcu();
        private void Abs_Click(object sender, RoutedEventArgs e) => ShowAbs();
        private void Body_Click(object sender, RoutedEventArgs e) => ShowBody();
        private void Fav_Click(object sender, RoutedEventArgs e) => ShowFav();
        private void All_Click(object sender, RoutedEventArgs e) => ShowEcu();

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshList();
    }

    public record DataItem(string FavoriteMark, string Name, string Value, string Unit, string Status);
}