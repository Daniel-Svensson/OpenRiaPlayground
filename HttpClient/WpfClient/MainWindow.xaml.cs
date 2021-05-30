using OpenRiaServices.Client;
using OpenRiaServices.Client.Authentication;
using OpenRiaServices.Client.HttpDomainClient;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using TestDomainServices;

namespace HttpClientExampleClient
{
    public partial class MainWindow : Window
    {
        ServerSideAsyncDomainContext _ctx = new ServerSideAsyncDomainContext();
        private CookieContainer _cookieContainer = new CookieContainer();

        public MainWindow()
        {
            InitializeComponent();

            this.entities.ItemsSource = _ctx.RangeItems;
            this._items.ItemsSource = _ctx.RangeItems;
        }

        private void SetupNewDomainContext()
        {
            _ctx = new ServerSideAsyncDomainContext();
            this.entities.ItemsSource = _ctx.RangeItems;
            this._items.ItemsSource = _ctx.RangeItems;
        }

        public string Status1
        {
            get { return _staus1; }
            set
            {
                _staus1 = DateTime.Now.ToString() + ": " + value; ;
                txt.Text = (_staus1 + Environment.NewLine + _staus2);
            }
        }
        private string _staus1 = string.Empty;

        public string Status2
        {
            get { return _staus2; }
            set
            {
                _staus2 = DateTime.Now.ToString() + ": " + value;
                txt.Text = (_staus1 + Environment.NewLine + _staus2);
            }
        }

        private string _staus2 = string.Empty;

        private void Invoke1_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Status1 = string.Format("starting Invoke 1");
                var res = _ctx.AddOne(23);
                res.Completed += (o, args) =>
                {
                    Status1 = string.Format("AddOneTaskAsync(23) = {0}", res.Value);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error" + ex.Message);
            }
        }

