⚠️ **This project follows an Academic Integrity Notice. See the [NOTICE](NOTICE) file for details.** ⚠️ 

# Spotify2-ElectricBoogaloo

## Phase 2 Application Requirements
- [X] Users will be able to create new accounts and access via login. The system must record the date and time an account is created. It must also stored the date and time an user access into the application

- [X] Users will be able to create collections of music.

- [X] Users will be able to see the list of all their collections by name in ascending order. The list must show the following information per collection: Collection’s name, Number of songs in the collection, Total duration in minutes

- [X] will be able to search for songs by name, artist, album, and genre. The resulting list of songs must show the song’s name, the artist’s name, the album, the length and the listen count. The list must be sorted alphabetically (ascending) by song’s name and artist’s name.

- [X] can sort by song name, artist’s name, genre, and released year (ascending and descending)

- [X] can add and delete albums, and songs from their collection

- [X] can modify the name of a collection. They can also delete an entire collection

- [x] can listen to a song individually or it can play an entire collection. You must record every time a song is played by a user. You do not need to actually be able to play songs, simply mark them as played

- [X] can follow a friend. Users can search for new friends by email

- [X] application must also allow an user to un-follow a friend

## Phase 3 Application Requirements

### Functional Requirements
**The application provides an user profile functionality that displays the following information:**
- [X] The number of collections the user has

- [X] The number of followers

- [X] The number of following

- [X] Their top 10 artists (by most plays, additions to collections, or combination)
  
**The application must provide a song recommendation system with the following options:**
- [X] The top 50 most popular songs in the last 30 days (rolling)

- [X] The top 50 most popular songs among my friends

- [X] The top 5 most popular genres of the month (calendar month)

- [x] For you: Recommend songs to listen to based on your play history (e.g. genre, artist) and the play history of similar users

### Non-Functional Requirements
**Report**
The [report](https://www.overleaf.com/3721374731njmxsbfqtjgb) will be updated to include:

- [ ] explaining the process/techniques used to analyze the data (what types of algorithms were used, did you use a tool for analytics, or did you create materialized views, etc)

- [ ] explaining the indexes created to boost your application program’s performance

- [ ] containing an appendix listing all of the SQL statements used in this phase

**[Poster](https://docs.google.com/presentation/d/17bgQ1haruhtRolHtjDmnebT8UTFJkmFl9M_u5jTmN50/edit?usp=sharing)**
- [X] your team name

- [X] the names of all team members

- [ ] the observations from the data analytics

- [ ] technologies used (Excel, Python, etc)

- [ ] visual representation of the data (charts, graphs, and other visual representations are required)

**Video Demo**
A single 7-10 minutes video that demonstrate the final version of your application and present your poster. Your video must be structured as follows:

1. Start by demonstrating the final version of your program. Make sure to demonstrate all functionalities defined in the Application Requirements from Phase
2. During the demonstration, you must show, from your source code, at least
   **two complex queries** (e.g. multiple joins, nested queries, correlated queries) implemented during this phase. We will not be running your application. It is important
   that you demonstrate all the required functionality.
3. After that, you will present your poster.

**Data Analysis**

# To run
**BEFORE RUNNING**
Create a `.env` file in the same directory as the `c#.csproj` with the following content:
```
DB_USER=<RIT ID>
DB_PASS=<RIT PW>
```
Then do `dotnet run`

# Dev command preset files
To run with preset command files, use the `-dev` flag like so:

`dotnet run -dev <cmdFile1> <cmdFile2> ... <cmdFileN>`

With each `cmdFile` containing a list of db commands on each line.
Example:
```
login
foobar
foobar
```
will execute `login` command with `foobar` as the username and password.
The special `$PROMPT` keyword can be used to ask for user input, then continue with the cmdFile
