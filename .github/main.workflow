workflow "hello_actions" {
  on = "push"
  resolves = ["GitHub Action for Heroku"]
}

# action "login" {
#   uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
#   secrets = ["HEROKU_API_KEY"]
#   args = "container:login"
# }

action "fake-maybe" {
  uses = "./action-fake/"

  # needs = ["login"]
}

action "actions/bin/sh@master" {
  uses = "actions/bin/sh@master"
  needs = ["fake-maybe"]
  args = ["ls -lah"]
}

action "GitHub Action for Heroku" {
  uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  needs = ["actions/bin/sh@master"]
  secrets = ["HEROKU_API_KEY"]
  args = "[\"container:push\", \"web\", \"--recursive\"]"
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
