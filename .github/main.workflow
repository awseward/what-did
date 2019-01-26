workflow "hello_actions" {
  on = "push"
  resolves = ["docker://microsoft/dotnet@2.2-sdk"]
}

action "login" {
  uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  secrets = ["HEROKU_API_KEY"]
  args = "container:login"
}

action "docker://microsoft/dotnet@2.2-sdk" {
  uses = "docker://microsoft/dotnet@2.2-sdk"
  needs = ["login"]
}
