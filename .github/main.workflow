workflow "hello_actions" {
  on = "push"
  resolves = [
    "container:push",
    "ls -lah deploy",
  ]
}

action "build" {
  uses = "./action-fake/"

  args = ["bundle:web"]
}

action "container:login" {
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]

  args = "container:login"
}

action "container:push" {
  uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  needs = ["build", "container:login"]
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did-staging"
  }

  args = ["container:push", "web", "--recursive", "-a", "$HEROKU_APP"]
}

action "ls -lah deploy" {
  uses = "actions/bin/sh@master"
  needs = ["build"]

  args = ["ls -lah deploy"]
}

