# Slash Commands

Below is an outline of every slash command currently implemented in Cloak, along with their descriptions and parameters.

## Public commands

### `/role give`

Grants a self-role to the user.

| Parameter | Required | Type | Description        | Autocomplete |
|:----------|:---------|:-----|:-------------------|:-------------|
| role      | ✅ Yes    | Role | The role to grant. | ✅ Yes        |

### `/role remove`

Revokes a self-role from the user.

| Parameter | Required | Type | Description         | Autocomplete |
|:----------|:---------|:-----|:--------------------|:-------------|
| role      | ✅ Yes    | Role | The role to revoke. | ✅ Yes        |

## Role management

### `/selfrole add`

Adds a self role to the database.

| Parameter | Required | Type   | Description          | Autocomplete |
|:----------|:---------|:-------|:---------------------|:-------------|
| role      | ✅ Yes    | Role   | The role to add.     | ✅ Yes        |
| group     | ❌ No     | String | The group to assign. | ✅ Yes        |

### `/selfrole edit`

Edits the group for a self role.

| Parameter | Required | Type   | Description                                                    | Autocomplete |
|:----------|:---------|:-------|:---------------------------------------------------------------|:-------------|
| role      | ✅ Yes    | Role   | The role to edit.                                              | ✅ Yes        |
| group     | ❌ No     | String | The new group to assign. Leave unspecified to clear the group. | ✅ Yes        |

### `/selfrole remove`

Removes a self role from the database.

| Parameter | Required | Type | Description                                | Autocomplete |
|:----------|:---------|:-----|:-------------------------------------------|:-------------|
| role      | ✅ Yes    | Role | The role to no longer consider persistent. | ✅ Yes        |

### `/persistentrole add`

Adds a role to the persistent role list.

| Parameter | Required | Type | Description                      | Autocomplete |
|:----------|:---------|:-----|:---------------------------------|:-------------|
| role      | ✅ Yes    | Role | The role to consider persistent. | ✅ Yes        |

### `/persistentrole remove`

Removes a role from the persistent role list.

| Parameter | Required | Type | Description                                | Autocomplete |
|:----------|:---------|:-----|:-------------------------------------------|:-------------|
| role      | ✅ Yes    | Role | The role to no longer consider persistent. | ✅ Yes        |

## Information embed management

### `/roleinfo display`

Displays the information embed, explaining the roles and how to use the bot.

| Parameter | Required | Type                  | Description                    | Autocomplete |
|:----------|:---------|:----------------------|:-------------------------------|:-------------|
| channel   | ❌ No     | Channel ID or mention | The channel to send the embed. | ✅ Yes        |

### `/roleinfo setintro`

Sets the intro text. This is the embed description. **This command responds with a modal.**

| Parameter | Required | Type | Description | Autocomplete |
|:----------|:---------|:-----|:------------|:-------------|
| -         | -        | -    | -           | -            |

### `/roleinfo addsection`

Adds a new paragraph/field to the information embed. **This command responds with a modal.**

| Parameter | Required | Type | Description | Autocomplete |
|:----------|:---------|:-----|:------------|:-------------|
| -         | -        | -    | -           | -            |

### `/roleinfo editsection`

Edits a section on the information embed. **This command responds with a modal.**

| Parameter | Required | Type | Description                      | Autocomplete |
|:----------|:---------|:-----|:---------------------------------|:-------------|
| sectionId | ✅ Yes    | GUID | The ID of the section to modify. | ✅ Yes        |

### `/roleinfo removesection`

Adds a new paragraph/field to the information embed.

| Parameter | Required | Type | Description                      | Autocomplete |
|:----------|:---------|:-----|:---------------------------------|:-------------|
| sectionId | ✅ Yes    | GUID | The ID of the section to remove. | ✅ Yes        |

# Ephemeral responses

Below is a table outlining all the commands and whether or not they have ephemeral responses.

| Command                   | Ephemeral Response                 |
|:--------------------------|:-----------------------------------|
| `/roleinfo display`       | ➖ Yes to interaction, no to result |
| `/roleinfo setintro`      | ❌ No                               |
| `/roleinfo addsection`    | ❌ No                               |
| `/roleinfo editsection`   | ❌ No                               |
| `/roleinfo removesection` | ❌ No                               |
| `/selfrole add`           | ❌ No                               |
| `/selfrole edit`          | ❌ No                               |
| `/selfrole remove`        | ❌ No                               |
| `/persistentrole add`     | ❌ No                               |
| `/persistentrole remove`  | ❌ No                               |
| `/role give`              | ✅ Yes                              |
| `/role remove`            | ✅ Yes                              |
