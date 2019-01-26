workflow "hello_actions" {
  on = "push"
  resolves = ["verify-staging"]
}

action "build" {
  uses = "./action-fake/"

  args = ["bundle:web"]
}

action "heroku-container-login" {
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]

  args = "container:login"
}

action "push-staging" {
  uses = "actions/heroku@master"
  needs = ["build", "heroku-container-login"]
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did-staging"
  }

  args = ["container:push", "web", "--recursive", "--app", "$HEROKU_APP"]
}

action "release-staging" {
  needs = ["push-staging"]
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did-staging"
  }

  args = ["container:release", "web", "--app", "$HEROKU_APP"]
}

action "verify-staging" {
  needs = ["release-staging"]
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did-staging"
  }

  args = ["apps:info", "$HEROKU_APP"]
}
