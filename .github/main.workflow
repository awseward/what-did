workflow "hello_actions" {
  on = "push"
  resolves = [
    "heroku container:push",
    "actions/bin/sh@master",
  ]
}

# action "login" {
#   uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
#   secrets = ["HEROKU_API_KEY"]
#   args = "container:login"
# }

action "build" {
  uses = "./action-fake/"

  # needs = ["login"]
}

action "actions/bin/sh@master" {
  uses = "actions/bin/sh@master"
  args = ["ls -lah"]
  needs = ["build"]

  # action "login" {
  #   uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  #   secrets = ["HEROKU_API_KEY"]
  #   args = "container:login"
  # }

  # needs = ["login"]
}

action "heroku container:push" {
  uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  args = "[\"container:push\", \"web\", \"--recursive\"]"
  secrets = ["HEROKU_API_KEY"]
  needs = ["build"]
}# action "login" {
#   uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
#   secrets = ["HEROKU_API_KEY"]
#   args = "container:login"
# }
# needs = ["login"]
# action "login" {
#   uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
#   secrets = ["HEROKU_API_KEY"]
#   args = "container:login"
# }
# needs = ["login"]
# action "login" {
#   uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
#   secrets = ["HEROKU_API_KEY"]
#   args = "container:login"
# }
# needs = ["login"]
