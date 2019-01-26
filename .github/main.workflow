workflow "hello_actions" {
  on = "push"
  resolves = [
    "heroku container:push",
    "ls -lah deploy",
  ]
}

action "build" {
  uses = "./action-fake/"
}

action "heroku container:push" {
  uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  args = ["container:push", "web", "--recursive"]
  secrets = ["HEROKU_API_KEY"]
  needs = ["build"]
}

action "ls -lah deploy" {
  uses = "actions/bin/sh@master"
  args = ["ls -lah deploy"]
  needs = ["build"]
}

