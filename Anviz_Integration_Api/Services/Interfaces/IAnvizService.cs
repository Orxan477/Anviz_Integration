using System.Text.Json;

namespace Anviz_Integration_Api.Services.Interfaces
{
	public interface IAnvizService
	{
		Task GetToken();
		Task GetRecord(string tokenRequestCustom);
		Task Bitrix(int id);
    }
}

