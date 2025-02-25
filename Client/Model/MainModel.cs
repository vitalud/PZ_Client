using Client.Service.Abstract;

namespace Client.Model
{
    /// <summary>
    /// Класс, представляющий собой основную модель приложения
    /// </summary>
    public class MainModel(Connector connector)
    {
        private readonly Connector _connector = connector;

        public void CloseApplication()
        {
            _connector.Close();
        }
    }
}
