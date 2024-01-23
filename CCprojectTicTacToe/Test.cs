using Xunit;

namespace CCproject;

public class Test {
	[Fact]
	public void DatabaseConnectTest() {
		DatabaseConnect db = new DatabaseConnect();

		Assert.False(db.spinning);
		Assert.False(db.game_info is not null);
	}
}