        private async void Invoke2_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Status2 = string.Format("starting Invoke 2");
                var res = await _ctx.AddOneAsync(22);
                Status2 = string.Format("AddOneTaskAsync(22) = {0}", res.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error" + ex.Message);
            }
        }

        private void Get_Click(object sender, RoutedEventArgs e)
        {
            var query = _ctx.GetRangeQuery();
            query.IncludeTotalCount = true;
            query = (from r in query
                     where r.Id > 1
                     orderby r.Id descending
                     select r
                    ).Take(2);
            _ctx.Load(query
               , res =>
               {
                   entities.ItemsSource = res.Entities;
                   _items.ItemsSource = _ctx.RangeItems;
               }, null);
        }

        private void Async1_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetRangeByIdTaskQuery(1), (res) =>
            {
                if (res.HasError)
                {
                    res.MarkErrorAsHandled();
                    async1Res.Text = res.Error.ToString();
                }
                else
                    async1Res.Text = string.Format("Returned a {0}", res.Entities.FirstOrDefault());
            }, null);
        }

        private void Async2_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetQueryableRangeTaskQuery(), (res) =>
            {
                if (res.HasError)
                {
                    res.MarkErrorAsHandled();
                    async2Res.Text = res.Error.ToString();
                }
                else
                    async2Res.Text = string.Format("Returned a {0}", res.Entities.FirstOrDefault());
            }, null);
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            _ctx.SubmitChanges(res =>
            {
                if (res.HasError)
                {
                    res.MarkErrorAsHandled();
                    submitRes.Text = res.Error.ToString();
                }
                else
                    submitRes.Text = "Submit ok";
            }, null);
        }

        private void NormalException_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetRangeWithNormalExceptionQuery(), res =>
            {
                UpdateResults(res, _normalExceptionRes, "Normal exception");
            }, null);
        }

        private void NotAuthorized_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetRangeWithNotAuthorizedQuery(), res =>
            {
                UpdateResults(res, _notAuthorizedRes, "Mot authorized");
            }, null);
        }

        private void DomainException_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetQueryableRangeWithExceptionFirstQuery(), res =>
            {
                UpdateResults(res, _domainExceptionRes, "Domain exception");
            }, null);
        }

        private static void UpdateResults(LoadOperation<RangeItem> res, TextBlock textBox, string operation)
        {
            if (res.HasError)
            {
                res.MarkErrorAsHandled();
                textBox.Text = string.Format("{0} had error of type {1} with message '{2}'",
                        operation,
                        res.Error.GetType(),
                        res.Error.Message);
            }
            else
                textBox.Text = string.Format("{0} loaded {1} entities successfully", operation, res.Entities.Count());
        }

        private static void UpdateResults<T>(InvokeOperation<T> res, TextBlock textBox, string operation)
        {
            if (res.HasError)
            {
                res.MarkErrorAsHandled();
                textBox.Text = string.Format("{0} had error of type {1} with message '{2}'",
                        operation,
                        res.Error.GetType(),
                        res.Error.Message);
            }
            else
                textBox.Text = string.Format("{0} invoked successfully with result {1}", operation, res.Value);
        }

        private void InvokeComplex_OnClick(object sender, RoutedEventArgs e)
        {
            ComplexType2 c = new ComplexType2 { A = 23, B = "Lite text" };
            _ctx.GetRangeWithComplexParameterPOST(c, 23, "heJ", res =>
            {
                UpdateResults(res, _invokeComplexRes1, "GetRangeWithComplexParameterPOST");
            }, null);

            _ctx.GetRangeWithComplexParameterGET(c, 23, "heJ", res =>
            {
                UpdateResults(res, _invokeComplexRes2, "GetRangeWithComplexParameterGET");
            }, null);
        }

        private void Login_OnClick(object sender, RoutedEventArgs e)
        {
            var loginParameters = new LoginParameters(_loginName.Text, _loginPassword.Text);

            WebContext.Current.Authentication.Login(loginParameters,
                (res) =>
                {
                    if (res.LoginSuccess)
                        _userName.Text = res.User.Identity.Name;
                    else if (!res.HasError)
                        _userName.Text = "Login failed (Wrong username password?)";
                    else
                        _userName.Text = string.Format("Login error: {0}", res.Error);
                }, null);
        }

        private void Logout_OnClick(object sender, RoutedEventArgs e)
        {
            WebContext.Current.Authentication.Logout(false);
        }

        private void OpenRiaWeb_Click(object sender, RoutedEventArgs e)
        {
            DomainContext.DomainClientFactory = new OpenRiaServices.Client.Web.WebDomainClientFactory()
            {
                ServerBaseUri = GetServerBaseUri(),
                CookieContainer = _cookieContainer,
            };
            SetupNewDomainContext();
        }

        private void OpenRiaPortableWeb_Click(object sender, RoutedEventArgs e)
        {
            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory()
            {
                ServerBaseUri = GetServerBaseUri(),
                HttpClientHandler = new HttpClientHandler()
                {
                    CookieContainer = _cookieContainer,
                    UseCookies = true,
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                }
            };
            SetupNewDomainContext();
        }

        private static Uri GetServerBaseUri()
        {
#if SILVERLIGHT
            return Application.Current.Host.Source;
#else
            return new Uri("https://localhost:44300/ClientBin/", UriKind.Absolute);
#endif
        }

        private void OpenRiaPortableWebWinHttp_Click(object sender, RoutedEventArgs e)
        {

            // Enable HTTP/2 support
            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory()
            {
                ServerBaseUri = GetServerBaseUri(),
                HttpClientHandler = new Http2CustomHandler()
                {
                    CookieContainer = _cookieContainer, 
                    CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer,
                    AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip,
                    WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy,
                },
            };
            SetupNewDomainContext();
        }

        private void WcfSoapClient_Click(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).IsChecked != true)
                return;

            DomainContext.DomainClientFactory = new OpenRiaServices.Client.Web.SoapDomainClientFactory()
            {
                ServerBaseUri = GetServerBaseUri(),
            };
            SetupNewDomainContext();
        }
    }
}
