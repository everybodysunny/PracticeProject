using PracticeProject.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PracticeProject.ViewModels
{
    /// <summary>
    /// SQL 数据库连接 ViewModel
    /// </summary>
    public class SqlConnectionViewModel : BindableBase
    {
        private readonly Dispatcher _dispatcher;

        #region 连接配置

        private string _server = "localhost";
        public string Server
        {
            get => _server;
            set => SetProperty(ref _server, value);
        }

        private string _database = "MyDB";
        public string Database
        {
            get => _database;
            set => SetProperty(ref _database, value);
        }

        private string _username = "sa";
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _isWindowsAuth = true;
        public bool IsWindowsAuth
        {
            get => _isWindowsAuth;
            set
            {
                if (SetProperty(ref _isWindowsAuth, value))
                {
                    RaisePropertyChanged(nameof(IsSqlAuth));
                }
            }
        }

        public bool IsSqlAuth => !IsWindowsAuth;

        private int _connectTimeout = 5;
        public int ConnectTimeout
        {
            get => _connectTimeout;
            set => SetProperty(ref _connectTimeout, value);
        }

        #endregion

        #region 状态

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(CanConnect));
                    RaisePropertyChanged(nameof(CanDisconnect));
                    RaisePropertyChanged(nameof(CanExecute));
                    ConnectCommand.RaiseCanExecuteChanged();
                    DisconnectCommand.RaiseCanExecuteChanged();
                    ExecuteQueryCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanConnect => !IsConnected && !IsConnecting;
        public bool CanDisconnect => IsConnected && !IsConnecting;
        public bool CanExecute => IsConnected && !IsConnecting;

        public string StatusText => IsConnected ? "已连接" : (IsConnecting ? "连接中..." : "未连接");

        private bool _isConnecting;
        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                if (SetProperty(ref _isConnecting, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(CanConnect));
                    RaisePropertyChanged(nameof(CanDisconnect));
                    RaisePropertyChanged(nameof(CanExecute));
                    ConnectCommand.RaiseCanExecuteChanged();
                    DisconnectCommand.RaiseCanExecuteChanged();
                    ExecuteQueryCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _lastError = string.Empty;
        public string LastError
        {
            get => _lastError;
            set => SetProperty(ref _lastError, value);
        }

        #endregion

        #region 查询

        private string _querySql =
            "SELECT TOP 100 * FROM dbo.DeviceData ORDER BY Timestamp DESC";
        public string QuerySql
        {
            get => _querySql;
            set => SetProperty(ref _querySql, value);
        }

        private DataView? _queryResult;
        public DataView? QueryResult
        {
            get => _queryResult;
            set => SetProperty(ref _queryResult, value);
        }

        private int _resultRowCount;
        public int ResultRowCount
        {
            get => _resultRowCount;
            set
            {
                if (SetProperty(ref _resultRowCount, value))
                {
                    RaisePropertyChanged(nameof(ResultRowCountText));
                }
            }
        }

        public string ResultRowCountText => $"共 {ResultRowCount} 行";

        private double _queryDurationMs;
        public double QueryDurationMs
        {
            get => _queryDurationMs;
            set
            {
                if (SetProperty(ref _queryDurationMs, value))
                {
                    RaisePropertyChanged(nameof(QueryDurationText));
                }
            }
        }

        public string QueryDurationText => $"耗时: {QueryDurationMs:F0} ms";

        #endregion

        #region 快速查询模板

        public ObservableCollection<QueryTemplate> QueryTemplates { get; } = new()
        {
            new() { Name = "查询最近 100 条数据", Sql = "SELECT TOP 100 * FROM dbo.DeviceData ORDER BY Timestamp DESC" },
            new() { Name = "按设备分组统计", Sql = "SELECT DeviceId, COUNT(*) AS Total, AVG(CastValue) AS AvgValue FROM dbo.DeviceData GROUP BY DeviceId" },
            new() { Name = "查询今日数据", Sql = "SELECT * FROM dbo.DeviceData WHERE CAST(Timestamp AS DATE) = CAST(GETDATE() AS DATE) ORDER BY Timestamp DESC" },
            new() { Name = "查询所有设备", Sql = "SELECT DISTINCT DeviceId FROM dbo.DeviceData" },
        };

        private QueryTemplate? _selectedTemplate;
        public QueryTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (SetProperty(ref _selectedTemplate, value))
                {
                    if (value != null)
                    {
                        QuerySql = value.Sql;
                    }
                }
            }
        }

        #endregion

        #region 集合

        public ObservableCollection<string> LogMessages { get; } = new();

        #endregion

        #region 命令

        public AsyncDelegateCommand ConnectCommand { get; }
        public AsyncDelegateCommand DisconnectCommand { get; }
        public AsyncDelegateCommand TestConnectionCommand { get; }
        public AsyncDelegateCommand ExecuteQueryCommand { get; }
        public AsyncDelegateCommand ClearLogCommand { get; }

        #endregion

        public SqlConnectionViewModel()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            ConnectCommand = new AsyncDelegateCommand(ConnectAsync, () => CanConnect);
            DisconnectCommand = new AsyncDelegateCommand(DisconnectAsync, () => CanDisconnect);
            TestConnectionCommand = new AsyncDelegateCommand(TestConnectionAsync, () => !IsConnecting);
            ExecuteQueryCommand = new AsyncDelegateCommand(ExecuteQueryAsync, () => CanExecute);
            ClearLogCommand = new AsyncDelegateCommand(ClearLogAsync);

            AddLog("系统", "SQL ViewModel 已初始化");
        }

        #region 命令实现

        private async Task ConnectAsync()
        {
            try
            {
                IsConnecting = true;
                LastError = string.Empty;
                AddLog("系统", $"正在连接到 {Server}/{Database}...");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ConnectTimeout + 2));
                var connectionString = BuildConnectionString();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cts.Token);

                IsConnected = true;
                AddLog("成功", $"已连接到 {Server}/{Database}");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastError = ex.Message;
                AddLog("错误", $"连接失败: {ex.Message}");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        private async Task DisconnectAsync()
        {
            try
            {
                AddLog("系统", "正在断开连接...");
                await Task.Delay(100);
                IsConnected = false;
                QueryResult = null;
                ResultRowCount = 0;
                AddLog("成功", "已断开连接");
            }
            catch (Exception ex)
            {
                AddLog("错误", $"断开失败: {ex.Message}");
            }
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                IsConnecting = true;
                AddLog("系统", "正在测试连接...");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ConnectTimeout + 2));
                var connectionString = BuildConnectionString();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cts.Token);

                AddLog("成功", "连接测试成功");
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AddLog("错误", $"连接测试失败: {ex.Message}");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        private async Task ExecuteQueryAsync()
        {
            if (string.IsNullOrWhiteSpace(QuerySql))
            {
                AddLog("错误", "SQL 语句不能为空");
                return;
            }

            try
            {
                IsConnecting = true;
                AddLog("查询", $"执行: {QuerySql.Substring(0, Math.Min(50, QuerySql.Length))}...");

                var startTime = DateTime.Now;
                var connectionString = BuildConnectionString();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(QuerySql, connection)
                {
                    CommandTimeout = 30
                };
                using var adapter = new SqlDataAdapter(command);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                QueryResult = dataTable.DefaultView;
                ResultRowCount = dataTable.Rows.Count;
                QueryDurationMs = (DateTime.Now - startTime).TotalMilliseconds;

                AddLog("成功", $"查询完成: {ResultRowCount} 行, 耗时 {QueryDurationMs:F0} ms");
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AddLog("错误", $"查询失败: {ex.Message}");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        private Task ClearLogAsync()
        {
            LogMessages.Clear();
            return Task.CompletedTask;
        }

        #endregion

        #region 辅助方法

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                InitialCatalog = Database,
                ConnectTimeout = ConnectTimeout
            };

            if (IsWindowsAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = Username;
                builder.Password = Password;
            }

            return builder.ConnectionString;
        }

        private void AddLog(string level, string message)
        {
            _dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogMessages.Insert(0, $"[{timestamp}] [{level}] {message}");

                while (LogMessages.Count > 200)
                {
                    LogMessages.RemoveAt(LogMessages.Count - 1);
                }
            });
        }

        #endregion
    }

    /// <summary>
    /// 查询模板
    /// </summary>
    public class QueryTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Sql { get; set; } = string.Empty;

        public override string ToString() => Name;
    }
}
