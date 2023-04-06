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
- [ ] The number of collections the user has

- [ ] The number of followers

- [ ] The number of following

- [ ] Their top 10 artists (by most plays, additions to collections, or combination)
  
**The application must provide a song recommendation system with the following options:**
- [ ] The top 50 most popular songs in the last 30 days (rolling)

- [ ] The top 50 most popular songs among my friends

- [ ] The top 5 most popular genres of the month (calendar month)

- [ ] For you: Recommend songs to listen to based on your play history (e.g. genre, artist) and the play history of similar users

# To run
**BEFORE RUNNING**
Create a `.env` file in the same directory as the `c#.csproj` with the following content:
```
DB_USER=<RIT ID>
DB_PASS=<RIT PW>
```
Then do `dotnet run`
