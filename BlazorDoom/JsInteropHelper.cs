namespace BlazorDoom
{
    using Microsoft.JSInterop;
    using System.Threading.Tasks;

    public class JsInteropHelper
    {
        private readonly IJSRuntime _jsRuntime;

        public JsInteropHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string> GetWindowLocationHref()
        {
            return await _jsRuntime.InvokeAsync<string>("getWindowLocationHref");
        }
    }

}
