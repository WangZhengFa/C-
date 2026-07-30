using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodEnterpriseIMS.Helpers
{
    /// <summary>
    /// 列显示顺序项
    /// </summary>
    public sealed class DisplayOrderItem : INotifyPropertyChanged
    {
        private bool _isVisible;

        public string Header { get; set; } = string.Empty;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 字段详细设置行
    /// </summary>
    public sealed class ColumnSettingRow : INotifyPropertyChanged
    {
        private string _widthMode = "自动";
        private int _fixedWidth = 120;
        private bool _isLeft;
        private bool _isCenter;
        private bool _isRight;
        private string _sort = "不排";

        public string Header { get; set; } = string.Empty;

        public string WidthMode
        {
            get => _widthMode;
            set
            {
                if (SetProperty(ref _widthMode, value))
                {
                    OnPropertyChanged(nameof(IsFixedWidth));
                }
            }
        }

        public bool IsFixedWidth => WidthMode == "固定";

        public int FixedWidth
        {
            get => _fixedWidth;
            set => SetProperty(ref _fixedWidth, value);
        }

        public bool IsLeft
        {
            get => _isLeft;
            set
            {
                if (!SetProperty(ref _isLeft, value)) return;
                if (value)
                {
                    IsCenter = false;
                    IsRight = false;
                }
            }
        }

        public bool IsCenter
        {
            get => _isCenter;
            set
            {
                if (!SetProperty(ref _isCenter, value)) return;
                if (value)
                {
                    IsLeft = false;
                    IsRight = false;
                }
            }
        }

        public bool IsRight
        {
            get => _isRight;
            set
            {
                if (!SetProperty(ref _isRight, value)) return;
                if (value)
                {
                    IsLeft = false;
                    IsCenter = false;
                }
            }
        }

        public string Sort
        {
            get => _sort;
            set => SetProperty(ref _sort, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 设置控件视图模型
    /// </summary>
    public sealed class DataGridSettingsEditorViewModel : INotifyPropertyChanged
    {
        private int _rowHeight = 22;
        private int _fontSize = 12;
        private int _maxDisplayRows;
        private bool _tableHeightEnabled;
        private string _tableHeightMode = "固定高度";
        private int _tableHeight = 300;
        private bool _allowTableHeight;

        public ObservableCollection<DisplayOrderItem> DisplayItems { get; } = new();
        public ObservableCollection<ColumnSettingRow> FieldRows { get; } = new();

        public int RowHeight
        {
            get => _rowHeight;
            set => SetProperty(ref _rowHeight, value);
        }

        public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public int MaxDisplayRows
        {
            get => _maxDisplayRows;
            set => SetProperty(ref _maxDisplayRows, value);
        }

        public bool TableHeightEnabled
        {
            get => _tableHeightEnabled;
            set => SetProperty(ref _tableHeightEnabled, value);
        }

        public bool TableHeightDisabled
        {
            get => !_tableHeightEnabled;
            set => TableHeightEnabled = !value;
        }

        public string TableHeightMode
        {
            get => _tableHeightMode;
            set => SetProperty(ref _tableHeightMode, value);
        }

        public int TableHeight
        {
            get => _tableHeight;
            set => SetProperty(ref _tableHeight, value);
        }

        public bool AllowTableHeight
        {
            get => _allowTableHeight;
            set => SetProperty(ref _allowTableHeight, value);
        }

        public List<string> TableHeightModes { get; } = new() { "固定高度", "自适应" };

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
