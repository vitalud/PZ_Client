using Client.Model.Burse;
using Client.Service.Abstract;

namespace Client.ViewModel.Burse
{
    public partial class BinanceViewModel(BinanceModel binanceModel) : BurseViewModel(binanceModel) { }
}
