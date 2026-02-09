using Newtonsoft.Json;
using System.Text;
using ATHMovil.Purchase.Storage;
using ATHMovil.Purchase.Model;
using ATHMovil.Purchase.Model.Manager;
using ATHMovil.PurchaseSDK.String;
using ATHMovil.Purchase.Internal;

namespace ATHMovil.Purchase.Utils
{
	public class PaymentServices : BindableObject
    {

        public Command CallApiCommand { get; set; }
        internal HttpClient _client;
        string scheme = SDKGlobal.Instance().Scheme ?? "N/A";

        private string Host
        {
            get{
                return PurchaseManager.SharedInstance.CurrentAWSTarget;
            }
        }
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set{
                _isBusy = value;
                OnPropertyChanged();
                OnIsBusyChanged();
            }
        }

        public PaymentServices() {
            var httpClientHandler = new HttpClientHandler();

            httpClientHandler.ServerCertificateCustomValidationCallback =
            (message, cert, chain, errors) => { return true; };

            _client = new HttpClient(httpClientHandler);
            _client.Timeout = TimeSpan.FromSeconds(30);

            IsBusy = false;
        }

        public async Task<string> PaymentServicesCall(PurchaseRequest purchase)
        {
            return await CallApi(purchase);
        }

        private void OnIsBusyChanged(){
            if (IsBusy){
                LoadingView.ShowLoadingOverlay();
            }else{
                LoadingView.HideLoadingOverlay();
            }
        }

        internal async Task<string> CallApi(PurchaseRequest request){

            IsBusy = true;

            try{
                string json =  JsonConvert.SerializeObject(request.Purchase);
                HttpContent callContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient client = new HttpClient())
                {
                    // Agregar la cabecera "Host"
                    client.DefaultRequestHeaders.Add("Host", Host);
                    HttpResponseMessage response = await client.PostAsync("https://"+ Host + "/api/business-transaction/ecommerce/payment", callContent);
                
                    if (response.IsSuccessStatusCode){
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<PaymentResponseModel>(content);
                        if (!string.IsNullOrEmpty(result.getData().getEcommerce()) && !string.IsNullOrEmpty(result.getData().getToken())){
                            SDKGlobal.Instance().EcommerceID = result.getData().getEcommerce();
                            SDKGlobal.Instance().Token = result.getData().getToken();
                             NewRelicConfig.SendEventToNewRelic(
                                eventType: NewRelicConstants.NR.INIT_PAYMENT_SUCCESS,
                                paymentReference: result.getData().getEcommerce(),
                                paymentStatus: NewRelicConstants.NR.SUCCESS_PAYMENT,
                                merchantAppId: scheme,
                                buildType: GetBuildType()
                            ); 
                            return result.getData().getEcommerce(); 

                        }else {
                            verificarError(result);
                        }
                    }else{
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<PaymentResponseModel>(content);
                        verificarError(result);
                    }
                }
            }
            catch (System.Exception ex){
                IsBusy = false;
                _ = genericErrorAsync();
            }
            finally{
                IsBusy = false;
            }

            return null;
        }

        public async void verificarError(PaymentResponseModel error) {
            if (error.getErrorcode() != null && error.getErrorcode().Equals("BCUS_0092")) {
                await Task.Delay(1000);
                _ = Application.Current.MainPage.DisplayAlert(StringMensaje.GetBusinessErrorTitle(), StringMensaje.GetBusinessErrorMessage(), "OK", FlowDirection.MatchParent);

                   NewRelicConfig.SendEventToNewRelic(
                            eventType: NewRelicConstants.NR.INIT_PAYMENT_FAILURE,
                            paymentReference: error.getMessage() ?? "N/A",
                            paymentStatus: NewRelicConstants.NR.FAILED_PAYMENT,
                            merchantAppId: scheme,
                            buildType: GetBuildType()
            );
            }
            else {
                _ = genericErrorAsync();
            }
        }

        public async Task genericErrorAsync() {
            await Task.Delay(1000);
            _ = Application.Current.MainPage.DisplayAlert(StringMensaje.GetGenericErrorTitle(), StringMensaje.GetGenericErrorMessage(), "OK", FlowDirection.MatchParent);
        }



         private string GetBuildType()
        {
         #if DEBUG
            return "QA";
         #else
            return "PROD";
         #endif
        }
    }
}

