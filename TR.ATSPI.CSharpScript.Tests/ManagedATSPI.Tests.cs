using NUnit.Framework;

using System.Threading.Tasks;

namespace TR.ATSPI.CSharpScript.Tests
{
	public class ManagedATSPITests
	{
		const int CreateActionFromScriptStringTest_Number = 123;
		static readonly string CreateActionFromScriptStringTest_Script = $"BrakePosReturn = {CreateActionFromScriptStringTest_Number}";

		[Test]
		public async Task CreateActionFromScriptStringTest()
		{
			var func = ManagedATSPI.CreateActionFromScriptString(CreateActionFromScriptStringTest_Script);

			Assert.IsNotNull(func);

			if (func is null)
				return;

			GlobalVariable gv = new();
			gv.BrakePosReturn = 20;
			await func.Invoke(gv);
			Assert.AreEqual(CreateActionFromScriptStringTest_Number, gv.BrakePosReturn);
		}
	}
}