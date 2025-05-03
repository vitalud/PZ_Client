using Client.Model;
using Client.Service.Abstract;

namespace Client.ViewModel
{
    public class QuikViewModel(QuikModel quik) : CommonTradeViewModel(quik) { }
}
