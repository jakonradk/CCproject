using CCprojectTicTacToe.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.SqlClient;
using System;
using System.Runtime.CompilerServices;

public class DatabaseConnect {
    public string username = "";
    public int username_id = 0;
    public int current_room_id = 0;

	public string opponent_name;
	public string opponent_info;
	public int turn = -1;

	public int room_owner;

	public Boolean spinning = false;
	public Boolean resettime = false;
	public Boolean winorlose = false;
	public Boolean opponentAbortCall = false;
	public Boolean moveAbortCall = false;

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

	public DatabaseConnect() {
		
    }

    public string moveToRoomList(string username) {
        if (username.Length < 1) {
            return "Username must be at least 1 character long";
        }
        else if (username.Length > 20) {
            return "Username cannot be longer than 20 characters";
        }
        else {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
                connection.Open();

                var check_available = new SqlCommand("select count(*) from Users where username=@username", connection);
                check_available.Parameters.AddWithValue("username", username);

                var command = new SqlCommand("insert into Users values (@username)", connection);
                command.Parameters.AddWithValue("username", username);

                try {
                    if (((int)check_available.ExecuteScalar()) != 0) {
                        return "Chosen username is already in use";
                    }
                    else {
                        this.username = username;

                        command.ExecuteNonQuery();

                        var checkID = new SqlCommand("select ID from Users where username=@username", connection);
                        checkID.Parameters.AddWithValue("username", this.username);

                        SqlDataReader sdr = checkID.ExecuteReader();
                        while (sdr.Read()) {
                            this.username_id = Int32.Parse(sdr["ID"].ToString());
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

		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			var command = new SqlCommand("insert into Rooms (Player1_ID, Player2_ID, Turn, rst, win) values (@player1_id, 0, 1, 'false', 0)", connection);
			command.Parameters.AddWithValue("player1_id", this.username_id);

			try {
				command.ExecuteNonQuery();

				var checkID = new SqlCommand("select ID from Rooms where Player1_ID=@player1_id", connection);
				checkID.Parameters.AddWithValue("player1_id", this.username_id);

				SqlDataReader sdr = checkID.ExecuteReader();
				while (sdr.Read()) {
					this.current_room_id = Int32.Parse(sdr["ID"].ToString());
				}
				sdr.Dispose();
			}
			catch (Exception ex) {
				Console.Write(ex.Message);
			}
		}

		this.room_owner = 1;
	}

	public void enterRoom(string selected_item) {
		opponentAbortCall = false;
		moveAbortCall = false;

		this.opponent_name = selected_item;

		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			var command = new SqlCommand("update Rooms set Player2_ID=@player2_id where Rooms.Player1_ID=(select ID from Users where username=@username)", connection);
			command.Parameters.AddWithValue("player2_id", this.username_id);
			command.Parameters.AddWithValue("username", selected_item);

			try {
				command.ExecuteNonQuery();

				var checkID = new SqlCommand("select ID from Rooms where Player1_ID=@player1_id", connection);
				checkID.Parameters.AddWithValue("player1_id", this.username_id);

				SqlDataReader sdr = checkID.ExecuteReader();
				while (sdr.Read()) {
					this.current_room_id = Int32.Parse(sdr["ID"].ToString());
				}
				sdr.Dispose();
			}
			catch (Exception ex) {
				Console.Write(ex.Message);
			}
		}

		this.room_owner = 0;
	}

	public List<string> reloadList() {
        List<string> result_set = new List<string>();

		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			var command = new SqlCommand("select Rooms.ID, Users.username from Rooms join Users on Users.ID = Rooms.Player1_ID where Rooms.win=0", connection);

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
		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			var command = new SqlCommand("delete from Users where username=@username", connection);
			command.Parameters.AddWithValue("username", this.username);

			try {
				command.ExecuteNonQuery();
			}
			catch (Exception ex) {
				Console.Write(ex.Message);
			}
		}
	}

	public void waitForOpponent() {
		this.opponent_name = "";
		this.opponent_info = "Waiting for a second player...";

		this.spinning = true;
		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			var command = new SqlCommand("select Rooms.Player2_ID, Users.username from Rooms join Users on Users.ID=Rooms.Player2_ID where Player1_ID=@player1_id", connection);
			command.Parameters.AddWithValue("player1_id", this.username_id);

			while (true) {
				Thread.Sleep(5000);
				if (opponentAbortCall) {
					return;
				}

				try {
					SqlDataReader sdr = command.ExecuteReader();
					while (sdr.Read()) {
						if (Int32.Parse(sdr["Player2_ID"].ToString()) != 0) {
							this.opponent_info = "You are playing against: ";
							this.opponent_name = sdr["username"].ToString();
							this.game_info = "Your turn now";
							this.spinning = false;
							this.turn = 1;
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
		this.opponent_info = "You are playing against: ";
		this.game_info = this.opponent_name + "'s turn now";

		this.spinning = true;

		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			SqlCommand command;

			if (this.room_owner == 1) {
				command = new SqlCommand("select Turn, rst, win from Rooms where Player1_ID=@player1_id", connection);
				command.Parameters.AddWithValue("player1_id", this.username_id);
			}
			else {
				command = new SqlCommand("select Turn, rst, win from Rooms where Player2_ID=@player2_id", connection);
				command.Parameters.AddWithValue("player2_id", this.username_id);
			}

			while (true) {
				Thread.Sleep(1000);
				if (moveAbortCall) {
					return;
				}

				try {
					SqlDataReader sdr = command.ExecuteReader();
					while (sdr.Read()) {
						if ((Int32.Parse(sdr["Turn"].ToString()) == 1 && this.room_owner == 1) || (Int32.Parse(sdr["Turn"].ToString()) == 2 && this.room_owner == 0)) {
							this.spinning = false;
						}
						if ((Int32.Parse(sdr["win"].ToString()) == 2 && this.room_owner == 1) || (Int32.Parse(sdr["win"].ToString()) == 1 && this.room_owner == 0)) {
							this.spinning = false;
							this.winorlose = true;
						}
						if (sdr["rst"].ToString() == "true") {
							this.spinning = false;
							this.resettime = true;
						}
					}
					sdr.Dispose();

					if (spinning == false) {
						if (this.resettime == true) {
							resetBoard();
							return;
						}
						if (this.winorlose == true) {
							updateBoard();
							this.game_info = "You lost!";
							return;
						}
						else {
							updateBoard();
							this.turn = 1;
							this.game_info = "Your turn now";
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
		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			SqlCommand command;

			if (this.room_owner == 1) {
				command = new SqlCommand("select * from Rooms where Player1_ID=@player1_id", connection);
				command.Parameters.AddWithValue("player1_id", this.username_id);
			}
			else {
				command = new SqlCommand("select * from Rooms where Player2_ID=@player2_id", connection);
				command.Parameters.AddWithValue("player2_id", this.username_id);
			}

			try {
				SqlDataReader sdr = command.ExecuteReader();
				while (sdr.Read()) {
					this.spot11 = sdr["oneone"].ToString();
					this.spot12 = sdr["onetwo"].ToString();
					this.spot13 = sdr["onethree"].ToString();
					this.spot21 = sdr["twoone"].ToString();
					this.spot22 = sdr["twotwo"].ToString();
					this.spot23 = sdr["twothree"].ToString();
					this.spot31 = sdr["threeone"].ToString();
					this.spot32 = sdr["threetwo"].ToString();
					this.spot33 = sdr["threethree"].ToString();
				}
				sdr.Dispose();
			}
			catch (Exception ex) {
				Console.Write(ex.Message);
			}
		}
	}

	public void resetBoard() {
		using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
			connection.Open();

			var command = new SqlCommand("update Rooms set Turn=1, oneone=NULL, onetwo=NULL, onethree=NULL, twoone=NULL, twotwo=NULL, twothree=NULL, threeone=NULL, threetwo=NULL, threethree=NULL, rst='false', win=0 where Player1_ID=@player1_id", connection);
			command.Parameters.AddWithValue("player1_id", this.username_id);

			try {
				command.ExecuteNonQuery();
			}
			catch (Exception ex) {
				Console.Write(ex.Message);
			}
		}
		this.opponent_name = "";
		this.opponent_info = "Waiting for a second player...";
		this.room_owner = 1;
		this.resettime = false;
		this.winorlose = false;
		this.turn = -1;

		updateBoard();

		Thread jumpstart = new Thread(new ThreadStart(waitForOpponent));
		jumpstart.Start();
	}

	public void winCheck() {
		if (this.room_owner == 1) {
			if ((this.spot11 == "X" && this.spot12 == "X" && this.spot13 == "X") ||
				(this.spot21 == "X" && this.spot22 == "X" && this.spot23 == "X") ||
				(this.spot31 == "X" && this.spot32 == "X" && this.spot33 == "X") ||
				(this.spot11 == "X" && this.spot21 == "X" && this.spot31 == "X") ||
				(this.spot12 == "X" && this.spot22 == "X" && this.spot32 == "X") ||
				(this.spot13 == "X" && this.spot23 == "X" && this.spot33 == "X") ||
				(this.spot11 == "X" && this.spot22 == "X" && this.spot33 == "X") ||
				(this.spot13 == "X" && this.spot21 == "X" && this.spot31 == "X")) {

				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					var command = new SqlCommand("update Rooms set win=1 where Player1_ID=@player1_id", connection);
					command.Parameters.AddWithValue("player1_id", this.username_id);

					try {
						command.ExecuteNonQuery();
						this.winorlose = true;
						this.game_info = "You won!";
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}
			}
		}
		else {
			if ((this.spot11 == "O" && this.spot12 == "O" && this.spot13 == "O") ||
				(this.spot21 == "O" && this.spot22 == "O" && this.spot23 == "O") ||
				(this.spot31 == "O" && this.spot32 == "O" && this.spot33 == "O") ||
				(this.spot11 == "O" && this.spot21 == "O" && this.spot31 == "O") ||
				(this.spot12 == "O" && this.spot22 == "O" && this.spot32 == "O") ||
				(this.spot13 == "O" && this.spot23 == "O" && this.spot33 == "O") ||
				(this.spot11 == "O" && this.spot22 == "O" && this.spot33 == "O") ||
				(this.spot13 == "O" && this.spot21 == "O" && this.spot31 == "O")) {

				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					var command = new SqlCommand("update Rooms set win=2 where Player2_ID=@player2_id", connection);
					command.Parameters.AddWithValue("player2_id", this.username_id);

					try {
						command.ExecuteNonQuery();
						this.winorlose = true;
						this.game_info = "You won!";
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}
			}
		}
	}

	public void spot11Click() {
		if (!this.winorlose) {
			if (this.spot11 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, oneone='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, oneone='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot11 = "X";
				}
				else {
					this.spot11 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				
				winCheck();

				if(!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot12Click() {
		if (!this.winorlose) {
			if (this.spot12 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, onetwo='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, onetwo='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot12 = "X";
				}
				else {
					this.spot12 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot13Click() {
		if (!this.winorlose) {
			if (this.spot13 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, onethree='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, onethree='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot13 = "X";
				}
				else {
					this.spot13 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot21Click() {
		if (!this.winorlose) {
			if (this.spot21 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, twoone='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, twoone='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot21 = "X";
				}
				else {
					this.spot21 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot22Click() {
		if (!this.winorlose) {
			if (this.spot22 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, twotwo='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, twotwo='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot22 = "X";
				}
				else {
					this.spot22 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot23Click() {
		if (!this.winorlose) {
			if (this.spot23 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, twothree='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, twothree='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot23 = "X";
				}
				else {
					this.spot23 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot31Click() {
		if (!this.winorlose) {
			if (this.spot31 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, threeone='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, threeone='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot31 = "X";
				}
				else {
					this.spot31 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot32Click() {
		if (!this.winorlose) {
			if (this.spot32 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, threetwo='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, threetwo='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot32 = "X";
				}
				else {
					this.spot32 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
					spin.Start();
				}
			}
		}
	}

	public void spot33Click() {
		if (!this.winorlose) {
			if (this.spot33 == "" && this.turn == 1) {
				using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
					connection.Open();

					SqlCommand command;

					if (this.room_owner == 1) {
						command = new SqlCommand("update Rooms set Turn=2, threethree='X' where Player1_ID=@player1_id", connection);
						command.Parameters.AddWithValue("player1_id", this.username_id);
					}
					else {
						command = new SqlCommand("update Rooms set Turn=1, threethree='O' where Player2_ID=@player2_id", connection);
						command.Parameters.AddWithValue("player2_id", this.username_id);
					}

					try {
						command.ExecuteNonQuery();
					}
					catch (Exception ex) {
						Console.Write(ex.Message);
					}
				}

				if (this.room_owner == 1) {
					this.spot33 = "X";
				}
				else {
					this.spot33 = "O";
				}

				this.turn = -1;

				this.game_info = this.opponent_name + "'s turn now";
				winCheck();

				if (!winorlose) {
					Thread spin = new Thread(new ThreadStart(waitForNextMove));
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

		if (this.room_owner == 0) {
			using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				var checkSecond = new SqlCommand("select Player2_ID from Rooms where Player1_ID=@player1_id", connection);
				checkSecond.Parameters.AddWithValue("player1_id", this.username_id);

				var command = new SqlCommand("update Rooms set Player2_ID=0, rst='true' where Player2_ID=@player2_id", connection);
				command.Parameters.AddWithValue("player2_id", this.username_id);

				var commandDelete = new SqlCommand("delete from Rooms where Player1_ID=@player1_id", connection);
				commandDelete.Parameters.AddWithValue("player1_id", this.username_id);

				try {
					SqlDataReader sdr = checkSecond.ExecuteReader();
					while (sdr.Read()) {
						if (Int32.Parse(sdr["Player2_ID"].ToString()) == 0) {
							commandDelete.ExecuteNonQuery();
						}
						else {
							command.ExecuteNonQuery();
						}
					}
				}
				catch (Exception ex) {
					Console.Write(ex.Message);
				}
			}
		}
		else {
			using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SQLCONNSTR_ADONETCONNECT"))) {
				connection.Open();

				var checkSecond = new SqlCommand("select Player2_ID from Rooms where Player1_ID=@player1_id", connection);
				checkSecond.Parameters.AddWithValue("player1_id", this.username_id);

				var commandMoveOwnership = new SqlCommand("update Rooms set Player1_ID=(select ID from Users where username=@username), Player2_ID=0, rst='true' where Player1_ID=@player1_id", connection);
				commandMoveOwnership.Parameters.AddWithValue("username", this.opponent_name);
				commandMoveOwnership.Parameters.AddWithValue("player1_id", this.username_id);

				var commandDelete = new SqlCommand("delete from Rooms where Player1_ID=@player1_id", connection);
				commandDelete.Parameters.AddWithValue("player1_id", this.username_id);

				try {
					SqlDataReader sdr = checkSecond.ExecuteReader();
					while (sdr.Read()) {
						if (Int32.Parse(sdr["Player2_ID"].ToString()) != 0) {
							commandMoveOwnership.ExecuteNonQuery();
						}
						else {
							commandDelete.ExecuteNonQuery();
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
