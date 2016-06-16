## Restfar V1

Currently an excellent RESTful http client for UWP(Universal Windows Plateform) application, type-safe, elegant.

It's inspired by [Retrofit](http://square.github.io/retrofit/).

## Install

You can install it from [nuget](https://www.nuget.org/packages/Restfar) or run the following command in the `Package Manager Console` in Visual Studio

```
PM> Install-Package Restfar
```

The only one dependency is [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/), nuget will automatic install this for you.


## Usage

### Basic
Restfar turns your HTTP API into a interface:

```c#
public interface APIService
{
    [Get("/users/{user}")]
    Task<User> GetUser([Path("user")] string user);

    [FormUrlEncoded]
    [Post("users")]
    Task CreateUser([Field("username")] string username, [Field("password")] string password);
}
```

After define your API, use `RestfarClient` to create an instance of your `APIService` interface.

```c#
var restfar = new RestfarClient("https://api.github.com");
var API = restfar.Create<APIService>(); // return an instance of type APIService 
```

Then, call the API! Unimaginable!

```c#
var user = await API.GetUser('forehalo');
```

### API Declaration

Restfar uses C# attribute to define your API. You can custom the URL, headers, query strings.

**Reqeust Method**

Every method must have an HTTP Method Attribute to specify the request method and relative URL. Six build-in attributes enable:
`GET`, `POST`, `PUT`, `DELETE`, `HEAD` and `OPTIONS`. These attribute can only decorate method. Query string is allowable.

```c#
[Get("/users?list=all")]
Task<List<User>> GetUsers();
```

### Dynamic URL

Not only const relative Url can be used. You can use replacement blocks as placeholder and replace it at runtime. A replacement block is
a string surrounded by `{` and `}`. There must be a `Path` attribute using the same string with placeholder. An ArgumentException will be thown if
`Path` attribute added but no the same name placeholder.

```c#
[Get("/users/{user}")]
Task<User> GetUser([Path("user")] string user);
```

Add query string using `Qeury` attribute.

```c#
[Get("/users/{user}")]
Task<User> GetUser([Path("user")] string user, [Query("list")] string list);
```

### Headers

There are three ways to custom your headers, ** global(static), method(static), parameter(dynamic) **.
All of them use a `Header` attribute which need an array of string as parameter.

You may want to add a same header to each HTTP request. Writing `Headers` again and again is foolish. So, a convenient way:

```c#
[Headers(new []{"global-header: content"})]
public interface APIService{}
```

Add headers to one method:

```c#
[Headers(new []{"method-header: content"})]
[Get("/users/{user}")]
Task<User> GetUser([Path("user")] string user);
```

Add headers dynamic when called:

```c#
[Get("/users/{user}")]
Task<User> GetUser([Path("user")] string user, [Headers] string[] headers);


string[] headers = {"dynamic-header: content", "header2: content"}
await GetUser("forehalo", headers);
```

### Form encoded and multipart

When send a post request, form will be encoded before send. You can use `Filed` attribute to add filed to the form.

```c#
[FormUrlEncoded]
[Post("users")]
Task CreateUser([Field("username")] string username, [Field("password")] string password);
```

Multipart requests allow to send with files using `File` attribute, add form part using `Part` attribute.

```c#
[Multipart]
[Post("users/{user}/avatar")]
Task UploadAvatar([Path("user")] string username, [Part("id")] int id, [File("file")] StorageFile file);
```

### Event

An event named `OnSuccess` occured if response returned with status code between `200` and `300`, 
else another event named `OnFailure` occured. 
You could bind the event with an event handler, only one argument passed to handler which is the raw [`HttpResponseMessage`](https://msdn.microsoft.com/library/windows/apps/dn279631) instance

```c#
[Get("/users")]
Task<User> GetUser([Success] ResponseHanlder OnSuccess, [Failure] ResponseHanlder OnFailure);


private void OnSuccess(HttpResponseMessage response)
{
    //
}

private void OnFailure(HttpResponseMessage response)
{
    //
}

var result = await API.GetUser(OnSuccess, OnFailure);
```




## LICENSE

Copyright 2016 Forehalo

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
