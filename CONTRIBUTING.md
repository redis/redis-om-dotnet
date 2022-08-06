# Getting Involved

Thank you for your interest in the Redis.OM project - if you want to contribute we'd be positively delighted for you to get involved. Check out the sections below to see what you can do next.

## Opening an Issue

If you encounter any issues while using Redis.OM please [open an issue in GitHub](https://github.com/redis/redis-om-dotnet/issues/new). Issues should be opened if you find a bug in the Redis.OM software, an issue in the documentation, or if there's a neat new feature you'd like to see in Redis.OM. If you have any support/usage related questions those are best asked in [Discord](https://discord.gg/redis). 

## Contributing a Code Change

We welcome code contributions from the community, if you want to contribute code we ask the following:

1. Open an issue, or have the code change directly related to an existing issue.
2. Do not introduce any breaking changes to the Library without specific discussion in an underlying issue. 
    * The standard we use for a breaking change is if a developer would need to modify any code to upgrade, or if a developer would have to recompile their code to upgrade.
3. Code changes should be relatively small and well described in the pull request.
4. Code changes should be accompanied by tests that demonstrate coverage and regression for the issue.

### Test Setup

1. Run a basic Redis container for functional tests: `docker run -d -p 6379:6379 redislabs/redismod`
2. Set the password environment variable for the private connection tests: `export PRIVATE_PASSWORD="my-cool-password"`
3. Run an authenticated Redis container for private connection tests: `docker run -d -p 36379:6379 redislabs/redismod --requirepass $PRIVATE_PASSWORD`

### How to Open a PR

1. Fork this repo.
2. Make your Code Changes.
3. Write your tests.
4. Verify the tests pass (there may be a couple of deployment-specific tests (e.g. Sentinel/Cluster) in Redis.OM which will fail outside of the GH environment we've setup so don't worry about those).
5. If it's your first time contributing please add your Github handle the the Contributors section in the README.
6. Push your changes to GitHub.
7. Open a PR.

## Contributing Docs changes

We love updates to the docs, there's a couple places you can contribute to docs for this Library:

1. The README here in GitHub is meant as a brief overview of the Redis.OM software
2. The [Redis Developer](https://github.com/redis-developer/redis-developer.github.io) Github site has a fair amount of documentation about Redis.OM that we'd welcome contributions to.
