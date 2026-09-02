using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignChatUserProfileUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE
                    existing_count NUMBER;
                BEGIN
                    SELECT COUNT(*) INTO existing_count
                    FROM USER_TAB_COLUMNS
                    WHERE TABLE_NAME = 'CHAT_USER_PROFILES'
                      AND COLUMN_NAME = 'PERSON_ID';

                    IF existing_count > 0 THEN
                        EXECUTE IMMEDIATE
                            'ALTER TABLE CHAT_USER_PROFILES RENAME COLUMN PERSON_ID TO USER_ID';
                    END IF;

                    SELECT COUNT(*) INTO existing_count
                    FROM USER_CONSTRAINTS
                    WHERE CONSTRAINT_NAME = 'FK_CHAT_USER_PROFILES_USERS_PERSON_ID';

                    IF existing_count > 0 THEN
                        EXECUTE IMMEDIATE
                            'ALTER TABLE CHAT_USER_PROFILES RENAME CONSTRAINT FK_CHAT_USER_PROFILES_USERS_PERSON_ID TO FK_CHAT_USER_PROFILES_USERS_USER_ID';
                    END IF;

                    SELECT COUNT(*) INTO existing_count
                    FROM USER_INDEXES
                    WHERE INDEX_NAME = 'IX_CHAT_USER_PROFILES_PERSON_ID';

                    IF existing_count > 0 THEN
                        EXECUTE IMMEDIATE
                            'ALTER INDEX IX_CHAT_USER_PROFILES_PERSON_ID RENAME TO IX_CHAT_USER_PROFILES_USER_ID';
                    END IF;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE
                    existing_count NUMBER;
                BEGIN
                    SELECT COUNT(*) INTO existing_count
                    FROM USER_TAB_COLUMNS
                    WHERE TABLE_NAME = 'CHAT_USER_PROFILES'
                      AND COLUMN_NAME = 'USER_ID';

                    IF existing_count > 0 THEN
                        EXECUTE IMMEDIATE
                            'ALTER TABLE CHAT_USER_PROFILES RENAME COLUMN USER_ID TO PERSON_ID';
                    END IF;

                    SELECT COUNT(*) INTO existing_count
                    FROM USER_CONSTRAINTS
                    WHERE CONSTRAINT_NAME = 'FK_CHAT_USER_PROFILES_USERS_USER_ID';

                    IF existing_count > 0 THEN
                        EXECUTE IMMEDIATE
                            'ALTER TABLE CHAT_USER_PROFILES RENAME CONSTRAINT FK_CHAT_USER_PROFILES_USERS_USER_ID TO FK_CHAT_USER_PROFILES_USERS_PERSON_ID';
                    END IF;

                    SELECT COUNT(*) INTO existing_count
                    FROM USER_INDEXES
                    WHERE INDEX_NAME = 'IX_CHAT_USER_PROFILES_USER_ID';

                    IF existing_count > 0 THEN
                        EXECUTE IMMEDIATE
                            'ALTER INDEX IX_CHAT_USER_PROFILES_USER_ID RENAME TO IX_CHAT_USER_PROFILES_PERSON_ID';
                    END IF;
                END;
                """);
        }
    }
}
