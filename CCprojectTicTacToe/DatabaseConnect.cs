using Microsoft.Data.SqlClient;

namespace CCproject {
	public class DatabaseConnect {
		public string username = "";
		public int username_id = 0;
		public int current_room_id = 0;

		public string opponent_name;
		public string opponent_info;
		public int turn = -1;

		public int room_owner;

		public bool spinning = false;
		public bool resettime = false;
		public bool winorlose = false;
		public bool opponentAbortCall = false;
		public bool moveAbortCall = false;

		public string spot11 = "";
		public string spot12 = "";
		public string spot13 = "";
		public string spot21 = "";
		public string spot22 = "";
		public string spot23 = "";
		public string spot31 = "";
		public string spot32 = "";
		public string spot33 = "";

		public string game_info;

		public DatabaseConnect() { }

		public string moveToRoomList(string username) {
			if (username.Length < 1) {
				return "Username must be at least 1 character long";
			}
			else if (username.Length > 20) {
				return "Username cannot be longer than 20 characters";
			}
			else {
				using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand check_available = new SqlCommand("select count(*) from Users where username=@username", connection);
					_ = check_available. Parameters.AddWithValue("username", username);

					SqlCommand command = new SqlCommand("insert into Users values (@username)", connection);
					_ = command.Parameters.AddWithValue("username", username);

					try {
						if (((int)check_available.ExecuteScalar()) != 0) {
							return "Chosen username is already in use";
						}
						else {
							this.username = username;

							_ = command.ExecuteNonQuery();

							SqlCommand checkID = new SqlCommand("select ID from Users where username=@username", connection);
							_ = checkID.Parameters.AddWithValue("username", this.username);

							SqlDataReader sdr = checkID.ExecuteReader();
							while (sdr.Read()) {
								username_id = int.Parse(sdr["ID"].ToString());
							}
							sdr.Dispose();
						}
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				return "";
			}
		}

		public void createNewRoom() {
			opponentAbortCall = false;
			moveAbortCall = false;

			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command = new SqlCommand("insert into Rooms (Player1_ID, Player2_ID, Turn, rst, win) values (@player1_id, 0, 1, 'false', 0)", connection);
				_ = command.Parameters.AddWithValue("player1_id", username_id);

				try {
					_ = command.ExecuteNonQuery();

					SqlCommand checkID = new SqlCommand("select ID from Rooms where Player1_ID=@player1_id", connection);
					_ = checkID.Parameters.AddWithValue("player1_id", username_id);

					SqlDataReader sdr = checkID.ExecuteReader();
					while (sdr.Read()) {
						current_room_id = int.Parse(sdr["ID"].ToString());
					}
					sdr.Dispose();
				}
				catch (Exception ex) {
					Console.Write(ex.Message);
				}
			}

			room_owner = 1;
		}

		public void enterRoom(string selected_item) {
			opponentAbortCall = false;
			moveAbortCall = false;

			opponent_name = selected_item;

			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command = new SqlCommand("update Rooms set Player2_ID=@player2_id where Rooms.Player1_ID=(select ID from Users where username=@username)", connection);
				_ = command.Parameters.AddWithValue("player2_id", username_id);
				_ = command.Parameters.AddWithValue("username", selected_item);

				try {
					_ = command.ExecuteNonQuery();

					SqlCommand checkID = new SqlCommand("select ID from Rooms where Player1_ID=@player1_id", connection);
					_ = checkID.Parameters.AddWithValue("player1_id", username_id);

					SqlDataReader sdr = checkID.ExecuteReader();
					while (sdr.Read()) {
						current_room_id = int.Parse(sdr["ID"].ToString());
					}
					sdr.Dispose();
				}
				catch (Exception ex) {
					Console.Write(ex.Message);
				}
			}

			room_owner = 0;
		}

		public List<string> reloadList() {
			List<string> result_set = [];

			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command = new SqlCommand("select Rooms.ID, Users.username from Rooms join Users on Users.ID = Rooms.Player1_ID where Rooms.win=0", connection);

				try {
					SqlDataReader sdr = command.ExecuteReader();
					while (sdr.Read()) {
						result_set.Add(sdr["username"].ToString());
					}
					sdr.Dispose();
				}
				catch (Exception ex) {
					Console.Write(ex.Message);
				}
			}

