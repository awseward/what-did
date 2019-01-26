workflow "hello_actions" {
  on = "push"
  resolves = [
    "heroku container:push",
    "ls -lah deploy",
  ]
}

action "build" {
  uses = "./action-fake/"
  args = ["bundle:web"]
}

action "heroku container:push" {
  uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  args = ["container:push", "web", "--recursive", "-a", "$HEROKU_APP"]
  secrets = ["HEROKU_API_KEY"]
  needs = ["build"]
  env = {
    HEROKU_APP = "what-did-staging"
  }
}

action "ls -lah deploy" {
  uses = "actions/bin/sh@master"
  args = ["ls -lah deploy"]
  needs = ["build"]
}

