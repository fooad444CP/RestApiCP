# RestApiCP
CP task


There are 6 functions avaialble : 

/api/delete - accepts "id" and "userName" to delete a post
/api/create - accepts "userName","subject" and content to create a post, calling this api will return the inserted row as JSON.
/api/update - accepts "id","userName","newSubject" and "newContent", the function updates existing post if the id and username match the ones in the DB
/api/getTrending - doesnt accept params, returns top trending post, it returns post with highest likes and if collision it returns newest one
/api/read - accept "id","userName" or nothing, if id provided it returns the post with that id, if userName provided it returns all posts by that user, if nothing provided then it returns all posts.
/api/like - accept "id" to like that post.


the project uses a live DB hosted on azure.
The project itself is also live on azure and can be accessed at "https://restapicp.azurewebsites.net", example of calling the api to like a post:
https://restapicp.azurewebsites.net/api/like?id=27 , which will like the post with id 27.

the program contains a docker file, create docker image and then you can run it on docker
docker build -t <imageName> .
docker run  -p <port> <imageName>
