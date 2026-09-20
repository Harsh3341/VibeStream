using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibeStream.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "artists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_artist_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "daily_statistics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    listening_seconds = table.Column<int>(type: "integer", nullable: false),
                    track_count = table.Column<int>(type: "integer", nullable: false),
                    artist_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_statistics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "listening_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    track_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listening_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    spotify_track_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    album_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    album_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracks", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracks_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vibe_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    energy = table.Column<double>(type: "double precision", nullable: false),
                    valence = table.Column<double>(type: "double precision", nullable: false),
                    danceability = table.Column<double>(type: "double precision", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vibe_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_vibe_snapshots_listening_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "listening_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "spotify_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    refresh_token = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spotify_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_spotify_connections_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listening_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    played_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listening_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_listening_events_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "session_tracks",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    played_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_session_tracks", x => new { x.session_id, x.position });
                    table.ForeignKey(
                        name: "fk_session_tracks_listening_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "listening_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_session_tracks_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_artists_spotify_artist_id",
                table: "artists",
                column: "spotify_artist_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_daily_statistics_user_id_date",
                table: "daily_statistics",
                columns: new[] { "user_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_listening_events_track_id",
                table: "listening_events",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "ix_listening_events_user_id_played_at",
                table: "listening_events",
                columns: new[] { "user_id", "played_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_listening_events_user_id_track_id_played_at",
                table: "listening_events",
                columns: new[] { "user_id", "track_id", "played_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_listening_sessions_user_id_started_at",
                table: "listening_sessions",
                columns: new[] { "user_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_session_tracks_track_id",
                table: "session_tracks",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "ix_spotify_connections_user_id",
                table: "spotify_connections",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tracks_artist_id",
                table: "tracks",
                column: "artist_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracks_spotify_track_id",
                table: "tracks",
                column: "spotify_track_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_spotify_user_id",
                table: "users",
                column: "spotify_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vibe_snapshots_session_id",
                table: "vibe_snapshots",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_vibe_snapshots_user_id_created_at",
                table: "vibe_snapshots",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_statistics");

            migrationBuilder.DropTable(
                name: "listening_events");

            migrationBuilder.DropTable(
                name: "session_tracks");

            migrationBuilder.DropTable(
                name: "spotify_connections");

            migrationBuilder.DropTable(
                name: "vibe_snapshots");

            migrationBuilder.DropTable(
                name: "tracks");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "listening_sessions");

            migrationBuilder.DropTable(
                name: "artists");
        }
    }
}
