using Client.Model.Crypto;
using Client.Service.Abstract;

namespace Client.ViewModel.Crypto
{
    public partial class OkxViewModel(OkxModel okxModel) : CommonTradeViewModel(okxModel) { }
}
