# Cameron's Notes
I tried to include comments and sources with .NET documentation where I thought it was worthwhile. At first I wasn't sure whether to include the comments all in code or only in GitHub, but I settled on a bit of both. I normally wouldn't leave such large comments at the top of files, but I thought it was relevant to include some of what my comments would be to a fellow engineer inside the code for the sake of this example.

Further changes I would make:
Some of these things are left a certain way because of the sake of this example. If this was an actual project, one thing I would love to change is the use of he dbcontext in the reporter engine in example3. I would recommend the Unit of Work Pattern for setting up data access. In the current setup, if the Daily Report Service needed another service, that other service would be making it's own new dbcontext and the Daily Report Service would basically have two contexts open at the same time, one with the ReportEngine, and one with the new service. Having multiple dbcontexts open is not ideal as it doesn't automatically guarantee atomic transactions in the database. In the Unit of Work pattern, the Daily Report Service would open up a unit of work, a new class/interface, and any data access would go through that. The Unit of Work would allow the Daily Report Service to call on any data access methods it needs, executed on a single dbcontext.
https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application


# FIG Code Review Assessment

For this code review assessment, take a look at the example classes. Each file is self contained:
it might define a handful of classes relevant to 1 example, but no classes in any example file refer to any class from another example file.

Please identify any changes you believe should be made in the code and implement those changes. Additionally, please include any comments you might make to a fellow engineer if you were reviewing this code as part of a PR.

We will meet with you to talk over these examples and your thoughts on them. There, you can explain what you changed and questions you
might ask a developer who submitted this code, as well as explain what you think the given code actually does.