			return result_set;
		}

		public void movetoMainMenu() {
			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command = new SqlCommand("delete from Users where username=@username", connection);
				_ = command.Parameters.AddWithValue("username", username);

				try {
					_ = command.ExecuteNonQuery();
				}
				catch (Exception ex) {
					Console.Write(ex.Message);
				}
			}
		}

		public void waitForOpponent() {
			opponent_name = "";
			opponent_info = "Waiting for a second player...";

			spinning = true;
			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command = new SqlCommand("select Rooms.Player2_ID, Users.username from Rooms join Users on Users.ID=Rooms.Player2_ID where Player1_ID=@player1_id", connection);
				_ = command.Parameters.AddWithValue("player1_id", username_id);

				while (true) {
					Thread.Sleep(5000);
					if (opponentAbortCall) {
						return;
					}

					try {
						SqlDataReader sdr = command.ExecuteReader();
						while (sdr.Read()) {
							if (int.Parse(sdr["Player2_ID"].ToString()) != 0) {
								opponent_info = "You are playing against: ";
								opponent_name = sdr["username"].ToString();
								game_info = "Your turn now";
								spinning = false;
								turn = 1;
								break;
							}
						}
						sdr.Dispose();

						if (spinning == false) {
							return;
						}
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}
			}
		}

		public void waitForNextMove() {
			opponent_info = "You are playing against: ";
			game_info = opponent_name + "'s turn now";

			spinning = true;

			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command;

				if (room_owner == 1) {
					command = new SqlCommand("select Turn, rst, win from Rooms where Player1_ID=@player1_id", connection);
					_ = command.Parameters.AddWithValue("player1_id", username_id);
				}
				else {
					command = new SqlCommand("select Turn, rst, win from Rooms where Player2_ID=@player2_id", connection);
					_ = command.Parameters.AddWithValue("player2_id", username_id);
				}

				while (true) {
					Thread.Sleep(1000);
					if (moveAbortCall) {
						return;
					}

					try {
						SqlDataReader sdr = command.ExecuteReader();
						while (sdr.Read()) {
							if ((int.Parse(sdr["Turn"].ToString()) == 1 && room_owner == 1) || (int.Parse(sdr["Turn"].ToString()) == 2 && room_owner == 0)) {
								spinning = false;
							}
							if ((int.Parse(sdr["win"].ToString()) == 2 && room_owner == 1) || (int.Parse(sdr["win"].ToString()) == 1 && room_owner == 0)) {
								spinning = false;
								winorlose = true;
							}
							if (sdr["rst"].ToString() == "true") {
								spinning = false;
								resettime = true;
							}
						}
						sdr.Dispose();

						if (spinning == false) {
							if (resettime == true) {
								this.resetBoard();
								return;
							}
							if (winorlose == true) {
								this.updateBoard();
								game_info = "You lost!";
								return;
							}
							else {
								this.updateBoard();
								turn = 1;
								game_info = "Your turn now";
								return;
							}
						}
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}
			}
		}

		public void updateBoard() {
			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command;

				if (room_owner == 1) {
					command = new SqlCommand("select * from Rooms where Player1_ID=@player1_id", connection);
					_ = command.Parameters.AddWithValue("player1_id", username_id);
				}
				else {
					command = new SqlCommand("select * from Rooms where Player2_ID=@player2_id", connection);
					_ = command.Parameters.AddWithValue("player2_id", username_id);
				}

				try {
					SqlDataReader sdr = command.ExecuteReader();
					while (sdr.Read()) {
						spot11 = sdr["oneone"].ToString();
						spot12 = sdr["onetwo"].ToString();
						spot13 = sdr["onethree"].ToString();
						spot21 = sdr["twoone"].ToString();
						spot22 = sdr["twotwo"].ToString();
						spot23 = sdr["twothree"].ToString();
						spot31 = sdr["threeone"].ToString();
						spot32 = sdr["threetwo"].ToString();
						spot33 = sdr["threethree"].ToString();
					}
					sdr.Dispose();
				}
				catch (Exception ex) {
					Console.Write(ex.Message);
				}
			}
		}

		public void resetBoard() {
			using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				SqlCommand command = new SqlCommand("update Rooms set Turn=1, oneone=NULL, onetwo=NULL, onethree=NULL, twoone=NULL, twotwo=NULL, twothree=NULL, threeone=NULL, threetwo=NULL, threethree=NULL, rst='false', win=0 where Player1_ID=@player1_id", connection);
				_ = command.Parameters.AddWithValue("player1_id", username_id);

				try {
					_ = command.ExecuteNonQuery();
				}
				catch (Exception ex) {
					Console.Write(ex.Message);
				}
			}
			opponent_name = "";
			opponent_info = "Waiting for a second player...";
			room_owner = 1;
			resettime = false;
			winorlose = false;
			turn = -1;

			this.updateBoard();

			Thread jumpstart = new Thread(new ThreadStart(this.waitForOpponent));
			jumpstart.Start();
		}

		public void winCheck() {
			if (room_owner == 1) {
				if ((spot11 == "X" && spot12 == "X" && spot13 == "X") ||
					(spot21 == "X" && spot22 == "X" && spot23 == "X") ||
					(spot31 == "X" && spot32 == "X" && spot33 == "X") ||
					(spot11 == "X" && spot21 == "X" && spot31 == "X") ||
					(spot12 == "X" && spot22 == "X" && spot32 == "X") ||
					(spot13 == "X" && spot23 == "X" && spot33 == "X") ||
					(spot11 == "X" && spot22 == "X" && spot33 == "X") ||
					(spot13 == "X" && spot21 == "X" && spot31 == "X")) {

					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command = new SqlCommand("update Rooms set win=1 where Player1_ID=@player1_id", connection);
						_ = command.Parameters.AddWithValue("player1_id", username_id);

						try {
							_ = command.ExecuteNonQuery();
							winorlose = true;
							game_info = "You won!";
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}
				}
			}
			else {
				if ((spot11 == "O" && spot12 == "O" && spot13 == "O") ||
					(spot21 == "O" && spot22 == "O" && spot23 == "O") ||
					(spot31 == "O" && spot32 == "O" && spot33 == "O") ||
					(spot11 == "O" && spot21 == "O" && spot31 == "O") ||
					(spot12 == "O" && spot22 == "O" && spot32 == "O") ||
					(spot13 == "O" && spot23 == "O" && spot33 == "O") ||
					(spot11 == "O" && spot22 == "O" && spot33 == "O") ||
					(spot13 == "O" && spot21 == "O" && spot31 == "O")) {

					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command = new SqlCommand("update Rooms set win=2 where Player2_ID=@player2_id", connection);
						_ = command.Parameters.AddWithValue("player2_id", username_id);

						try {
							_ = command.ExecuteNonQuery();
							winorlose = true;
							game_info = "You won!";
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}
				}
			}
		}

		public void spot11Click() {
			if (!winorlose) {
				if (spot11 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, oneone='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, oneone='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot11 = "X";
					}
					else {
						spot11 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";

					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot12Click() {
			if (!winorlose) {
				if (spot12 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, onetwo='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, onetwo='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot12 = "X";
					}
					else {
						spot12 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot13Click() {
			if (!winorlose) {
				if (spot13 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, onethree='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, onethree='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot13 = "X";
					}
					else {
						spot13 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot21Click() {
			if (!winorlose) {
				if (spot21 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, twoone='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, twoone='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot21 = "X";
					}
					else {
						spot21 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot22Click() {
			if (!winorlose) {
				if (spot22 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, twotwo='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, twotwo='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot22 = "X";
					}
					else {
						spot22 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot23Click() {
			if (!winorlose) {
				if (spot23 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, twothree='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, twothree='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot23 = "X";
					}
					else {
						spot23 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot31Click() {
			if (!winorlose) {
				if (spot31 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, threeone='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, threeone='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot31 = "X";
					}
					else {
						spot31 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot32Click() {
			if (!winorlose) {
				if (spot32 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, threetwo='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, threetwo='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot32 = "X";
					}
					else {
						spot32 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void spot33Click() {
			if (!winorlose) {
				if (spot33 == "" && turn == 1) {
					using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
						connection.Open();

						SqlCommand command;

						if (room_owner == 1) {
							command = new SqlCommand("update Rooms set Turn=2, threethree='X' where Player1_ID=@player1_id", connection);
							_ = command.Parameters.AddWithValue("player1_id", username_id);
						}
						else {
							command = new SqlCommand("update Rooms set Turn=1, threethree='O' where Player2_ID=@player2_id", connection);
							_ = command.Parameters.AddWithValue("player2_id", username_id);
						}

						try {
							_ = command.ExecuteNonQuery();
						}
						catch (Exception ex) {
							Console.Write(ex.Message);
						}
					}

					if (room_owner == 1) {
						spot33 = "X";
					}
					else {
						spot33 = "O";
					}

					turn = -1;

					game_info = opponent_name + "'s turn now";
					this.winCheck();

					if (!winorlose) {
						Thread spin = new Thread(new ThreadStart(this.waitForNextMove));
						spin.Start();
					}
				}
			}
		}

		public void moveToRoomList() {
			current_room_id = 0;
			opponent_name = "";
			opponent_info = "";
			turn = -1;
			game_info = "";

			spinning = false;
			resettime = false;
			winorlose = false;

			spot11 = "";
			spot12 = "";
			spot13 = "";
			spot21 = "";
			spot22 = "";
			spot23 = "";
			spot31 = "";
			spot32 = "";
			spot33 = "";

			if (room_owner == 0) {
				using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand checkSecond = new SqlCommand("select Player2_ID from Rooms where Player1_ID=@player1_id", connection);
					_ = checkSecond.Parameters.AddWithValue("player1_id", username_id);

					SqlCommand command = new SqlCommand("update Rooms set Player2_ID=0, rst='true' where Player2_ID=@player2_id", connection);
					_ = command.Parameters.AddWithValue("player2_id", username_id);

					SqlCommand commandDelete = new SqlCommand("delete from Rooms where Player1_ID=@player1_id", connection);
					_ = commandDelete.Parameters.AddWithValue("player1_id", username_id);

					try {
						SqlDataReader sdr = checkSecond.ExecuteReader();
						while (sdr.Read()) {
							if (int.Parse(sdr["Player2_ID"].ToString()) == 0) {
								_ = commandDelete.ExecuteNonQuery();
							}
							else {
								_ = command.ExecuteNonQuery();
							}
						}
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}
			}
			else {
				using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand checkSecond = new SqlCommand("select Player2_ID from Rooms where Player1_ID=@player1_id", connection);
					_ = checkSecond.Parameters.AddWithValue("player1_id", username_id);

					SqlCommand commandMoveOwnership = new SqlCommand("update Rooms set Player1_ID=(select ID from Users where username=@username), Player2_ID=0, rst='true' where Player1_ID=@player1_id", connection);
					_ = commandMoveOwnership.Parameters.AddWithValue("username", opponent_name);
					_ = commandMoveOwnership.Parameters.AddWithValue("player1_id", username_id);

					SqlCommand commandDelete = new SqlCommand("delete from Rooms where Player1_ID=@player1_id", connection);
					_ = commandDelete.Parameters.AddWithValue("player1_id", username_id);

					try {
						SqlDataReader sdr = checkSecond.ExecuteReader();
						while (sdr.Read()) {
							if (int.Parse(sdr["Player2_ID"].ToString()) != 0) {
								_ = commandMoveOwnership.ExecuteNonQuery();
							}
							else {
								_ = commandDelete.ExecuteNonQuery();
							}
						}
						sdr.Dispose();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}
			}

			opponentAbortCall = true;
			moveAbortCall = true;
		}
	}
}
