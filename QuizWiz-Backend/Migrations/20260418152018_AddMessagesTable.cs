using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizWiz_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendship_users_AddresseeId",
                table: "Friendship");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendship_users_RequesterId",
                table: "Friendship");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_users_ReceiverId",
                table: "Message");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_users_SenderId",
                table: "Message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Message",
                table: "Message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Friendship",
                table: "Friendship");

            migrationBuilder.RenameTable(
                name: "Message",
                newName: "messages");

            migrationBuilder.RenameTable(
                name: "Friendship",
                newName: "friendships");

            migrationBuilder.RenameColumn(
                name: "SentAt",
                table: "messages",
                newName: "sentat");

            migrationBuilder.RenameColumn(
                name: "SenderId",
                table: "messages",
                newName: "senderid");

            migrationBuilder.RenameColumn(
                name: "ReceiverId",
                table: "messages",
                newName: "receiverid");

            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "messages",
                newName: "isread");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "messages",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "messages",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Message_SenderId",
                table: "messages",
                newName: "IX_messages_senderid");

            migrationBuilder.RenameIndex(
                name: "IX_Message_ReceiverId",
                table: "messages",
                newName: "IX_messages_receiverid");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "friendships",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "RequesterId",
                table: "friendships",
                newName: "requesterid");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "friendships",
                newName: "createdat");

            migrationBuilder.RenameColumn(
                name: "AddresseeId",
                table: "friendships",
                newName: "addresseeid");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "friendships",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Friendship_RequesterId",
                table: "friendships",
                newName: "IX_friendships_requesterid");

            migrationBuilder.RenameIndex(
                name: "IX_Friendship_AddresseeId",
                table: "friendships",
                newName: "IX_friendships_addresseeid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_messages",
                table: "messages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_friendships",
                table: "friendships",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_friendships_users_addresseeid",
                table: "friendships",
                column: "addresseeid",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_friendships_users_requesterid",
                table: "friendships",
                column: "requesterid",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_messages_users_receiverid",
                table: "messages",
                column: "receiverid",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_messages_users_senderid",
                table: "messages",
                column: "senderid",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_friendships_users_addresseeid",
                table: "friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_friendships_users_requesterid",
                table: "friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_messages_users_receiverid",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "FK_messages_users_senderid",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_messages",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_friendships",
                table: "friendships");

            migrationBuilder.RenameTable(
                name: "messages",
                newName: "Message");

            migrationBuilder.RenameTable(
                name: "friendships",
                newName: "Friendship");

            migrationBuilder.RenameColumn(
                name: "sentat",
                table: "Message",
                newName: "SentAt");

            migrationBuilder.RenameColumn(
                name: "senderid",
                table: "Message",
                newName: "SenderId");

            migrationBuilder.RenameColumn(
                name: "receiverid",
                table: "Message",
                newName: "ReceiverId");

            migrationBuilder.RenameColumn(
                name: "isread",
                table: "Message",
                newName: "IsRead");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Message",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Message",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_messages_senderid",
                table: "Message",
                newName: "IX_Message_SenderId");

            migrationBuilder.RenameIndex(
                name: "IX_messages_receiverid",
                table: "Message",
                newName: "IX_Message_ReceiverId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Friendship",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "requesterid",
                table: "Friendship",
                newName: "RequesterId");

            migrationBuilder.RenameColumn(
                name: "createdat",
                table: "Friendship",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "addresseeid",
                table: "Friendship",
                newName: "AddresseeId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Friendship",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_friendships_requesterid",
                table: "Friendship",
                newName: "IX_Friendship_RequesterId");

            migrationBuilder.RenameIndex(
                name: "IX_friendships_addresseeid",
                table: "Friendship",
                newName: "IX_Friendship_AddresseeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Message",
                table: "Message",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Friendship",
                table: "Friendship",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendship_users_AddresseeId",
                table: "Friendship",
                column: "AddresseeId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendship_users_RequesterId",
                table: "Friendship",
                column: "RequesterId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Message_users_ReceiverId",
                table: "Message",
                column: "ReceiverId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Message_users_SenderId",
                table: "Message",
                column: "SenderId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
